@echo off
REM Quick release script - bumps version level
REM Usage: release.cmd [major|minor|patch]  (default: patch)

SET BUMP=%1
IF "%BUMP%"=="" SET BUMP=patch

echo Running automated release (%BUMP%)...
powershell -ExecutionPolicy Bypass -File release.ps1 %BUMP%