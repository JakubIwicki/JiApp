#!/usr/bin/env bash
# Generic Black formatter for Claude Code PostToolUse hook (WSL).
# Formats ONLY the edited .py file.
set -uo pipefail

payload="$(cat)"
case "$payload" in *.py*) ;; *) exit 0 ;; esac  # fast bail before spawning python3
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

case "$file" in *.py) ;; *) exit 0 ;; esac
[ -f "$file" ] || exit 0

if command -v black >/dev/null 2>&1; then
  black -q "$file" >/dev/null 2>&1 || true
elif python3 -m black --version >/dev/null 2>&1; then
  python3 -m black -q "$file" >/dev/null 2>&1 || true
fi
