# https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Import-Module $PSScriptRoot/../modules/github-workflows/sentry-cli/integration-test/action.psm1 -Force

Describe "Crash reporter" {
    BeforeAll {
        Write-Host "::group::Build Sentry.CrashReporter"
        dotnet build `
            --configuration Release `
            --framework net9.0-desktop `
            --property IntegrationTest=true `
            $PSScriptRoot/../Sentry.CrashReporter/Sentry.CrashReporter.csproj
        | ForEach-Object { Write-Host $_ }
        Write-Host "::endgroup::"
        $LASTEXITCODE | Should -Be 0
    }

    It "without feedback" {
        $result = Invoke-SentryServer {
            param([string]$url)
            $dsn = $url.Replace('http://', 'http://key@') + '/0'
            dotnet run `
                $PSScriptRoot/../Envelopes/two_items.envelope `
                --no-build `
                --configuration Release `
                --framework net9.0-desktop `
                --environment SENTRY_TEST_DSN=$dsn `
                --environment SENTRY_TEST_AUTO_SUBMIT=1 `
                --project $PSScriptRoot/../Sentry.CrashReporter/Sentry.CrashReporter.csproj
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes().Count | Should -Be 1

        $result.Envelopes()[0] | Should -Match """event_id"":""9ec79c33ec9942ab8353589fcb2e04dc"""
        $result.Envelopes()[0] | Should -Match """type"":""event"""
        $result.Envelopes()[0] | Should -Match """level"":""error"""
    }

    It "with feedback" {
        $result = Invoke-SentryServer {
            param([string]$url)
            $dsn = $url.Replace('http://', 'http://key@') + '/0'
            dotnet run `
                $PSScriptRoot/../Envelopes/two_items.envelope `
                --no-build `
                --configuration Release `
                --framework net9.0-desktop `
                --environment SENTRY_TEST_DSN=$dsn `
                --environment SENTRY_TEST_AUTO_SUBMIT=1 `
                --environment SENTRY_FEEDBACK_NAME="John Doe" `
                --environment SENTRY_FEEDBACK_EMAIL="john.doe@example.com" `
                --environment SENTRY_FEEDBACK_MESSAGE="It crashed!" `
                --project $PSScriptRoot/../Sentry.CrashReporter/Sentry.CrashReporter.csproj
            | ForEach-Object { Write-Host $_ }
            $LASTEXITCODE | Should -Be 0
        }

        $result.HasErrors() | Should -BeFalse
        $result.Envelopes().Count | Should -Be 2

        $result.Envelopes()[0] | Should -Match """event_id"":""9ec79c33ec9942ab8353589fcb2e04dc"""
        $result.Envelopes()[0] | Should -Match """type"":""event"""
        $result.Envelopes()[0] | Should -Match """level"":""error"""

        $result.Envelopes()[1] | Should -Match """type"":""feedback"""
        $result.Envelopes()[1] | Should -Match """name"":""John Doe"""
        $result.Envelopes()[1] | Should -Match """contact_email"":""john.doe@example.com"""
        $result.Envelopes()[1] | Should -Match """message"":""It crashed!"""
    }
}
