param([int]$Port=7793,[string]$OutputDirectory='',[switch]$Replication,[switch]$Visual)
$ErrorActionPreference='Stop'
$taskRoot=$PSScriptRoot
$taskPlayer=Join-Path $taskRoot 'Builds/Windows/Tavern and Chaos.exe'
if(-not (Test-Path -LiteralPath $taskPlayer)) { throw 'Build the Windows LAN demo in Unity first.' }
if(-not $OutputDirectory) { $OutputDirectory=Join-Path $taskRoot ('Evidence/pre-hito1/'+$(if($Replication){'replication-'}else{'run-'})+(Get-Date -Format 'yyyyMMdd-HHmmss')) }
$taskOutput=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskProcesses=@()
try {
    $taskRoles=if($Replication){@('replica-host','replica-client')}else{@('smoke-host','smoke-client')}
    foreach($taskRole in $taskRoles) {
        if($taskRole -like '*-client') {
            $taskDeadline=[DateTime]::UtcNow.AddSeconds(25)
            while(-not (Test-Path -LiteralPath (Join-Path $taskOutput 'host.ready')) -and [DateTime]::UtcNow -lt $taskDeadline -and -not $taskProcesses[0].Process.HasExited) { Start-Sleep -Milliseconds 100 }
            if(-not (Test-Path -LiteralPath (Join-Path $taskOutput 'host.ready'))) { throw "The host did not start. Check $taskOutput" }
        }
        $taskArguments=@('-batchmode','-nographics','-lan-check','-lan-role',$taskRole,'-lan-port',"$Port",'-evidencePath',('"{0}"' -f $taskOutput),'-logFile',('"{0}"' -f (Join-Path $taskOutput ($taskRole+'.log'))))
        if($Visual -and $taskRole -eq 'replica-client') { $taskArguments=$taskArguments | Where-Object {$_ -notin @('-batchmode','-nographics')}; $taskArguments+=@('-screen-width','1280','-screen-height','720','-screen-fullscreen','0') }
        $taskProcess=Start-Process -FilePath $taskPlayer -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
        $null=$taskProcess.Handle
        $taskProcesses+= [pscustomobject]@{Role=$taskRole;Process=$taskProcess}
    }
    $taskDeadline=[DateTime]::UtcNow.AddSeconds(90)
    while(@($taskProcesses | Where-Object {-not $_.Process.HasExited}).Count -and [DateTime]::UtcNow -lt $taskDeadline) { Start-Sleep -Milliseconds 200 }
    $taskResults=@(foreach($taskEntry in $taskProcesses) {
        if(-not $taskEntry.Process.HasExited) { throw "Automated checks timed out. Check $taskOutput" }
        $taskEntry.Process.WaitForExit(); $taskEntry.Process.Refresh()
        $taskReportFile=Join-Path $taskOutput ($taskEntry.Role+'0.json')
        if(-not (Test-Path -LiteralPath $taskReportFile)) { throw "Missing report: $taskReportFile" }
        $taskReport=Get-Content -LiteralPath $taskReportFile -Raw | ConvertFrom-Json
        [pscustomobject]@{role=$taskEntry.Role;exitCode=$taskEntry.Process.ExitCode;passed=$taskReport.passed;checks=$taskReport.checks.Count;failures=$taskReport.failures;runtimeErrors=$taskReport.runtimeErrors;rttMilliseconds=$taskReport.rttMilliseconds;bytesReceived=$taskReport.bytesReceived;snapshots=$taskReport.snapshots;maxStandingBodyDrift=$taskReport.maxStandingBodyDrift;maxStandingCameraDrift=$taskReport.maxStandingCameraDrift;renderedFrames=$taskReport.renderedFrames}
    })
    $taskPassed=@($taskResults | Where-Object {-not $_.passed -or $_.exitCode -ne 0}).Count -eq 0
    $taskScope=if($Replication){'Rendered-body and feedback regression with separate Windows players, real loopback UDP, controlled poses and client-camera captures. Not manual Hito 1 acceptance.'}else{'Automated pre-Hito 1 component integration: separate Windows players, real loopback UDP, controlled server fixtures. Not multi-machine LAN or manual acceptance.'}
    $taskSummary=[pscustomobject]@{passed=$taskPassed;scope=$taskScope;manualHito1='Pending: user will perform acceptance';results=$taskResults}
    $taskSummary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $taskOutput 'summary.json') -Encoding utf8
    $taskSummary | ConvertTo-Json -Depth 8
    if(-not $taskPassed) { throw "Some checks failed. Reports: $taskOutput" }
} finally {
    foreach($taskEntry in $taskProcesses) { if(-not $taskEntry.Process.HasExited) { $taskEntry.Process.Kill(); $taskEntry.Process.WaitForExit() }; $taskEntry.Process.Dispose() }
}
