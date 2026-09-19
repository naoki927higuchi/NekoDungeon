param([string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath $UnityEditor)) { throw 'Unity Editor not found.' }
$buildLog = Join-Path $PSScriptRoot 'build.log'
$buildProcess = Start-Process -FilePath $UnityEditor -ArgumentList @('-batchmode', '-quit', '-nographics', '-projectPath', ('"' + $PSScriptRoot + '"'), '-executeMethod', 'BuildGame.Build', '-logFile', ('"' + $buildLog + '"')) -PassThru -Wait -WindowStyle Hidden
if ($buildProcess.ExitCode -ne 0) { throw "Build failed. See $buildLog" }
$gamePath = Join-Path $PSScriptRoot 'Builds\Windows\NekoDungeon.exe'
$testLog = Join-Path $PSScriptRoot 'selftest.log'
$testProcess = Start-Process -FilePath $gamePath -ArgumentList @('-batchmode', '-nographics', '-selftest', '-logFile', ('"' + $testLog + '"')) -PassThru -Wait -WindowStyle Hidden
if ($testProcess.ExitCode -ne 0) { throw "Self-tests failed. See $testLog" }
Write-Host "Build and tests complete: $gamePath"
