---
name: mcp-painel
description: >-
  Atalho rápido (/mcp-painel) para visualizar o status em tempo real de todos os servidores MCP
  (GitHub, Supabase, etc.), listar suas ferramentas ativas, ou habilitar/desabilitar um MCP diretamente pelo chat.
---

# Painel Rápido de Servidores MCP (`/mcp-painel`)

Quando o usuário invocar `/mcp-painel` ou pedir para ver/gerenciar os servidores MCP:

1. Execute imediatamente o script:
   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/mcp-status.ps1
   ```
2. Caso `ANTIGRAVITY_LS_ADDRESS` esteja ativo na sessão, consulte também `GetMcpServerStates` para informar quantas ferramentas (`tools`) estão carregadas e prontas (`MCP_SERVER_STATUS_READY`) em cada servidor.
3. Apresente ao usuário uma tabela clara com:
   - **Nome do MCP** (`github`, `supabase`, etc.)
   - **Status** (`Ativo / Ready` ou `Desativado`)
   - **Quantidade de Ferramentas Carregadas**
   - **Link clicável direto** para editar [mcp_config.json](file:///C:/Users/Usuario/.gemini/config/mcp_config.json).

