# Atomic Implementation Tasks: AI Contextual Financial Advisor (`TASKS-002`)

## Definition of Ready (DoR) Checklist
- [x] Clear business problem defined (Contextual AI chat grounded on MarketPulse MVP).
- [x] Functional & Non-Functional Requirements documented in `SPEC-002`.
- [x] Security, Secrets & LGPD privacy guardrails mapped.
- [x] Hexagonal layer dependencies decoupled.

---

## Task Decomposition

### Phase 1: Backend C# Domain & Application Layers
- [ ] **Task 1.1 (Domain)**: Create `IChatAdvisor` port interface and `AiChatMessage` value record in `App.Domain.MarketPulse.Ports` and `App.Domain.MarketPulse.Models`.
- [ ] **Task 1.2 (Application DTOs & Validation)**: Create `AskAiAdvisorRequest`, `AiAdvisorResponseDto`, and `AskAiAdvisorValidator` (FluentValidation) with message length guardrails (max 500 chars).
- [ ] **Task 1.3 (Application Query & Handler)**: Implement `AskAiAdvisorQuery` and `AskAiAdvisorQueryHandler` with live market grounding from `IExchangeRatesCacheService` / `HybridCache` and Result Pattern.

### Phase 2: Backend C# Infrastructure & Adapter
- [ ] **Task 2.1 (Infrastructure Adapter)**: Implement `AiAdvisorServiceAdapter` consuming `Microsoft.Extensions.AI` / OpenAI compatible client with strict Domain System Prompt and fallback resilience engine.
- [ ] **Task 2.2 (Dependency Injection)**: Register `IChatAdvisor`, configuration options (`Ai:ApiKey`, `Ai:ModelId`, `Ai:Endpoint`), and telemetry in `App.Infrastructure.DependencyInjection`.

### Phase 3: Backend C# Minimal API Driving Adapter
- [ ] **Task 3.1 (Endpoint)**: Implement `AiAdvisorEndpoints` mapped to `POST /api/v1/ai/chat` with rate limiting, OpenAPI docstrings, and RFC 7807 problem details.
- [ ] **Task 3.2 (Unit Tests)**: Add unit tests for `AskAiAdvisorQueryHandler` and domain guardrails in `App.UnitTests`.

### Phase 4: Frontend Web Implementation (Next.js 14)
- [ ] **Task 4.1 (API Hook & Schemas)**: Create `useAiChat` hook with TanStack Query mutation in `src/frontend/src/features/ai/api/use-ai-chat.ts`.
- [ ] **Task 4.2 (Chat Widget Component)**: Create floating `AiChatWidget` with chat bubbles, quick prompt chips, and accessible drawer.
- [ ] **Task 4.3 (Integration & Tests)**: Integrate `AiChatWidget` into main dashboard and add Vitest component tests.

### Phase 5: Verification & Deployment
- [ ] **Task 5.1 (CI/CD Verification)**: Run full backend and frontend validation suites (`dotnet test`, `npm run lint`, `npm run test`, `npm run build`).
- [ ] **Task 5.2 (E2E & Live Test)**: Verify in browser with test queries (in-scope vs off-topic query).
