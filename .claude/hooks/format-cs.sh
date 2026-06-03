#!/usr/bin/env bash
# Generic ReSharper C# formatter for Claude Code PostToolUse hook.
# Formats ONLY the edited file by targeting its nearest .csproj + --include.
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "$(readlink -f "${BASH_SOURCE[0]}")")" && pwd)"
SETTINGS="$SCRIPT_DIR/wrap-lines-override.DotSettings"

payload="$(cat)"
case "$payload" in *.cs*) ;; *) exit 0 ;; esac  # fast bail before spawning python3
file="$(printf '%s' "$payload" | python3 -c 'import sys, json
try:
    d = json.load(sys.stdin)
except Exception:
    print("")
    sys.exit()
print((d.get("tool_response") or {}).get("filePath") or (d.get("tool_input") or {}).get("file_path") or "")' 2>/dev/null)"

# Windows path -> WSL path
case "$file" in
  [A-Za-z]:\\*|[A-Za-z]:/*)
    command -v wslpath >/dev/null 2>&1 && file="$(wslpath -u "$file")" ;;
esac

case "$file" in *.cs) ;; *) exit 0 ;; esac
[ -f "$file" ] || exit 0

# Walk up from the edited file to the nearest enclosing .csproj.
dir="$(cd "$(dirname "$file")" && pwd)"
target=""
while [ "$dir" != "/" ]; do
  proj="$(ls "$dir"/*.csproj 2>/dev/null | head -n1)"
  [ -n "$proj" ] && { target="$proj"; break; }
  dir="$(dirname "$dir")"
done
[ -n "$target" ] || exit 0

JB="$HOME/.dotnet/tools/jb"
[ -x "$JB" ] || JB="$(command -v jb || true)"
[ -n "$JB" ] || exit 0

"$JB" cleanupcode \
  "--profile=Built-in: Reformat Code" \
  "$target" \
  "--include=$file" \
  "--settings=$SETTINGS" \
  --no-build >/dev/null 2>&1 || true

dotnet format whitespace --include "$file" --no-restore "$target" >/dev/null 2>&1 || true
