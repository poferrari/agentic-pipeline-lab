# Technical Specification: AI Contextual Financial Advisor Chat (`SPEC-002`)

## 1. Executive Summary & Purpose

This specification defines the integration of an **AI-powered Contextual Financial Assistant ("Agentic FX Advisor")** into the MarketPulse platform. Using modern enterprise .NET AI abstractions (**`Microsoft.Extensions.AI`** / Semantic Kernel pattern) in the C# backend and an interactive chat widget in the Next.js 14 frontend, users can query real-time foreign exchange trends, ask how outbox alerts work, and receive contextual assistance.

Strict **System Guardrails** restrict the AI exclusively to the MarketPulse MVP domain (FX rates, currency conversion, watchlist threshold alerts, and system architecture). Any off-topic queries (sports, politics, unrelated general topics) are deterministically rejected with a polite disclaimer.

---

## 2. Functional Requirements (FR)

- **FR-01 (Contextual Chat Endpoint)**: `POST /api/v1/ai/chat` accepts user prompt messages and optional conversation history.
- **FR-02 (Domain Boundary Guardrails)**: AI must refuse off-topic inquiries outside MarketPulse domain (FX rates, alerts, system architecture).
- **FR-03 (Live Market Grounding)**: Ground AI responses with current cached exchange rates from `HybridCache` and active user alert counts.
- **FR-04 (Secrets & Provider Configuration)**: LLM API credentials (`Ai:ApiKey`, `Ai:ModelId`, `Ai:Endpoint`) configured via environment secrets (`AI__ApiKey`), with zero hardcoded keys.
- **FR-05 (Resilient Offline Fallback)**: If no external LLM key is configured (local dev / CI), a deterministic Domain Knowledge Assistant responds accurately without throwing 500 errors.
- **FR-06 (Frontend Interactive Widget)**: Floating chat drawer in Next.js with typing indicators, suggested quick prompt chips, and message history.

---

## 3. Non-Functional Requirements (NFR)

- **NFR-01 (Architecture Purity)**: Strict Hexagonal Ports & Adapters:
  - `App.Domain`: `IChatAdvisor` outbound port and `ChatMessage` domain models.
  - `App.Application`: `AskAiAdvisorQuery` / `AskAiAdvisorHandler` returning `Result<AiAdvisorResponseDto>`.
  - `App.Infrastructure`: `MicrosoftExtensionsAiAdvisorAdapter` (implements `IChatAdvisor` via `Microsoft.Extensions.AI.IChatClient` or OpenAI/Groq compatible REST client).
  - `App.Api`: Minimal API endpoint organized via `IEndpoint` with RFC 7807 ProblemDetails and Rate Limiting.
- **NFR-02 (Performance & Latency)**: Response generation p95 < 2.5s; rate limited to 15 req/min per IP/user to prevent token abuse.
- **NFR-03 (Zero PII Exposure - LGPD)**: Prompts and AI outputs scrubbed of customer PII before logging.

---

## 4. API Contract & OpenAPI Specification

### `POST /api/v1/ai/chat`
**Request Body (`AskAiAdvisorRequest`):**
```json
{
  "message": "What is the current exchange rate for USD to BRL and how do alerts work?",
  "history": [
    { "role": "user", "content": "Hello" },
    { "role": "assistant", "content": "Hello! I am your Agentic FX Advisor..." }
  ]
}
```

**Response Body (`AiAdvisorResponseDto`):**
```json
{
  "reply": "The latest European Central Bank rate for USD/BRL is 5.3516...",
  "suggestedActions": ["View USD/BRL rate", "Create threshold alert"],
  "modelUsed": "gpt-4o-mini",
  "generatedAtUtc": "2026-10-30T02:40:00Z"
}
```
