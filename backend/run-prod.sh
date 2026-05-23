#!/usr/bin/env bash
set -euo pipefail

# ── Colors ──────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Banner ──────────────────────────────────────────────
echo -e "${CYAN}${BOLD}╭──────────────────────────────────────────╮${NC}"
echo -e "${CYAN}${BOLD}│${NC}     ${BOLD}JiApp API — Production Mode${NC}           ${CYAN}${BOLD}│${NC}"
echo -e "${CYAN}${BOLD}╰──────────────────────────────────────────╯${NC}"
echo ""

# ── Prerequisites ───────────────────────────────────────
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}ERROR: dotnet CLI not found. Install .NET SDK 10.${NC}"
    exit 1
fi
echo -e "${GREEN}✓${NC} dotnet $(dotnet --version)"

# ── Load .env ───────────────────────────────────────────
if [ -f "$SCRIPT_DIR/.env" ]; then
    echo -e "${GREEN}✓${NC} Loading backend/.env"
    set -a
    source "$SCRIPT_DIR/.env"
    set +a
fi

# ── Validate required vars ──────────────────────────────
MISSING=()

if [ -z "${JWT_KEY:-}" ]; then
    MISSING+=("JWT_KEY — signing key for JWT tokens (generate: openssl rand -base64 32)")
fi
if [ -z "${JWT_AUDIENCE:-}" ]; then
    MISSING+=("JWT_AUDIENCE — expected audience claim (e.g. https://your-domain.com)")
fi
if [ -z "${YOUTUBE_API_KEY:-}" ]; then
    MISSING+=("YOUTUBE_API_KEY — YouTube Data API v3 key from Google Cloud Console")
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

# ── Optional: HTTPS certificate ─────────────────────────
HAS_CERT=false
if [ -n "${CERT_PATH:-}" ] && [ -n "${CERT_PASSWORD:-}" ]; then
    if [ -f "$CERT_PATH" ]; then
        HAS_CERT=true
        echo -e "${GREEN}✓${NC} HTTPS cert: ${CERT_PATH}"
    else
        echo -e "${RED}ERROR: CERT_PATH is set but file not found: ${CERT_PATH}${NC}"
        exit 1
    fi
elif [ -n "${CERT_PATH:-}" ] || [ -n "${CERT_PASSWORD:-}" ]; then
    echo -e "${YELLOW}⚠${NC}  Both CERT_PATH and CERT_PASSWORD must be set for HTTPS — running HTTP-only"
fi

# ── Build dotnet args ───────────────────────────────────
DOTNET_ARGS=(
    --project "$SCRIPT_DIR/src/JiApp.Api/JiApp.Api.csproj"
    --
    --Jwt:Key "$JWT_KEY"
    --Jwt:Audience "$JWT_AUDIENCE"
    --Youtube:api-key "$YOUTUBE_API_KEY"
)

# Optional overrides from environment
if [ -n "${CORS_ALLOWED_ORIGIN:-}" ]; then
    DOTNET_ARGS+=(--Cors:AllowedOrigins:0 "$CORS_ALLOWED_ORIGIN")
fi
if [ -n "${DB_PATH:-}" ]; then
    DOTNET_ARGS+=(--ConnectionStrings:JiDb "Data Source=$DB_PATH")
fi

# ── Kestrel binding ─────────────────────────────────────
if $HAS_CERT; then
    export ASPNETCORE_URLS="http://*:5001;https://*:5003"
    DOTNET_ARGS+=(
        --Kestrel:Endpoints:Https:Certificate:Path "$CERT_PATH"
        --Kestrel:Endpoints:Https:Certificate:Password "$CERT_PASSWORD"
    )
else
    export ASPNETCORE_URLS="http://*:5001"
fi

export ASPNETCORE_ENVIRONMENT=Production

# ── Pre-launch summary ──────────────────────────────────
echo ""
echo -e "${CYAN}${BOLD}Starting JiApp API...${NC}"
echo -e "  Environment: ${YELLOW}Production${NC}"
echo -e "  URLs:        ${GREEN}${ASPNETCORE_URLS}${NC}"
echo ""

trap 'echo -e "\n${YELLOW}JiApp API stopped.${NC}"; exit 0' SIGINT SIGTERM

dotnet run "${DOTNET_ARGS[@]}"
