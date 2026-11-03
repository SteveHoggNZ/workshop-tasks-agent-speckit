#!/usr/bin/env bash
# Usage: counters.sh <pid> [seconds=20]  -> .diag/counters.json
set -euo pipefail

pid=${1:?usage: counters.sh <pid> [seconds]}
secs=${2:-20}
mkdir -p .diag
# Name the counters: plain "System.Runtime" selects the .NET 9+ meter, which has no gc-heap-size/cpu-usage.
dotnet-counters collect -p "$pid" \
  --counters 'System.Runtime[cpu-usage,gc-heap-size,working-set,gen-0-gc-count,gen-2-gc-count]' \
  --format json -o .diag/counters.json \
  --duration "$(printf '00:%02d:%02d:%02d' $((secs / 3600)) $((secs % 3600 / 60)) $((secs % 60)))"
echo "Wrote .diag/counters.json"
