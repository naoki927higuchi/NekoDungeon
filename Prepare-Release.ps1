param(
    [Parameter(Mandatory=$true)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [Parameter(Mandatory=$true)][string]$ZipPath
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (-not [IO.Path]::IsPathRooted($ZipPath)) { $ZipPath = Join-Path $PSScriptRoot $ZipPath }
$source = (Resolve-Path -LiteralPath $ZipPath).Path
$buildRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'Builds')) + [IO.Path]::DirectorySeparatorChar
if (-not $source.StartsWith($buildRoot,[StringComparison]::OrdinalIgnoreCase) -or [IO.Path]::GetExtension($source) -ne '.zip') { throw 'Select a local ZIP under Builds.' }
$hashFile = "$source.sha256"
if (-not (Test-Path -LiteralPath $hashFile)) { throw 'Source ZIP checksum is required.' }
$expected = ((Get-Content -LiteralPath $hashFile -Raw).Trim() -split '\s+')[0]
$hash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne $expected) { throw 'Source ZIP checksum mismatch.' }
if ((Get-Item -LiteralPath $source).Length -ge 100MB) { throw 'ZIP is too large for this Git distribution workflow.' }
$destinationRoot = Join-Path $PSScriptRoot 'Distribution'
$name = "NekoDungeon-$Version-Windows.zip"
$destination = Join-Path $destinationRoot $name
foreach ($path in @($destination,"$destination.sha256","$destination.json")) {
    if (Test-Path -LiteralPath $path) { throw "Release already exists: $path" }
}
$zip = [IO.Compression.ZipFile]::OpenRead($source)
try {
    foreach ($entry in $zip.Entries) {
        $entryName = $entry.FullName.Replace('\','/')
        if ($entryName -notlike 'NekoDungeon-Windows/*' -or $entryName -match '(^|/)\.\.(/|$)|:') { throw "Unexpected archive path: $entryName" }
    }
    foreach ($required in @('NekoDungeon.exe','UnityPlayer.dll','NekoDungeon_Data/globalgamemanagers','README.txt','THIRD_PARTY_NOTICES.md')) {
        if (-not ($zip.Entries | Where-Object { $_.FullName.Replace('\','/') -eq "NekoDungeon-Windows/$required" })) { throw "Required runtime entry missing: $required" }
    }
    if ($zip.Entries.FullName -match '(?i)\.(pdb|pfx|key)$|BackUpThisFolder_ButDontShip|BurstDebugInformation_DoNotShip') { throw 'Development/private files in archive.' }
} finally { $zip.Dispose() }
$verifyRoot = Join-Path $PSScriptRoot ('Builds\ReleaseVerification-' + [guid]::NewGuid().ToString('N'))
[IO.Compression.ZipFile]::ExtractToDirectory($source,$verifyRoot)
$log = Join-Path $verifyRoot 'selftest.log'
$exe = Join-Path $verifyRoot 'NekoDungeon-Windows\NekoDungeon.exe'
$process = Start-Process -FilePath $exe -ArgumentList @('-batchmode','-nographics','-selftest','-logFile',('"' + $log + '"')) -PassThru -WindowStyle Hidden
if (-not $process.WaitForExit(120000)) { $process.Kill(); throw 'Packaged self-test timed out.' }
$process.WaitForExit()
if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -SimpleMatch 'SELFTEST PASS:' -Quiet)) { throw "Packaged self-test failed. See $log" }
$commit = git -c "safe.directory=$PSScriptRoot" -C $PSScriptRoot rev-parse HEAD
if ($LASTEXITCODE) { throw 'Cannot read preparation commit.' }
New-Item -ItemType Directory -Path $destinationRoot -Force | Out-Null
Copy-Item -LiteralPath $source -Destination $destination
if ((Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash -ne $hash) { throw 'Release copy mismatch.' }
"$hash  $name" | Set-Content -LiteralPath "$destination.sha256" -Encoding ascii
[ordered]@{
    Product = 'NekoDungeon'
    ReleaseVersion = $Version
    PreparedAt = (Get-Date).ToString('o')
    PreparationCommit = $commit
    SourceZip = $source.Substring($PSScriptRoot.Length + 1).Replace('\','/')
    SHA256 = $hash
    Size = (Get-Item -LiteralPath $destination).Length
    Validation = 'Source checksum, ZIP structure, and extracted player self-tests passed'
    Note = 'Selected existing binary unchanged; release version does not rewrite embedded binary metadata.'
} | ConvertTo-Json | Set-Content -LiteralPath "$destination.json" -Encoding utf8
Write-Host "Prepared $destination"
Write-Host 'Record release notes, commit the selected Distribution files and source, then push explicitly.'
