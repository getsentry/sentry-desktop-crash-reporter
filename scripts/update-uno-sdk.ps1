Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'

$path = "$PSScriptRoot/../global.json"
$content = Get-Content -Raw -Path $path

switch ("$($args[0])")
{
    "get-version"
    {
        ($content | ConvertFrom-Json).'msbuild-sdks'.'Uno.Sdk'
    }
    "get-repo"
    {
        "https://github.com/unoplatform/uno"
    }
    "set-version"
    {
        $content -replace '(?<="Uno\.Sdk"\s*:\s*")[^"]*', "$($args[1])" |
            Set-Content -Path $path -Encoding UTF8 -NoNewline
    }
}
