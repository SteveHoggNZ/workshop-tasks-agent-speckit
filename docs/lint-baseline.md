# Lint baseline

Lint warnings in this repo are **deliberate lab debt**. They are visible in every build but never fail it
(`TreatWarningsAsErrors=false`). Labs ask attendees (and their coding agents) to fix them; **Lab 06 (Hooks)**
promotes selected rules to errors so a harness hook/CI can block regressions.

Configuration lives in [`Directory.Build.props`](../Directory.Build.props) (analyzers, `AnalysisLevel=latest-recommended`,
`EnforceCodeStyleInBuild`, `GenerateDocumentationFile` so IDE0005 fires on build, `CS1591` suppressed) and
[`.editorconfig`](../.editorconfig) (rule severities).

## Baseline

<!-- lint-baseline-total: 19 -->

`dotnet build CRUDTasksWithAgent.slnx --no-incremental` → **0 errors, 19 unique warnings**.

| Rule | Count | Meaning |
| --- | ---: | --- |
| IDE0161 | 11 | Use file-scoped namespace (`csharp_style_namespace_declarations = file_scoped:warning`) |
| IDE0005 | 2 | Unnecessary `using` directive |
| CA1305 | 1 | Specify `IFormatProvider` (tests) |
| CA1848 | 1 | Use `LoggerMessage` delegates |
| CA1861 | 1 | Constant array argument → `static readonly` field (tests) |
| IDE0051 | 1 | Unused private member (`TaskCrudTool.FormatTask`) |
| IDE0059 | 1 | Unnecessary assignment (`task` in `TaskCrudTool`) |
| NU1510 | 1 | `System.Text.Json` package reference will not be pruned (restore warning) |
| **Total** | **19** | |

Rules configured as `warning` with currently **zero** hits: IDE0052, CA1822.

### How warnings are counted

msbuild prints every warning once per project build and again in the end-of-build summary, so its own
"N Warning(s)" figure over-counts. We count **unique** warning lines instead: each line matching
`: warning <ID>` has its trailing `[project.csproj]` removed and is de-duplicated as a set
(`file(line,col): warning ID: message`). `scripts/lint-count.mjs` does exactly this.

### Policy choices

- Naming rule: private/internal fields must be `_camelCase`, severity **suggestion** (visible in IDEs, not in the
  build baseline). Raise it to `warning` in `.editorconfig` if a lab needs it.
- `CA1707` (underscores in names) is disabled for `tests/**` because `Method_Scenario_Expected` is the xUnit
  convention in this repo; otherwise it would add 29 noisy warnings.
- Line endings and charset are intentionally not enforced: the repo mixes CRLF/LF and BOM/no-BOM files.

## `dotnet format --verify-no-changes`

`dotnet format CRUDTasksWithAgent.slnx --verify-no-changes` exits with **code 2** (changes needed). It reports the
same diagnostics it can auto-fix or see: 16 unique findings — 11× IDE0161, 2× IDE0005, 1× IDE0051, 1× IDE0059,
1× CA1848 (each repeated once per target in the raw output). It does not report CA1305, CA1861 or NU1510.
Only ever run it with `--verify-no-changes` here; running `dotnet format` without it would erase the lab debt.

## Tooling

```bash
node scripts/lint-count.mjs           # per-rule totals
node scripts/lint-count.mjs --check   # exit 1 if total != the number above (CI runs this)
```

If you intentionally fix (or add) debt, update the `lint-baseline-total` comment and the table in the same commit.
