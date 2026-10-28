# Definition of Done (DoD) — Critérios de Pronto para Entrega e Pull Request

Nenhuma Task, Feature ou Pull Request é considerada **Pronta (Done)** sem que **100% dos itens aplicáveis abaixo** tenham sido verificados de forma automatizada e auditados pelo agente `qa-security-reviewer`.

---

## 1. Qualidade de Código e Arquitetura (KISS, Clean Code, Hexagonal)
- [ ] **Isolamento Hexagonal e DDD**:
  - A camada `Domain` não possui referências a pacotes de infraestrutura (`Microsoft.EntityFrameworkCore`, `MassTransit`, `AspNetCore`, etc.).
  - Entidades e Aggregates protegem suas invariantes (construtores privados/factory methods, coleções expostas como `IReadOnlyCollection<T>`).
  - Erros de negócio utilizam o **Result Pattern** (`Result<T>`) e retornam `ProblemDetails` (RFC 7807) na Minimal API; nenhuma exceção é lançada para controle de fluxo esperado.
- [ ] **KISS & Clean Code**:
  - Nenhum código morto, comentado, `TODO` sem issue vinculada ou `console.log` / `Console.WriteLine` esquecido.
  - Métodos coesos, nomes claros, zero duplicação desnecessária (DRY equilibrado com KISS).
- [ ] **Zero Warnings de Compilação e Linter**:
  - Backend: `dotnet build --configuration Release -warnaserror` e `dotnet format --verify-no-changes` passam com **0 erros e 0 warnings**.
  - Frontend: `npm run lint`, `npm run typecheck` (`tsc --noEmit`) e `npm run build` passam com **0 erros e 0 warnings**.

## 2. Testes Automatizados (Unidade, Integração e Contrato)
- [ ] **Testes de Unidade (Backend & Frontend)**:
  - Todos os testes de unidade passam (`dotnet test --filter Category=Unit` e `npm run test:unit`).
  - Regras de domínio, Value Objects, Command/Query Handlers e Validators possuem cobertura >= 85% nos novos módulos.
- [ ] **Testes de Integração**:
  - Backend: Endpoints da Minimal API, persistência no PostgreSQL e publicação/consumo no RabbitMQ foram testados via `WebApplicationFactory` + `Testcontainers` (ou ambiente de teste isolado).
  - Frontend: Componentes, páginas e hooks do `TanStack Query` foram testados com `Vitest` + `React Testing Library` + `MSW` cobrindo estados de `loading`, `success`, `empty` e `error`.
- [ ] **Sincronia de Contratos (Web & Mobile Ready)**:
  - A especificação OpenAPI/Swagger está atualizada e sincronizada com os tipos TypeScript do frontend.

## 3. Resiliência, Mensageria e Performance
- [ ] **Idempotência e Consistência (RabbitMQ & DB)**:
  - Publicação de eventos utiliza o **Transactional Outbox Pattern** (ou garantia transacional equivalente).
  - Consumidores RabbitMQ são idempotentes e possuem tratamento de retry com backoff exponencial.
- [ ] **Queries e Cache Otimizados**:
  - Queries EF Core de leitura usam `.AsNoTracking()`, projeção (`.Select()`) e paginação (`PageSize` limitado). Nenhuma query N+1 introduzida.
  - Cache (`HybridCache` / `TanStack Query`) configurado com TTL e invalidação correta em mutações.

## 4. Segurança OWASP (API & Web Top 10)
- [ ] **API1:2023 - Broken Object Level Authorization (BOLA/IDOR)**: Todas as rotas que acessam recursos por ID validam que o usuário autenticado (JWT Supabase) tem permissão sobre aquele registro específico.
- [ ] **API2:2023 & API5:2023 - Broken Authentication & Function Level Authorization**: Endpoints protegidos possuem `.RequireAuthorization()` explícito (ou política específica); rotas públicas possuem `.AllowAnonymous()` intencional e justificado.
- [ ] **API3:2023 - Broken Object Property Level Authorization (Mass Assignment)**: Endpoints nunca recebem entidades de banco (`DbContext`) diretamente no Request Body; utilizam exclusivamente Request DTOs/Records imutáveis.
- [ ] **API4:2023 - Unrestricted Resource Consumption**: Paginação máxima imposta (ex.: `MaxPageSize = 100`) e Rate Limiting ativo.
- [ ] **API8:2023 - Security Misconfiguration & Secrets**:
  - Nenhuma credencial, chave do Supabase, connection string ou segredo hardcoded no código ou versionado no Git.
  - Logs estruturados não vazam PII (dados pessoais sensíveis), senhas ou tokens JWT.

## 5. CI/CD e Documentação do Pull Request
- [ ] **Pipeline GitHub Actions Verde**: Todos os jobs de build, lint, testes, análise estática (CodeQL / Dependency Review / Trivy) estão configurados e passando.
- [ ] **Migrations Seguras**: Se houve alteração de banco, a migration do EF Core foi gerada, revisada e testada (up e down).
- [ ] **Relatório de Pull Request Completo**: O PR contém a descrição detalhada gerada pela skill `pr-delivery-automation` (O que foi feito, Decisões Arquiteturais, Output real dos testes executados, Checklist OWASP e Instruções de Deploy/Rollback).

