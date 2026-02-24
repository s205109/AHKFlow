# GitHub Open Source Setup Guide for AHKFlow

Quick setup guide for GitHub Issues, Projects, labels, and branch protection.

---

## 1. Create Labels

Create these new labels (in addition to GitHub defaults like `bug`, `enhancement`):

```bash
gh repo set-default s205109/AHKFlow
gh label list --repo s205109/AHKFlow --limit 100
gh label create "epic" --color "7057ff" --description "Large feature spanning multiple issues"
gh label create "wip" --color "fbca04" --description "Work in progress"
gh label create "api" --color "1d76db" --description "Affects API layer"
gh label create "ui" --color "5319e7" --description "Affects UI/Blazor layer"
gh label create "cli" --color "e99695" --description "Affects CLI tool"
gh label list --repo s205109/AHKFlow --limit 100
```

---

## 2. Branch Protection

### Initial Setup (Without Status Checks)

**Note**: Status checks require at least one GitHub Actions workflow to run first. Set up basic protection now, add status checks later.

1. Go to **Settings** → **Branches** → **Add branch ruleset**
2. Configure:

```plaintext
Ruleset name: Protect main
Enforcement status: Active
Target branches: Include default branch

Rules:
✅ Restrict deletions
✅ Require linear history
✅ Require a pull request before merging
   - Required approvals: 0
   - Dismiss stale approvals: ✅
   - Require conversation resolution: ✅
⬜ Require status checks to pass (skip for now)
✅ Block force pushes
```

### Add Status Checks (After CI Workflow Exists)

After you have a CI workflow running (e.g., from backlog item 010), return to add status checks:

1. Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    name: build
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --verbosity normal
```

1. Commit and push to `main`:

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add CI workflow"
git push origin main
```

1. Wait for workflow to complete in **Actions** tab

1. Edit your branch ruleset:
   - Go to **Settings** → **Branches** → **Rulesets** → **Protect main**
   - ✅ Check **Require status checks to pass**
   - Select `build` from the status check list
   - ✅ Check **Require branches to be up to date**
   - Save

---

## 3. PR Template

Create `.github/PULL_REQUEST_TEMPLATE.md`:

```markdown
## Related Issue

Closes #<issue_number>

## Summary

Brief description of changes.

## Checklist

- [ ] Tests pass (`dotnet test`)
- [ ] Build succeeds (`dotnet build`)
```

---

## 4. Create GitHub Project

1. Go to **Projects** tab → **New project** → **Board**
2. Name: `AHKFlow Backlog`
3. Create columns: `Todo`, `Ready`, `In Progress`, `Review`, `Done`

### Configure Automation

1. Click **⋯** menu → **Settings** → **Workflows**
2. Enable:
   - **Item added to project** → Set status to `Todo`
   - **Pull request opened** → Set status to `In Progress`
   - **Item closed** → Set status to `Done`

---

## 5. Create Issues from Backlog

### Batch Script

Create `scripts/create-github-issues.ps1`:

```powershell
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
```

### Run

```powershell
gh auth status          # Verify authenticated
.\scripts\create-github-issues.ps1   # Dry run
# Edit script, uncomment Invoke-Expression, run again to create
```

### Add Issues to Project

After creating issues, add them to your project:

```bash
# List issues and add to project
gh issue list --limit 100 --json number | ConvertFrom-Json | ForEach-Object { gh project item-add 1 --owner s205109 --url "https://github.com/s205109/AHKFlow/issues/$($_.number)" }
```

---

## Quick Checklist

- [ ] Create labels (`epic`, `wip`, `api`, `ui`, `cli`)
- [ ] Configure branch protection ruleset
- [ ] Create PR template
- [ ] Create GitHub Project with columns
- [ ] Configure project automation
- [ ] Run script to create issues from backlog
- [ ] Add issues to project
