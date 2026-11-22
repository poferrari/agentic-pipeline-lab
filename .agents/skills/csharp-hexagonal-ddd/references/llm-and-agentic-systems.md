# Multi-LLM Providers and Agentic Systems in .NET

Reference for the `csharp-hexagonal-ddd` skill. Load only when the task touches this area.

## 9. Multi-LLM Provider & Native Agentic Systems Architecture (.NET 9+)

### 9.1. Multi-LLM Provider Architecture & Resilient Fallback Pattern
When implementing AI or LLM capabilities in .NET:
1. **Hexagonal Domain Port**: The Domain defines an interface (e.g. `IChatAdvisor`) without knowing about any AI SDK or provider.
2. **Universal OpenAI-Compatible REST Protocol**: Google Gemini, Groq, DeepSeek, Mistral, Ollama, and OpenAI all support `POST /chat/completions`. Use a generic HTTP adapter rather than provider-specific tight coupling.
3. **Resilient Fallback Chaining**:
   - Implement cascading provider attempts (e.g., Gemini 2.5 Flash -> Groq Llama 3.3 -> Local deterministic rules).
   - If an external provider returns an error (such as 404 deprecated model, 429 quota exhausted, or network timeout), log the event and fallback gracefully without throwing unhandled 500 exceptions to users.
4. **.NET Ecosystem Standard**: Utilize `Microsoft.Extensions.AI` (`IChatClient`) or Semantic Kernel connectors for enterprise model swappability.

### 9.2. Enterprise Agentic Systems Engineering in .NET (Visual Studio Magazine & Alon Fliess Blueprint)
Moving from passive chatbots to autonomous **Agentic Systems** in C#:
1. **The Cognitive Loop (Perceive -> Plan -> Tool Call -> Reflect)**:
   - Agents operate in an autonomous loop: parse intent, plan steps, invoke strongly-typed C# tools (`AIFunctionFactory.Create`), and reflect on tool results before final answer.
2. **Domain Protection & Security Guardrails**:
   - Agents NEVER execute raw SQL or directly mutate database tables.
   - All agent tools are driven through **Application Use Cases (Commands/Queries)** with `FluentValidation` and the `Result` pattern, guaranteeing illegal states cannot compile or execute.
3. **Agent Identity as First-Class Service Principals**:
   - Treat each agent as a dedicated system identity with least-privilege tokens (Supabase JWT or Entra ID) and distinct audit logging.
4. **Model Context Protocol (MCP) in .NET**:
   - Decouple tool registration via standardized MCP servers so agents can securely access local tools or external systems without tight code coupling.
5. **OpenTelemetry Agent Distributed Tracing**:
   - Trace agent reasoning loops, tool calls, latency, and token consumption using standard .NET `ActivitySource` exported to Grafana Tempo or Aspire Dashboard.
