$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    python -m unittest discover -s tests -p 'test_*.py'
    if ($LASTEXITCODE -ne 0) { throw 'Profile tests failed' }
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    $testSources = @('src/Probe.cs', 'src/ProbeResult.cs', 'src/ScanReport.cs', 'src/GameFilter.cs', 'src/Engine.cs', 'src/Autostart.cs', 'tests/OfflineTests.cs', 'tests/AutostartTests.cs') | ForEach-Object { Join-Path $projectRoot $_ }
    & $compiler /nologo /target:exe /platform:x64 /out:dist/offline-tests.exe /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll /reference:System.Xml.Linq.dll /reference:Microsoft.CSharp.dll @testSources
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
    & ./dist/offline-tests.exe
    if ($LASTEXITCODE -ne 0) { throw 'Offline logic tests failed' }
} finally { Pop-Location }
