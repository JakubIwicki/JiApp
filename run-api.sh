#!/usr/bin/env bash
set -euo pipefail

# ── Colors ──────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

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

# ── Check if already running ────────────────────────────
if [ -f "$PID_FILE" ]; then
    OLD_PID=$(cat "$PID_FILE")
    if kill -0 "$OLD_PID" 2>/dev/null; then
        echo -e "${YELLOW}API is already running (PID $OLD_PID). Use stop-api.sh to stop it first.${NC}"
        exit 1
    else
        rm -f "$PID_FILE"
    fi
fi

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
        export ASPNETCORE_URLS="http://*:5001;https://*:5003"
        DOTNET_ARGS+=(
            --Kestrel:Endpoints:Https:Certificate:Path "$CERT_PATH"
            --Kestrel:Endpoints:Https:Certificate:Password "$CERT_PASSWORD"
        )
    else
        export ASPNETCORE_URLS="http://*:5001"
    fi

else
    # ── Debug/Development mode ───────────────────────────
    DEV_CERT="$BACKEND_DIR/src/JiApp.Api/Infrastructure/dev-cert.pfx"

    export ASPNETCORE_ENVIRONMENT=Development
    export ASPNETCORE_URLS="http://*:5001;https://*:5003"

    if [ -f "$DEV_CERT" ]; then
        echo -e "${GREEN}✓${NC} Dev certificate: Infrastructure/dev-cert.pfx"
        DOTNET_ARGS+=(
            --Kestrel:Endpoints:Https:Certificate:Path "$DEV_CERT"
            --Kestrel:Endpoints:Https:Certificate:Password "JiAppDev2026!"
        )
    else
        echo -e "${YELLOW}⚠${NC}  Dev certificate not found, running HTTP-only"
        export ASPNETCORE_URLS="http://*:5001"
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
API_PID=$!
echo "$API_PID" > "$PID_FILE"

# Give it a moment to start up
sleep 2

if kill -0 "$API_PID" 2>/dev/null; then
    echo -e "${GREEN}${BOLD}✓ API started${NC} (PID ${BOLD}$API_PID${NC})"
    echo -e "  Stop it with: ${BOLD}./stop-api.sh${NC}"
    echo -e "  Tail logs:    ${BOLD}tail -f ${LOG_FILE}${NC}"
else
    echo -e "${RED}${BOLD}✗ API failed to start${NC}"
    echo -e "  Check the log: ${BOLD}cat ${LOG_FILE}${NC}"
    rm -f "$PID_FILE"
    exit 1
fi
