param([string]$OutputName = 'Zapretus.exe')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    python scripts/prepare_engine.py
    if ($LASTEXITCODE -ne 0) { throw 'Engine preparation failed' }
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    $sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object FullName)
    $appIcon = 'src/Zapretus.ico'
    & $compiler /nologo /target:winexe /platform:x64 /optimize+ "/out:dist/$OutputName" /win32manifest:src/app.manifest "/win32icon:$appIcon" "/resource:$appIcon,Zapretus.AppIcon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.Linq.dll /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll /reference:Microsoft.CSharp.dll @sourceFiles
    if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
    Copy-Item -LiteralPath README.md -Destination dist/README.md
    New-Item -ItemType Directory -Path dist/docs -Force | Out-Null
    Copy-Item -LiteralPath docs/main-window.png -Destination dist/docs/main-window.png
    Copy-Item -LiteralPath LICENSE -Destination dist/LICENSE
    Write-Host "Built dist/$OutputName. Application and engine were NOT launched."
} finally { Pop-Location }
