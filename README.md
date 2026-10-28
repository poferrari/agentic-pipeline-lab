# 🚀 Esteira de Desenvolvimento Agêntico Autônoma (Enterprise MVP & Scale-Ready)

Bem-vindo à base da **Esteira de Desenvolvimento de Software Agêntico**.
Esta pasta está pré-configurada com **Agentes Especializados, Subagentes, Skills, Plugins, Hooks de Segurança, Templates de Especificação (DoR/DoD) e Automação de CI/CD (GitHub Actions)** para transformar qualquer contexto de negócio ou estrutura de projeto em software de produção com **custo mínimo (MVP)**, **zero ambiguidade** e **qualidade enterprise**.

---

## 🏗️ 1. Stack Tecnológica Oficial e Estratégia de Baixo Custo

| Camada | Tecnologia Escolhida | Justificativa de Mercado (MVP Low-Cost & Scale) |
| :--- | :--- | :--- |
| **Arquitetura Geral** | **Monolito Modular (Backend + Frontend)** | Simplicidade operacional (KISS), zero custo de rede entre microsserviços prematuros, pronto para escalar horizontalmente. |
| **Backend API** | **C# (.NET 8/9+) Minimal APIs** | Código mínimo, altíssima performance, contratos **OpenAPI v1** nativos para atender **Web** hoje e **App Mobile** no futuro. |
| **Design Backend** | **Arquitetura Hexagonal (Ports & Adapters) + DDD + Result Pattern** | `Domain` 100% puro e isolado de infraestrutura; `FluentValidation` na entrada; zero exceções para fluxo de negócio. |
| **Mensageria (EDA)** | **RabbitMQ + MassTransit (Transactional Outbox)** | Processamento assíncrono resiliente, consumidores idempotentes, retries exponenciais e Dead-Letter Queues (DLQ). |
| **Banco & Cache** | **PostgreSQL 17 (EF Core) + .NET `HybridCache`** | Banco relacional líder open-source + cache L1 em memória gratuito (protegido contra *Cache Stampede*) pronto para Redis L2. |
| **AuthN / AuthZ** | **Supabase Auth (JWT Assimétrico JWKS)** | Delega login, registro, OAuth, MFA e recuperação de senha para ferramenta líder de mercado com Free Tier de até 50.000 MAUs. |
| **Frontend Web** | **Next.js (React) + TypeScript (`strict`) + TanStack Query v5 + Zod** | Stack número 1 do mercado para consumo de REST APIs: cache declarativo, tipos sincronizados via `openapi-typescript`, UI com Tailwind + `shadcn/ui`. |
| **Testes & Qualidade** | **xUnit, NetArchTest, Testcontainers, Vitest, MSW, Playwright** | Pirâmide completa: Unidade, Arquitetura Hexagonal, Integração real (Postgres/RabbitMQ) e Mock de rede no Frontend. |
| **Segurança & CI/CD** | **GitHub Actions + OWASP API Top 10 + Trivy + Dependabot** | Validação automática de build, linter, testes, vulnerabilidades e relatório completo no Pull Request. |

---

## 🤖 2. Ecossistema Agêntico Configurado Nesta Pasta

### Agentes e Subagentes (`.agents/agents/`)
1. **[`pipeline-orchestrator`](.agents/agents/pipeline-orchestrator.md)**: Maestro que coordena todas as fases do início ao Pull Request.
2. **[`product-requirements-analyst`](.agents/agents/product-requirements-analyst.md)**: Analisa o projeto, levanta Requisitos Funcionais (RF) e Não Funcionais (RNF), prevê falhas/edge cases com soluções e elimina todas as *Open Questions*.
3. **[`domain-architect-planner`](.agents/agents/domain-architect-planner.md)**: Modela DDD (Aggregates, Value Objects, Domain Events), Portas Hexagonais, contratos REST/RabbitMQ/SQL, decompõe em Tasks atômicas e valida a **Definition of Ready (DoR)**.
4. **[`backend-csharp-developer`](.agents/agents/backend-csharp-developer.md)**: Implementa o Backend C# Minimal API seguindo KISS, Clean Code, Hexagonal, DDD, EF Core, MassTransit RabbitMQ, HybridCache, Supabase JWT e testes (`xUnit` + `Testcontainers`).
5. **[`frontend-web-developer`](.agents/agents/frontend-web-developer.md)**: Implementa o Frontend React/Next.js + TypeScript, hooks `TanStack Query v5`, formulários `Zod` e testes (`Vitest` + `MSW`).
6. **[`qa-security-reviewer`](.agents/agents/qa-security-reviewer.md)**: Audita Clean Code, cobertura de testes, **OWASP API Security Top 10** (BOLA/IDOR, Mass Assignment, Auth, Rate Limit), valida a **Definition of Done (DoD)** e redige o relatório de Pull Request.
7. **[`devops-ci-engineer`](.agents/agents/devops-ci-engineer.md)**: Mantém os pipelines GitHub Actions, Docker Compose e otimização de custos (FinOps).

### Skills Operacionais (`.agents/skills/`)
- **[`discovery-and-spec`](.agents/skills/discovery-and-spec/SKILL.md)**: Guia de Discovery proativo, matriz de previsão de falhas e geração de Spec + Tasks.
- **[`csharp-hexagonal-ddd`](.agents/skills/csharp-hexagonal-ddd/SKILL.md)**: Blueprints e receitas de código C# Minimal API, Result Pattern, Outbox RabbitMQ, HybridCache e Supabase JWT.
- **[`frontend-api-architecture`](.agents/skills/frontend-api-architecture/SKILL.md)**: Blueprints de consumo de REST API com TanStack Query v5, `ProblemDetails` RFC 7807 e testes MSW.
- **[`owasp-quality-gate`](.agents/skills/owasp-quality-gate/SKILL.md)**: Checklist executável de segurança OWASP Top 10 e qualidade de código.
- **[`pr-delivery-automation`](.agents/skills/pr-delivery-automation/SKILL.md)**: Padronização de commits e geração de relatórios ricos de Pull Request.
- **[`run-agentic-pipeline`](.agents/skills/run-agentic-pipeline/SKILL.md)**: Orquestrador completo (inclui script dinâmico em [`.agents/skills/run-agentic-pipeline/scripts/pipeline_workflow.py`](.agents/skills/run-agentic-pipeline/scripts/pipeline_workflow.py)).

---

## 🔄 3. Como Usar a Esteira no Dia a Dia

1. **Coloque o contexto do seu projeto ou feature nesta pasta** (ou descreva no chat o que deseja construir).
2. **Peça ao agente para iniciar a esteira**:
   > *"Analise o contexto desta pasta usando a esteira agêntica, levante todos os requisitos (RF/RNF), preveja problemas e soluções, resolva as Open Questions, valide o DoR e gere a Spec e as Tasks."*
3. **Execução Autônoma**:
   - Após validar a Spec (zero ambiguidade), peça:
   > *"Siga o plano de Tasks de ponta a ponta (Backend C# + Frontend + Testes + Auditoria OWASP) até cumprir o DoD e preparar o Pull Request."*
4. **Validação Local Instantânea**:
   ```powershell
   pwsh -File scripts/validate-all.ps1
   ```
5. **Subir Infraestrutura Local (PostgreSQL 17 + RabbitMQ 4)**:
   ```powershell
   docker compose up -d
   ```

