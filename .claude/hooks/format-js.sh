#!/usr/bin/env bash
# Generic Prettier formatter for Claude Code PostToolUse hook (WSL).
# Formats ONLY the edited .js/.jsx/.ts/.tsx file. Prefers the project's own
# prettier (version + config), falls back to a standard global/on-demand one.
set -uo pipefail

payload="$(cat)"
case "$payload" in *.js*|*.ts*) ;; *) exit 0 ;; esac  # fast bail before spawning python3
file="$(printf '%s' "$payload" | python3 -c 'import sys, json
try:
    d = json.load(sys.stdin)
except Exception:
    print(""); sys.exit()
print((d.get("tool_response") or {}).get("filePath") or (d.get("tool_input") or {}).get("file_path") or "")' 2>/dev/null)"

# Windows path -> WSL path
case "$file" in
  [A-Za-z]:\\*|[A-Za-z]:/*)
    command -v wslpath >/dev/null 2>&1 && file="$(wslpath -u "$file")" ;;
esac

case "$file" in *.js|*.jsx|*.ts|*.tsx) ;; *) exit 0 ;; esac
[ -f "$file" ] || exit 0

dir="$(cd "$(dirname "$file")" && pwd)"

# 1) Project-local prettier (respects the repo's own version + .prettierrc).
if ( cd "$dir" && npx --no-install prettier --write "$file" ) >/dev/null 2>&1; then
  exit 0
fi
# 2) Globally installed prettier, else 3) the standard one on demand.
if command -v prettier >/dev/null 2>&1; then
  prettier --write "$file" >/dev/null 2>&1 || true
else
  npx --yes prettier --write "$file" >/dev/null 2>&1 || true
fi
