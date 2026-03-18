# ═══════════════════════════════════════════════════════════════════
# SAFARIstack PMS - Full Redeploy Script
# Rebuilds all images and restarts containers with latest code
# ═══════════════════════════════════════════════════════════════════

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SAFARIstack PMS - Full System Redeploy" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
Write-Host "[$timestamp] Starting deployment..." -ForegroundColor Yellow

try {
    # Step 1: Stop and remove all containers
    Write-Host ""
    Write-Host "Step 1/4: Stopping containers..." -ForegroundColor Green
    docker compose down --remove-orphans
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to stop containers" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Containers stopped" -ForegroundColor Green

    # Step 2: Rebuild frontend image
    Write-Host ""
    Write-Host "Step 2/4: Rebuilding frontend image..." -ForegroundColor Green
    docker compose build frontend --no-cache
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to build frontend" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Frontend image rebuilt" -ForegroundColor Green

    # Step 3: Rebuild API image
    Write-Host ""
    Write-Host "Step 3/4: Rebuilding API image..." -ForegroundColor Green
    docker compose build api --no-cache
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to build API" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ API image rebuilt" -ForegroundColor Green

    # Step 4: Start all containers
    Write-Host ""
    Write-Host "Step 4/4: Starting all containers..." -ForegroundColor Green
    docker compose up -d
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to start containers" -ForegroundColor Red
        exit 1
    }
    Write-Host "✓ Containers started" -ForegroundColor Green

    # Wait for containers to become healthy
    Write-Host ""
    Write-Host "Waiting for containers to become healthy..." -ForegroundColor Yellow
    Start-Sleep -Seconds 15

    # Show final status
    Write-Host ""
    Write-Host "Container Status:" -ForegroundColor Cyan
    docker compose ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "DEPLOYMENT COMPLETE" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Application URLs:" -ForegroundColor Cyan
    Write-Host "  - Frontend:  http://localhost:3000" -ForegroundColor White
    Write-Host "  - API:       http://localhost:8080" -ForegroundColor White
    Write-Host "  - PWA:       http://localhost:3002" -ForegroundColor White
    Write-Host "  - Portal:    http://localhost:3001" -ForegroundColor White
    Write-Host "  - Database:  localhost:5432" -ForegroundColor White
    Write-Host ""

}
catch {
    Write-Host ""
    Write-Host "ERROR: $_" -ForegroundColor Red
    exit 1
}
