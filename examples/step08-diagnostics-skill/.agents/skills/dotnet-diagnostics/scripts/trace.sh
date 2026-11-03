#!/usr/bin/env bash
# Usage: trace.sh <pid> [seconds=15]  -> .diag/trace.speedscope.json
set -euo pipefail

pid=${1:?usage: trace.sh <pid> [seconds]}
secs=${2:-15}
mkdir -p .diag
echo "Tracing $pid for ${secs}s. Reproduce the slow action now." >&2
dotnet-trace collect -p "$pid" --format speedscope -o .diag/trace.nettrace \
  --duration "$(printf '00:%02d:%02d:%02d' $((secs / 3600)) $((secs % 3600 / 60)) $((secs % 60)))"
echo "Wrote .diag/trace.speedscope.json"
