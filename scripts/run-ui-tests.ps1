Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'

$env:UNO_UITEST_PLATFORM = "Browser"
$env:UNO_UITEST_TARGETURI = "http://localhost:5000"
$env:UNO_UITEST_CHROME_CONTAINER_MODE = $true
$env:UNO_UITEST_DRIVER_PATH = $env:CHROMEWEBDRIVER

$server = Start-Process -FilePath dotnet -ArgumentList "run --no-build -c Release -f net9.0-browserwasm --project Sentry.CrashReporter/Sentry.CrashReporter.csproj --launch-profile ""Sentry.CrashReporter (WebAssembly)""" -PassThru
try
{
    dotnet test --no-build -c Release -f net9.0 -l GitHubActions -l trx Sentry.CrashReporter.UITests/Sentry.CrashReporter.UITests.csproj
}
finally
{
    Stop-Process -Id $server.Id -Force
}
