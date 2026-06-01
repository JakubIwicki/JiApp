#!/usr/bin/env bash
set -euo pipefail

# =============================================================================
# JiApp Production Deployment Script
# =============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILES="-f ${SCRIPT_DIR}/docker-compose.yml -f ${SCRIPT_DIR}/docker-compose.prod.yml"

# ── Colors ──────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

# ── Banner ──────────────────────────────────────────────────────────────────
banner() {
    echo -e "${CYAN}${BOLD}╭──────────────────────────────────────────╮${NC}"
    echo -e "${CYAN}${BOLD}│${NC}     ${BOLD}JiApp API — Production Deploy${NC}       ${CYAN}${BOLD}│${NC}"
    echo -e "${CYAN}${BOLD}╰──────────────────────────────────────────╯${NC}"
    echo ""
}

# ── Usage ───────────────────────────────────────────────────────────────────
usage() {
    echo "Usage: $0 [COMMAND]"
    echo ""
    echo "Commands:"
    echo "  up        Build and start all services in production mode"
    echo "  down      Stop and remove all containers (pgdata volume preserved)"
    echo "  restart   Stop, rebuild, and start (full redeploy)"
    echo "  status    Show container status"
    echo "  logs      Tail logs from all services (pass service name to filter)"
    echo "  build     Build images without starting"
    echo "  help      Show this help"
    echo ""
    echo "Examples:"
    echo "  $0 up              # Deploy production"
    echo "  $0 logs gateway    # Tail gateway logs only"
    echo "  $0 status          # Check if everything is running"
    exit 1
}

# ── Environment Check ───────────────────────────────────────────────────────
check_env() {
    if [ ! -f "${SCRIPT_DIR}/.env" ]; then
        echo -e "${RED}ERROR: .env file not found in ${SCRIPT_DIR}${NC}"
        echo ""
        echo "To get started:"
        echo "  cp .env.example .env"
        echo "  nano .env   # fill in your production values"
        exit 1
    fi
}

# ── Config Validation ───────────────────────────────────────────────────────
validate_config() {
    echo -e "${YELLOW}Validating compose configuration...${NC}"
    docker compose $COMPOSE_FILES config --quiet 2>&1 || {
        echo ""
        echo -e "${RED}Compose configuration validation failed.${NC}"
        echo "Check the errors above and fix your .env file."
        exit 1
    }
    echo -e "${GREEN}Configuration valid.${NC}"
}

# ── Commands ────────────────────────────────────────────────────────────────

cmd_up() {
    check_env
    validate_config
    banner
    echo -e "${CYAN}Building images and starting services...${NC}"
    echo ""
    docker compose $COMPOSE_FILES up -d --build
    echo ""
    echo -e "${GREEN}All services started.${NC}"
    echo ""
    echo "  Gateway:   http://localhost:6700"
    echo "  Health:    http://localhost:6700/health"
    echo ""
    echo "  Status:    ./deploy.sh status"
    echo "  Logs:      ./deploy.sh logs"
    echo "  Stop:      ./deploy.sh down"
}

cmd_down() {
    banner
    echo -e "${YELLOW}Stopping and removing containers...${NC}"
    docker compose $COMPOSE_FILES down
    echo ""
    echo -e "${GREEN}Containers stopped.${NC}"
    echo "  PostgreSQL data in 'pgdata' volume is preserved."
    echo "  To remove all data: docker compose $COMPOSE_FILES down -v"
}

cmd_restart() {
    check_env
    banner
    echo -e "${YELLOW}Stopping existing containers...${NC}"
    docker compose $COMPOSE_FILES down
    echo ""
    validate_config
    echo ""
    echo -e "${CYAN}Rebuilding images and starting services...${NC}"
    echo ""
    docker compose $COMPOSE_FILES up -d --build
    echo ""
    echo -e "${GREEN}Services restarted.${NC}"
}

cmd_status() {
    echo -e "${CYAN}${BOLD}Production Container Status:${NC}"
    echo ""
    docker compose $COMPOSE_FILES ps
    echo ""
    echo -e "${CYAN}PostgreSQL Volume:${NC}"
    docker volume ls --filter name=pgdata 2>/dev/null || true
}

cmd_logs() {
    shift
    docker compose $COMPOSE_FILES logs --tail=50 -f "$@"
}

cmd_build() {
    check_env
    banner
    echo -e "${CYAN}Building images (no start)...${NC}"
    echo ""
    docker compose $COMPOSE_FILES build
    echo ""
    echo -e "${GREEN}Build complete.${NC}"
}

# ── Dispatch ────────────────────────────────────────────────────────────────
cd "$SCRIPT_DIR"

case "${1:-help}" in
    up)       cmd_up ;;
    down)     cmd_down ;;
    restart)  cmd_restart ;;
    status)   cmd_status ;;
    logs)     cmd_logs "$@" ;;
    build)    cmd_build ;;
    help|*)   usage ;;
esac
