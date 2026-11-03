---
paths:
  - "**/*.razor"
---

# Blazor components

- Give every element rendered in a `@foreach` a stable `@key` (for tasks, `@key="task.Id"`).
- A component that subscribes to an event (`+=`) must `@implements IDisposable` and unsubscribe (`-=`) in `Dispose`.
- Update UI from events or background work with `InvokeAsync(StateHasChanged)`, never `StateHasChanged()` directly.
- Render lists that can exceed ~100 rows with `<Virtualize>`.
- Debounce search inputs (~300 ms with a `CancellationTokenSource`) instead of filtering on every keystroke.
- Do not build a `Regex` per row; prefer `string.Contains(term, StringComparison.OrdinalIgnoreCase)`.
- Never inject `TasksDbContext`; inject `TaskService`.
- Inject `TimeProvider` for the current time; never use `DateTime.Now`.
- Do not allocate large buffers per component instance.
