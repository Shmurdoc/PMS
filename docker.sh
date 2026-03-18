#!/bin/bash
# ═══════════════════════════════════════════════════════════════════════
#  SAFARIstack PMS — Docker Management Script (Linux/Mac)
# ═══════════════════════════════════════════════════════════════════════

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="${SCRIPT_DIR}/docker-compose.yml"
ENV_FILE="${SCRIPT_DIR}/.env"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Functions
print_header() {
    echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}  SAFARIstack PMS — Docker Management${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}ℹ $1${NC}"
}

check_env() {
    if [ ! -f "$ENV_FILE" ]; then
        print_error ".env file not found!"
        echo "Create it from .env.example:"
        echo "  cp .env.example .env"
        echo "  nano .env"
        exit 1
    fi
}

# Commands
cmd_up() {
    print_header
    check_env
    print_info "Starting all services..."
    docker compose -f "$COMPOSE_FILE" up -d
    print_success "All services started"
    echo
    print_info "Services running:"
    echo "  Frontend:   http://localhost:3000"
    echo "  Portal:     http://localhost:3001"
    echo "  Guest PWA:  http://localhost:3002"
    echo "  API:        http://localhost:8080"
    echo "  Database:   localhost:5432"
}

cmd_down() {
    print_header
    print_info "Stopping all services..."
    docker compose -f "$COMPOSE_FILE" down
    print_success "All services stopped"
}

cmd_logs() {
    service="${1:-}"
    if [ -z "$service" ]; then
        docker compose -f "$COMPOSE_FILE" logs -f --tail 50
    else
        docker compose -f "$COMPOSE_FILE" logs -f --tail 50 "$service"
    fi
}

cmd_status() {
    print_header
    echo
    docker compose -f "$COMPOSE_FILE" ps
    echo
    print_info "Health checks:"
    docker compose -f "$COMPOSE_FILE" ps --status=running | while read -r line; do
        if echo "$line" | grep -q "healthy"; then
            echo -e "  ${GREEN}✓ $(echo "$line" | awk '{print $1}')${NC}"
        elif echo "$line" | grep -q "unhealthy"; then
            echo -e "  ${RED}✗ $(echo "$line" | awk '{print $1}')${NC}"
        fi
    done
}

cmd_restart() {
    service="${1:-}"
    if [ -z "$service" ]; then
        print_info "Restarting all services..."
        docker compose -f "$COMPOSE_FILE" restart
    else
        print_info "Restarting $service..."
        docker compose -f "$COMPOSE_FILE" restart "$service"
    fi
    print_success "Service(s) restarted"
}

cmd_rebuild() {
    print_header
    service="${1:-}"
    if [ -z "$service" ]; then
        print_info "Rebuilding all services..."
        docker compose -f "$COMPOSE_FILE" build --no-cache
    else
        print_info "Rebuilding $service..."
        docker compose -f "$COMPOSE_FILE" build --no-cache "$service"
    fi
    print_success "Build complete"
}

cmd_clean() {
    print_header
    echo -e "${YELLOW}⚠ This will remove containers and volumes (data loss!)${NC}"
    read -p "Continue? (yes/no): " confirm
    if [ "$confirm" = "yes" ]; then
        print_info "Cleaning up..."
        docker compose -f "$COMPOSE_FILE" down -v
        print_success "Cleanup complete"
    else
        print_info "Cancelled"
    fi
}

cmd_backup() {
    print_header
    backup_file="backup-$(date +%Y%m%d_%H%M%S).sql"
    print_info "Backing up database to $backup_file..."
    docker exec safaristack-db pg_dump -U safaristack safaristack > "$backup_file"
    print_success "Backup complete: $backup_file"
}

cmd_shell() {
    service="${1:-api}"
    print_info "Opening shell in $service..."
    docker compose -f "$COMPOSE_FILE" exec "$service" sh
}

cmd_help() {
    print_header
    echo
    echo "Usage: ./docker.sh <command> [options]"
    echo
    echo "Commands:"
    echo "  up              Start all services"
    echo "  down            Stop all services"
    echo "  status          Show service status   "
    echo "  logs [service]  View logs (default: all)"
    echo "  restart [srv]   Restart service(s)"
    echo "  rebuild [srv]   Rebuild service(s)"
    echo "  clean           Stop and remove all (⚠️ data loss)"
    echo "  backup          Backup database"
    echo "  shell [srv]     Open shell in service"
    echo "  help            Show this help"
    echo
    echo "Examples:"
    echo "  ./docker.sh up"
    echo "  ./docker.sh logs safaristack-api"
    echo "  ./docker.sh restart safaristack-frontend"
    echo "  ./docker.sh rebuild"
}

# Main
main() {
    command="${1:-help}"
    
    case "$command" in
        up)       cmd_up ;;
        down)     cmd_down ;;
        status)   cmd_status ;;
        logs)     cmd_logs "${2:-}" ;;
        restart)  cmd_restart "${2:-}" ;;
        rebuild)  cmd_rebuild "${2:-}" ;;
        clean)    cmd_clean ;;
        backup)   cmd_backup ;;
        shell)    cmd_shell "${2:-api}" ;;
        help)     cmd_help ;;
        *)        print_error "Unknown command: $command"; cmd_help; exit 1 ;;
    esac
}

main "$@"
