# Regras Globais da Esteira de Desenvolvimento Agêntico (`AGENTS.md`)

Você opera dentro de uma **Esteira de Desenvolvimento de Software Agêntica Autônoma** de padrão Enterprise, otimizada para **MVP de Baixo Custo, Alta Escalabilidade, Qualidade Rigorosa e Segurança (OWASP)**.
Qualquer estrutura de projeto ou contexto fornecido nesta pasta deve ser processado seguindo rigorosamente este protocolo, sem ambiguidades, sem redundâncias e com foco absoluto em engenharia de software moderna.

---

## 1. Princípios Fundamentais de Engenharia e Código

1. **KISS (Keep It Simple, Stupid) & YAGNI (You Aren't Gonna Need It)**:
   - Construa apenas o que é necessário para resolver o problema atual com excelência. Proibido over-engineering, abstrações prematuras ou camadas inúteis.
2. **Clean Code & Legibilidade**:
   - Código é lido muito mais vezes do que escrito. Nomes de variáveis, métodos e classes devem revelar intenção clara em inglês técnico estrito (EN-US).
   - Métodos curtos (idealmente < 20 linhas), responsabilidade única (SRP/SOLID), early-returns (Guard Clauses) ao invés de `if/else` aninhados.
3. **Zero Ambiguidade & Proatividade**:
   - **NUNCA** codifique sobre premissas não validadas ou requisitos ambíguos.
   - Durante o planejamento e análise, levante **todas as Open Questions**, antecipe edge cases, falhas de rede, concorrência, limites de cota e vulnerabilidades antes de escrever código.
4. **Custo Mínimo para MVP e Escala (Buy/Delegate over Build)**:
   - Delegue commodities para ferramentas validadas de mercado com generosos Free Tiers:
     - **AuthN / AuthZ**: **Supabase Auth** (validação de JWT assimétrico JWKS no backend C# e SDK nativo no frontend). Nunca implemente tabelas de senhas ou fluxos de OAuth/MFA do zero.
     - **Banco de Dados**: **PostgreSQL** (via Supabase ou Neon/Docker local) com Row-Level Security (RLS) ou isolamento estrito na aplicação.
     - **Mensageria**: **RabbitMQ** (CloudAMQP no free tier ou container local).
     - **Cache**: `HybridCache` (.NET 9+) combinando `IMemoryCache` (L1 gratuito) e Redis/Upstash ou Postgres (L2 opcional quando necessário).
5. **Padrão Obrigatório de Idioma Técnico (English EN-US)**:
   - **TODO e qualquer artefato gerado ou mantido no repositório DEVE ser estritamente em INGLÊS (EN-US)**:
     - Código-fonte (nomes de projetos, namespaces, classes, métodos, records, enums, propriedades, variáveis).
     - Comentários de código e docstrings.
     - Mensagens de log estruturado e traces OpenTelemetry.
     - Testes automatizados (nomes de classes de teste, métodos de teste `Given_When_Then`, asserções e fixtures).
     - Documentação técnica (`docs/specs/`, `docs/architecture/`, ADRs, checklists de DoR/DoD e Tasks).
     - Mensagens de commit Git (formato Conventional Commits em EN-US).
     - Títulos e descrições de Pull Requests e Issues no GitHub.
   - *Nota de Comunicação*: A interação conversacional no chat direto com o usuário é mantida no idioma de preferência do usuário (Português), mas absolutamente **nenhum texto em português deve constar no código-fonte, testes, documentação do repositório, commits ou PRs**.

---

## 2. Arquitetura e Stack Tecnológica Oficial

### 2.1. Backend: Monolito Modular / Hexagonal em C# (.NET 8/9+)
- **Estilo Arquitetural**: **Arquitetura Hexagonal (Ports & Adapters)** aliada a **Domain-Driven Design (DDD)** e **Event-Driven Architecture (EDA)**.
- **API**: **ASP.NET Core Minimal APIs** (código mínimo, alta performance, pronto para Web e futuro App Mobile).
  - Endpoints organizados via padrão `IEndpoint` / `MapGroup`, retornando sempre `TypedResults` (`IResult`) e `ProblemDetails` (RFC 7807) para erros padronizados.
  - Documentação **OpenAPI / Scalar / Swagger** gerada estaticamente ou em runtime com tipagem forte para consumo do Frontend e Mobile.
- **Divisão Estrita de Camadas (Projetos ou Pastas Isoladas)**:
  1. `Domain` (Core): Entidades, Aggregate Roots, Value Objects (`readonly record struct` / `record`), Domain Events, Domain Errors e Interfaces de Portas de Saída (Repositories). **Zero dependências de frameworks externos** (sem EF Core, sem HTTP, sem RabbitMQ).
  2. `Application` (Use Cases): Commands, Queries, Handlers (CQRS leve sem sobrecarga desnecessária), DTOs/Contracts, Input Validators (**FluentValidation**), Output Ports. Retorno obrigatório via **Result Pattern** (`Result<T>` / `Error`) para fluxos de negócio — nunca lance exceções para erros de validação ou regras de domínio.
  3. `Infrastructure` (Driven Adapters): Persistência com **Entity Framework Core (PostgreSQL / Npgsql)** + **Dapper** (para queries de leitura complexas se necessário), **MassTransit** com **RabbitMQ** (incluindo **Transactional Outbox Pattern** e consumidores idempotentes), Cache (`HybridCache`), integração com **Supabase JWT**.
  4. `Api` (Driving Adapter): Minimal APIs, Middlewares globais (Exception Handler -> ProblemDetails, CorrelationId, Rate Limiting, CORS estrito, Security Headers), HealthChecks (`/health/live`, `/health/ready`).

### 2.2. Frontend: Monolito Web Moderno (React / Next.js + TypeScript)
- **Framework Recomendado de Mercado para Consumo de API**: **Next.js (App Router)** ou **React + Vite** com **TypeScript (`strict: true`)**.
- **Consumo de API & Estado Assíncrono**:
  - **TanStack Query v5 (React Query)**: Gerenciamento de cache no cliente, invalidação automática, paginação, retries, optimistic updates e deduplicação de requests.
  - **Geração Automática de Clientes/Tipos**: Uso de `openapi-typescript` + `openapi-fetch` (ou Orval) lendo o `openapi.json` do backend C# para garantir **100% de sincronia de tipos End-to-End** sem digitar interfaces manualmente.
- **Formulários e Validação**: **React Hook Form** + **Zod** (schemas espelhados nas regras da API).
- **UI & Estilização**: **Tailwind CSS** + **shadcn/ui** (acessível WAI-ARIA, sem custo de licença, customizável).
- **Autenticação**: `@supabase/ssr` / `@supabase/supabase-js` injetando automaticamente o `Bearer <access_token>` nas requisições para a API C#.

---

## 3. Fluxo da Esteira Agêntica (Discovery -> Spec -> DoR -> Code -> QA/Sec -> DoD -> PR)

Todo desenvolvimento deve respeitar os **Quality Gates** abaixo. Nenhum agente pode pular etapas:

### Gate 1: Discovery, Requisitos e Previsão de Riscos (`product-requirements-analyst`)
- Analisar a estrutura ou pedido do usuário.
- Levantar **Requisitos Funcionais (RF)**, **Requisitos Não Funcionais (RNF)** (SLAs, latência, throughput, custo, retenção de dados, compatibilidade Web/Mobile).
- Mapear **Riscos, Edge Cases e Problemas Previstos** com suas respectivas **Soluções Recomendadas**.
- Listar todas as **Open Questions** bloqueantes. Se houver ambiguidade crítica, questionar o usuário antes de prosseguir ou documentar a decisão arquitetural padrão (ADR).

### Gate 2: Arquitetura, Spec Técnica e Tasks (`domain-architect-planner`)
- Modelar os **Bounded Contexts**, **Aggregates**, **Value Objects**, **Domain Events** e **Integration Events (RabbitMQ)**.
- Definir contratos REST (rotas, payloads, status codes, idempotency keys) e schemas de banco PostgreSQL (índices, constraints).
- Quebrar o trabalho em **Tasks Atômicas** ordenadas por dependência.
- Validar a **Definition of Ready (DoR)** (ver `docs/pipeline/DEFINITION_OF_READY.md`). Nenhuma task vai para codificação sem cumprir 100% da DoR.

### Gate 3: Implementação Guiada por Testes (`backend-csharp-developer` & `frontend-web-developer`)
- Implementar seguindo estritamente a Spec e a Arquitetura Hexagonal.
- **Testes Automatizados Obrigatórios**:
  - **Backend (.NET)**: `xUnit` + `FluentAssertions` + `NSubstitute` (Unidade para Domain/Application) + `Testcontainers` (`PostgreSql`, `RabbitMq`) e `WebApplicationFactory` (Integração real da API e mensageria).
  - **Frontend (TS)**: `Vitest` + `React Testing Library` + `MSW (Mock Service Worker)` (Unidade/Integração de componentes e hooks de API) + `Playwright` (fluxos críticos E2E).

### Gate 4: Auditoria de Qualidade, Segurança OWASP e PR (`qa-security-reviewer` & `devops-ci-engineer`)
- Validar **OWASP Top 10** e **OWASP API Security Top 10** (BOLA/IDOR, Broken Auth, Mass Assignment, Injection, Rate Limiting, Secrets Exposure).
- Executar Linters e Formatadores (`dotnet format`, Roslyn Analyzers, `eslint`, `tsc --noEmit`).
- Validar a **Definition of Done (DoD)** (ver `docs/pipeline/DEFINITION_OF_DONE.md`).
- Gerar relatório rico de Pull Request contendo: Resumo Executivo, Arquitetura/Decisões, Evidência de Testes Executados, Checklist OWASP e Guia de Rollback.

---

## 4. Mapa de Agentes, Skills e Documentação da Pasta

Consulte e acione os recursos abaixo conforme a etapa do processo:

- **Agentes Especializados (`.agents/agents/`)**:
  - `pipeline-orchestrator`: Maestro da esteira (coordena do Discovery ao PR).
  - `product-requirements-analyst`: Especialista em Discovery, RF/RNF, Open Questions, Previsão de Falhas e DoR.
  - `domain-architect-planner`: Arquiteto DDD, Hexagonal, EDA (RabbitMQ), PostgreSQL, API REST e decomposição de Tasks.
  - `backend-csharp-developer`: Engenheiro C# .NET Minimal API, DDD, EF Core, MassTransit/RabbitMQ, Cache e xUnit/Testcontainers.
  - `frontend-web-developer`: Engenheiro React/Next.js, TypeScript, TanStack Query, Zod, Supabase Auth e Vitest/Playwright.
  - `qa-security-reviewer`: Auditor implacável de Clean Code, KISS, Testes, OWASP API Top 10 e revisor de PR.
  - `devops-ci-engineer`: Engenheiro de GitHub Actions (CI/CD), Docker, automação de qualidade e controle de custos.
- **Skills Operacionais (`.agents/skills/`)**:
  - `discovery-and-spec`: Como transformar uma ideia ou pasta em Spec técnica completa, RF/RNF, ADRs e Tasks sem ambiguidades.
  - `csharp-hexagonal-ddd`: Receitas de código e padrões para C# Minimal API, Arquitetura Hexagonal, Result Pattern, Outbox RabbitMQ, Cache e Supabase Auth.
  - `frontend-api-architecture`: Arquitetura Frontend moderna orientada a consumo de REST API, TanStack Query, OpenAPI codegen e testes com MSW/Vitest.
  - `owasp-quality-gate`: Checklist executável de segurança OWASP, lint, cobertura de testes e revisão de código automatizada.
  - `pr-delivery-automation`: Como preparar commits semânticos, rodar a bateria de validação local/CI e abrir PRs detalhados no GitHub.
  - `sre-observability-telemetry`: Padrões e runbooks de SRE, OpenTelemetry (Tracing, Metrics e Structured Logging), Grafana / Aspire Dashboard e RCA.
  - `run-agentic-pipeline`: Orquestração automatizada multi-agente de ponta a ponta.
- **Documentação e Templates (`docs/`)**:
  - `docs/pipeline/DEFINITION_OF_READY.md` & `docs/pipeline/DEFINITION_OF_DONE.md`
  - `docs/templates/SPEC_TEMPLATE.md`, `docs/templates/ADR_TEMPLATE.md`, `docs/templates/TASKS_TEMPLATE.md`
  - `docs/architecture/STACK_AND_DECISIONS.md`

