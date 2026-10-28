# Plano de Execução e Decomposição de Tasks: `MarketPulse Lab`

> **Spec de Referência**: `docs/specs/market-pulse-lab-spec.md`
> **ADR de Referência**: `docs/architecture/ADR-001-hexagonal-public-api-and-render-containers.md`

---

## Resumo de Progresso

| Task ID | Camada | Título da Task | Agente Responsável | Status |
| :--- | :--- | :--- | :--- | :--- |
| `TASK-01` | Solution & Domain | Criar Solution .NET 9, `App.Domain` (Aggregate, Value Objects, Events, Ports) e `App.UnitTests` (Domain) | `backend-csharp-developer` | `DONE` |
| `TASK-02` | Application | Implementar `App.Application` (Commands, Queries, DTOs, FluentValidation, Handlers) e `App.UnitTests` (Application) | `backend-csharp-developer` | `DONE` |
| `TASK-03` | Infrastructure | Implementar `App.Infrastructure` (`FrankfurterExchangeAdapter` + `HybridCache`, EF Core Postgres/InMemory, MassTransit RabbitMQ/InMemory, Auth) | `backend-csharp-developer` | `DONE` |
| `TASK-04` | API, Arch & Integration | Implementar `App.Api` (Minimal APIs, OpenAPI, Rate Limit, OWASP Headers), `App.ArchTests` e `App.IntegrationTests` | `backend-csharp-developer` | `DONE` |
| `TASK-05` | Container & CI/CD | Configurar `Dockerfile` multi-stage para Render (`PORT=8080`), `render.yaml` e workflow GitHub Actions | `devops-ci-engineer` | `DONE` |
| `TASK-06` | QA, OWASP & Deploy | Executar Auditoria OWASP API Top 10, validar DoD, publicar no GitHub `poferrari/agentic-pipeline-lab` e Render | `qa-security-reviewer` | `DONE` |
