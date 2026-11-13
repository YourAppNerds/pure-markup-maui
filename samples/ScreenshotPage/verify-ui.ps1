# Launches the built sample and drives it through UI Automation: sets the Entry value, clicks
# Register, checks the bound status label, toggles the theme (presets + tokens) and checks the page
# background changes, clicks the DangerButton-preset Reset, saves light and dark screenshots and
# closes the app. Works on a locked workstation (ValuePattern instead of SendKeys, PrintWindow
# instead of a screen copy).
# Run with Windows PowerShell: powershell -File verify-ui.ps1 -Exe <path to ScreenshotPage.exe> -Shot shot.png
# Writes shot.png (start theme) and shot-toggled.png.
param([string]$Exe, [string]$Shot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class W {
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
'@
$p = Start-Process $Exe -PassThru
$A = [System.Windows.Automation.AutomationElement]
$C = [System.Windows.Automation.PropertyCondition]
function FindId($root, $id) {
  for ($i = 0; $i -lt 60; $i++) {
    $e = $root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, (New-Object $C ($A::AutomationIdProperty, $id)))
    if ($e) { return $e }
    Start-Sleep -Milliseconds 500
  }
  throw "not found: $id"
}
function Click($id) {
  (FindId $win $id).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
  Start-Sleep -Milliseconds 800
}
# Window capture with PW_RENDERFULLCONTENT; returns the page background pixel (bottom-left padding).
function Capture($path) {
  $r = New-Object W+RECT; [W]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
  $bmp = New-Object System.Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc(); [W]::PrintWindow($p.MainWindowHandle, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc)
  $px = $bmp.GetPixel(20, $bmp.Height - 20)
  $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
  return '#{0:X2}{1:X2}{2:X2}' -f $px.R, $px.G, $px.B
}
for ($i = 0; $i -lt 60 -and $p.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 500; $p.Refresh() }
if ($p.HasExited) { throw "app exited with $($p.ExitCode)" }
$win = $A::FromHandle($p.MainWindowHandle)
$entry = FindId $win 'CodeEntry'
"status before: $((FindId $win 'StatusLabel').Current.Name)"
$vp = $entry.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
$vp.SetValue('424242')
Start-Sleep -Milliseconds 500
"entry value: $($vp.Current.Value)"
Click 'RegisterButton'
"status after register: $((FindId $win 'StatusLabel').Current.Name)"
$first = Capture $Shot
"page background: $first"
Click 'ThemeButton'
Start-Sleep -Milliseconds 700
$toggled = Capture ($Shot -replace '\.png$', '-toggled.png')
"page background after toggle: $toggled"
if ($first -eq $toggled) { "THEME DID NOT CHANGE" }
Click 'ResetButton'
"status after reset: $((FindId $win 'StatusLabel').Current.Name)"
$p.CloseMainWindow() | Out-Null
if (-not $p.WaitForExit(10000)) { $p.Kill(); "killed" } else { "exit code: $($p.ExitCode)" }
