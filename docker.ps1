# ═══════════════════════════════════════════════════════════════════════
#  SAFARIstack PMS — Docker Management Script (Windows PowerShell)
# ═══════════════════════════════════════════════════════════════════════

param(
    [string]$Command = "help",
    [string]$Service = ""
)

$ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Definition
$ComposeFile = Join-Path $ScriptPath "docker-compose.yml"
$EnvFile = Join-Path $ScriptPath ".env"

# Colors
function Write-Header {
    Write-Host "═════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  SAFARIstack PMS — Docker Management" -ForegroundColor Cyan
    Write-Host "═════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Write-Success {
    Write-Host "✓ $args" -ForegroundColor Green
}

function Write-Error-Custom {
    Write-Host "✗ $args" -ForegroundColor Red
}

function Write-Info {
    Write-Host "ℹ $args" -ForegroundColor Yellow
}

function Check-Env {
    if (-not (Test-Path $EnvFile)) {
        Write-Error-Custom ".env file not found!"
        Write-Host "Create it from .env.example:"
        Write-Host "  copy .env.example .env"
        Write-Host "  notepad .env"
        exit 1
    }
}

function Cmd-Up {
    Write-Header
    Check-Env
    Write-Info "Starting all services..."
    docker compose -f $ComposeFile up -d
    Write-Success "All services started"
    Write-Host ""
    Write-Info "Services running:"
    Write-Host "  Frontend:   http://localhost:3000"
    Write-Host "  Portal:     http://localhost:3001"
    Write-Host "  Guest PWA:  http://localhost:3002"
    Write-Host "  API:        http://localhost:8080"
    Write-Host "  Database:   localhost:5432"
}

function Cmd-Down {
    Write-Header
    Write-Info "Stopping all services..."
    docker compose -f $ComposeFile down
    Write-Success "All services stopped"
}

function Cmd-Logs {
    if ([string]::IsNullOrEmpty($Service)) {
        docker compose -f $ComposeFile logs -f --tail 50
    }
    else {
        docker compose -f $ComposeFile logs -f --tail 50 $Service
    }
}

function Cmd-Status {
    Write-Header
    Write-Host ""
    docker compose -f $ComposeFile ps
    Write-Host ""
    Write-Info "Health checks:"
    $output = docker compose -f $ComposeFile ps --format "{{.Names}} {{.Status}}"
    foreach ($line in $output) {
        if ($line -match "healthy") {
            Write-Host "  ✓ $line" -ForegroundColor Green
        }
        elseif ($line -match "unhealthy") {
            Write-Host "  ✗ $line" -ForegroundColor Red
        }
    }
}

function Cmd-Restart {
    if ([string]::IsNullOrEmpty($Service)) {
        Write-Info "Restarting all services..."
        docker compose -f $ComposeFile restart
    }
    else {
        Write-Info "Restarting $Service..."
        docker compose -f $ComposeFile restart $Service
    }
    Write-Success "Service(s) restarted"
}

function Cmd-Rebuild {
    Write-Header
    if ([string]::IsNullOrEmpty($Service)) {
        Write-Info "Rebuilding all services..."
        docker compose -f $ComposeFile build --no-cache
    }
    else {
        Write-Info "Rebuilding $Service..."
        docker compose -f $ComposeFile build --no-cache $Service
    }
    Write-Success "Build complete"
}

function Cmd-Clean {
    Write-Header
    Write-Host "⚠ This will remove containers and volumes (data loss!)" -ForegroundColor Yellow
    $confirm = Read-Host "Continue? (yes/no)"
    if ($confirm -eq "yes") {
        Write-Info "Cleaning up..."
        docker compose -f $ComposeFile down -v
        Write-Success "Cleanup complete"
    }
    else {
        Write-Info "Cancelled"
    }
}

function Cmd-Backup {
    Write-Header
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFile = "backup-$timestamp.sql"
    Write-Info "Backing up database to $backupFile..."
    docker exec safaristack-db pg_dump -U safaristack safaristack | Out-File $backupFile -Encoding ASCII
    Write-Success "Backup complete: $backupFile"
}

function Cmd-Shell {
    $service = if ([string]::IsNullOrEmpty($Service)) { "api" } else { $Service }
    Write-Info "Opening shell in $service..."
    docker compose -f $ComposeFile exec $service sh
}

function Cmd-Help {
    Write-Header
    Write-Host ""
    Write-Host "Usage: .\docker.ps1 -Command <command> -Service [service]"
    Write-Host ""
    Write-Host "Commands:"
    Write-Host "  up              Start all services"
    Write-Host "  down            Stop all services"
    Write-Host "  status          Show service status"
    Write-Host "  logs            View logs (optionally specify service)"
    Write-Host "  restart         Restart service(s)"
    Write-Host "  rebuild         Rebuild service(s)"
    Write-Host "  clean           Stop and remove all (⚠️ data loss)"
    Write-Host "  backup          Backup database"
    Write-Host "  shell           Open shell in service"
    Write-Host "  help            Show this help"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\docker.ps1 -Command up"
    Write-Host "  .\docker.ps1 -Command logs -Service safaristack-api"
    Write-Host "  .\docker.ps1 -Command restart -Service safaristack-frontend"
    Write-Host "  .\docker.ps1 -Command rebuild"
}

# Main
switch ($Command.ToLower()) {
    "up"      { Cmd-Up }
    "down"    { Cmd-Down }
    "status"  { Cmd-Status }
    "logs"    { Cmd-Logs }
    "restart" { Cmd-Restart }
    "rebuild" { Cmd-Rebuild }
    "clean"   { Cmd-Clean }
    "backup"  { Cmd-Backup }
    "shell"   { Cmd-Shell }
    "help"    { Cmd-Help }
    default   { Write-Error-Custom "Unknown command: $Command"; Cmd-Help; exit 1 }
}
