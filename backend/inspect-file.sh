#!/usr/bin/env bash
# Runs ReSharper inspectcode on a single C# file and saves results.
# Usage: inspect-file.sh <absolute-path-to-cs-file>

set -euo pipefail
FILE="$1"
SOLUTION="/home/jakub/JiApp/backend/JiApp.sln"
RESULTS_DIR="/home/jakub/JiApp/backend/.inspect-results"
export DOTNET_ROOT=/var/snap/dotnet/common/dotnet
export PATH="/var/snap/dotnet/common/dotnet:$PATH"

REL=$(realpath --relative-to=/home/jakub/JiApp/backend "$FILE")
OUTFILE="$RESULTS_DIR/$REL.txt"
mkdir -p "$(dirname "$OUTFILE")"

# inspection results go to stdout, Roslyn noise goes to stderr
RAW=$(/home/jakub/.dotnet/tools/jb inspectcode \
    --include="$REL" \
    --format=Text \
    --output=- \
    --severity=SUGGESTION \
    --no-build \
    "$SOLUTION" 2>/dev/null) || true

# Keep only the actual inspection findings (lines with file: message pattern)
echo "$RAW" | grep -E '^\s*(Solution|Project|[^\s].*\.cs:)' > "$OUTFILE" || {
    echo "No issues found." > "$OUTFILE"
}
