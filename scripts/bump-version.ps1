Param(
    [Parameter(Mandatory = $false)][String]$oldVersion,
    [Parameter(Mandatory = $true)][String]$newVersion
)
Set-StrictMode -Version latest

$projectFile = "$PSScriptRoot/../Sentry.CrashReporter/Sentry.CrashReporter.csproj"

$content = Get-Content $projectFile

$content -replace '<ApplicationDisplayVersion>.*</ApplicationDisplayVersion>', "<ApplicationDisplayVersion>$newVersion</ApplicationDisplayVersion>" | Out-File $projectFile
if ("$content" -eq "$(Get-Content $projectFile)")
{
    $versionInFile = [regex]::Match("$content", '<ApplicationDisplayVersion>([^<]+)</ApplicationDisplayVersion>').Groups[1].Value
    if ("$versionInFile" -ne "$newVersion")
    {
        Throw "Failed to update version in $projectFile - the content didn't change. The version found in the file is '$versionInFile'."
    }
}
