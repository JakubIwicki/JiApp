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
DATA_DIR="$SCRIPT_DIR/.data"
LOG_DIR="$SCRIPT_DIR/.logs"

mkdir -p "$DATA_DIR" "$LOG_DIR"

# ── Banner ──────────────────────────────────────────────
echo -e "${CYAN}${BOLD}╭──────────────────────────────────────────╮${NC}"
echo -e "${CYAN}${BOLD}│${NC}        ${BOLD}JiApp — DB Setup & Seed${NC}              ${CYAN}${BOLD}│${NC}"
echo -e "${CYAN}${BOLD}╰──────────────────────────────────────────╯${NC}"
echo ""

# ── Prerequisites ───────────────────────────────────────
if ! command -v dotnet &>/dev/null; then
    echo -e "${RED}ERROR: dotnet CLI not found. Install .NET SDK 10.${NC}"
    exit 1
fi
echo -e "${GREEN}✓${NC} dotnet $(dotnet --version)"

# ── Restore local tools (dotnet-ef) ─────────────────────
echo -n "Restoring local tools..."
cd "$SCRIPT_DIR"
dotnet tool restore > "$LOG_DIR/setup-toolrestore.log" 2>&1
echo -e " ${GREEN}done${NC}"

# ── Identity DB ─────────────────────────────────────────
IDENTITY_DB="$DATA_DIR/identity_dev.db"
echo ""
echo -e "${BOLD}Identity (auth + users)${NC}"
echo "  DB: $IDENTITY_DB"

# Apply migrations (idempotent — safe for new or existing DBs)
echo -n "  Applying migrations..."
dotnet ef database update \
    --project "$SCRIPT_DIR/src/JiApp.Identity/JiApp.Identity.csproj" \
    --connection "Data Source=$IDENTITY_DB" \
    > "$LOG_DIR/setup-identity.log" 2>&1
echo -e " ${GREEN}done${NC}"

# Ensure UserModuleGrants table exists (created by migration, verify)
TABLE_EXISTS=$(sqlite3 "$IDENTITY_DB" \
    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='UserModuleGrants';" 2>/dev/null || echo "0")
echo "  UserModuleGrants table: $([ "$TABLE_EXISTS" = "1" ] && echo -e "${GREEN}✓${NC}" || echo -e "${RED}✗${NC}")"

# ── Scheduler DB ────────────────────────────────────────
SCHEDULER_DB="$DATA_DIR/scheduler_dev.db"
echo ""
echo -e "${BOLD}Scheduler${NC}"
echo "  DB: $SCHEDULER_DB"

echo -n "  Applying migrations..."
dotnet ef database update \
    --project "$SCRIPT_DIR/src/JiApp.Scheduler/JiApp.Scheduler.csproj" \
    --connection "Data Source=$SCHEDULER_DB" \
    > "$LOG_DIR/setup-scheduler.log" 2>&1
echo -e " ${GREEN}done${NC}"

# ── YtDownloader DB ─────────────────────────────────────
YTDOWNLOADER_DB="$DATA_DIR/ytdownloader_dev.db"
echo ""
echo -e "${BOLD}YtDownloader${NC}"
echo "  DB: $YTDOWNLOADER_DB"

echo -n "  Applying migrations..."
dotnet ef database update \
    --project "$SCRIPT_DIR/src/JiApp.YtDownloader/JiApp.YtDownloader.csproj" \
    --connection "Data Source=$YTDOWNLOADER_DB" \
    > "$LOG_DIR/setup-ytdownloader.log" 2>&1
echo -e " ${GREEN}done${NC}"

# ── Seed module grants (for existing users after reset) ──
echo ""
echo -e "${BOLD}Seeding module grants for existing users${NC}"
USER_COUNT=$(sqlite3 "$IDENTITY_DB" "SELECT COUNT(*) FROM AspNetUsers;" 2>/dev/null || echo "0")

if [ "$USER_COUNT" -gt 0 ]; then
    # Grant all modules + full_access to every existing user
    sqlite3 "$IDENTITY_DB" "
        INSERT OR IGNORE INTO UserModuleGrants (UserId, ModuleName, GrantedAt)
        SELECT Id, 'YtDownloader', datetime('now') FROM AspNetUsers;
        INSERT OR IGNORE INTO UserModuleGrants (UserId, ModuleName, GrantedAt)
        SELECT Id, 'Scheduler', datetime('now') FROM AspNetUsers;
        INSERT OR IGNORE INTO UserModuleGrants (UserId, ModuleName, GrantedAt)
        SELECT Id, 'full_access', datetime('now') FROM AspNetUsers;
    "
    GRANT_COUNT=$(sqlite3 "$IDENTITY_DB" "SELECT COUNT(*) FROM UserModuleGrants;" 2>/dev/null || echo "0")
    echo -e "  Users: ${GREEN}${USER_COUNT}${NC}, Grants: ${GREEN}${GRANT_COUNT}${NC}"
else
    echo -e "  ${YELLOW}No users yet — grants will be assigned on registration${NC}"
fi

# ── WAL mode for better concurrent access ────────────────
for db in "$IDENTITY_DB" "$SCHEDULER_DB" "$YTDOWNLOADER_DB"; do
    if [ -f "$db" ]; then
        sqlite3 "$db" "PRAGMA journal_mode=WAL;" > /dev/null 2>&1 || true
    fi
done
echo -e "  Journal mode: ${GREEN}WAL${NC}"

# ── Summary ──────────────────────────────────────────────
echo ""
echo -e "${GREEN}${BOLD}✓ Database setup complete${NC}"
echo ""
echo "  Identity:    $IDENTITY_DB"
echo "  Scheduler:   $SCHEDULER_DB"
echo "  YtDownloader: $YTDOWNLOADER_DB"
echo ""
echo "  Run the API:  ./start-dev.sh"
echo "  Clear all:    rm -f $DATA_DIR/*.db $DATA_DIR/*.db-*"
echo ""
