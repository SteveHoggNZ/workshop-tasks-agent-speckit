---
name: tdd-red
description: TDD red phase. Writes one failing test under tests/ and nothing else.
tools: ['read', 'search', 'edit', 'execute']
handoffs:
  - label: Make it pass (green)
    agent: tdd-green
    prompt: Make the failing test above pass with the smallest change to src/.
    send: false
---

Follow [your instructions](../../.agents/agents/tdd-red.md) exactly.
