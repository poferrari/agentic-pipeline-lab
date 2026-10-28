# Plano de Execução e Decomposição de Tasks: `[NOME_DA_FEATURE]`

> **Spec de Referência**: `docs/specs/[nome-da-spec].md`
> **Regra de Execução**: As tasks são atômicas e sequenciais. Um agente só avança para a próxima task quando o **Comando de Verificação** da task atual passar com **0 erros**.

---

## Resumo de Progresso

| Task ID | Camada | Título da Task | Agente Responsável | Status (`TODO` \| `IN_PROGRESS` \| `DONE`) |
| :--- | :--- | :--- | :--- | :--- |
| `TASK-01` | Domain | Modelar Aggregate, Value Objects, Domain Events e Testes de Unidade | `backend-csharp-developer` | `TODO` |
| `TASK-02` | Application | Implementar Commands, Queries, Validators (FluentValidation) e Testes | `backend-csharp-developer` | `TODO` |
| `TASK-03` | Infrastructure | Mapear EF Core (PostgreSQL), Migrations, Outbox RabbitMQ, Cache e Auth | `backend-csharp-developer` | `TODO` |
| `TASK-04` | API & Integration | Expor Minimal API Endpoints, OpenAPI e Testes de Integração (Testcontainers) | `backend-csharp-developer` | `TODO` |
| `TASK-05` | Frontend | Gerar tipos OpenAPI, criar Hooks TanStack Query, UI, Zod Forms e Testes MSW | `frontend-web-developer` | `TODO` |
| `TASK-06` | QA & Security | Executar Auditoria OWASP, Testes de Arquitetura, Linter e Relatório de PR | `qa-security-reviewer` | `TODO` |

---

## Detalhamento das Tasks Atômicas

### `TASK-01`: Modelagem do Core de Domínio e Testes de Unidade
- **Objetivo**: Implementar as entidades, Value Objects, erros de domínio e interfaces de repositório sem dependências externas.
- **Arquivos a Criar/Modificar**:
  - `src/backend/src/App.Domain/[Contexto]/[Aggregate].cs`
  - `src/backend/src/App.Domain/[Contexto]/ValueObjects/[ValueObject].cs`
  - `src/backend/src/App.Domain/[Contexto]/I[Aggregate]Repository.cs`
  - `src/backend/tests/App.UnitTests/Domain/[Aggregate]Tests.cs`
- **Critérios de Pronto da Task**:
  - Construtor privado com Factory Method retornando `Result<T>`.
  - 100% das invariantes cobertas por testes de unidade em `xUnit`.
- **Comando de Verificação**:
  ```powershell
  dotnet test src/backend/tests/App.UnitTests --filter "FullyQualifiedName~Domain"
  ```

### `TASK-02`: Casos de Uso (Application), Validações e Testes de Handlers
- **Objetivo**: Implementar Command/Query Handlers, `FluentValidation` e contratos DTO.
- **Arquivos a Criar/Modificar**:
  - `src/backend/src/App.Application/[Contexto]/[Comando]/[Comando]Command.cs`
  - `src/backend/src/App.Application/[Contexto]/[Comando]/[Comando]Validator.cs`
  - `src/backend/src/App.Application/[Contexto]/[Comando]/[Comando]Handler.cs`
  - `src/backend/tests/App.UnitTests/Application/[Comando]HandlerTests.cs`
- **Comando de Verificação**:
  ```powershell
  dotnet test src/backend/tests/App.UnitTests --filter "FullyQualifiedName~Application"
  ```

### `TASK-03`: Infraestrutura (EF Core PostgreSQL, RabbitMQ MassTransit, HybridCache)
- **Objetivo**: Implementar os Driven Adapters (`IEntityTypeConfiguration`, Repositories, Consumers RabbitMQ com Outbox).
- **Arquivos a Criar/Modificar**:
  - `src/backend/src/App.Infrastructure/Persistence/Configurations/[Aggregate]Configuration.cs`
  - `src/backend/src/App.Infrastructure/Persistence/Repositories/[Aggregate]Repository.cs`
  - `src/backend/src/App.Infrastructure/Messaging/[Evento]Consumer.cs`
- **Comando de Verificação**:
  ```powershell
  dotnet build src/backend -warnaserror
  ```

### `TASK-04`: Minimal API Endpoints, Segurança JWT e Testes de Integração
- **Objetivo**: Expor rotas HTTP em Minimal API com `TypedResults`, `.RequireAuthorization()`, `ProblemDetails` e validar com `Testcontainers`.
- **Arquivos a Criar/Modificar**:
  - `src/backend/src/App.Api/Endpoints/[Contexto]Endpoints.cs`
  - `src/backend/tests/App.IntegrationTests/[Contexto]/[Contexto]ApiTests.cs`
  - `src/backend/tests/App.ArchTests/HexagonalArchitectureTests.cs`
- **Comando de Verificação**:
  ```powershell
  dotnet test src/backend
  ```

### `TASK-05`: Integração Frontend (Tipos OpenAPI, TanStack Query, Componentes e Vitest)
- **Objetivo**: Sincronizar contrato OpenAPI, criar hooks de busca/mutação, telas e testes com `Vitest` + `MSW`.
- **Arquivos a Criar/Modificar**:
  - `src/frontend/src/features/[contexto]/api/use[Recurso].ts`
  - `src/frontend/src/features/[contexto]/components/[Componente].tsx`
  - `src/frontend/src/features/[contexto]/__tests__/[Componente].test.tsx`
- **Comando de Verificação**:
  ```powershell
  npm --prefix src/frontend run lint; npm --prefix src/frontend run typecheck; npm --prefix src/frontend run test:run
  ```

### `TASK-06`: Quality Gate Final, Revisão OWASP e Preparação de Pull Request
- **Objetivo**: Validar toda a Definition of Done (DoD), checar vulnerabilidades OWASP Top 10 e gerar o documento/PR completo.
- **Comando de Verificação**:
  ```powershell
  pwsh -File scripts/validate-all.ps1
  ```

