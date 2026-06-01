#!/usr/bin/env bash
set -euo pipefail

# ── Colors ──────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

# ── Helper: discover PID listening on a TCP port ───────
# Uses fuser, lsof, or ss (tried in that order).
# Returns empty string if port is free or no tools are available.
discover_pid_by_port() {
    local port="$1"
    local pid=""
    if command -v fuser &>/dev/null; then
        pid=$(fuser "$port/tcp" 2>/dev/null | grep -oP '\d+' | head -1)
    elif command -v lsof &>/dev/null; then
        pid=$(lsof -ti :"$port" 2>/dev/null | head -1)
    elif command -v ss &>/dev/null; then
        pid=$(ss -tlnp "sport = :$port" 2>/dev/null | grep -oP 'pid=\K\d+' | head -1)
    fi
    echo "$pid"
}

MODE="${1:-debug}"

# ── Validate mode ───────────────────────────────────────
if [[ "$MODE" != "debug" && "$MODE" != "prod" ]]; then
    echo -e "${RED}ERROR: Invalid mode '${MODE}'. Use 'debug' or 'prod'.${NC}"
    echo "Usage: $0 [debug|prod]"
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$SCRIPT_DIR/backend"

# ── PID / log file management ───────────────────────────
PID_FILE="$BACKEND_DIR/.api-pid"
LOG_FILE="$BACKEND_DIR/.api-${MODE}.log"

# ── Check if already running via PID file ───────────────
if [ -f "$PID_FILE" ]; then
    OLD_PID=$(cat "$PID_FILE")
    if [ -n "$OLD_PID" ] && kill -0 "$OLD_PID" 2>/dev/null; then
        echo -e "${YELLOW}API is already running (PID $OLD_PID). Use stop-api.sh to stop it first.${NC}"
        exit 1
    fi
    rm -f "$PID_FILE"
fi

# ── Check for orphaned processes on our ports ───────────
for port in 6701 6703; do
    PORT_PID=$(discover_pid_by_port "$port")
    if [ -n "$PORT_PID" ]; then
        if ps -p "$PORT_PID" -o comm= 2>/dev/null | grep -qE "JiApp\.Api|dotnet"; then
            echo -e "${YELLOW}Found orphaned process (PID $PORT_PID) on port $port. Killing...${NC}"
            kill "$PORT_PID" 2>/dev/null || true
            sleep 1
            if kill -0 "$PORT_PID" 2>/dev/null; then
                kill -9 "$PORT_PID" 2>/dev/null || true
                sleep 1
            fi
            if kill -0 "$PORT_PID" 2>/dev/null; then
                echo -e "${RED}Failed to kill PID $PORT_PID on port $port. Cannot start.${NC}"
                exit 1
            fi
            echo -e "${GREEN}✓${NC} Killed orphaned process on port $port"
        else
            echo -e "${RED}Port $port is in use by PID $PORT_PID ($(ps -p "$PORT_PID" -o comm= 2>/dev/null)). Cannot start.${NC}"
            exit 1
        fi
    fi
done

# ── Banner ──────────────────────────────────────────────
ENV_LABEL=$([ "$MODE" = "debug" ] && echo "Development" || echo "Production")
MODE_COLOR=$([ "$MODE" = "debug" ] && echo "$GREEN" || echo "$YELLOW")

echo -e "${CYAN}${BOLD}╭──────────────────────────────────────────╮${NC}"
echo -e "${CYAN}${BOLD}│${NC}     ${BOLD}JiApp API — ${MODE_COLOR}${ENV_LABEL} Mode${NC}         ${CYAN}${BOLD}│${NC}"
echo -e "${CYAN}${BOLD}╰──────────────────────────────────────────╯${NC}"
echo ""

# ── Prerequisites ───────────────────────────────────────
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}ERROR: dotnet CLI not found. Install .NET SDK 10.${NC}"
    exit 1
fi
echo -e "${GREEN}✓${NC} dotnet $(dotnet --version)"

# ── Build dotnet args & env ─────────────────────────────
DOTNET_ARGS=(
    --project "$BACKEND_DIR/src/JiApp.Api/JiApp.Api.csproj"
    --
)

if [ "$MODE" = "prod" ]; then
    # ── Load .env for production ─────────────────────────
    if [ -f "$BACKEND_DIR/.env" ]; then
        echo -e "${GREEN}✓${NC} Loading backend/.env"
        set -a
        source "$BACKEND_DIR/.env"
        set +a
    fi

    # ── Validate required vars ───────────────────────────
    MISSING=()

    if [ -z "${JWT_KEY:-}" ]; then
        MISSING+=("JWT_KEY — signing key for JWT tokens")
    fi
    if [ -z "${JWT_AUDIENCE:-}" ]; then
        MISSING+=("JWT_AUDIENCE — expected audience claim")
    fi
    if [ -z "${YOUTUBE_API_KEY:-}" ]; then
        MISSING+=("YOUTUBE_API_KEY — YouTube Data API v3 key")
    fi

    if [ ${#MISSING[@]} -gt 0 ]; then
        echo -e "\n${RED}${BOLD}Missing required variables:${NC}\n"
        for msg in "${MISSING[@]}"; do
            echo -e "  ${RED}✗${NC} $msg"
        done
        echo -e "\n${YELLOW}Set them in backend/.env or export them before running.${NC}"
        exit 1
    fi

    echo -e "${GREEN}✓${NC} JWT_KEY"
    echo -e "${GREEN}✓${NC} JWT_AUDIENCE"
    echo -e "${GREEN}✓${NC} YOUTUBE_API_KEY"

    DOTNET_ARGS+=(
        --Jwt:Key "$JWT_KEY"
        --Jwt:Audience "$JWT_AUDIENCE"
        --Youtube:api-key "$YOUTUBE_API_KEY"
    )

    # ── Optional: CORS / DB overrides ────────────────────
    if [ -n "${CORS_ALLOWED_ORIGIN:-}" ]; then
        DOTNET_ARGS+=(--Cors:AllowedOrigins:0 "$CORS_ALLOWED_ORIGIN")
    fi
    if [ -n "${DB_PATH:-}" ]; then
        DOTNET_ARGS+=(--ConnectionStrings:JiDb "Data Source=$DB_PATH")
    fi

    # ── HTTPS certificate (production) ───────────────────
    HAS_CERT=false
    if [ -n "${CERT_PATH:-}" ] && [ -n "${CERT_PASSWORD:-}" ]; then
        if [ -f "$CERT_PATH" ]; then
            HAS_CERT=true
            echo -e "${GREEN}✓${NC} HTTPS cert: ${CERT_PATH}"
        else
            echo -e "${RED}ERROR: CERT_PATH is set but file not found: ${CERT_PATH}${NC}"
            exit 1
        fi
    fi

    export ASPNETCORE_ENVIRONMENT=Production

    if $HAS_CERT; then
        export ASPNETCORE_URLS="http://*:6701;https://*:6703"
        DOTNET_ARGS+=(
            --Kestrel:Endpoints:Https:Certificate:Path "$CERT_PATH"
            --Kestrel:Endpoints:Https:Certificate:Password "$CERT_PASSWORD"
        )
    else
        export ASPNETCORE_URLS="http://*:6701"
    fi

else
    # ── Debug/Development mode ───────────────────────────
    DEV_CERT="$BACKEND_DIR/src/JiApp.Api/Infrastructure/dev-cert.pfx"

    export ASPNETCORE_ENVIRONMENT=Development
    export ASPNETCORE_URLS="http://*:6701;https://*:6703"

    if [ -f "$DEV_CERT" ]; then
        echo -e "${GREEN}✓${NC} Dev certificate: Infrastructure/dev-cert.pfx"
        DOTNET_ARGS+=(
            --Kestrel:Endpoints:Https:Certificate:Path "$DEV_CERT"
            --Kestrel:Endpoints:Https:Certificate:Password "JiAppDev2026!"
        )
    else
        echo -e "${YELLOW}⚠${NC}  Dev certificate not found, running HTTP-only"
        export ASPNETCORE_URLS="http://*:6701"
    fi
fi

# ── Pre-launch summary ──────────────────────────────────
echo ""
echo -e "${CYAN}${BOLD}Starting JiApp API in background...${NC}"
echo -e "  Environment: ${MODE_COLOR}${ENV_LABEL}${NC}"
echo -e "  URLs:        ${GREEN}${ASPNETCORE_URLS}${NC}"
echo -e "  Log file:    ${CYAN}${LOG_FILE}${NC}"
echo ""

# ── Launch ──────────────────────────────────────────────
cd "$BACKEND_DIR"
nohup dotnet run "${DOTNET_ARGS[@]}" > "$LOG_FILE" 2>&1 &
LAUNCHER_PID=$!
echo "$LAUNCHER_PID" > "$PID_FILE"

# ── Wait for the app to bind a port and discover real PID ──
REAL_PID=""
for ((i=1; i<=15; i++)); do
    for port in 6701 6703; do
        REAL_PID=$(discover_pid_by_port "$port")
        if [ -n "$REAL_PID" ]; then
            break 2
        fi
    done
    sleep 1
done

if [ -n "$REAL_PID" ]; then
    echo "$REAL_PID" > "$PID_FILE"
    echo -e "${GREEN}${BOLD}✓ API started${NC} (PID ${BOLD}$REAL_PID${NC})"
    echo -e "  Stop it with: ${BOLD}./stop-api.sh${NC}"
    echo -e "  Tail logs:    ${BOLD}tail -f ${LOG_FILE}${NC}"
else
    if kill -0 "$LAUNCHER_PID" 2>/dev/null; then
        echo -e "${YELLOW}⚠  API may have started but port not detected (launcher PID $LAUNCHER_PID)${NC}"
        echo -e "  Check the log: ${BOLD}tail -f ${LOG_FILE}${NC}"
    else
        echo -e "${RED}${BOLD}✗ API failed to start${NC}"
        echo -e "  Check the log: ${BOLD}cat ${LOG_FILE}${NC}"
        rm -f "$PID_FILE"
        exit 1
    fi
fi
