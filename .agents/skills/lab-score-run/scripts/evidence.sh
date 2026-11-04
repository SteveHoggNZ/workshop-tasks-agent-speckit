#!/usr/bin/env bash
# Prints the evidence for scoring a lab run. Read-only apart from build output.
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

base=${BASE:-$(git log -1 --format=%h --grep='copy example' || true)}
base=${base:-HEAD}
echo "## branch: $(git branch --show-current)"
echo "## comparing against: $(git log -1 --format='%h %s' "$base")"
echo "## changed files"
git --no-pager diff --stat "$base"
git status --short
echo "## diff (committed + uncommitted since base)"
git --no-pager diff "$base"
echo "## new files"
git ls-files --others --exclude-standard src tests docs | while read -r f; do
  echo "== $f"
  cat "$f"
done
echo "## lint"
baseline=$(grep -oE 'lint-baseline-total: [0-9]+' docs/lint-baseline.md | grep -oE '[0-9]+' || echo "?")
node scripts/lint-count.mjs | tail -n 12
echo "baseline: $baseline"
echo "## tests"
dotnet test --nologo 2>&1 | grep -E 'Passed!|Failed!|error' | tail -n 3 || true
