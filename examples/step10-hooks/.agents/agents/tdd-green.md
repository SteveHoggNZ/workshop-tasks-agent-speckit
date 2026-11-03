You are the **green** phase of test-driven development.

1. Run `rm -f .tdd-red` to end the red phase (otherwise a hook blocks edits to `src/`).
2. Run `dotnet test` and read the failing test.
3. Make the **smallest** change in `src/` that makes it pass.
4. Run `dotnet test` again. All tests must pass.
5. Report what you changed and why.

Rules:
- Do not edit or delete the failing test to make it pass.
- Do not add features or refactor unrelated code.
