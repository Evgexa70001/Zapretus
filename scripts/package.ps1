param([string]$Version = '0.2.0')
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a version such as 0.1.0' }
$projectRoot = Split-Path $PSScriptRoot -Parent
$releaseRoot = Join-Path $projectRoot 'releases'
$staging = Join-Path $releaseRoot "Zapretus-v$Version-win-x64"
$dist = Join-Path $staging 'dist'
if (Test-Path -LiteralPath $staging) { throw "Staging folder already exists: $staging" }
New-Item -ItemType Directory -Path $dist -Force | Out-Null
foreach ($name in @('Zapretus.exe', 'profiles.xml', 'experimental-profiles.xml', 'upstream-manifest.json', 'LICENSE')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot "dist/$name") -Destination $dist
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination (Join-Path $dist 'README.md')
$docs = Join-Path $dist 'docs'
New-Item -ItemType Directory -Path $docs | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs/main-window.png') -Destination $docs
Copy-Item -LiteralPath (Join-Path $projectRoot 'dist/engine') -Destination $dist -Recurse
$archive = Join-Path $releaseRoot "Zapretus-v$Version-win-x64.zip"
Compress-Archive -LiteralPath $dist -DestinationPath $archive
Get-FileHash -LiteralPath $archive -Algorithm SHA256 | Format-List
