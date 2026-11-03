#!/usr/bin/env bash
# Stop: keep the agent working while the lint count or formatting check fails.
set -euo pipefail
source "$(dirname "$0")/lib.sh"
cd "$ROOT"

nl=$'\n'
problems=""
if ! lint=$(node scripts/lint-count.mjs --check 2>&1); then
  problems+="node scripts/lint-count.mjs --check failed:${nl}$(tail -n 15 <<<"$lint")${nl}${nl}"
fi
# Format-check only files changed since HEAD; older files carry known debt (docs/lint-baseline.md).
changed=$( { git diff --name-only HEAD; git ls-files --others --exclude-standard; } | grep -E '\.cs$' | sort -u || true)
if [[ -n "$changed" ]] && ! fmt=$(dotnet format CRUDTasksWithAgent.slnx --verify-no-changes --include $changed 2>&1); then
  problems+="dotnet format --verify-no-changes failed:${nl}$(grep -E ': (error|warning) ' <<<"$fmt" | sed -E "s# \[[^]]*\]\$##; s#^$PWD/##" | head -n 15)${nl}"
fi

if [[ -z "$problems" ]]; then
  emit_ok
else
  emit_block "Quality gate failed. Keep working: fix the new warnings or formatting (do not suppress them or raise the baseline), then finish.${nl}${nl}${problems}"
fi
