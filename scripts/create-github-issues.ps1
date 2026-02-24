# Create GitHub Issues from Backlog
# Usage: .\scripts\create-github-issues.ps1

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
    
    # Interface labels
    if ($content -match '\*\*Interfaces\*\*:.*API') { $labels += "api" }
    if ($content -match '\*\*Interfaces\*\*:.*UI') { $labels += "ui" }
    if ($content -match '\*\*Interfaces\*\*:.*CLI') { $labels += "cli" }
    
    $labelArgs = ($labels | ForEach-Object { "--label `"$_`"" }) -join " "
    
    Write-Host "Creating: $firstLine" -ForegroundColor Cyan
    Write-Host "  Labels: $($labels -join ', ')" -ForegroundColor Gray
    
    $command = "gh issue create --title `"$firstLine`" --body-file `"$($file.FullName)`" $labelArgs"
    
    # Uncomment to create issues:
    # Invoke-Expression $command
    
    Write-Host "  $command" -ForegroundColor DarkGray
}

Write-Host "`nDry run complete. Uncomment Invoke-Expression to create issues." -ForegroundColor Yellow
