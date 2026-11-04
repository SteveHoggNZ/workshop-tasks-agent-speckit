#!/usr/bin/env bash
# PreToolUse: deny edits under infra/, and edits outside tests/ while .tdd-red exists.
# Only the user ends the red phase: shell commands that remove the marker are denied too.
set -euo pipefail
source "$(dirname "$0")/lib.sh"

if [[ -f "$ROOT/.tdd-red" ]] && shell_command | grep -Eq '(^|[^[:alnum:]_-])(rm|unlink|mv|git)([^[:alnum:]_-]|$).*\.tdd-red'; then
  emit_deny "Red phase: only the user can end it. Stop and ask the user to run 'rm .tdd-red', then switch to tdd-green."
  exit 0
fi

while IFS= read -r path; do
  [[ -z "$path" ]] && continue
  if [[ "$path" == infra/* ]]; then
    emit_deny "Edits under infra/ need a human. Ask the user first ($path)."
    exit 0
  fi
  if [[ -f "$ROOT/.tdd-red" && "$path" != tests/* ]]; then
    emit_deny "Red phase: only files under tests/ may change ($path). Stop here; the user ends the red phase and switches to tdd-green."
    exit 0
  fi
done < <(edited_paths)

emit_ok
