# Dev helper: build and launch FlowWhisper.App using the locally installed
# .NET 10 SDK (the machine-wide dotnet install only has runtimes up to 8.0.18,
# so DOTNET_ROOT must point at our local SDK for the app to resolve net10.0).
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;" + $env:PATH
$env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

dotnet build src\FlowWhisper.App\FlowWhisper.App.csproj -c Debug
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exe = Join-Path $root "src\FlowWhisper.App\bin\Debug\net10.0-windows10.0.26100.0\win-x64\FlowWhisper.App.exe"
Start-Process -FilePath $exe
