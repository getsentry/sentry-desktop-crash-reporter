param(
    [switch] $Clean,
    [string] $Configuration = "Release"
)

Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'

$env:UNO_UITEST_PLATFORM = "Browser"
$env:UNO_UITEST_TARGETURI = "http://localhost:5000"
$env:UNO_UITEST_CHROME_CONTAINER_MODE = $true
if ([string]::IsNullOrWhiteSpace($env:UNO_UITEST_DRIVER_PATH)) {
    $env:UNO_UITEST_DRIVER_PATH = $env:CHROMEWEBDRIVER
}

function Stop-UiTestProcesses {
    param(
        [System.Diagnostics.Process] $Server = $null
    )

    if ($null -ne $Server) {
        try {
            if (!$Server.HasExited) {
                $Server.Kill($true)
                $Server.WaitForExit(5000) | Out-Null
            }
        }
        catch {
            Write-Verbose "Failed to stop UI test server process $($Server.Id): $_"
        }
    }

    if ($Clean) {
        Get-Process chrome, chromedriver -ErrorAction SilentlyContinue |
            Stop-Process -Force -ErrorAction SilentlyContinue
    }
}

if ($Clean) {
    Stop-UiTestProcesses
}

$server = $null
$server = Start-Process -FilePath dotnet -ArgumentList "run --no-build -c $Configuration -f net10.0-browserwasm --project Sentry.CrashReporter/Sentry.CrashReporter.csproj --launch-profile ""Sentry.CrashReporter (WebAssembly)""" -PassThru
try
{
    dotnet test --no-build -c $Configuration -f net10.0 -l GitHubActions -l trx tests/Sentry.CrashReporter.UITests/Sentry.CrashReporter.UITests.csproj
}
finally
{
    Stop-UiTestProcesses -Server $server
}
