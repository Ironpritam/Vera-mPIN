$ErrorActionPreference = "Stop"

$projectDirectory = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectDirectory "VeraMPIN.csproj"
$scriptFile = Join-Path $PSScriptRoot "VeraMPIN.iss"
$payloadDirectory = Join-Path $projectDirectory "artifacts\VeraMPIN-InstallerPayload"
$sourceArchive = Join-Path $projectDirectory "ThirdParty\VeraCrypt_1.26.24_Source.zip"
$veraCryptExecutable = Join-Path $projectDirectory "ThirdParty\VeraCrypt-x64.exe"
$compiler = Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"

if (-not (Test-Path $compiler)) {
    $compilerCommand = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($null -eq $compilerCommand) {
        throw "Inno Setup 6 is required. Install it from https://jrsoftware.org/isinfo.php."
    }

    $compiler = $compilerCommand.Source
}

if (-not (Test-Path $sourceArchive)) {
    throw "The matching VeraCrypt source archive is missing: $sourceArchive"
}

if (-not (Test-Path $veraCryptExecutable)) {
    throw "The signed VeraCrypt 1.26.24 executable is missing: $veraCryptExecutable"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$sourceZip = [System.IO.Compression.ZipFile]::OpenRead($sourceArchive)
try {
    if (-not ($sourceZip.Entries | Where-Object FullName -Match '(^|/)License\.txt$')) {
        throw "The VeraCrypt source archive does not contain its License.txt file."
    }
}
finally {
    $sourceZip.Dispose()
}

$veraCryptVersion = (Get-Item $veraCryptExecutable).VersionInfo.ProductVersion
if ($veraCryptVersion -ne "1.26.24" -or
    (Get-AuthenticodeSignature $veraCryptExecutable).Status -ne "Valid") {
    throw "The bundled VeraCrypt executable must be the validly signed 1.26.24 release."
}

if (Test-Path $payloadDirectory) {
    Remove-Item -LiteralPath $payloadDirectory -Recurse -Force
}

& dotnet publish $projectFile -c Release -r win-x64 --self-contained true `
    -p:DebugType=None -p:DebugSymbols=false -o $payloadDirectory
if ($LASTEXITCODE -ne 0) {
    throw "Self-contained Vera mPIN publish failed with exit code $LASTEXITCODE."
}

$dotnetRoot = Join-Path $env:ProgramFiles "dotnet"
$dotnetLicense = Join-Path $dotnetRoot "LICENSE.txt"
$dotnetNotices = Join-Path $dotnetRoot "ThirdPartyNotices.txt"
if (-not (Test-Path $dotnetLicense) -or -not (Test-Path $dotnetNotices)) {
    throw "The .NET license and third-party notices could not be found under $dotnetRoot."
}

$licenseDirectory = Join-Path $payloadDirectory "Licenses"
New-Item -ItemType Directory -Force $licenseDirectory | Out-Null
Copy-Item $dotnetLicense (Join-Path $licenseDirectory "DotNet-LICENSE.txt") -Force
Copy-Item $dotnetNotices (Join-Path $licenseDirectory "DotNet-ThirdPartyNotices.txt") -Force
Copy-Item $veraCryptExecutable (Join-Path $payloadDirectory "VeraCrypt-x64.exe") -Force

& $compiler $scriptFile
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup compilation failed with exit code $LASTEXITCODE."
}

$installerDirectory = Join-Path $projectDirectory "artifacts\VeraMPIN-Installer"
$installer = Get-ChildItem $installerDirectory -Filter "VeraMPIN-Setup-*-x64.exe" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if ($null -eq $installer) {
    throw "Inno Setup completed without producing the expected installer."
}

$payloadBytes = (Get-ChildItem $payloadDirectory -File -Recurse |
    Measure-Object Length -Sum).Sum
$sourceBytes = (Get-Item $sourceArchive).Length
$installedBytes = $payloadBytes + $sourceBytes

[pscustomobject]@{
    Installer = $installer.FullName
    InstallerMiB = [math]::Round($installer.Length / 1MB, 1)
    InstalledFilesMiB = [math]::Round($installedBytes / 1MB, 1)
    SelfContainedAppMiB = [math]::Round($payloadBytes / 1MB, 1)
    VeraCryptSourceArchiveMiB = [math]::Round($sourceBytes / 1MB, 1)
}
