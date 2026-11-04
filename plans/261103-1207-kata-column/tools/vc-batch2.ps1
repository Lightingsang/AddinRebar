param([string]$Base,[string]$Variants,[string]$OutDir,[string]$Only='')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'dump-lib.ps1')
$AcadPid=[int](Get-Content (Join-Path $PSScriptRoot 'acad.pid'))
if(-not (Get-Process -Id $AcadPid -ErrorAction SilentlyContinue)){ throw "acad $AcadPid not running" }
$Base=(Resolve-Path $Base).Path; $OutDir=(Resolve-Path $OutDir).Path
$stop=Join-Path $PSScriptRoot 'watch.stop'; Remove-Item $stop -ErrorAction SilentlyContinue
$watch=Start-Job -FilePath (Join-Path $PSScriptRoot 'dlg-watch.ps1') -ArgumentList $AcadPid,$stop
function Wait-Idle($acad,[int]$sec=60){ $sw=[Diagnostics.Stopwatch]::StartNew(); while($sw.Elapsed.TotalSeconds -lt $sec){ try{ if($acad.GetAcadState().IsQuiescent){ return $true } }catch{}; Start-Sleep -Milliseconds 500 }; return $false }
function Try-Count($doc){ try{ return [int]$doc.ModelSpace.Count }catch{ return -1 } }
try {
$xl=[Runtime.InteropServices.Marshal]::GetActiveObject('Excel.Application'); $xlVisible0=$xl.Visible
$acad=[Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application')
$apid=[uint32]0; Add-Type -Name U -Namespace N -MemberDefinition '[DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);'
[N.U]::GetWindowThreadProcessId([IntPtr]$acad.HWND,[ref]$apid) | Out-Null
if([int]$apid -ne $AcadPid){ throw "COM AutoCAD pid $apid is not ours ($AcadPid)" }
$tpl=Join-Path $acad.Preferences.Files.TemplateDwgPath 'acadiso.dwt'
$list=Get-Content $Variants -Raw | ConvertFrom-Json
foreach($v in $list){
  if($Only -and $v.tag -notlike $Only){ continue }
  $path=Join-Path (Split-Path $Base) ("kata-col-"+$v.tag+".xlsm")
  Copy-Item $Base $path -Force
  $wb=$xl.Workbooks.Open($path); $wb.Activate(); $ws=$wb.Worksheets.Item('Cot'); $ws.Activate()
  foreach($p in $v.cells.PSObject.Properties){
    $raw=$p.Value
    if($raw -is [bool]){ $val=[bool]$raw } elseif($raw -is [string]){ $val=[string]$raw } else { $val=[double]$raw }
    $r=$ws.Range([string]$p.Name)
    [void]$r.GetType().InvokeMember('Value2',[Reflection.BindingFlags]::SetProperty,$null,$r,[object[]]@($val))
  }
  $xl.Calculate()
  if($xl.ActiveWorkbook.FullName -ne $path){ throw "active workbook mismatch for $($v.tag)" }
  $doc=$acad.Documents.Add($tpl); $name=$doc.Name
  Wait-Idle $acad 60 | Out-Null; $doc.Activate(); Wait-Idle $acad 30 | Out-Null
  $sw=[Diagnostics.Stopwatch]::StartNew()
  $job=Start-Job -ScriptBlock { param($n) $a=[Runtime.InteropServices.Marshal]::GetActiveObject('AutoCAD.Application'); foreach($d in $a.Documents){ if($d.Name -eq $n){ $d.SendCommand("VE_COT`n0,0`n") } } } -ArgumentList $name
  $last=-2; $stable=0; $esc=''
  while($sw.Elapsed.TotalSeconds -lt 120){
    Start-Sleep 2
    if($job.State -ne 'Running'){ break }
    $c=Try-Count $doc
    if($c -gt 0 -and $c -eq $last){ $stable++ } else { $stable=0 }; $last=$c
    if($stable -ge 4){ break }
  }
  if($job.State -eq 'Running'){ $env:HP_HARNESS_ACAD_PID="$AcadPid"; & python (Join-Path $PSScriptRoot 'esc.py') | Out-Null; $esc='esc'; Wait-Job $job -Timeout 30 | Out-Null }
  Remove-Job $job -Force
  Wait-Idle $acad 60 | Out-Null
  $n=Export-Doc $doc (Join-Path $OutDir ($v.tag+".json")) $v.tag
  $lp=''; try{ $lp=$doc.GetVariable('LASTPROMPT') }catch{}
  "$($v.tag): $n entities $([int]$sw.Elapsed.TotalSeconds)s $esc | last: $lp"
  for($k=0;$k -lt 20;$k++){ try{ $doc.Close($false); break }catch{ Start-Sleep 1 } }
  Wait-Idle $acad 30 | Out-Null
  $wb.Close($false)
}
$xl.Visible=$xlVisible0
} finally {
  New-Item $stop -ItemType File -Force | Out-Null
  Wait-Job $watch -Timeout 10 | Out-Null; Receive-Job $watch; Remove-Job $watch -Force
}
