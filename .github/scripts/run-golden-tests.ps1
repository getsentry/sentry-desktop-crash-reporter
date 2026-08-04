param(
    [switch] $UpdateGoldens,
    [switch] $NoPublish,
    [string] $Configuration = "Release",
    [string] $Framework = "net10.0-desktop",
    [string] $RuntimeIdentifier = "",
    [string] $PublishOutput = "",
    [string] $Fixture = "",
    [string] $ViewName = "",
    [string] $ThemeName = "",
    [string] $GoldenRoot = "tests/goldens",
    [string] $ResultsRoot = "tests/Sentry.CrashReporter.GoldenTests/TestResults",
    [int] $TimeoutSeconds = 60
)

Set-StrictMode -Version latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")

function Resolve-RepoPath {
    param([string] $Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $repoRoot $Path
}

function Resolve-RuntimeIdentifier {
    if (![string]::IsNullOrWhiteSpace($RuntimeIdentifier)) {
        return $RuntimeIdentifier
    }

    foreach ($line in dotnet --info) {
        if ($line -match "^\s*RID:\s+(\S+)") {
            return $matches[1]
        }
    }

    throw "Unable to resolve the current .NET runtime identifier."
}

function Get-AppExecutable {
    param([string] $PublishDir)

    if ($IsMacOS) {
        $appBundle = Join-Path $PublishDir "Sentry Crash Reporter.app"
        $macOSDir = Join-Path $appBundle "Contents/MacOS"
        $preferred = Join-Path $macOSDir "Sentry Crash Reporter"
        if (Test-Path $preferred) {
            return $preferred
        }

        $candidate = Get-ChildItem -Path $macOSDir -File | Select-Object -First 1
        if ($null -ne $candidate) {
            return $candidate.FullName
        }
    }
    elseif ($IsWindows) {
        $candidate = Join-Path $PublishDir "Sentry.CrashReporter.exe"
        if (Test-Path $candidate) {
            return $candidate
        }
    }
    else {
        $candidate = Join-Path $PublishDir "Sentry.CrashReporter"
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "Unable to find the published app executable in $PublishDir."
}

function Resolve-Fixture {
    param(
        [string] $RequestedFixture,
        [string] $RequestedViewName
    )

    if (![string]::IsNullOrWhiteSpace($RequestedFixture)) {
        return Resolve-RepoPath $RequestedFixture
    }

    $view = $goldenManifest.Views.PSObject.Properties[$RequestedViewName]
    if ($null -ne $view) {
        $fixture = $view.Value.PSObject.Properties["fixture"]
        if ($null -ne $fixture -and ![string]::IsNullOrWhiteSpace($fixture.Value)) {
            return Resolve-RepoPath $fixture.Value
        }
    }

    return Resolve-RepoPath "tests/data/inproc.envelope"
}

function Get-GoldenManifest {
    $manifestPath = Resolve-RepoPath "tests/goldens/views.json"
    if (!(Test-Path $manifestPath)) {
        return [pscustomobject]@{
            Themes = @("light")
            Views = [pscustomobject]@{
                feedback = [pscustomobject]@{
                    fixture = "tests/data/inproc.envelope"
                }
            }
        }
    }

    $manifest = Get-Content -Path $manifestPath -Raw | ConvertFrom-Json
    $themes = $manifest.PSObject.Properties["themes"]
    $views = $manifest.PSObject.Properties["views"]

    return [pscustomobject]@{
        Themes = if ($null -ne $themes) {
            @($themes.Value | ForEach-Object { Resolve-ThemeName $_ })
        }
        else {
            @("light")
        }
        Views = if ($null -ne $views) {
            $views.Value
        }
        else {
            $manifest
        }
    }
}

function Resolve-ThemeName {
    param([string] $Theme)

    switch ($Theme.ToLowerInvariant()) {
        "light" { return "light" }
        "dark" { return "dark" }
        "default" { return "default" }
        default { throw "Unknown golden theme: $Theme" }
    }
}

function Get-AppThemeName {
    param([string] $Theme)

    switch (Resolve-ThemeName $Theme) {
        "dark" { return "Dark" }
        "default" { return "Default" }
        default { return "Light" }
    }
}

function Get-GoldenCaseName {
    param(
        [string] $View,
        [string] $Theme
    )

    $theme = Resolve-ThemeName $Theme
    return "$View-$theme"
}

function Get-GoldenFileName {
    param(
        [string] $Rid,
        [string] $View,
        [string] $Theme
    )

    return "$Rid-$(Get-GoldenCaseName $View $Theme).png"
}

$rid = Resolve-RuntimeIdentifier
$goldenManifest = Get-GoldenManifest
$publishDir = if ([string]::IsNullOrWhiteSpace($PublishOutput)) {
    Resolve-RepoPath "tests/Sentry.CrashReporter.GoldenTests/publish/$rid"
}
else {
    Resolve-RepoPath $PublishOutput
}

function Get-ManifestGoldenCases {
    param(
        [string] $RequestedViewName,
        [string] $RequestedThemeName
    )

    if (![string]::IsNullOrWhiteSpace($RequestedViewName)) {
        $viewNames = @($RequestedViewName)
    }
    else {
        $viewNames = @($goldenManifest.Views.PSObject.Properties.Name)
    }

    if (![string]::IsNullOrWhiteSpace($RequestedThemeName)) {
        $themeNames = @(Resolve-ThemeName $RequestedThemeName)
    }
    else {
        $themeNames = @($goldenManifest.Themes)
    }

    $cases = @()
    foreach ($view in $viewNames) {
        if ($null -eq $goldenManifest.Views.PSObject.Properties[$view]) {
            throw "Unknown golden view: $view"
        }

        foreach ($theme in $themeNames) {
            $caseName = Get-GoldenCaseName $view $theme
            $cases += [pscustomobject]@{
                View = $view
                Theme = Resolve-ThemeName $theme
                CaseName = $caseName
                FileName = Get-GoldenFileName $rid $view $theme
            }
        }
    }

    return $cases
}

function Resolve-GoldenCases {
    return Get-ManifestGoldenCases $ViewName $ThemeName
}

$goldenCases = Resolve-GoldenCases
$resultDir = Resolve-RepoPath (Join-Path $ResultsRoot $rid)
New-Item -ItemType Directory -Force -Path $resultDir | Out-Null

if (!$NoPublish) {
    $project = Resolve-RepoPath "Sentry.CrashReporter/Sentry.CrashReporter.csproj"
    dotnet publish -c $Configuration -f $Framework -r $rid $project -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

$appExecutable = Get-AppExecutable $publishDir

$goldenTestProject = Resolve-RepoPath "tests/Sentry.CrashReporter.GoldenTests/Sentry.CrashReporter.GoldenTests.csproj"

foreach ($case in $goldenCases) {
    $fixturePath = Resolve-Fixture $Fixture $case.View
    $goldenPath = Resolve-RepoPath (Join-Path $GoldenRoot $case.FileName)
    $actualPath = Join-Path $resultDir "$($case.CaseName).actual.png"
    $diffPath = Join-Path $resultDir "$($case.CaseName).diff.png"
    $stdoutPath = Join-Path $resultDir "$($case.CaseName).app.stdout.log"
    $stderrPath = Join-Path $resultDir "$($case.CaseName).app.stderr.log"

    Remove-Item -Force -ErrorAction SilentlyContinue $actualPath, $diffPath, $stdoutPath, $stderrPath

    $launchFile = $appExecutable
    $launchArguments = @($fixturePath)

    if (!$IsWindows -and !$IsMacOS -and [string]::IsNullOrWhiteSpace($env:DISPLAY)) {
        $xvfb = Get-Command xvfb-run -ErrorAction SilentlyContinue
        if ($null -ne $xvfb) {
            $launchFile = $xvfb.Source
            $launchArguments = @(
                "-a",
                "-s",
                "-screen 0 1280x900x24 -dpi 96",
                $appExecutable,
                $fixturePath
            )
        }
    }

    $oldOutput = $env:SENTRY_CRASH_REPORTER_GOLDEN_TEST_OUTPUT
    $oldTheme = $env:SENTRY_CRASH_REPORTER_GOLDEN_TEST_THEME
    $oldView = $env:SENTRY_CRASH_REPORTER_GOLDEN_TEST_VIEW
    $oldLang = $env:LANG
    $oldCliLanguage = $env:DOTNET_CLI_UI_LANGUAGE

    try {
        $env:SENTRY_CRASH_REPORTER_GOLDEN_TEST_OUTPUT = $actualPath
        $env:SENTRY_CRASH_REPORTER_GOLDEN_TEST_THEME = Get-AppThemeName $case.Theme
        $env:SENTRY_CRASH_REPORTER_GOLDEN_TEST_VIEW = $case.View
        $env:LANG = "en_US.UTF-8"
        $env:DOTNET_CLI_UI_LANGUAGE = "en"

        $process = Start-Process `
            -FilePath $launchFile `
            -ArgumentList $launchArguments `
            -WorkingDirectory $publishDir `
            -RedirectStandardOutput $stdoutPath `
            -RedirectStandardError $stderrPath `
            -PassThru

        if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
            try {
                $process.Kill($true)
            }
            catch {
                $process.Kill()
            }

            throw "Timed out waiting for the golden capture after $TimeoutSeconds seconds."
        }

        if ($process.ExitCode -ne 0) {
            if (Test-Path $stderrPath) {
                Get-Content $stderrPath | Write-Error
            }

            throw "The published app exited with code $($process.ExitCode)."
        }

        if (!(Test-Path $actualPath)) {
            throw "The published app did not create the golden screenshot: $actualPath"
        }
    }
    finally {
        $env:SENTRY_CRASH_REPORTER_GOLDEN_TEST_OUTPUT = $oldOutput
        $env:SENTRY_CRASH_REPORTER_GOLDEN_TEST_THEME = $oldTheme
        $env:SENTRY_CRASH_REPORTER_GOLDEN_TEST_VIEW = $oldView
        $env:LANG = $oldLang
        $env:DOTNET_CLI_UI_LANGUAGE = $oldCliLanguage
    }

    $compareArgs = @(
        "run",
        "--project", $goldenTestProject,
        "-c", "Release",
        "--",
        "compare",
        "--expected", $goldenPath,
        "--actual", $actualPath,
        "--diff", $diffPath
    )

    if ($UpdateGoldens) {
        $compareArgs += "--update"
    }

    dotnet @compareArgs
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

exit 0
