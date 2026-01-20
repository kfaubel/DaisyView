# DaisyView Release Documentation - Complete Guide

This document provides an overview of all release-related documentation and a quick-start guide for releasing DaisyView.

## Documentation Overview

### 📚 Release Documentation Files

1. **[GITHUB_RELEASES_GUIDE.md](GITHUB_RELEASES_GUIDE.md)** ⭐ START HERE
   - **Purpose**: Complete step-by-step guide for creating releases
   - **Sections**:
     - Initial Setup (first release)
     - Creating Your First Release (detailed walkthrough)
     - Publishing Subsequent Releases (quick process)
     - Release Notes Template
     - Troubleshooting guide
     - Automation suggestions
   - **Best For**: First time releasers and detailed reference

2. **[RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md)** ⭐ USE FOR EACH RELEASE
   - **Purpose**: Pre-release and release-day checklist
   - **Sections**:
     - Pre-Release Checks (1-2 days before)
     - Release Day Steps (hour-by-hour)
     - Post-Release Actions
     - Rollback procedures
     - Version bump examples
     - Quick command reference
   - **Best For**: Ensuring no steps are missed

3. **[README.md](README.md)** - Section: "Publishing Releases to GitHub"
   - **Purpose**: Quick reference in main documentation
   - **Content**:
     - Prerequisites checklist
     - Initial release steps
     - Subsequent releases
     - Version numbering guide
     - Quick release checklist
   - **Best For**: Quick reference, onboarding

4. **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Section: "Quick Commands"
   - **Purpose**: Common commands at a glance
   - **Content**:
     - Build commands
     - Test commands
     - Release commands (condensed)
   - **Best For**: Terminal-friendly quick access

---

## Quick Start - First Release

**Estimated Time**: 30-45 minutes

### Step 1: Prepare (5 minutes)
```bash
git checkout main
git pull origin main
git status  # Verify clean
```

### Step 2: Update Version (5 minutes)
Edit `DaisyView/DaisyView.csproj`:
```xml
<Version>1.0.0</Version>
```

### Step 3: Build & Test (10 minutes)
```bash
dotnet build DaisyView.sln --configuration Release
dotnet test DaisyView.sln
```

### Step 4: Commit & Tag (5 minutes)
```bash
git add DaisyView/DaisyView.csproj
git commit -m "chore: bump version to 1.0.0"
git push origin main
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

### Step 5: Create Release (10 minutes)
1. Visit: https://github.com/kfaubel/DaisyView/releases
2. Click "Create a new release"
3. Tag: `v1.0.0`
4. Title: `DaisyView 1.0.0 - Initial Release`
5. Description: [Use template from GITHUB_RELEASES_GUIDE.md]
6. Publish

**Total Time**: ~30 minutes

---

## Quick Start - Subsequent Releases

**Estimated Time**: 15-20 minutes

### Step 1: Update Version
Edit `DaisyView/DaisyView.csproj` (e.g., 1.0.0 → 1.0.1)

### Step 2: Build & Test
```bash
dotnet build DaisyView.sln --configuration Release
dotnet test DaisyView.sln
```

### Step 3: Commit & Tag
```bash
git add DaisyView/DaisyView.csproj
git commit -m "chore: bump version to 1.0.1"
git push origin main
git tag -a v1.0.1 -m "Release 1.0.1 - Bug fixes"
git push origin v1.0.1
```

### Step 4: Create Release
- Visit https://github.com/kfaubel/DaisyView/releases/new
- Select tag `v1.0.1`
- Fill in title and notes
- Publish

**Total Time**: ~15 minutes

---

## Version Numbering

Follow [Semantic Versioning 2.0.0](https://semver.org/)

### Format: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes (1.0.0 → 2.0.0)
- **MINOR**: New features, backward compatible (1.0.0 → 1.1.0)
- **PATCH**: Bug fixes only (1.0.0 → 1.0.1)

### Examples
| Change | Current | New | Reason |
|--------|---------|-----|--------|
| Fix WebM freezing | 1.0.0 | 1.0.1 | Patch - bug fix |
| Add HEIC support | 1.0.1 | 1.1.0 | Minor - new feature |
| Remove legacy API | 1.1.0 | 2.0.0 | Major - breaking change |
| Beta testing | 1.0.0 | 1.0.0-beta.1 | Pre-release |

---

## Release Decision Matrix

**What should trigger a release?**

| Scenario | Release? | Version | Notes |
|----------|----------|---------|-------|
| One-line bug fix | Yes | Patch | Security fix = immediate release |
| Feature complete | Yes | Minor | Stable, well-tested |
| Multiple features | Yes | Minor | Can bundle features |
| API redesign | Yes | Major | Plan communication |
| Work in progress | No | N/A | Wait for stable state |
| All tests failing | No | N/A | Fix issues first |

---

## Common Release Scenarios

### Scenario 1: Critical Bug Fix
```bash
# Bug is blocking users
# Version: 1.0.0 → 1.0.1

git tag -a v1.0.1 -m "Release 1.0.1 - Critical bug fix"
# Keep release notes brief, focus on the fix
```

### Scenario 2: New Feature Ready
```bash
# WebM support completed
# Version: 1.0.0 → 1.1.0

# Update version
# Add release notes highlighting new feature
git tag -a v1.1.0 -m "Release 1.1.0 - WebM video support"
```

### Scenario 3: Major Refactor
```bash
# Complete API redesign
# Version: 1.0.0 → 2.0.0

# Document migration guide
# Add breaking changes section
# Consider beta release first: 2.0.0-rc.1
git tag -a v2.0.0 -m "Release 2.0.0 - Major architectural redesign"
```

### Scenario 4: Beta Testing
```bash
# Testing new features before stable release
# Version: 1.0.0 → 1.1.0-beta.1

git tag -a v1.1.0-beta.1 -m "Beta release - WebM support testing"
# Mark as "Pre-release" on GitHub
# Gather feedback for one week
# Then release as 1.1.0 stable
```

---

## Release Checklist at a Glance

**☑️ Pre-Release (1-2 days before)**
- [ ] Tests pass
- [ ] Code reviewed
- [ ] Documentation updated
- [ ] No open blocking issues

**☑️ Release Day**
- [ ] Decide version number
- [ ] Update `DaisyView.csproj`
- [ ] Build Release config: `dotnet build -c Release`
- [ ] Run tests: `dotnet test`
- [ ] Commit version: `git add/commit/push`
- [ ] Create tag: `git tag -a vX.Y.Z`
- [ ] Push tag: `git push origin vX.Y.Z`
- [ ] Create release on GitHub
- [ ] Write release notes
- [ ] Publish release

**☑️ Post-Release**
- [ ] Verify release page
- [ ] Test downloads (if applicable)
- [ ] Monitor for issues
- [ ] Start planning next release

See [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) for detailed checklist.

---

## Common Commands Reference

### Build
```bash
# Debug
dotnet build DaisyView.sln

# Release
dotnet build DaisyView.sln --configuration Release

# Clean and build
dotnet clean DaisyView.sln
dotnet build DaisyView.sln --configuration Release
```

### Testing
```bash
# All tests
dotnet test DaisyView.sln

# Specific test file
dotnet test DaisyView.Tests/SettingsServiceTests.cs

# With coverage
dotnet test DaisyView.sln --collect:"XPlat Code Coverage"
```

### Publishing
```bash
# Publish Release build
dotnet publish -c Release -o ./publish

# Run published executable
./publish/DaisyView.exe
```

### Git Release
```bash
# Commit version change
git add DaisyView/DaisyView.csproj
git commit -m "chore: bump version to X.Y.Z"
git push origin main

# Create annotated tag (preferred)
git tag -a vX.Y.Z -m "Release message"

# Push tag
git push origin vX.Y.Z

# List all tags
git tag -l

# Delete tag (if needed)
git tag -d vX.Y.Z
git push origin --delete vX.Y.Z
```

---

## Troubleshooting Quick Links

| Problem | Solution | Link |
|---------|----------|------|
| Tag already exists | Delete and recreate | GITHUB_RELEASES_GUIDE.md → Troubleshooting |
| Release not appearing | Separate tag from release | GITHUB_RELEASES_GUIDE.md → Verify Release |
| Version mismatch | Verify .csproj update | RELEASE_CHECKLIST.md → Build & Test |
| Tests failing | Run locally before releasing | RELEASE_CHECKLIST.md → Pre-Release |
| Permission denied | Check GitHub auth | GITHUB_RELEASES_GUIDE.md → Troubleshooting |

---

## Release Documentation Map

```
Getting Started:
├── README.md (Publishing section)
└── GITHUB_RELEASES_GUIDE.md ← Start here for detailed walkthrough

For Each Release:
├── RELEASE_CHECKLIST.md ← Use this every time
├── QUICK_REFERENCE.md (Quick Commands)
└── README.md (Quick reference in main doc)

Troubleshooting:
└── GITHUB_RELEASES_GUIDE.md → Troubleshooting section

Development:
├── QUICK_REFERENCE.md (entire file)
├── DEVELOPMENT.md
└── CODE_REVIEW.md
```

---

## Step-by-Step Guides by Experience Level

### 👶 First Time (v1.0.0)
1. Read: [GITHUB_RELEASES_GUIDE.md](GITHUB_RELEASES_GUIDE.md) - "Creating Your First Release"
2. Use: [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) - Check off each step
3. Reference: [README.md](README.md) - Section "Publishing Releases to GitHub"
4. Estimate: 30-45 minutes

### 🚶 Second Release (v1.0.1 or v1.1.0)
1. Reference: [QUICK_REFERENCE.md](QUICK_REFERENCE.md) - Quick Commands section
2. Use: [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) - Check off each step
3. Estimate: 15-20 minutes

### 🏃 Experienced (v2+)
1. Use: [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) - Just check it off
2. Reference: This file or [QUICK_REFERENCE.md](QUICK_REFERENCE.md) if needed
3. Estimate: 10-15 minutes

### 🎓 Teaching Someone Else
1. Have them read: [GITHUB_RELEASES_GUIDE.md](GITHUB_RELEASES_GUIDE.md)
2. Have them use: [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md)
3. Watch them complete: First release
4. Then let them do: Second release independently

---

## Maintaining Release Documentation

### When to Update Release Docs

- [ ] After major workflow changes
- [ ] When GitHub UI changes
- [ ] After releasing v2.0.0 (bigger refactor)
- [ ] When onboarding new team members
- [ ] Quarterly review for accuracy

### How to Update
1. Edit relevant .md file
2. Test steps locally
3. Commit: `git commit -m "docs: update release procedure"`
4. Push: `git push origin main`

---

## FAQ

**Q: How often should we release?**
A: As often as needed. Can be: weekly (bug fixes), monthly (features), or as-needed (when ready).

**Q: Should I use pre-releases?**
A: Yes, for beta testing or RC (Release Candidate) versions. Clearly mark as pre-release on GitHub.

**Q: What if a release has a critical bug?**
A: Release a patch immediately (e.g., 1.0.1). See RELEASE_CHECKLIST.md → Rollback section.

**Q: Can I delete a release after publishing?**
A: Yes, but not recommended if people downloaded it. Instead, mark as pre-release and release a fix.

**Q: Do I need to attach executables?**
A: No, but recommended. Helps users get the right version.

**Q: Can I edit release notes after publishing?**
A: Yes, click the pencil icon on the release page on GitHub.

---

## Summary

🚀 **To Release DaisyView:**

1. **Read**: [GITHUB_RELEASES_GUIDE.md](GITHUB_RELEASES_GUIDE.md) first time
2. **Follow**: [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) every time
3. **Reference**: [QUICK_REFERENCE.md](QUICK_REFERENCE.md) or [README.md](README.md)

**Three files, simple process, repeatable results.**

---

*Last Updated: January 18, 2026*
*Current Version: 1.0.0*
*Next Release: [Plan accordingly]*

**For detailed procedures, see [GITHUB_RELEASES_GUIDE.md](GITHUB_RELEASES_GUIDE.md)** ⭐
