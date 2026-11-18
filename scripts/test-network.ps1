# Run explicitly: live tests temporarily enable packet processing and need elevation.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    $sources = @('src/Engine.cs', 'src/GameFilter.cs', 'src/Probe.cs', 'src/ProbeResult.cs', 'src/ScanReport.cs', 'tests/NetworkTests.cs') | ForEach-Object { Join-Path $projectRoot $_ }
    & $compiler /nologo /target:exe /platform:x64 /out:dist/network-tests.exe /win32manifest:src/app.manifest /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll /reference:System.Xml.Linq.dll @sources
    if ($LASTEXITCODE -ne 0) { throw 'Network test compilation failed' }
    $runner = Start-Process -FilePath (Join-Path $projectRoot 'dist/network-tests.exe') -Verb RunAs -WindowStyle Hidden -PassThru
    Write-Host "Network test PID: $($runner.Id). Results: dist/network-tests.log and dist/scan-*.csv"
} finally { Pop-Location }
