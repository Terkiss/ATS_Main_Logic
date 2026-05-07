$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host " Starting Official Release Gate" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

Write-Host "1. Cleaning solution..."
dotnet clean
if ($LASTEXITCODE -ne 0) { throw "Clean failed" }

Write-Host "2. Building in Release mode..."
dotnet build -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed" }

Write-Host "3. Running tests in Release mode..."
dotnet test -c Release --no-build
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

Write-Host "======================================" -ForegroundColor Green
Write-Host " Release Gate Passed Successfully!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
