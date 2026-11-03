#!/usr/bin/env bash
# Sourced by the hook scripts. Usage: <script> --tool copilot|claude|codex  (event JSON on stdin).
# Reads both payload styles (tool_name/tool_input and toolName/toolArgs) and writes the caller's output format.
set -euo pipefail

TOOL=claude
[[ "${1:-}" == "--tool" && -n "${2:-}" ]] && TOOL=$2
EVENT=$(cat)
ROOT=$(git rev-parse --show-toplevel 2>/dev/null || pwd)

# Repo-relative paths an edit-like tool call touches, one per line.
edited_paths() {
  jq -r '
    (.toolName // .tool_name // "") as $tool
    | select($tool | test("edit|create|write|replace|insert|patch|delete|rename|move"; "i"))
    | (.toolArgs // .tool_input // {}) | (if type == "string" then (fromjson? // {input: .}) else . end)
    | [.path, .filePath, .file_path, .notebook_path, .newPath, .oldPath, (.filePaths // [])[], (.files // [])[]?, (.replacements // [])[]?.filePath?]
      + ([(.input // .patch // .command // "") | strings | scan("\\*\\*\\* (?:Add|Update|Delete) File: (.+)")[]])
    | .[] | select(type == "string" and . != "")
  ' <<<"$EVENT" | sed -e "s#^$ROOT/##" -e 's#^\./##'
}

# PreToolUse deny. Copilot also gets the top-level fields of its CLI harness format.
emit_deny() {
  jq -n --arg r "$1" --arg t "$TOOL" '
    {hookSpecificOutput: {hookEventName: "PreToolUse", permissionDecision: "deny", permissionDecisionReason: $r}}
    + (if $t == "copilot" then {permissionDecision: "deny", permissionDecisionReason: $r} else {} end)'
}

# PostToolUse context for the agent (never blocks).
emit_context() {
  jq -n --arg c "$1" --arg t "$TOOL" '
    {hookSpecificOutput: {hookEventName: "PostToolUse", additionalContext: $c}}
    + (if $t == "copilot" then {additionalContext: $c} else {} end)'
}

# Stop: keep the agent working. Same shape for all three tools.
emit_block() {
  jq -n --arg r "$1" '{decision: "block", reason: $r}'
}

emit_ok() { echo '{}'; }
