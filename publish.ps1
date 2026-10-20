# Builds a self-contained, unpackaged FlowWhisper for x64, zips it, and packs a
# Velopack installer (artifacts\installer\FlowWhisperApp-win-Setup.exe).
# The output runs on a machine without .NET or the Windows App SDK installed;
# speech models are downloaded by the app itself on first start.
#
# Requires the vpk tool, same version as the Velopack package in FlowWhisper.App.csproj:
#   dotnet tool install -g vpk --version 1.2.161
param(
    [string]$Version = "0.1.0",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"
$env:PATH = "$env:DOTNET_ROOT;$env:USERPROFILE\.dotnet\tools;" + $env:PATH

$out = Join-Path $root "artifacts\FlowWhisper"
$zip = Join-Path $root "artifacts\FlowWhisper-$Version-win-x64.zip"
$installer = Join-Path $root "artifacts\installer"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }

dotnet publish (Join-Path $root "src\FlowWhisper.App\FlowWhisper.App.csproj") `
    -c Release -r win-x64 -p:Platform=x64 --self-contained true `
    -p:Version=$Version -o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zip
"{0}  {1:N0} MB" -f $zip, ((Get-Item $zip).Length / 1MB)

if ($SkipInstaller) { return }

# vpk refuses to pack a version that already exists in the output folder, and there is
# no update feed to keep older releases for yet, so start clean.
if (Test-Path $installer) { Remove-Item $installer -Recurse -Force }

# packId differs from the data folder name (%LOCALAPPDATA%\FlowWhisper) on purpose:
# Velopack installs into %LOCALAPPDATA%\<packId> and wipes that folder on uninstall,
# which must not take the downloaded models with it.
vpk pack `
    --packId FlowWhisperApp `
    --packVersion $Version `
    --packDir $out `
    --mainExe FlowWhisper.App.exe `
    --packTitle FlowWhisper `
    --packAuthors FlowWhisper `
    --icon (Join-Path $root "src\FlowWhisper.App\Assets\AppIcon.ico") `
    --outputDir $installer
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Get-ChildItem $installer -Filter *Setup.exe | ForEach-Object { "{0}  {1:N0} MB" -f $_.FullName, ($_.Length / 1MB) }
