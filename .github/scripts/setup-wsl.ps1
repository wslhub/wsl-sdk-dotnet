$ErrorActionPreference = 'Stop'

# This script is only for a disposable GitHub-hosted Windows runner.
if ($env:GITHUB_ACTIONS -ne 'true' -or $env:RUNNER_ENVIRONMENT -ne 'github-hosted') {
    throw 'This script requires a disposable GitHub-hosted runner.'
}

$imageName = 'ubuntu-jammy-wsl-amd64-ubuntu22.04lts.rootfs.tar.gz'
$imageBase = 'https://cloud-images.ubuntu.com/wsl/jammy/current'
$imagePath = Join-Path $env:RUNNER_TEMP $imageName
$checksumsPath = Join-Path $env:RUNNER_TEMP 'SHA256SUMS'

curl.exe --fail --location --retry 3 --output $imagePath "$imageBase/$imageName"
if ($LASTEXITCODE -ne 0) { throw 'Ubuntu root filesystem download failed.' }
curl.exe --fail --location --retry 3 --output $checksumsPath "$imageBase/SHA256SUMS"
if ($LASTEXITCODE -ne 0) { throw 'Ubuntu checksum download failed.' }

$checksumLine = Get-Content $checksumsPath | Where-Object { $_ -match ('\s+\*?' + [regex]::Escape($imageName) + '$') }
if (@($checksumLine).Count -ne 1) { throw 'No unique checksum found for the Ubuntu image.' }
$expectedHash = ($checksumLine -split '\s+')[0]
if ((Get-FileHash $imagePath -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'Ubuntu root filesystem checksum mismatch.'
}

# Two imports exercise enumeration of a non-default distribution as well.
foreach ($distroName in @('WslSdkTest', 'WslSdkSecondary')) {
    $installPath = Join-Path $env:RUNNER_TEMP $distroName
    wsl.exe --import $distroName $installPath $imagePath --version 1
    if ($LASTEXITCODE -ne 0) { throw "Failed to import $distroName." }
}
wsl.exe --set-default WslSdkTest
if ($LASTEXITCODE -ne 0) { throw 'Failed to select the integration test distribution.' }
wsl.exe --distribution WslSdkTest --exec sh -c 'cat /etc/os-release'
if ($LASTEXITCODE -ne 0) { throw 'Failed to start the integration test distribution.' }
wsl.exe --list --verbose
if ($LASTEXITCODE -ne 0) { throw 'Failed to list integration test distributions.' }
