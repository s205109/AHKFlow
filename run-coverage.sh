#!/usr/bin/env bash
set -e
echo "AHKFlow Code Coverage Runner"
echo "============================"
echo "[1/4] Cleaning previous coverage..."
rm -rf ./coverage ./TestResults
echo "[2/4] Running tests with coverage..."
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage --settings coverlet.runsettings
echo "[3/4] Checking ReportGenerator..."
command -v reportgenerator >/dev/null 2>&1 || dotnet tool install --global dotnet-reportgenerator-globaltool
echo "[4/4] Generating report..."
reportgenerator "-reports:./coverage/**/coverage.cobertura.xml" "-targetdir:./coverage/report" "-reporttypes:Html;Cobertura"
echo "Report: ./coverage/report/index.html"
echo "Done!"
