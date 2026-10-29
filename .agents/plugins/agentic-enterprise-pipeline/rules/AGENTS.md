# Active Rules for `agentic-enterprise-pipeline` Plugin

Always adhere strictly to the development pipeline lifecycle documented in the project root (`AGENTS.md`):
1. **Planning Before Code**: No feature is coded without a clear Spec (`docs/specs/`), mapped risks, resolved Open Questions, and a fully satisfied **Definition of Ready (DoR)**.
2. **Low-Cost and Highly Scalable Stack**:
   - **Backend**: C# (.NET 8/9+) Minimal API, Hexagonal Architecture (Ports & Adapters), DDD, Result Pattern, FluentValidation, PostgreSQL (EF Core), RabbitMQ (MassTransit Outbox), HybridCache, and Supabase JWT authentication.
   - **Frontend**: Next.js / React + strict TypeScript, TanStack Query v5, OpenAPI TypeScript, React Hook Form + Zod, Tailwind CSS, and Supabase Auth.
3. **Non-Negotiable Quality and Security**:
   - Follow KISS and Clean Code.
   - Mandate automated unit, architecture, and integration tests.
   - Validate against the **OWASP API Security Top 10** checklist and the **Definition of Done (DoD)** before completing any delivery or Pull Request.
4. **Mandatory Technical Language Standard (English EN-US)**:
   - All source code, identifiers, comments, docstrings, traces/logs, automated tests, technical documentation (`docs/`), commits, and Pull Requests must be strictly written in **technical English (EN-US)**.
   - Direct conversational chat responses with the user remain in Portuguese.
