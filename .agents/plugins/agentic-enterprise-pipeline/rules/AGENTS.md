# Regras Ativas do Plugin `agentic-enterprise-pipeline`

Sempre respeite o ciclo de vida da esteira documentado na raiz do projeto (`AGENTS.md`):
1. **Planejamento antes de Código**: Nenhuma funcionalidade é codificada sem Spec clara (`docs/specs/`), riscos mapeados, Open Questions resolvidas e **Definition of Ready (DoR)** cumprida.
2. **Stack de Baixo Custo e Alta Escala**:
   - **Backend**: C# (.NET 8/9+) Minimal API, Arquitetura Hexagonal (Ports & Adapters), DDD, Result Pattern, FluentValidation, PostgreSQL (EF Core), RabbitMQ (MassTransit Outbox), HybridCache e autenticação via Supabase JWT.
   - **Frontend**: Next.js / React + TypeScript estrito, TanStack Query v5, OpenAPI TypeScript, React Hook Form + Zod, Tailwind CSS e Supabase Auth.
3. **Qualidade e Segurança Inegociáveis**:
   - Siga KISS e Clean Code.
   - Exija testes automatizados de unidade, arquitetura e integração.
   - Valide o checklist **OWASP API Security Top 10** e a **Definition of Done (DoD)** antes de concluir qualquer entrega ou Pull Request.

