"""Make Spec Kit's git extension auto-commit after every step, so nobody has to remember to.

Run from a repo where `specify extension add git` has been done:
    uv run --with pyyaml python scripts/configure_speckit_git.py <repo-root>

- after_* commit hooks become mandatory (optional: false), so the agent runs them instead of suggesting them.
- A mandatory before_specify commit captures assess output before the feature branch is created.
- before_* commit hooks (other than before_specify) are disabled to keep the run quiet.
- Fixed commit messages let pin-checkpoints.sh find each step afterwards.
"""
import sys
from pathlib import Path

import yaml

AFTER = {
    "after_constitution": "[Spec Kit] Add project constitution",
    "after_specify": "[Spec Kit] Add specification",
    "after_clarify": "[Spec Kit] Clarify specification",
    "after_plan": "[Spec Kit] Add implementation plan",
    "after_checklist": "[Spec Kit] Add checklist",
    "after_tasks": "[Spec Kit] Add tasks",
    "after_analyze": "[Spec Kit] Record analysis",
    "after_implement": "[Spec Kit] Implement tasks",
    "after_converge": "[Spec Kit] Converge",
}
BEFORE_SPECIFY = "[Spec Kit] Add idea assessment"


def commit_hook(description: str, priority: int = 10) -> dict:
    return {
        "extension": "git",
        "command": "speckit.git.commit",
        "enabled": True,
        "optional": False,
        "priority": priority,
        "prompt": "Commit changes?",
        "description": description,
        "condition": None,
    }


def main(root: Path) -> None:
    ext_path = root / ".specify/extensions.yml"
    cfg_path = root / ".specify/extensions/git/git-config.yml"

    ext = yaml.safe_load(ext_path.read_text())
    hooks = ext.setdefault("hooks", {})

    for event, entries in hooks.items():
        for hook in entries:
            if hook.get("command") == "speckit.git.commit" and event.startswith("before_"):
                hook["enabled"] = False

    for event in AFTER:
        entries = hooks.setdefault(event, [])
        commits = [h for h in entries if h.get("command") == "speckit.git.commit"]
        if not commits:
            entries.append(commit_hook(f"Auto-commit after {event.removeprefix('after_')}"))
        for hook in commits:
            hook["enabled"] = True
            hook["optional"] = False

    before_specify = hooks.setdefault("before_specify", [])
    if not any(h.get("command") == "speckit.git.commit" for h in before_specify):
        before_specify.insert(0, commit_hook("Auto-commit assess output before the feature branch is created", 5))
    for hook in before_specify:
        if hook.get("command") == "speckit.git.commit":
            hook["enabled"] = True
            hook["optional"] = False

    ext_path.write_text(yaml.safe_dump(ext, sort_keys=False))

    cfg = yaml.safe_load(cfg_path.read_text())
    auto_commit = cfg.setdefault("auto_commit", {})
    for event, message in AFTER.items():
        auto_commit[event] = {"enabled": True, "message": message}
    auto_commit["before_specify"] = {"enabled": True, "message": BEFORE_SPECIFY}
    cfg_path.write_text(yaml.safe_dump(cfg, sort_keys=False))
    print(f"Configured auto-commit in {root}")


if __name__ == "__main__":
    main(Path(sys.argv[1] if len(sys.argv) > 1 else "."))
