param([Parameter(Mandatory)] [string] $ExePath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient,UIAutomationTypes,System.Drawing,System.Windows.Forms
$p = Start-Process $ExePath -PassThru
try {
  $window = $null
  for ($i=0; $i -lt 60; $i++) {
    Start-Sleep -Milliseconds 500
    $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst([System.Windows.Automation.TreeScope]::Children, [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ProcessIdProperty,$p.Id))
    if ($window) { break }
    if ($p.HasExited) { throw 'Kopi exited during startup.' }
  }
  if (!$window) { throw 'WPF window did not appear.' }
  $tabs=$window.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::ControlTypeProperty,[System.Windows.Automation.ControlType]::TabItem))
  if ($tabs.Count -ne 4) { throw "Expected 4 navigation tabs, got $($tabs.Count)." }
  foreach ($tab in $tabs) {
    $tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
    Start-Sleep -Milliseconds 200
  }
  $code=$window.FindFirst([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty,'Browser pairing code'))
  if (!$code) { throw 'Pairing control unavailable in Setup.' }
  $pair=$code.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
  if ($pair.Length -ne 24) { throw 'Invalid pairing code.' }
  $bounds=$window.Current.BoundingRectangle
  $screen=[System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
  if ($bounds.Right -gt $screen.Right+12 -or $bounds.Bottom -gt $screen.Bottom+12) { throw "Window extends beyond usable screen area: $bounds vs $screen" }
  $image=[System.Drawing.Bitmap]::new([int]$bounds.Width,[int]$bounds.Height)
  $graphics=[System.Drawing.Graphics]::FromImage($image)
  $graphics.CopyFromScreen([int]$bounds.X,[int]$bounds.Y,0,0,$image.Size)
  $image.Save("$PWD/artifacts/ui-smoke.png")
  $graphics.Dispose();$image.Dispose()
  Write-Output 'WPF UI pass: startup, four navigation steps, and pairing control verified.'
} finally { if (!$p.HasExited) { Stop-Process $p.Id } }
