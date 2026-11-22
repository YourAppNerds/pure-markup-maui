# Verifies the dotnet new templates end to end (the CI gate from the design: instantiate and build
# every target this machine can build).
#  1. Packs PureMarkup.Maui and PureMarkup.Maui.Templates into artifacts/packages and installs the
#     template package into a private hive under -Scratch (your own template list is untouched).
#  2. Instantiates puremarkup-app as default, --sample-content, --no-windows and --framework net11.0
#     into -Scratch, with a nuget.config that adds artifacts/packages as a feed; checks that no
#     variant has a .xaml file.
#  3. Builds the Windows TFM of the default and sample apps, launches each and clicks the counter
#     button through UI Automation (works on a locked workstation); in the sample app it also opens
#     the Todo tab and adds an item. Builds net10.0-android of the no-windows and sample apps
#     (build only, docs/android.md); NETSDK1147 (missing workload) is reported as SKIP, not as a failure.
#  4. Adds a page with puremarkup-page to the default app and builds it again.
# Run with Windows PowerShell (UI Automation): powershell -File scripts/verify-template.ps1
param(
    [string]$Scratch = (Join-Path $env:TEMP 'puremarkup-template-verify'),
    [switch]$SkipUi
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot
$packages = Join-Path $repo 'artifacts\packages'
$winTfm = 'net10.0-windows10.0.19041.0'
$results = New-Object System.Collections.Generic.List[object]

function Result($check, $status, $detail) {
    $results.Add([pscustomobject]@{ Check = $check; Status = $status; Detail = $detail })
    Write-Host ('[{0}] {1} {2}' -f $status, $check, $detail)
}

# Runs dotnet with the given arguments; returns @{ Code; Out } without throwing on stderr output.
function Run-Dotnet {
    $eap = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    try { $out = & dotnet.exe @args 2>&1 | Out-String } finally { $ErrorActionPreference = $eap }
    return @{ Code = $LASTEXITCODE; Out = $out }
}

function Must($r, $what) {
    if ($r.Code -ne 0) { Write-Host $r.Out; throw "$what failed (exit $($r.Code))" }
}

function FirstErrors($text) {
    (($text -split "`r?`n") | Where-Object { $_ -match 'error [A-Z]+\d+' } | Select-Object -First 3) -join ' | '
}

# --- 1. Pack and install -------------------------------------------------------------------------
$libVersion = ([xml](Get-Content (Join-Path $repo 'src\PureMarkup.Maui\PureMarkup.Maui.csproj'))).Project.PropertyGroup |
    ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1
$templateJson = Get-Content (Join-Path $repo 'templates\PureMarkup.Maui.Templates\content\puremarkup-app\.template.config\template.json') -Raw | ConvertFrom-Json
$templateDefault = $templateJson.symbols.pureMarkupVersion.defaultValue
if ($libVersion -ne $templateDefault) {
    Result 'version' 'FAIL' "library is $libVersion, template references $templateDefault"
} else {
    Result 'version' 'PASS' "template references PureMarkup.Maui $libVersion"
}

Must (Run-Dotnet pack (Join-Path $repo 'src\PureMarkup.Maui') -c Release -o $packages) 'pack PureMarkup.Maui'
Must (Run-Dotnet pack (Join-Path $repo 'templates\PureMarkup.Maui.Templates') -c Release -o $packages) 'pack templates'
$templatePackage = Join-Path $packages "PureMarkup.Maui.Templates.$libVersion.nupkg"

# A -dev version is repacked under the same number: drop the stale copy from the NuGet cache.
$globalPackages = ((Run-Dotnet nuget locals global-packages --list).Out -replace '(?s)^.*?global-packages:\s*', '').Trim()
$cached = Join-Path $globalPackages "puremarkup.maui\$libVersion"
if (Test-Path $cached) { Remove-Item -Recurse -Force $cached }

if (Test-Path $Scratch) { Remove-Item -Recurse -Force $Scratch }
New-Item -ItemType Directory -Force $Scratch | Out-Null
$hive = Join-Path $Scratch 'hive'
Set-Content (Join-Path $Scratch 'nuget.config') @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="puremarkup-local" value="$packages" />
  </packageSources>
</configuration>
"@
Must (Run-Dotnet new install $templatePackage --debug:custom-hive $hive) 'dotnet new install'
Result 'install' 'PASS' $templatePackage

# --- 2. Instantiate ------------------------------------------------------------------------------
function NewApp($name, [string[]]$options) {
    $r = Run-Dotnet new puremarkup-app -n $name -o (Join-Path $Scratch $name) --no-restore --debug:custom-hive $hive @options
    if ($r.Code -ne 0) { Result "new $name" 'FAIL' (FirstErrors $r.Out); return $null }
    $dir = Join-Path $Scratch $name
    $xaml = @(Get-ChildItem $dir -Recurse -File -Filter *.xaml | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
    if ($xaml.Count -eq 0) { Result "new $name ($($options -join ' '))" 'PASS' 'zero .xaml files' }
    else { Result "new $name" 'FAIL' "xaml files: $($xaml.Name -join ', ')" }
    return $dir
}

$default = NewApp 'DefaultApp' @()
$sample = NewApp 'SampleApp' @('--sample-content')
$noWin = NewApp 'NoWinApp' @('--no-windows')
$net11 = NewApp 'Net11App' @('--framework', 'net11.0')

if ($noWin) {
    $proj = Get-Content (Join-Path $noWin 'NoWinApp.csproj') -Raw
    $ok = ($proj -notmatch 'windows10') -and -not (Test-Path (Join-Path $noWin 'Platforms\Windows'))
    if ($ok) { Result 'no-windows shape' 'PASS' 'no Windows TFM, no Platforms/Windows' }
    else { Result 'no-windows shape' 'FAIL' 'Windows TFM or folder still present' }
}
if ($net11) {
    $proj = Get-Content (Join-Path $net11 'Net11App.csproj') -Raw
    if ($proj -match 'net11\.0-android' -and $proj -notmatch 'net10\.0') { Result 'net11.0 shape' 'PASS' 'TFMs are net11.0-* (not built)' }
    else { Result 'net11.0 shape' 'FAIL' 'TFMs not replaced' }
}

# --- 3. Build ------------------------------------------------------------------------------------
function BuildTfm($dir, $tfm) {
    $name = Split-Path $dir -Leaf
    $r = Run-Dotnet build (Join-Path $dir "$name.csproj") -f $tfm "-p:TargetFrameworks=$tfm"
    if ($r.Code -eq 0) {
        $warn = ([regex]::Matches($r.Out, '(?m)^\s*(\d+) Warning\(s\)') | Select-Object -Last 1)
        $count = 0; if ($warn) { $count = [int]$warn.Groups[1].Value }
        Result "build $name $tfm" 'PASS' "$count warning(s)"
        return $true
    }
    if ($r.Out -match 'NETSDK1147') { Result "build $name $tfm" 'SKIP' 'NETSDK1147: workload missing on this machine'; return $false }
    Result "build $name $tfm" 'FAIL' (FirstErrors $r.Out)
    return $false
}

$defaultBuilt = $false; $sampleBuilt = $false
if ($default) { $defaultBuilt = BuildTfm $default $winTfm }
if ($sample) { $sampleBuilt = BuildTfm $sample $winTfm }
if ($noWin) { [void](BuildTfm $noWin 'net10.0-android') }
if ($sample) { [void](BuildTfm $sample 'net10.0-android') }

# Sample content: opens the Todo tab, adds an item through the Entry and Add button.
function RunTodo($win) {
    $A = [System.Windows.Automation.AutomationElement]
    $all = [System.Windows.Automation.Condition]::TrueCondition
    $tab = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $all) |
        Where-Object { $_.Current.Name -eq 'Todo' -and $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::TabItem } |
        Select-Object -First 1
    if (-not $tab) { Result 'run SampleApp Todo' 'FAIL' 'Todo tab not found'; return }
    $tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    Start-Sleep -Milliseconds 1500
    $byId = { param($id) $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants,
        (New-Object System.Windows.Automation.PropertyCondition ($A::AutomationIdProperty, $id))) }
    $entry = & $byId 'NewItemEntry'
    if (-not $entry) { Result 'run SampleApp Todo' 'FAIL' 'NewItemEntry not found'; return }
    $entry.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Verify the template')
    Start-Sleep -Milliseconds 500
    (& $byId 'AddButton').GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Milliseconds 800
    $added = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $all) |
        Where-Object { $_.Current.Name -eq 'Verify the template' -and $_.Current.AutomationId -ne 'NewItemEntry' }
    if ($added) { Result 'run SampleApp Todo' 'PASS' 'tab opened, item added to the list' }
    else { Result 'run SampleApp Todo' 'FAIL' 'added item not in the list' }
}

# Launches the app, clicks the counter button and checks its text; closes the app.
function RunUi($dir) {
    $name = Split-Path $dir -Leaf
    $exe = Get-ChildItem (Join-Path $dir "bin\Debug\$winTfm") -Recurse -Filter "$name.exe" | Select-Object -First 1
    if (-not $exe) { Result "run $name" 'FAIL' 'exe not found'; return }
    Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
    $A = [System.Windows.Automation.AutomationElement]
    $p = Start-Process $exe.FullName -PassThru
    try {
        for ($i = 0; $i -lt 60 -and $p.MainWindowHandle -eq 0 -and -not $p.HasExited; $i++) { Start-Sleep -Milliseconds 500; $p.Refresh() }
        if ($p.HasExited) { Result "run $name" 'FAIL' "exited with $($p.ExitCode)"; return }
        if ($p.MainWindowHandle -eq 0) { Result "run $name" 'FAIL' 'no window'; return }
        $win = $A::FromHandle($p.MainWindowHandle)
        $cond = New-Object System.Windows.Automation.PropertyCondition ($A::AutomationIdProperty, 'CounterButton')
        $button = $null
        for ($i = 0; $i -lt 40 -and -not $button; $i++) {
            $button = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
            if (-not $button) { Start-Sleep -Milliseconds 500 }
        }
        if (-not $button) { Result "run $name" 'FAIL' "window '$($win.Current.Name)' shows, CounterButton not found"; return }
        $before = $button.Current.Name
        $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
        Start-Sleep -Milliseconds 800
        $after = $button.Current.Name
        if ($after -eq 'Clicked 1 time') { Result "run $name" 'PASS' "window '$($win.Current.Name)', button '$before' -> '$after'" }
        else { Result "run $name" 'FAIL' "button '$before' -> '$after'" }
        if ($name -eq 'SampleApp') { RunTodo $win }
    } finally {
        if (-not $p.HasExited) {
            $p.CloseMainWindow() | Out-Null
            if (-not $p.WaitForExit(10000)) { $p.Kill() }
        }
    }
}

if (-not $SkipUi) {
    if ($defaultBuilt) { RunUi $default }
    if ($sampleBuilt) { RunUi $sample }
}

# --- 4. Item template ----------------------------------------------------------------------------
if ($default) {
    $r = Run-Dotnet new puremarkup-page -n SettingsPage -o (Join-Path $default 'Pages') --project (Join-Path $default 'DefaultApp.csproj') --debug:custom-hive $hive
    $page = Join-Path $default 'Pages\SettingsPage.cs'
    if ($r.Code -ne 0 -or -not (Test-Path $page)) { Result 'new puremarkup-page' 'FAIL' (FirstErrors $r.Out) }
    else {
        $text = Get-Content $page -Raw
        if ($text -match 'namespace DefaultApp;' -and $text -match 'class SettingsPage') { Result 'new puremarkup-page' 'PASS' 'Pages/SettingsPage.cs, namespace DefaultApp' }
        else { Result 'new puremarkup-page' 'FAIL' 'unexpected namespace or class name' }
        [void](BuildTfm $default $winTfm)
    }
}

# --- Summary -------------------------------------------------------------------------------------
$results | Format-Table -AutoSize | Out-String -Width 200 | Write-Host
$failed = @($results | Where-Object { $_.Status -eq 'FAIL' }).Count
if ($failed -gt 0) { Write-Host "$failed check(s) failed"; exit 1 }
Write-Host 'All checks passed (SKIP = not buildable on this machine).'
