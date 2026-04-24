Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'

$path = "$PSScriptRoot/../global.json"

switch ("$($args[0])")
{
    "get-repo"
    {
        "https://github.com/unoplatform/uno"
    }
    "get-version"
    {
        (Get-Content -Raw -Path $path | ConvertFrom-Json).'msbuild-sdks'.'Uno.Sdk'
    }
    "set-version"
    {
        (Get-Content -Raw -Path $path) -replace '(?<="Uno\.Sdk"\s*:\s*")[^"]*', "$($args[1])" |
            Set-Content -Path $path -Encoding UTF8 -NoNewline
    }
}
