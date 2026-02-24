# Create GitHub Issues from Backlog
# Usage: 
#   .\scripts\create-github-issues.ps1           # Dry run (default)
#   .\scripts\create-github-issues.ps1 -Execute  # Create issues

param(
    [switch]$Execute
)

# Epic name to label mapping
$epicMapping = @{
    "Backlog setup" = "epic: backlog setup"
    "Initial project / solution" = "epic: initial project"
    "Versioning" = "epic: versioning"
    "Logging" = "epic: logging"
    "CI/CD" = "epic: ci/cd"
    "Authentication and authorization" = "epic: authentication"
    "Hotstrings" = "epic: hotstrings"
    "Hotkeys" = "epic: hotkeys"
    "Profiles" = "epic: profiles"
    "Script generation & download" = "epic: script generation"
}

$backlogPath = ".github\backlog"
$files = Get-ChildItem -Path $backlogPath -Filter "*.md" | 
         Where-Object { $_.Name -ne "000-backlog-item-template.md" } |
         Sort-Object Name

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $firstLine = (Get-Content $file.FullName -First 1) -replace "^#\s*", ""

    $labels = @()

    # Type label
    if ($content -match '\*\*Type\*\*:\s*Feature') { $labels += "enhancement" }

    # Epic label
    if ($content -match '\*\*Epic\*\*:\s*(.+)') {
        $epicName = $Matches[1].Trim()
        if ($epicMapping.ContainsKey($epicName)) {
            $labels += $epicMapping[$epicName]
        }
    }

    # Interface labels
    if ($content -match '\*\*Interfaces\*\*:.*API') { $labels += "api" }
    if ($content -match '\*\*Interfaces\*\*:.*UI') { $labels += "ui" }
    if ($content -match '\*\*Interfaces\*\*:.*CLI') { $labels += "cli" }

    $labelArgs = ($labels | ForEach-Object { "--label `"$_`"" }) -join " "

    Write-Host "Creating: $firstLine" -ForegroundColor Cyan
    Write-Host "  Labels: $($labels -join ', ')" -ForegroundColor Gray

    $command = "gh issue create --title `"$firstLine`" --body-file `"$($file.FullName)`" $labelArgs"

    if ($Execute) {
        Invoke-Expression $command
        Write-Host "  ? Created" -ForegroundColor Green
    }
    else {
        Write-Host "  [DRY RUN] $command" -ForegroundColor DarkGray
    }
}

if (-not $Execute) {
    Write-Host "`nDry run complete. Run with -Execute to create issues." -ForegroundColor Yellow
}
else {
    Write-Host "`nAll issues created!" -ForegroundColor Green
}
