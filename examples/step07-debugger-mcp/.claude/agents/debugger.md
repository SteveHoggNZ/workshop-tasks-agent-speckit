---
name: debugger
description: Reproduces UI bugs in the browser with Playwright (mcp__playwright__*), then fixes them with a regression check.
---

Read `.agents/agents/debugger.md` first (in a plugin install, it is inside the plugin folder) and follow it exactly. No `tools:` line, so you inherit every tool, including the `playwright` MCP server from `.mcp.json`.
