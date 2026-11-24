param(
    [string]$UnityPath = '',
    [int]$TimeoutSeconds = 900
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$versionLine = Get-Content (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1
$editorVersion = ($versionLine -split ': ', 2)[1].Trim()
if (-not $UnityPath) {
    $UnityPath = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$editorVersion/Editor/Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityPath)) { throw "Matching Unity editor not found: $UnityPath" }
$snapshot = Join-Path ([IO.Path]::GetTempPath()) ('cp-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
$output = Join-Path $projectRoot 'Artifacts/Current'
New-Item -ItemType Directory -Path $snapshot, $output -Force | Out-Null
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $folder) -Destination (Join-Path $snapshot $folder) -Recurse
}
$log = Join-Path $output 'unity.log'
$result = Join-Path $output 'result.txt'
Set-Content -LiteralPath $result -Value "Verification running in $snapshot"
Write-Output "Checking isolated source snapshot: $snapshot"
$arguments = @('-batchmode', '-nographics', '-projectPath', $snapshot,
    '-executeMethod', 'ProjectHandoffChecks.RunBatch', '--burst-disable-compilation', '-logFile', $log)
$quotedArguments = $arguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' }
$verificationProcess = Start-Process -FilePath $UnityPath -ArgumentList $quotedArguments -WindowStyle Hidden -PassThru
if (-not $verificationProcess.WaitForExit($TimeoutSeconds * 1000)) {
    Stop-Process -Id $verificationProcess.Id
    Set-Content -LiteralPath $result -Value "FAIL: Unity timed out. See $log. Snapshot: $snapshot"
    throw "Verification timed out; only the Unity process started by this script was stopped."
}
$verificationProcess.Refresh()
$snapshotResult = Join-Path $snapshot 'Library/HandoffResult.txt'
if (Test-Path -LiteralPath $snapshotResult) {
    Copy-Item -LiteralPath $snapshotResult -Destination $result -Force
} else {
    Set-Content -LiteralPath $result -Value "FAIL: Unity exited before completing checks. See $log. Snapshot: $snapshot"
}
$report = Get-Content -LiteralPath $result -Raw
Write-Output $report
Write-Output "Log: $log"
if ($verificationProcess.ExitCode -ne 0 -or -not $report.StartsWith('PASS:')) {
    throw 'Current-project verification failed.'
}
