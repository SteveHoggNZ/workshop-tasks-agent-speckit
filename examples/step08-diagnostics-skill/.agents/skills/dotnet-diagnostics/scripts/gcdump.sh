#!/usr/bin/env bash
# Usage: gcdump.sh <pid> [label=heap-<time>]  -> .diag/<label>.gcdump + top types
set -euo pipefail

pid=${1:?usage: gcdump.sh <pid> [label]}
label=${2:-heap-$(date +%H%M%S)}
mkdir -p .diag
dotnet-gcdump collect -p "$pid" -o ".diag/$label.gcdump"
dotnet-gcdump report ".diag/$label.gcdump" | head -n 30
