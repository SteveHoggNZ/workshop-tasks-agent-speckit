#!/usr/bin/env python3
"""Summarise Copilot session logs (~/.copilot/session-state) for a lab run."""
import argparse
import json
import os
import sys
from pathlib import Path

ROOT = Path(os.environ.get("COPILOT_SESSION_DIR", Path.home() / ".copilot" / "session-state"))
DEFAULT_MARKER = "is the only data access"


def meta(d: Path) -> dict:
    out = {}
    try:
        for line in (d / "workspace.yaml").read_text().splitlines():
            if ": " in line:
                k, v = line.split(": ", 1)
                out[k.strip()] = v.strip()
    except OSError:
        pass
    return out


def sessions(repo: str):
    dirs = [d for d in ROOT.iterdir() if (d / "events.jsonl").exists()]
    dirs.sort(key=lambda d: d.stat().st_mtime, reverse=True)
    return [d for d in dirs if meta(d).get("cwd", "").rstrip("/") == repo]


def inspect(d: Path, marker: str) -> None:
    m = meta(d)
    loaded = None
    prompts, skills, agents_read = [], [], False
    for raw in (d / "events.jsonl").read_text().splitlines():
        try:
            e = json.loads(raw)
        except json.JSONDecodeError:
            continue
        t, data = e.get("type"), e.get("data") or {}
        if t == "system.message" and loaded is None:
            loaded = marker in raw
        elif t == "user.message":
            prompts.append(data.get("content", "")[:120])
        elif t == "tool.execution_start":
            args = data.get("arguments") or {}
            if data.get("toolName") == "skill":
                skills.append(args.get("skill"))
            if "AGENTS.md" in json.dumps(args):
                agents_read = True
    print(f"session: {d.name}")
    print(f"client: {m.get('client_name')}  branch: {m.get('branch')}  created: {m.get('created_at')}")
    print(f"instructions in system prompt: {loaded}  (marker: {marker!r})")
    print(f"read AGENTS.md: {agents_read}")
    print(f"skills: {skills or 'none'}")
    for p in prompts:
        print(f"prompt: {p}")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("id", nargs="?", help="session id prefix (default: newest for this repo)")
    ap.add_argument("--marker", default=DEFAULT_MARKER, help="text that proves AGENTS.md was loaded")
    ap.add_argument("--list", action="store_true", help="list recent sessions for this repo")
    ap.add_argument("--repo", default=os.getcwd())
    a = ap.parse_args()
    found = sessions(a.repo.rstrip("/"))
    if not found:
        print(f"no Copilot sessions for {a.repo} in {ROOT}", file=sys.stderr)
        return 1
    if a.list:
        for d in found[:10]:
            m = meta(d)
            print(d.name, m.get("client_name"), m.get("branch"), m.get("created_at"))
        return 0
    target = next((d for d in found if d.name.startswith(a.id)), None) if a.id else found[0]
    if target is None:
        print(f"no session starting with {a.id}", file=sys.stderr)
        return 1
    inspect(target, a.marker)
    return 0


if __name__ == "__main__":
    sys.exit(main())
