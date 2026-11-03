#!/usr/bin/env bash
# PreToolUse: deny edits under infra/, and edits outside tests/ while .tdd-red exists.
set -euo pipefail
source "$(dirname "$0")/lib.sh"

while IFS= read -r path; do
  [[ -z "$path" ]] && continue
  if [[ "$path" == infra/* ]]; then
    emit_deny "Edits under infra/ need a human. Ask the user first ($path)."
    exit 0
  fi
  if [[ -f "$ROOT/.tdd-red" && "$path" != tests/* ]]; then
    emit_deny "Red phase: only files under tests/ may change ($path). Hand off to tdd-green to change src/."
    exit 0
  fi
done < <(edited_paths)

emit_ok
