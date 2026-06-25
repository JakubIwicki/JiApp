#!/usr/bin/env bash
# JiApp Portfolio Site Deployer — mirror web/ → JakubIwicki.github.io Pages repo.
# Usage: ./deploy-site.sh [--dry-run] [--no-wait] [-m <msg>] [--help]
set -euo pipefail

# ── Helpers ──────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

info()    { echo -e "${CYAN}[INFO]${NC}    $*"; }
success() { echo -e "${GREEN}[OK]${NC}      $*"; }
warn()    { echo -e "${YELLOW}[WARN]${NC}    $*"; }
error()   { echo -e "${RED}[ERROR]${NC}   $*"; }

# ── Paths & constants ────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SITE_REPO="JakubIwicki/JakubIwicki.github.io"
SITE_URL="https://jakubiwicki.github.io"
WEB_DIR="$SCRIPT_DIR/web"

# ── Flags ────────────────────────────────────────────────────────────────
DRY_RUN=false
NO_WAIT=false
COMMIT_MSG="chore: sync portfolio site from monorepo"

print_help() {
	echo "Usage: $0 [--dry-run] [--no-wait] [-m|--message <msg>] [--help|-h]"
	echo ""
	echo "  --dry-run          Build and check locally but do NOT clone/commit/push"
	echo "  --no-wait          Push but skip polling the deploy run + live verification"
	echo "  -m, --message <msg>  Commit message (default: \"$COMMIT_MSG\")"
	echo "  --help, -h         Show this help"
	echo ""
	echo "  Examples:"
	echo "    $0                              # full deploy: clone, build, push, wait, verify"
	echo "    $0 --dry-run                    # local build + check only"
	echo "    $0 --no-wait                    # push and return immediately"
	echo "    $0 -m \"fix: update APK URL\"    # custom commit message"
	exit 0
}

while [ $# -gt 0 ]; do
	case "$1" in
		--dry-run)
			DRY_RUN=true
			shift
			;;
		--no-wait)
			NO_WAIT=true
			shift
			;;
		-m|--message)
			COMMIT_MSG="$2"
			shift 2
			;;
		--help|-h)
			print_help
			;;
		*)
			error "Unknown flag: $1. Use --help for usage."
			exit 1
			;;
	esac
done

# ── Preconditions ────────────────────────────────────────────────────────
for cmd in gh npm rsync jq; do
	if ! command -v "$cmd" &>/dev/null; then
		error "'$cmd' is required but not found on PATH."
		exit 1
	fi
done

# ── Load env ─────────────────────────────────────────────────────────────
ENV_FILE="$SCRIPT_DIR/aws/.env"
if [ -f "$ENV_FILE" ]; then
	set -a
	source "$ENV_FILE"
	set +a
	success "aws/.env loaded"
else
	error "aws/.env not found at $ENV_FILE"
	error "This file is required — it contains AWS_ACCOUNT_ID (gitignored)."
	exit 1
fi

ACCOUNT_ID="${AWS_ACCOUNT_ID:-}"
if [ -z "$ACCOUNT_ID" ]; then
	error "AWS_ACCOUNT_ID is empty or not set in aws/.env"
	exit 1
fi
success "AWS_ACCOUNT_ID: $ACCOUNT_ID"

# ── Warn on uncommitted web/ changes ─────────────────────────────────────
if [ -n "$(git -C "$SCRIPT_DIR" status --porcelain "$WEB_DIR" 2>/dev/null)" ]; then
	warn "web/ has uncommitted changes — you are about to deploy local edits."
fi

# ── Temp working dir ─────────────────────────────────────────────────────
TMP=""
trap 'rm -rf "${TMP:-}"' EXIT INT TERM
TMP=$(mktemp -d)
info "Working in $TMP"

# ── Step 1: Clone Pages repo ─────────────────────────────────────────────
if $DRY_RUN; then
	info "DRY-RUN: skipping clone; using rsync mirror of web/ for local build"
	mkdir -p "$TMP/site"
	rsync -a --exclude='.git' --exclude='node_modules' --exclude='dist' "$WEB_DIR"/ "$TMP/site"/
else
	info "Cloning $SITE_REPO ..."
	gh repo clone "$SITE_REPO" "$TMP/site" -- --depth 1
	success "Cloned $SITE_REPO"
fi

# ── Step 2: Mirror web/ into the clone ───────────────────────────────────
info "Mirroring web/ → clone (rsync) ..."
rsync -a --delete --exclude='.git' --exclude='node_modules' --exclude='dist' "$WEB_DIR"/ "$TMP/site"/
success "Mirror complete"

# ── Step 3: Substitute account id placeholder ────────────────────────────
CONFIG_FILE="$TMP/site/src/config.ts"
info "Replacing jiapp-downloads-REPLACE_ME → jiapp-downloads-${ACCOUNT_ID} in config.ts"
sed -i "s/jiapp-downloads-REPLACE_ME/jiapp-downloads-${ACCOUNT_ID}/g" "$CONFIG_FILE"

if grep -rq 'jiapp-downloads-REPLACE_ME' "$TMP/site/src" 2>/dev/null; then
	error "jiapp-downloads-REPLACE_ME still present under src/ after substitution — aborting."
	error "The account-id substitution did not match; check src/config.ts."
	exit 1
fi
success "Account-id placeholder substituted (dist is guarded by check:no-placeholder)"

# ── Step 4: Build + ship-guard locally ───────────────────────────────────
info "Installing dependencies (npm ci) ..."
(cd "$TMP/site" && npm ci)
success "npm ci OK"

info "Building (npm run build) ..."
(cd "$TMP/site" && npm run build)
success "Build OK"

info "Running ship guard (npm run check:no-placeholder) ..."
(cd "$TMP/site" && npm run check:no-placeholder)
success "Ship guard passed"

# ── Dry-run: summarize and exit ──────────────────────────────────────────
if $DRY_RUN; then
	echo ""
	echo -e "${CYAN}╔══════════════════════════════════════════════╗${NC}"
	echo -e "${CYAN}║           DRY RUN — no push                  ║${NC}"
	echo -e "${CYAN}╚══════════════════════════════════════════════╝${NC}"
	echo ""
	echo "  Would commit to:  $SITE_REPO"
	echo "  Commit message:   $COMMIT_MSG"
	echo "  Live URL:         $SITE_URL"
	echo ""
	echo "  Changed files:"
	git -C "$TMP/site" status --short 2>/dev/null || echo "    (not a git worktree — dry-run skipped clone)"
	echo ""
	exit 0
fi

# ── Step 5: Commit & push ────────────────────────────────────────────────
info "Staging changes ..."
git -C "$TMP/site" add -A

if git -C "$TMP/site" diff --cached --quiet 2>/dev/null; then
	success "No changes to deploy."
	exit 0
fi

info "Committing ..."
FULL_MSG="${COMMIT_MSG}"$'\n\n'"Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
git -C "$TMP/site" commit -m "$FULL_MSG"
COMMIT_SHA=$(git -C "$TMP/site" rev-parse --short HEAD)
success "Committed: $COMMIT_SHA"

info "Pushing to origin/main ..."
git -C "$TMP/site" push origin HEAD:main
success "Push complete"
PUSHED_SHA=$(git -C "$TMP/site" rev-parse HEAD)

# ── Step 6: Poll deploy run + verify live (unless --no-wait) ─────────────
if $NO_WAIT; then
	echo ""
	echo -e "${GREEN}╔══════════════════════════════════════════════╗${NC}"
	echo -e "${GREEN}║           Site deployed!                      ║${NC}"
	echo -e "${GREEN}╚══════════════════════════════════════════════╝${NC}"
	echo ""
	echo "  Repo:       $SITE_REPO"
	echo "  Commit:     $COMMIT_SHA"
	echo "  Live URL:   $SITE_URL"
	echo ""
	exit 0
fi

info "Waiting for Pages deploy workflow to complete ..."
MAX_WAIT=300  # 5 minutes
POLL_INTERVAL=10
ELAPSED=0

while [ $ELAPSED -lt $MAX_WAIT ]; do
	RUN_JSON=$(gh run list -R "$SITE_REPO" --workflow=deploy.yml --limit 10 --json databaseId,status,conclusion,headSha 2>/dev/null || echo "")
	if [ -z "$RUN_JSON" ]; then
		warn "Could not fetch deploy run info; retrying in ${POLL_INTERVAL}s ..."
		sleep "$POLL_INTERVAL"
		ELAPSED=$((ELAPSED + POLL_INTERVAL))
		continue
	fi

	OUR_RUN=$(echo "$RUN_JSON" | jq -c --arg sha "$PUSHED_SHA" '[.[] | select(.headSha == $sha)] | .[0] // empty')
	if [ -z "$OUR_RUN" ]; then
		info "Deploy run for $PUSHED_SHA not registered yet; waiting ..."
		sleep "$POLL_INTERVAL"
		ELAPSED=$((ELAPSED + POLL_INTERVAL))
		continue
	fi
	STATUS=$(echo "$OUR_RUN" | jq -r '.status // "unknown"')
	CONCLUSION=$(echo "$OUR_RUN" | jq -r '.conclusion // "null"')
	RUN_ID=$(echo "$OUR_RUN" | jq -r '.databaseId // "?"')

	info "Run #$RUN_ID status=$STATUS conclusion=$CONCLUSION (${ELAPSED}s elapsed)"

	case "$STATUS" in
		completed)
			if [ "$CONCLUSION" = "success" ]; then
				success "Deploy workflow succeeded."
				break
			else
				error "Deploy workflow failed (conclusion: $CONCLUSION)."
				error "Check: https://github.com/${SITE_REPO}/actions/runs/${RUN_ID}"
				exit 1
			fi
			;;
		queued|in_progress|pending|waiting)
			sleep "$POLL_INTERVAL"
			ELAPSED=$((ELAPSED + POLL_INTERVAL))
			;;
		*)
			warn "Unexpected workflow status '$STATUS'; retrying ..."
			sleep "$POLL_INTERVAL"
			ELAPSED=$((ELAPSED + POLL_INTERVAL))
			;;
	esac
done

if [ $ELAPSED -ge $MAX_WAIT ]; then
	warn "Timed out after ${MAX_WAIT}s waiting for deploy workflow."
	warn "It may still complete — check: https://github.com/${SITE_REPO}/actions"
fi

# ── Step 7: Verify live site ─────────────────────────────────────────────
info "Verifying live site at $SITE_URL ..."
HTTP_CODE=$(curl -fsS -o /dev/null -w '%{http_code}' "$SITE_URL" 2>/dev/null || echo "000")

if [ "$HTTP_CODE" = "200" ]; then
	success "Live site returns HTTP 200"
else
	warn "Live site returned HTTP $HTTP_CODE (expected 200)"
fi

# Check that live JS bundle contains no REPLACE_ME
info "Checking live JS for REPLACE_ME ..."
INDEX_HTML=$(curl -fsS "$SITE_URL" 2>/dev/null || echo "")
JS_PATH=$(echo "$INDEX_HTML" | grep -oP '/assets/index-[A-Za-z0-9_-]+\.js' | head -1 || echo "")

if [ -n "$JS_PATH" ]; then
	JS_URL="${SITE_URL}${JS_PATH}"
	JS_CONTENT=$(curl -fsS "$JS_URL" 2>/dev/null || echo "")
	if [ -n "$JS_CONTENT" ] && echo "$JS_CONTENT" | grep -q REPLACE_ME; then
		warn "REPLACE_ME found in live JS bundle ($JS_PATH)! The substitution may not have applied."
	else
		success "No REPLACE_ME in live JS bundle"
	fi
else
	warn "Could not extract JS bundle path from index — skipping REPLACE_ME live check."
fi

# ── Summary ──────────────────────────────────────────────────────────────
echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║           Site deployed!                      ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════╝${NC}"
echo ""
echo "  Repo:       $SITE_REPO"
echo "  Commit:     $COMMIT_SHA"
echo "  Live URL:   $SITE_URL"
echo ""
