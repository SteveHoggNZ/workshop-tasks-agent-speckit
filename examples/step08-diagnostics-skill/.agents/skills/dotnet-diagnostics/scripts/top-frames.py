#!/usr/bin/env python3
"""Usage: top-frames.py <file.speedscope.json> [--inclusive]

Sums time per frame across all threads and prints the top 15 by self time
(or inclusive time with --inclusive).
"""
import json
import re
import sys
from collections import Counter

IDLE = ("UNMANAGED_CODE_TIME", "CPU_TIME", "Thread (", "Threads", "(Non-Activities)", "Process")
BLOCKED = re.compile(r"Wait|Sleep|EventLoop|ThreadStart|TimerLoop|RunLoop|Finaliz|^\?!\?$")


def main() -> None:
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if len(args) != 1:
        sys.exit(__doc__)
    inclusive_sort = "--inclusive" in sys.argv
    with open(args[0], encoding="utf-8") as f:
        data = json.load(f)
    names = [fr["name"] for fr in data["shared"]["frames"]]
    self_t: Counter = Counter()
    incl_t: Counter = Counter()

    for prof in data["profiles"]:
        if prof["type"] == "sampled":
            for stack, w in zip(prof["samples"], prof["weights"]):
                if stack:
                    self_t[stack[-1]] += w
                for fr in set(stack):
                    incl_t[fr] += w
        else:  # evented
            opened: list[tuple[int, float]] = []
            last = None
            for ev in prof["events"]:
                t = ev["at"]
                if opened and last is not None:
                    # Leaves are UNMANAGED_CODE_TIME/CPU_TIME pseudo-frames; charge their time to the managed caller.
                    owner = next((f for f, _ in reversed(opened) if not names[f].startswith(IDLE)), opened[-1][0])
                    self_t[owner] += t - last
                last = t
                if ev["type"] == "O":
                    opened.append((ev["frame"], t))
                elif opened:
                    fr, start = opened.pop()
                    if all(fr != s[0] for s in opened):
                        incl_t[fr] += t - start

    total = sum(self_t.values()) or 1
    ranked = incl_t if inclusive_sort else self_t
    print(f"{'self%':>6} {'incl%':>6}  frame")
    shown = 0
    for fr, _ in ranked.most_common():
        if names[fr].startswith(IDLE) or BLOCKED.search(names[fr]):
            continue
        print(f"{100 * self_t[fr] / total:6.1f} {100 * incl_t[fr] / total:6.1f}  {names[fr]}")
        shown += 1
        if shown == 15:
            break


if __name__ == "__main__":
    main()
