#!/usr/bin/env bash
# PostToolUse: after a .cs/.razor edit, run a quiet build and report errors to the agent (never blocks).
set -euo pipefail
source "$(dirname "$0")/lib.sh"

if ! edited_paths | grep -Eq '\.(cs|razor)$'; then
  emit_ok
  exit 0
fi

if out=$(cd "$ROOT" && dotnet build -v q -nologo 2>&1); then
  emit_ok
else
  errors=$(grep -E ': error ' <<<"$out" | sed -E 's/ \[[^]]*\]$//' | sort -u | head -n 20 || true)
  emit_context "dotnet build failed after this edit. Fix these errors:"$'\n'"${errors:-$out}"
fi
