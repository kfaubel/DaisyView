# DaisyView Release Checklist

Use this checklist for every release to ensure consistency and quality.

## Pre-Release (1-2 days before)

### Code Quality
- [ ] All feature branches merged to `main`
- [ ] Code review completed for all changes
- [ ] All tests passing: `dotnet test DaisyView.sln`
- [ ] No compiler warnings: `dotnet build DaisyView.sln`
- [ ] Release build successful: `dotnet build DaisyView.sln --configuration Release`

### Documentation
- [ ] README.md updated with new features/fixes
- [ ] QUICK_REFERENCE.md reflects any new features
- [ ] DEVELOPMENT.md updated if process changed
- [ ] Inline code comments clear and up-to-date
- [ ] Release notes outline prepared

### Testing
- [ ] Unit tests pass locally
- [ ] Manual testing completed on Windows 7+ machines
- [ ] WebM video support tested (if video features added)
- [ ] Slideshow mode tested with images and videos
- [ ] Theme switching tested (Light/Dark/System)
- [ ] Logging and cache features verified

---

## Release Day

### Version Management (1-2 hours before tag)

- [ ] Decide version number following [Semantic Versioning](https://semver.org/)
  - [ ] For bug fix: PATCH version (1.0.0 → 1.0.1)
  - [ ] For new feature: MINOR version (1.0.0 → 1.1.0)
  - [ ] For breaking change: MAJOR version (1.0.0 → 2.0.0)

- [ ] Update [DaisyView/DaisyView.csproj](DaisyView/DaisyView.csproj)
  ```xml
  <Version>X.Y.Z</Version>
  ```

- [ ] Verify version update
  ```bash
  grep -A 1 "Version>" DaisyView/DaisyView.csproj
  ```

### Build & Test

- [ ] Clean build directory
  ```bash
  dotnet clean DaisyView.sln
  ```

- [ ] Build Debug configuration
  ```bash
  dotnet build DaisyView.sln --configuration Debug
  ```

- [ ] Build Release configuration
  ```bash
  dotnet build DaisyView.sln --configuration Release
  ```

- [ ] Run all tests
  ```bash
  dotnet test DaisyView.sln
  ```

- [ ] Verify no warnings or errors
  ```
  Build succeeded.
      0 Warning(s)
      0 Error(s)
  ```

### Git Operations

- [ ] Verify working directory is clean
  ```bash
  git status
  # Should show "nothing to commit, working tree clean"
  ```

- [ ] Commit version change
  ```bash
  git add DaisyView/DaisyView.csproj
  git commit -m "chore: bump version to X.Y.Z"
  git push origin main
  ```

- [ ] Verify commit pushed
  ```bash
  git log --oneline -1 origin/main
  ```

- [ ] Create annotated git tag
  ```bash
  git tag -a vX.Y.Z -m "Release version X.Y.Z"
  ```

- [ ] Verify tag created
  ```bash
  git tag -v vX.Y.Z
  ```

- [ ] Push tag to GitHub
  ```bash
  git push origin vX.Y.Z
  ```

- [ ] Verify tag pushed
  ```bash
  git ls-remote --tags origin | grep vX.Y.Z
  ```

### Build Release Artifacts (Optional)

If distributing executable:

- [ ] Publish Release build
  ```bash
  dotnet publish -c Release -o ./publish
  ```

- [ ] Verify executable exists
  ```bash
  ls ./publish/DaisyView.exe
  ```

- [ ] Test published executable
  - [ ] Launch executable
  - [ ] Verify version displays correctly
  - [ ] Test basic functionality
  - [ ] Test slideshow mode
  - [ ] Test video support (if applicable)

### Create GitHub Release

- [ ] Visit https://github.com/kfaubel/DaisyView/releases

- [ ] Click "Create a new release" or "Draft a new release"

- [ ] Select Tag
  - [ ] Type or select: `vX.Y.Z`

- [ ] Release Title
  - [ ] Enter: `DaisyView X.Y.Z`
  - [ ] Add subtitle if applicable: `- Bug Fix Release` or `- Major Feature Release`

- [ ] Release Notes (use provided template)
  - [ ] ✨ New Features (if any)
  - [ ] 🐛 Bug Fixes (if any)
  - [ ] 🔧 Improvements (if any)
  - [ ] 📚 Documentation updates (if any)
  - [ ] ⚠️ Breaking Changes (if any)
  - [ ] 💻 System Requirements
  - [ ] 📥 Installation Instructions
  - [ ] 🙏 Contributors (if applicable)

- [ ] Release Type
  - [ ] Leave unchecked for stable releases
  - [ ] Check "Pre-release" for beta/rc versions

- [ ] Attach Files (optional)
  - [ ] Drag/upload `DaisyView-X.Y.Z.exe` (if built)
  - [ ] Drag/upload `DaisyView-X.Y.Z.zip` (if portable)
  - [ ] Verify files uploaded successfully

- [ ] Publish Release
  - [ ] Click "Publish release" (not "Save as draft")

### Verification

- [ ] Visit https://github.com/kfaubel/DaisyView/releases
  - [ ] New release appears at top of list
  - [ ] Release title is correct
  - [ ] Release notes display properly (check Markdown rendering)
  - [ ] Files attached and downloadable (if applicable)
  - [ ] Version tag correct

- [ ] Test release page
  - [ ] Click tag link (should link to commit)
  - [ ] Click version link (if any)
  - [ ] Try downloading file (if applicable)

---

## Post-Release (After Publishing)

### Communication (Optional)

- [ ] Update project website (if applicable)
- [ ] Post on social media (if applicable)
- [ ] Announce in forums/communities (if applicable)
- [ ] Update any external download pages (if applicable)

### Monitoring

- [ ] Monitor GitHub Issues for bug reports
- [ ] Monitor download stats (if applicable)
- [ ] Collect user feedback
- [ ] Track any critical issues reported

### Next Steps

- [ ] Start planning next release
- [ ] Create GitHub Issues for upcoming features
- [ ] Schedule next release date (if regular cadence)

---

## Rollback (If Release Has Critical Issues)

If a released version has critical bugs:

### Option 1: Pull the Release

- [ ] Go to https://github.com/kfaubel/DaisyView/releases
- [ ] Click the release to edit
- [ ] Click pencil icon to edit
- [ ] Check "Pre-release" checkbox
- [ ] Update title to indicate pre-release status
- [ ] Save changes
- [ ] Immediately release patched version (vX.Y.1)

### Option 2: Delete the Release

Only if no one has downloaded:

- [ ] Edit the release
- [ ] Click "Delete this release"
- [ ] Delete the git tag: `git push origin --delete vX.Y.Z`
- [ ] Fix the issue locally
- [ ] Rebuild and recreate release

---

## Checklists by Release Type

### Patch Release (1.0.0 → 1.0.1)
- [ ] Bug fixes only, no new features
- [ ] Update PATCH version only
- [ ] Note any security fixes
- [ ] Test regression - existing features still work

### Minor Release (1.0.0 → 1.1.0)
- [ ] New features added
- [ ] Backward compatible
- [ ] Update MINOR version, reset PATCH to 0
- [ ] Document new features in release notes
- [ ] Update README with new features
- [ ] Ensure backward compatibility tested

### Major Release (1.0.0 → 2.0.0)
- [ ] Breaking changes introduced
- [ ] Update MAJOR version, reset MINOR and PATCH to 0
- [ ] Document all breaking changes prominently
- [ ] Provide migration guide if applicable
- [ ] Update installation instructions
- [ ] Consider deprecation warnings in previous version

### Beta/RC Release (1.0.0-beta.1)
- [ ] Clearly mark as pre-release
- [ ] Note what's being tested
- [ ] Encourage feedback
- [ ] Set timeline for release
- [ ] Plan feedback collection

---

## Common Version Bumps

**Current Version: 1.0.0** (from DaisyView.csproj)

| Scenario | New Version | Notes |
|----------|-------------|-------|
| Bug fix in WebM handling | 1.0.1 | Patch |
| Add support for HEIC images | 1.1.0 | Minor feature |
| Add plugin API (breaking) | 2.0.0 | Major change |
| Beta for next version | 1.1.0-beta.1 | Pre-release |
| Release candidate | 1.1.0-rc.1 | Pre-release |

---

## Quick Reference Commands

```bash
# Check git status before release
git status

# Update version and commit
git add DaisyView/DaisyView.csproj
git commit -m "chore: bump version to X.Y.Z"
git push origin main

# Create and push tag
git tag -a vX.Y.Z -m "Release version X.Y.Z"
git push origin vX.Y.Z

# Build Release configuration
dotnet build DaisyView.sln --configuration Release
dotnet test DaisyView.sln

# Publish executable
dotnet publish -c Release -o ./publish
```

---

## Helpful Links

- [GitHub Releases Documentation](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)
- [Semantic Versioning](https://semver.org/)
- [Keep a Changelog](https://keepachangelog.com/)
- [DaisyView GITHUB_RELEASES_GUIDE.md](GITHUB_RELEASES_GUIDE.md) - Detailed release guide

---

## Notes for Team

- **Release cadence**: [Define your schedule - e.g., monthly, as-needed]
- **Release manager**: [Define who handles releases]
- **Approval required**: [Define if multiple approvals needed]
- **Communication template**: [Define how to announce releases]

---

*Last Updated: January 18, 2026*
*Current Version: 1.0.0*
