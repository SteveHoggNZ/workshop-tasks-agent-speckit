---
paths:
  - "tests/**"
---

# Tests

- Use xUnit (`[Fact]`, `[Theory]` + `[InlineData]`). One behaviour per test; name tests `Method_Condition_Result`.
- For data tests, create a fresh database with `TestDb.Create()` from `tests/CRUDTasksWithAgent.Tests/Helpers/TestDb.cs`.
- For HTTP tests, use `ApiFactory` from `tests/CRUDTasksWithAgent.Tests/Helpers/ApiFactory.cs`.
- Never depend on the real clock. Pass a fixed `DateTimeOffset` (with an explicit offset) or a fake `TimeProvider`.
- Add new tests in new files; do not rewrite existing tests to make them pass.
- Run `dotnet test` and confirm the new test fails for the right reason before fixing code.
