---
name: dotnet-diagnostics
description: Measure the running CRUDTasksWithAgent app with dotnet-counters, dotnet-trace and dotnet-gcdump. Use when a page is slow, CPU is high, memory grows, or you need numbers before and after a performance fix.
---

# .NET diagnostics

Measure first, fix second, measure again. Report numbers, not impressions.

Requires `dotnet tool install -g dotnet-counters dotnet-trace dotnet-gcdump` and the app running (`dotnet run --project src/CRUDTasksWithAgent`). Output goes to `.diag/` (gitignored). Scripts are in this skill's `scripts/` folder (in this repo: `.agents/skills/dotnet-diagnostics/scripts/`).

| Script | Does |
|---|---|
| `find-pid.sh` | Prints the app's process id. |
| `counters.sh <pid> [seconds=20]` | Collects `System.Runtime` counters to `.diag/counters.json`. |
| `trace.sh <pid> [seconds=15]` | Collects a CPU trace to `.diag/trace.speedscope.json`. |
| `top-frames.py <file.speedscope.json> [--inclusive]` | Prints the top 15 frames by self (or inclusive) time. |
| `gcdump.sh <pid> [label]` | Collects a heap dump to `.diag/<label>.gcdump` and prints the top types. |

## Slow page (CPU)

1. `PID=$(.agents/skills/dotnet-diagnostics/scripts/find-pid.sh)`
2. Start `trace.sh "$PID" 15`, then reproduce the slow action in the browser while it runs (e.g. type in the `/bulk` search box).
3. `python3 .agents/skills/dotnet-diagnostics/scripts/top-frames.py .diag/trace.speedscope.json`
4. Name the hottest app-related frames (e.g. `Regex`, `List.Contains`, rendering). Ignore idle/wait frames.
5. Fix, restart the app, repeat steps 1–3 and compare the top frames.

## Memory growth

1. `gcdump.sh "$PID" before` right after start.
2. Repeat the suspect action (e.g. navigate to `/dashboard` and away 20 times).
3. `gcdump.sh "$PID" after`.
4. Compare the two reports: types whose count or size grew (e.g. `byte[]`, a component type) are leaking. Note the MB difference.
5. Fix, restart, repeat and compare the MB difference.

Use `counters.sh` to watch `gc-heap-size` and `cpu-usage` over time when you need a trend rather than a snapshot.

## Rules

- Only read from the process; never kill it or delete files outside `.diag/`.
- Give before and after numbers (ms, % CPU, MB, instance count) in your report.
