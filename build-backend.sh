#!/usr/bin/env bash
set -euo pipefail

# Build the backend. WSL output goes to .artifacts-wsl/, Windows/Rider output
# goes to .artifacts/ — they never conflict. Standard dotnet build is safe.
#
# Usage: ./build-backend.sh [--test]

TEST=false
for arg in "$@"; do
  case "$arg" in
    --test) TEST=true ;;
    *)      echo "Unknown arg: $arg"; exit 1 ;;
  esac
done

dotnet build backend/JiApp.sln

if [ "$TEST" = true ]; then
  dotnet test backend/JiApp.sln
fi
