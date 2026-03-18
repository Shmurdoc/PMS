# Redeploy script for SAFARIstack (Windows PowerShell)
# Rebuilds frontend, portal, pwa and api without cache and recreates containers
# Usage: Open PowerShell as administrator and run: .\redeploy.ps1

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "[redeploy] Starting full rebuild and redeploy..." -ForegroundColor Cyan
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Definition)

# Ensure docker compose project dir
$composeDir = Get-Location
Write-Host "[redeploy] Working directory: $composeDir"

# Build images without cache (frontend and dependent projects)
$servicesToBuild = @('frontend','portal','guest-pwa','api')
foreach ($s in $servicesToBuild) {
    Write-Host "[redeploy] Building service: $s (no-cache)..." -ForegroundColor Yellow
    docker compose build --no-cache $s
}

# Recreate containers with updated images
Write-Host "[redeploy] Recreating containers..." -ForegroundColor Yellow
docker compose up -d --force-recreate --remove-orphans

# Wait briefly and display status
Start-Sleep -Seconds 6
Write-Host "[redeploy] Current containers:" -ForegroundColor Green
docker compose ps --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'

Pop-Location
Write-Host "[redeploy] Redeploy finished." -ForegroundColor Cyan
