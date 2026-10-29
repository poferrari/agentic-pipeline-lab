---
name: mcp-painel
description: >-
  Quick shortcut (/mcp-painel) to inspect real-time status of all MCP servers
  (GitHub, Supabase, etc.), list active tools, or enable/disable an MCP server directly.
---

# MCP Servers Dashboard (`/mcp-painel`)

When the user invokes `/mcp-painel` or asks to inspect/manage MCP servers:

1. Immediately run:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/mcp-status.ps1
   ```
2. If `ANTIGRAVITY_LS_ADDRESS` is active in the session, query `GetMcpServerStates` to report how many tools are loaded and ready (`MCP_SERVER_STATUS_READY`) on each server.
3. Present a clear table to the user containing:
   - **MCP Server Name** (`github`, `supabase`, etc.)
   - **Status** (`Active / Ready` or `Disabled`)
   - **Count of Loaded Tools**
   - **Direct Clickable Link** to edit [mcp_config.json](file:///C:/Users/Usuario/.gemini/config/mcp_config.json).
