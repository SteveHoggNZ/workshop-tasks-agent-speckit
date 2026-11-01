# How Spec Kit is set up in this repo

This records every Spec Kit customisation made for the workshop, what was changed from the defaults, and why. [INSTRUCTIONS.md](../INSTRUCTIONS.md) explains how to *use* Spec Kit; this file explains how it was *configured*.

The configuration lives on branch `demo/01-init`. You can reproduce it on any repo with the commands under [Reproduce on your own repo](#reproduce-on-your-own-repo).

## Versions

| Component | Version | Installed with |
| --- | --- | --- |
| Spec Kit CLI (`specify-cli`) | 1.2.5 | `uv tool install specify-cli`, or `uvx --from specify-cli` |
| Agent integration | `copilot`, with `sh` scripts | `specify init --here --force --integration copilot --script sh` |
| `assess` extension | 1.0.1 (spec-kit-core) | `specify extension add assess` |
| `git` extension | 1.0.1 (spec-kit-core) | `specify extension add git` |

## Customisations

| # | Change | Where | Why |
| ---: | --- | --- | --- |
| 1 | Removed the `openspec/` folder | Repo root (`demo/00-baseline`) | One spec tool at a time. The main branch keeps OpenSpec for its own lab |
| 2 | Initialised Spec Kit for Copilot CLI | `.specify/`, `.github/skills/speckit-*` | The demo runs in Copilot CLI. Other agents are covered in [INSTRUCTIONS.md](../INSTRUCTIONS.md#using-another-agent) |
| 3 | Added the `assess` extension | `.specify/extensions/assess/`, `.github/skills/speckit-assess-*` | Checks whether an idea is worth building (go, needs-clarification or kill) before writing a spec |
| 4 | Added the `git` extension | `.specify/extensions/git/`, `.github/skills/speckit-git-*` | A feature branch per spec, plus auto-commit |
| 5 | **Turned auto-commit on** after every step | `.specify/extensions/git/git-config.yml` (`auto_commit`) | Every step lands as its own commit, so nobody has to remember to commit |
| 6 | **Made the commit hooks mandatory** | `.specify/extensions.yml` (`optional: false`) | Optional hooks are only *suggested* to you. Mandatory ones are *run* by the agent |
| 7 | Added a commit **before** specify | `.specify/extensions.yml` (`before_specify`, first in the list) | The assess steps are an extension and have no commit hooks of their own; this captures their output before the feature branch is created |
| 8 | Added a commit **after** converge | `.specify/extensions.yml` (`after_converge`) | The git extension doesn't register this event; without it, converge's changes would stay uncommitted |
| 9 | Disabled the other `before_*` commit hooks | `.specify/extensions.yml` (`enabled: false`) | Keeps the run quiet; the `after_*` commits already capture every step |

Customisations 5–9 were applied with [`scripts/configure_speckit_git.py`](../scripts/configure_speckit_git.py). The resulting YAML is committed, so you can read exactly what changed.

### What gets committed when

| Step | Hook | Commit message |
| --- | --- | --- |
| constitution | `after_constitution` | `[Spec Kit] Add project constitution` |
| assess (intake → decide) | `before_specify` | `[Spec Kit] Add idea assessment` |
| specify | `before_specify` creates branch `NNN-<feature>`, then `after_specify` | `[Spec Kit] Add specification` |
| clarify | `after_clarify` | `[Spec Kit] Clarify specification` |
| plan | `after_plan` | `[Spec Kit] Add implementation plan` |
| checklist | `after_checklist` | `[Spec Kit] Add checklist` |
| tasks | `after_tasks` | `[Spec Kit] Add tasks` |
| analyze | `after_analyze` | `[Spec Kit] Record analysis` (usually nothing; analyze is read-only) |
| implement | `after_implement` | `[Spec Kit] Implement tasks` |
| converge | `after_converge` | `[Spec Kit] Converge` |

Steps with nothing to commit are skipped. The messages are fixed (`commit_style: fixed`), so you can find any step in `git log --oneline`.

### Other hooks the git extension adds

- `before_constitution` runs `speckit.git.initialize`. It does nothing in an existing repo.
- `before_specify` runs `speckit.git.feature`, which creates and switches to a numbered feature branch (e.g. `001-what-first`). That's standard Spec Kit practice: one branch per spec.

### A caveat: these are instructions, not a guarantee

Spec Kit's hooks are run **by the agent**, which follows the instructions in each `SKILL.md`. Marking a hook mandatory makes the agent required to run it, and in practice it does. It isn't the same guarantee as a git hook or CI check, though. **Glance at `git log --oneline` after each step.** If a commit is missing, run `/speckit-git-commit` or commit by hand.

## Reproduce on your own repo

```bash
git switch -c speckit/setup
specify init --here --force --integration <copilot|codex|claude> --script sh
specify extension add assess
specify extension add git
# copy scripts/configure_speckit_git.py from this repo into yours first
uv run --with pyyaml python scripts/configure_speckit_git.py .   # customisations 5–9
git add -A && git commit -m "Set up Spec Kit with auto-commit"
```

Without the script, the manual equivalent is:

1. In `git-config.yml`, set `enabled: true` with a message for each `after_*` event and for `before_specify`.
2. In `extensions.yml`:
   - set `optional: false` on the `after_*` `speckit.git.commit` hooks;
   - add `after_converge` and `before_specify` commit hooks;
   - set `enabled: false` on the other `before_*` commit hooks.

## Turn auto-commit off

Set `auto_commit.default: false` and each event's `enabled: false` in `git-config.yml`. Or remove the extension with `specify extension remove git`.
