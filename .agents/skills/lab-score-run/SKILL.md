---
name: lab-score-run
description: Collect the evidence to score a workshop lab run on the current lab/* branch (changes since the last "copy example" commit, lint total vs baseline, tests). Use only when reviewing a workshop lab run, not for feature work.
---

# Score a lab run

1. Collect evidence: `bash .agents/skills/lab-score-run/scripts/evidence.sh`
   It compares against the last "copy example" commit, so committed and uncommitted After changes both show. If that commit is wrong (e.g. the After run was committed with a "copy example" message), pass `BASE=<commit>`. It prints the branch, the diff and new files, the lint total vs `docs/lint-baseline.md`, and the test summary. It runs a build and tests, so allow a minute or two.
2. Open the step in `WORKSHOP.md` and score each checklist line 0/1. Quote the evidence line for every point.
3. Flag anything the agent claimed but did not do (for example "lint count reported" while the total rose above the baseline).
4. Do not change the attendee's code.
