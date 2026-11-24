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
                Start-Sleep -Seconds 1
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

function Wait-UiTestServer {
    param(
        [System.Diagnostics.Process] $Server,
        [int] $TimeoutSeconds = 120
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        if ($Server.HasExited) {
            throw "UI test server exited with code $($Server.ExitCode)."
        }

        try {
            Invoke-WebRequest -Uri $env:UNO_UITEST_TARGETURI -TimeoutSec 5 | Out-Null
            return
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    throw "UI test server did not respond at $env:UNO_UITEST_TARGETURI within $TimeoutSeconds seconds."
}

if ($Clean) {
    Stop-UiTestProcesses
}

$server = $null
$server = Start-Process -FilePath dotnet -ArgumentList "run --no-build -c $Configuration -f net10.0-browserwasm --project Sentry.CrashReporter/Sentry.CrashReporter.csproj --launch-profile ""Sentry.CrashReporter (WebAssembly)""" -PassThru
try
{
    Wait-UiTestServer -Server $server
    dotnet test --no-build -c $Configuration -f net10.0 -l GitHubActions -l trx tests/Sentry.CrashReporter.UITests/Sentry.CrashReporter.UITests.csproj
}
finally
{
    Stop-UiTestProcesses -Server $server
}
