---
name: tdd-green
description: TDD green phase. Makes the failing test pass with the smallest code change.
tools: ['read', 'search', 'edit', 'execute']
handoffs:
  - label: Tidy up (refactor)
    agent: tdd-refactor
    prompt: Refactor the code and tests just changed without changing behaviour. Keep all tests green.
    send: false
---

Follow [your instructions](../../.agents/agents/tdd-green.md) exactly.
