param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe',
    [string]$ProjectPath = (Split-Path $PSScriptRoot -Parent),
    [string]$OutputRoot = (Split-Path $PSScriptRoot -Parent),
    [switch]$SkipTests,
    [switch]$SkipBuild,
    [switch]$SkipProbe,
    [switch]$DisableBurst,
    [string[]]$Resolutions = @('1600x900','1280x720')
)
$ErrorActionPreference = 'Stop'
$ProjectPath = [IO.Path]::GetFullPath($ProjectPath)
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
$qaPath = Join-Path $OutputRoot 'Artifacts\QA'
New-Item -ItemType Directory -Path $qaPath -Force | Out-Null
function Run-CheckedProcess([string]$File, [string[]]$Arguments, [int]$TimeoutSeconds = 900) {
    $quotedArguments = $Arguments | ForEach-Object { '"' + $_.Replace('"','\"') + '"' }
    $taskProcess = Start-Process -FilePath $File -ArgumentList $quotedArguments -WindowStyle Hidden -PassThru
    if (-not $taskProcess.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-Process -Id $taskProcess.Id
        throw "Verification process exceeded $TimeoutSeconds seconds; only the process launched by this script was stopped."
    }
    $taskProcess.Refresh()
    if ($taskProcess.ExitCode -ne 0) { throw "$File exited with code $($taskProcess.ExitCode). Check Artifacts/QA logs." }
}
if (-not $SkipTests) {
    Write-Output 'Running EditMode tests...'
    $editorArguments = @('-batchmode','-nographics','-projectPath',$ProjectPath,'-runTests','-testPlatform','EditMode','-testResults',(Join-Path $qaPath 'editmode-results.xml'),'-logFile',(Join-Path $qaPath 'editmode.log'))
    if ($DisableBurst) { $editorArguments += '--burst-disable-compilation' }
    Run-CheckedProcess $UnityPath $editorArguments
    [xml]$results = Get-Content -LiteralPath (Join-Path $qaPath 'editmode-results.xml')
    if ($results.'test-run'.failed -ne '0' -or [int]$results.'test-run'.total -lt 1) { throw 'EditMode tests failed or were not discovered.' }
    Write-Output "EditMode: $($results.'test-run'.passed) passed."
}
if (-not $SkipBuild) {
    Write-Output 'Building Windows player...'
    $editorArguments = @('-batchmode','-nographics','-quit','-projectPath',$ProjectPath,'-executeMethod','Checkpoint.Editor.CheckpointBootstrap.BuildWindows','-logFile',(Join-Path $qaPath 'build.log'))
    if ($DisableBurst) { $editorArguments += '--burst-disable-compilation' }
    Run-CheckedProcess $UnityPath $editorArguments
    $sourceBuild = Join-Path $ProjectPath 'Builds\Windows'
    $targetBuild = Join-Path $OutputRoot 'Builds\Windows'
    if ($sourceBuild -ne $targetBuild) {
        New-Item -ItemType Directory -Path $targetBuild -Force | Out-Null
        Get-ChildItem -LiteralPath $sourceBuild | Copy-Item -Destination $targetBuild -Recurse -Force
    }
    Write-Output 'Windows build succeeded.'
}
if (-not $SkipProbe) {
    $player = Join-Path $OutputRoot 'Builds\Windows\NotJustAcceptButDeny.exe'
    foreach ($resolution in $Resolutions) {
        $size = $resolution.Split('x')
        if ($size.Length -ne 2) { throw "Invalid resolution: $resolution" }
        $probePath = Join-Path $qaPath $resolution
        New-Item -ItemType Directory -Path $probePath -Force | Out-Null
        Write-Output "Running rendered UI playthrough at $resolution..."
        Run-CheckedProcess $player @('-screen-fullscreen','0','-screen-width',$size[0],'-screen-height',$size[1],'-checkpointProbe','-checkpointProbeOutput',$probePath,'-logFile',(Join-Path $probePath 'player.log')) 120
        $report = Get-Content -LiteralPath (Join-Path $probePath 'probe-results.json') -Raw | ConvertFrom-Json
        if (-not $report.success) { throw "Rendered playthrough failed at $resolution" }
        Write-Output "UI playthrough: $($report.checks.Count) checks passed at $($report.resolution)."
    }
}
Write-Output 'Verification completed.'
