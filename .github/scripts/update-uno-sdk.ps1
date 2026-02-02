Set-StrictMode -Version latest
$ErrorActionPreference = 'Stop'

$path = "$PSScriptRoot/../../global.json"
$json = (Get-Content -Raw -Path $path | ConvertFrom-Json)
$current = $json.'msbuild-sdks'.'Uno.Sdk'

$action = "$($args[0])"
switch ($action)
{
    "get-version"
    {
        $current
    }
    "set-version"
    {
        $index = "https://api.nuget.org/v3-flatcontainer/uno.sdk/index.json"
        $versions = (Invoke-RestMethod -Uri $index -UseBasicParsing).versions
        $latest = $versions | Where-Object { $_ -notmatch '-' } | Sort-Object { [version]$_ } -Descending | Select-Object -First 1

        if ($latest -ne $current)
        {
            $json.'msbuild-sdks'.'Uno.Sdk' = $latest
            $json | ConvertTo-Json -Depth 10 | Set-Content -Path $path -Encoding UTF8
        }
    }
}
