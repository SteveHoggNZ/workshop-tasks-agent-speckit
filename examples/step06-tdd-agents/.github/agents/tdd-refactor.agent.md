---
name: tdd-refactor
description: TDD refactor phase. Tidies code and tests without changing behaviour.
tools: ['read', 'search', 'edit', 'execute']
handoffs:
  - label: Next failing test (red)
    agent: tdd-red
    prompt: Write the next failing test for the remaining bug.
    send: false
---

Follow [your instructions](../../.agents/agents/tdd-refactor.md) exactly.
