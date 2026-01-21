# deploy.ps1
# Script to deploy Antigravity API to Render
# Since Render uses Continuous Deployment, this script commits changes and pushes to Git.

Write-Host "🚀 Preparing API Deployment..." -ForegroundColor Cyan

# Navigate to the script's directory
Set-Location $PSScriptRoot

# Check for changes
$status = git status --porcelain
if ([string]::IsNullOrWhiteSpace($status)) {
    Write-Host "✨ No changes to deploy." -ForegroundColor Yellow
    exit
}

Write-Host "📦 Staging changes..." -ForegroundColor Yellow
git add .

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
$message = "Deploy API - $timestamp"

Write-Host "💾 Committing: $message" -ForegroundColor Yellow
git commit -m "$message"

Write-Host "⬆️  Pushing to repository (Triggers Render)..." -ForegroundColor Cyan
git push

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Changes pushed! Render should start building automatically." -ForegroundColor Green
    Write-Host "⏳ Wait a few minutes for the API to update." -ForegroundColor Gray
}
else {
    Write-Host "❌ Push failed." -ForegroundColor Red
}
