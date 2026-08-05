param(
    [Parameter(Mandatory = $true)]
    [string] $RuntimeIdentifier,
    [string] $GoldenRoot = "tests/goldens",
    [string] $ManifestPath = "tests/goldens/views.json"
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

function Write-StepOutput {
    param(
        [string] $Name,
        [string] $Value
    )

    if (![string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
        "$Name=$Value" | Out-File -FilePath $env:GITHUB_OUTPUT -Append
    }
}

$manifest = Get-Content -Path (Resolve-RepoPath $ManifestPath) -Raw | ConvertFrom-Json
$themesProperty = $manifest.PSObject.Properties["themes"]
$viewsProperty = $manifest.PSObject.Properties["views"]
$themes = if ($null -ne $themesProperty) {
    @($themesProperty.Value)
}
else {
    @("light")
}
$views = if ($null -ne $viewsProperty) {
    @($viewsProperty.Value.PSObject.Properties.Name)
}
else {
    @($manifest.PSObject.Properties.Name | Where-Object { $_ -ne "themes" })
}

$missing = @()
foreach ($view in $views) {
    foreach ($theme in $themes) {
        $path = Resolve-RepoPath (Join-Path $GoldenRoot "$RuntimeIdentifier-$view-$theme.png")
        if (!(Test-Path $path)) {
            $missing += $path
        }
    }
}

if ($missing.Count -gt 0) {
    Write-StepOutput "exists" "false"
    Write-Host "::error::Missing goldens for $RuntimeIdentifier. Run 'make update-goldens'."
    exit 1
}

Write-StepOutput "exists" "true"
