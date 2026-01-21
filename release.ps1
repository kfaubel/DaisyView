<#
.SYNOPSIS
    Bumps the version, commits, tags, and pushes to trigger a GitHub release.

.DESCRIPTION
    This script reads the current version from DaisyView.csproj, bumps either the
    major, minor, or patch number, updates the file, commits the change, creates
    a git tag, and pushes to trigger the GitHub Actions release workflow.

.PARAMETER BumpType
    The type of version bump: 'major', 'minor', or 'patch' (default: patch)

.PARAMETER DryRun
    If specified, shows what would happen without making changes

.EXAMPLE
    .\release.ps1 patch
    Bumps 1.0.4 -> 1.0.5, commits, tags v1.0.5, and pushes

.EXAMPLE
    .\release.ps1 minor
    Bumps 1.0.4 -> 1.1.0, commits, tags v1.1.0, and pushes

.EXAMPLE
    .\release.ps1 major
    Bumps 1.0.4 -> 2.0.0, commits, tags v2.0.0, and pushes

.EXAMPLE
    .\release.ps1 patch -DryRun
    Shows what would happen without making changes
#>

param(
    [Parameter(Position = 0)]
    [ValidateSet('major', 'minor', 'patch')]
    [string]$BumpType = 'patch',
    
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# Configuration
$csprojPath = Join-Path $PSScriptRoot 'DaisyView\DaisyView.csproj'

function Get-CurrentVersion {
    $content = Get-Content $csprojPath -Raw
    if ($content -match '<Version>(\d+)\.(\d+)\.(\d+)</Version>') {
        return @{
            Major = [int]$Matches[1]
            Minor = [int]$Matches[2]
            Patch = [int]$Matches[3]
            Full  = "$($Matches[1]).$($Matches[2]).$($Matches[3])"
        }
    }
    throw "Could not find <Version> tag in $csprojPath"
}

function Get-BumpedVersion {
    param($Current, $Type)
    
    switch ($Type) {
        'major' { 
            return @{
                Major = $Current.Major + 1
                Minor = 0
                Patch = 0
            }
        }
        'minor' { 
            return @{
                Major = $Current.Major
                Minor = $Current.Minor + 1
                Patch = 0
            }
        }
        'patch' { 
            return @{
                Major = $Current.Major
                Minor = $Current.Minor
                Patch = $Current.Patch + 1
            }
        }
    }
}

function Update-VersionInFile {
    param($NewVersion)
    
    $content = Get-Content $csprojPath -Raw
    $newVersionString = "$($NewVersion.Major).$($NewVersion.Minor).$($NewVersion.Patch)"
    $updatedContent = $content -replace '<Version>\d+\.\d+\.\d+</Version>', "<Version>$newVersionString</Version>"
    Set-Content $csprojPath $updatedContent -NoNewline
    return $newVersionString
}

function Test-GitClean {
    $status = git status --porcelain
    # Filter out the csproj file since we're about to modify it
    $otherChanges = $status | Where-Object { $_ -notmatch 'DaisyView\.csproj' }
    if ($otherChanges) {
        Write-Warning "You have uncommitted changes (other than version bump):"
        Write-Host $otherChanges
        $response = Read-Host "Continue anyway? (y/N)"
        if ($response -ne 'y' -and $response -ne 'Y') {
            exit 1
        }
    }
}

function Test-OnMainBranch {
    $branch = git rev-parse --abbrev-ref HEAD
    if ($branch -ne 'main') {
        Write-Warning "You are on branch '$branch', not 'main'"
        $response = Read-Host "Continue anyway? (y/N)"
        if ($response -ne 'y' -and $response -ne 'Y') {
            exit 1
        }
    }
}

# Main script
Write-Host "`n=== DaisyView Release Script ===" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
if (-not (Test-Path $csprojPath)) {
    throw "Cannot find $csprojPath"
}

# Get current version
$currentVersion = Get-CurrentVersion
Write-Host "Current version: " -NoNewline
Write-Host "v$($currentVersion.Full)" -ForegroundColor Yellow

# Calculate new version
$newVersion = Get-BumpedVersion -Current $currentVersion -Type $BumpType
$newVersionString = "$($newVersion.Major).$($newVersion.Minor).$($newVersion.Patch)"
$tagName = "v$newVersionString"

Write-Host "New version:     " -NoNewline
Write-Host $tagName -ForegroundColor Green
Write-Host "Bump type:       $BumpType"
Write-Host ""

if ($DryRun) {
    Write-Host "[DRY RUN] Would perform the following actions:" -ForegroundColor Magenta
    Write-Host "  1. Update version in $csprojPath to $newVersionString"
    Write-Host "  2. Commit with message: 'Bump version to $newVersionString'"
    Write-Host "  3. Create tag: $tagName"
    Write-Host "  4. Push commit and tag to origin"
    Write-Host "  5. GitHub Actions would create release: DaisyView $tagName"
    exit 0
}

# Confirm
Write-Host "This will:" -ForegroundColor Yellow
Write-Host "  1. Update version in .csproj"
Write-Host "  2. Commit the change"
Write-Host "  3. Create git tag $tagName"
Write-Host "  4. Push to GitHub (triggers release build)"
Write-Host ""
$confirm = Read-Host "Proceed? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Host "Aborted." -ForegroundColor Red
    exit 1
}

# Safety checks
Test-OnMainBranch
Test-GitClean

# Pull latest
Write-Host "`nPulling latest changes..." -ForegroundColor Cyan
git pull origin main
if ($LASTEXITCODE -ne 0) { throw "git pull failed" }

# Update version
Write-Host "Updating version to $newVersionString..." -ForegroundColor Cyan
Update-VersionInFile -NewVersion $newVersion

# Commit
Write-Host "Committing..." -ForegroundColor Cyan
git add $csprojPath
git commit -m "Bump version to $newVersionString"
if ($LASTEXITCODE -ne 0) { throw "git commit failed" }

# Tag
Write-Host "Creating tag $tagName..." -ForegroundColor Cyan
git tag $tagName
if ($LASTEXITCODE -ne 0) { throw "git tag failed" }

# Push
Write-Host "Pushing to origin..." -ForegroundColor Cyan
git push origin main
if ($LASTEXITCODE -ne 0) { throw "git push failed" }

git push origin $tagName
if ($LASTEXITCODE -ne 0) { throw "git push tag failed" }

Write-Host ""
Write-Host "=== Success! ===" -ForegroundColor Green
Write-Host "Version bumped to $newVersionString and tag $tagName pushed."
Write-Host "GitHub Actions will now build and create the release."
Write-Host ""
Write-Host "Monitor the release at:" -ForegroundColor Cyan
Write-Host "  https://github.com/kfaubel/DaisyView/actions"
Write-Host "  https://github.com/kfaubel/DaisyView/releases"
