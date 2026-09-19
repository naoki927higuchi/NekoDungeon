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
if (-not (Select-String -LiteralPath $testLog -SimpleMatch 'SELFTEST PASS:' -Quiet)) { throw 'Self-test success marker missing.' }

# Stage only runtime files and player-facing documentation, never old builds or debug backups.
$playerRoot = Join-Path $PSScriptRoot 'Builds\Windows'
$packageRoot = Join-Path $PSScriptRoot ('Builds\Package-' + [guid]::NewGuid().ToString('N'))
$packageGame = Join-Path $packageRoot 'NekoDungeon-Windows'
New-Item -ItemType Directory -Path $packageGame | Out-Null
$runtimeNames = @('NekoDungeon.exe', 'NekoDungeon_Data', 'UnityPlayer.dll', 'UnityCrashHandler64.exe', 'MonoBleedingEdge', 'D3D12', 'dstorage.dll', 'dstoragecore.dll', 'README.txt', 'THIRD_PARTY_NOTICES.md', 'Licenses')
foreach ($name in $runtimeNames) {
    $source = Join-Path $playerRoot $name
    if (-not (Test-Path -LiteralPath $source)) { throw "Expected runtime file missing: $source" }
    Copy-Item -LiteralPath $source -Destination $packageGame -Recurse
}
$zipPath = Join-Path $PSScriptRoot 'Builds\NekoDungeon-Windows.zip'
$stagedZip = Join-Path $packageRoot 'NekoDungeon-Windows.zip'
Compress-Archive -LiteralPath $packageGame -DestinationPath $stagedZip -CompressionLevel Optimal

# Validate the actual extracted archive, rather than just the source player directory.
$verifyRoot = Join-Path $packageRoot 'Extracted'
Expand-Archive -LiteralPath $stagedZip -DestinationPath $verifyRoot
$verifiedGame = Join-Path $verifyRoot 'NekoDungeon-Windows'
foreach ($file in Get-ChildItem -LiteralPath $packageGame -Recurse -File) {
    $relative = $file.FullName.Substring($packageGame.Length + 1)
    $extracted = Join-Path $verifiedGame $relative
    if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $extracted).Hash) { throw "Archive mismatch: $relative" }
}
$packageTestLog = Join-Path $PSScriptRoot 'package-selftest.log'
$packageTest = Start-Process -FilePath (Join-Path $verifiedGame 'NekoDungeon.exe') -ArgumentList @('-batchmode', '-nographics', '-selftest', '-logFile', ('"' + $packageTestLog + '"')) -PassThru -Wait -WindowStyle Hidden
if ($packageTest.ExitCode -ne 0 -or -not (Select-String -LiteralPath $packageTestLog -SimpleMatch 'SELFTEST PASS:' -Quiet)) { throw 'Packaged player self-tests failed.' }
Copy-Item -LiteralPath $stagedZip -Destination $zipPath -Force
(Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash + '  NekoDungeon-Windows.zip' | Set-Content -LiteralPath ($zipPath + '.sha256') -Encoding ascii

# Only remove the unique staging directory we created, after confirming containment and no links.
$buildsRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Builds'))
$resolvedStage = (Resolve-Path -LiteralPath $packageRoot).Path
if ([IO.Path]::GetDirectoryName($resolvedStage) -ne $buildsRoot -or [IO.Path]::GetFileName($resolvedStage) -notmatch '^Package-[0-9a-f]{32}$') { throw 'Unsafe staging cleanup path.' }
foreach ($entry in @((Get-Item -LiteralPath $buildsRoot), (Get-Item -LiteralPath $resolvedStage)) + @(Get-ChildItem -LiteralPath $resolvedStage -Recurse -Force)) {
    if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refusing staging cleanup through a link.' }
}
Remove-Item -LiteralPath $resolvedStage -Recurse -Force
Write-Host "Build, player tests, archive integrity and extracted player tests complete: $zipPath"
