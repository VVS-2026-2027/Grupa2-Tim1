# Pokreće sve testove, mjeri pokrivenost i otvara izvještaj u browseru.
# Ako Windows blokira skriptu ("running scripts is disabled on this system"),
# pokrenite pokrivenost.cmd ili:
#   powershell -ExecutionPolicy Bypass -File .\pokrivenost.ps1
Set-Location $PSScriptRoot
Remove-Item -Recurse -Force TestResults, coverage-report -ErrorAction SilentlyContinue

# ReportGenerator se instalira samo prvi put (ili kad se promijeni verzija u dotnet-tools.json)
$verzija = (Get-Content dotnet-tools.json | ConvertFrom-Json).tools.'dotnet-reportgenerator-globaltool'.version
$nuget = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE ".nuget\packages" }
if (-not (Test-Path (Join-Path $nuget "dotnet-reportgenerator-globaltool\$verzija"))) {
    dotnet tool restore
}

dotnet test --settings coverage.runsettings --collect "XPlat Code Coverage" --results-directory TestResults

if (-not (Get-ChildItem TestResults -Recurse -Filter coverage.cobertura.xml -ErrorAction SilentlyContinue)) {
    Write-Host "`nNema rezultata pokrivenosti: build nije uspio. Pogledajte gore liniju sa 'error'." -ForegroundColor Red
    exit 1
}

dotnet reportgenerator "-reports:TestResults/**/coverage.cobertura.xml" "-targetdir:coverage-report" "-reporttypes:Html"
Start-Process "coverage-report/index.html"