#!/usr/bin/env bash
# Prints the process id of the running CRUDTasksWithAgent app.
set -euo pipefail

pids=$(dotnet-counters ps | awk '/CRUDTasksWithAgent/ && $1 ~ /^[0-9]+$/ {print $1}')
if [[ -z "$pids" ]]; then
  echo "CRUDTasksWithAgent is not running. Start it with: dotnet run --project src/CRUDTasksWithAgent" >&2
  exit 1
fi
echo "$pids" | head -n 1
