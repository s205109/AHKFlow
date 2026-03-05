#!/usr/bin/env pwsh
param([switch]$SkipReport, [switch]$NoOpen)
$ErrorActionPreference = "Stop"
Write-Host "AHKFlow Code Coverage Runner"
Write-Host "============================"
Write-Host "[1/4] Cleaning previous coverage..."
if (Test-Path "./coverage") { Remove-Item "./coverage" -Recurse -Force }
if (Test-Path "./TestResults") { Remove-Item "./TestResults" -Recurse -Force }
Write-Host "[2/4] Running tests with coverage..."
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage --settings coverlet.runsettings
if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: Tests failed."; exit $LASTEXITCODE }
if ($SkipReport) { Write-Host "Done. Coverage in ./coverage"; exit 0 }
Write-Host "[3/4] Checking ReportGenerator..."
if (-not (Get-Command "reportgenerator" -EA SilentlyContinue)) { dotnet tool install --global dotnet-reportgenerator-globaltool }
Write-Host "[4/4] Generating report..."
reportgenerator "-reports:./coverage/**/coverage.cobertura.xml" "-targetdir:./coverage/report" "-reporttypes:Html;Cobertura"
Write-Host "Report: ./coverage/report/index.html"
if (-not $NoOpen) { Start-Process (Join-Path $PWD "coverage/report/index.html") }
Write-Host "Done!"
