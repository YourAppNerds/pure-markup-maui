# Launches the built sample, types into the Entry via UI Automation, clicks Register, checks the
# status label shows the bound value, saves a window screenshot and closes the app.
# Run with Windows PowerShell: powershell -File verify-ui.ps1 -Exe <path to ScreenshotPage.exe> -Shot screenshot.png
param([string]$Exe, [string]$Shot)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class W {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
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
for ($i = 0; $i -lt 60 -and $p.MainWindowHandle -eq 0; $i++) { Start-Sleep -Milliseconds 500; $p.Refresh() }
if ($p.HasExited) { throw "app exited with $($p.ExitCode)" }
$win = $A::FromHandle($p.MainWindowHandle)
$entry = FindId $win 'CodeEntry'
$button = FindId $win 'RegisterButton'
$status = FindId $win 'StatusLabel'
"status before: $($status.Current.Name)"
$entry.SetFocus()
[W]::SetForegroundWindow($p.MainWindowHandle) | Out-Null
$entry.SetFocus()
Start-Sleep -Milliseconds 300
[System.Windows.Forms.SendKeys]::SendWait('424242')
Start-Sleep -Milliseconds 500
$vp = $entry.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
"entry value: $($vp.Current.Value)"
$button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep -Milliseconds 800
$status = FindId $win 'StatusLabel'
"status after: $($status.Current.Name)"
# Screenshot of the window
$r = New-Object W+RECT; [W]::GetWindowRect($p.MainWindowHandle, [ref]$r) | Out-Null
$bmp = New-Object System.Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
$bmp.Save($Shot, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
$p.CloseMainWindow() | Out-Null
if (-not $p.WaitForExit(10000)) { $p.Kill(); "killed" } else { "exit code: $($p.ExitCode)" }
