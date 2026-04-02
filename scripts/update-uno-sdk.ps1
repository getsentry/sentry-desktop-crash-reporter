Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'

$path = "$PSScriptRoot/../global.json"

function Get-Json($key)
{
    (Get-Content -Raw -Path $path | ConvertFrom-Json).$key
}

function Set-Json([string[]]$keys, $value)
{
    $json = $node = Get-Content -Raw -Path $path | ConvertFrom-Json
    $keys | Select-Object -SkipLast 1 | ForEach-Object { $node = $node.$_ }
    $node.($keys[-1]) = $value
    $json | ConvertTo-Json -Depth 10 | Set-Content -Path $path -Encoding UTF8
}

switch ("$($args[0])")
{
    "get-version"
    {
        (Get-Json 'msbuild-sdks').'Uno.Sdk'
    }
    "get-repo"
    {
        "https://github.com/unoplatform/uno"
    }
    "set-version"
    {
        Set-Json 'msbuild-sdks', 'Uno.Sdk' "$($args[1])"
    }
}
