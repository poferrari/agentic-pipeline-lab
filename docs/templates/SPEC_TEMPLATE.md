# Especificação Técnica e Funcional (Spec): `[NOME_DA_FEATURE_OU_PROJETO]`

> **Status**: `[DRAFT | IN_REVIEW | READY_FOR_DEV | IMPLEMENTED]`
> **Data**: `YYYY-MM-DD`
> **Autor(es)**: `product-requirements-analyst` & `domain-architect-planner`

---

## 1. Contexto, Problema e Objetivo de Negócio (MVP)
- **Problema**: Qual dor real estamos resolvendo?
- **Objetivo do MVP**: Qual é a entrega mínima de alto valor e baixo custo que valida esta funcionalidade?
- **Fora de Escopo (Non-Goals / YAGNI)**: O que decidimos explicitamente **NÃO** construir agora para manter a simplicidade (KISS)?

---

## 2. Open Questions, Premissas e Decisões
> **Regra**: Nenhuma task pode iniciar desenvolvimento se houver `[OPEN QUESTION]` bloqueante sem resposta.

| ID | Pergunta / Ponto de Atenção | Status (`OPEN` / `RESOLVED`) | Decisão / Resposta Acordada |
| :--- | :--- | :--- | :--- |
| `OQ-01` | Ex: Qual o limite máximo de tentativas antes de bloquear? | `RESOLVED` | Definido em 5 tentativas em 15 minutos via Rate Limiter. |

---

## 3. Requisitos Funcionais (RF) e Critérios de Aceite (BDD)

### `RF-001`: [Título Conciso do Requisito]
- **Descrição**: O sistema deve...
- **Atores / Permissões (Supabase Auth)**: `[Anônimo | Usuário Autenticado | Admin]`
- **Critérios de Aceite (Gherkin)**:
  - **Cenário 1 ( Caminho Feliz )**:
    - **Dado** que...
    - **Quando**...
    - **Então**...
  - **Cenário 2 ( Regra de Negócio / Falha Esperada )**:
    - **Dado** que...
    - **Quando**...
    - **Então** deve retornar status `400/422` com código de erro `Dominio.RegraViolada`.

---

## 4. Requisitos Não Funcionais (RNF) e Restrições
- **Performance & Latência**: `p95 < 200ms` para leituras REST; processamento pesado assíncrono via RabbitMQ.
- **Escalabilidade & Compatibilidade**: API stateless pronta para consumo simultâneo por Web e App Mobile.
- **Segurança (OWASP)**: Proteção contra BOLA/IDOR (filtro obrigatório por `UserId` extraído do JWT), Rate Limiting, validação estrita de payload.
- **Custo (FinOps)**: Uso exclusivo dos recursos de banco (PostgreSQL), fila (RabbitMQ) e Auth (Supabase) já provisionados.

---

## 5. Análise Preventiva de Riscos, Edge Cases e Soluções

| ID | Cenário de Falha / Edge Case Previsto | Impacto | Solução Técnica Incorporada na Spec |
| :--- | :--- | :--- | :--- |
| `RISK-01` | Duplo clique do usuário ou retry de rede gerando duplicidade | Alto | Uso de `Idempotency-Key` ou constraint única no PostgreSQL. |
| `RISK-02` | Banco salva transação, mas RabbitMQ está fora do ar no momento do publish | Alto | Uso obrigatório de **Transactional Outbox Pattern** com MassTransit + EF Core. |
| `RISK-03` | Usuário tenta acessar ID de recurso pertencente a outro tenant/usuário (IDOR) | Crítico | Query do repositório filtra por `(Id == request.Id && OwnerId == currentUser.Id)`. |

---

## 6. Modelagem de Domínio (DDD) e Arquitetura Hexagonal

### 6.1. Bounded Context & Linguagem Ubíqua
- **Contexto**: `[NomeDoContexto]`
- **Aggregate Root**: `[NomeDoAggregate]`
  - **Propriedades e Value Objects**: Ex: `Email`, `Money`, `Cpf`, `OrderId` (`readonly record struct`).
  - **Invariantes (Regras Protegidas)**:
    1. `[Regra 1]`
    2. `[Regra 2]`
  - **Domain Events Disparados**: `[Ex: OrderCreatedDomainEvent]`

### 6.2. Portas (Interfaces) e Caso de Uso (Application Layer)
- **Driving Ports (Commands / Queries)**:
  - `CreateXCommand(string Prop1, decimal Prop2) : ICommand<Result<CreateXResponse>>`
  - `GetXByIdQuery(Guid Id) : IQuery<Result<XDto>>`
- **Validators (`FluentValidation`)**:
  - `Prop1`: `NotEmpty()`, `MaximumLength(100)`
- **Driven Ports (Interfaces no Domain/Application)**:
  - `IXRepository`
  - `IUnitOfWork`

---

## 7. Contratos de API REST (Web & Mobile)

### `POST /api/v1/[recurso]`
- **Autenticação**: `Bearer <Supabase_JWT>` (`RequireAuthorization`)
- **Request Body (`application/json`)**:
  ```json
  {
    "fieldA": "string",
    "fieldB": 123.45
  }
  ```
- **Responses**:
  - `201 Created`: Retorna `Location` header e payload do recurso criado.
  - `400 Bad Request` / `422 Unprocessable Entity`: Retorna `ProblemDetails` (RFC 7807) com dicionário de erros de validação.
  - `401 Unauthorized` / `403 Forbidden`.
  - `409 Conflict`: Recurso duplicado.

---

## 8. Mensageria Assíncrona (RabbitMQ) e Cache

### 8.1. Eventos de Integração (RabbitMQ / MassTransit)
- **Evento**: `[NomeDoEventoIntegrationEvent]`
- **Exchange / Queue**: `[nome-da-fila]`
- **Produtor**: `[Comando que publica via Outbox]`
- **Consumidor**: `[Consumer que processa]`
- **Garantia de Idempotência**: `[Como o consumidor evita processar 2x a mesma mensagem]`
- **Política de Retry & DLQ**: `3 retries exponenciais (2s, 5s, 15s) -> fila _error`.

### 8.2. Estratégia de Cache (`HybridCache`)
- **Chave de Cache**: `"[contexto]:[entidade]:{id}"`
- **TTL**: `L1 = 5 min`, `L2 = 15 min`
- **Invalidação**: Remover chave ao executar `Update` ou `Delete`.

---

## 9. Modelo de Dados (PostgreSQL)

```sql
-- Representação da tabela e índices obrigatórios
CREATE TABLE nome_tabela (
    id UUID PRIMARY KEY,
    owner_id UUID NOT NULL,
    status VARCHAR(30) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL
);

CREATE INDEX idx_nome_tabela_owner_id ON nome_tabela (owner_id);
```

---

## 10. Arquitetura Frontend (Next.js / React + TanStack Query)
- **Rotas / Páginas**: `/[rota]`
- **Componentes UI**: `[Lista de componentes limpos e reutilizáveis]`
- **Hooks TanStack Query**:
  - `useGet[Recurso](id)` (`queryKey: ['recurso', id]`)
  - `useCreate[Recurso]()` (invalida `['recurso']` no `onSuccess`)
- **Estados de Interface Obrigatórios**:
  - Skeleton/Loading state
  - Empty state (quando lista vazia)
  - Error state amigável (lendo `ProblemDetails` da API)
  - Feedback visual de sucesso (Toast)

---

## 11. Plano de Testes Automatizados

| Camada | Tipo de Teste | Ferramenta | Cenários Obrigatórios a Testar |
| :--- | :--- | :--- | :--- |
| **Backend Domain** | Unidade | `xUnit` + `FluentAssertions` | Criação válida do Aggregate, violação de cada invariante, igualdade de Value Objects. |
| **Backend App** | Unidade | `xUnit` + `NSubstitute` | Sucesso do Handler, falha de validação FluentValidation, retorno de `Result.Failure`. |
| **Backend Arch** | Arquitetura | `NetArchTest` | `Domain` não referencia `Infrastructure` nem `Api`. |
| **Backend API/Infra** | Integração | `Testcontainers` + `WebApplicationFactory` | POST `201` persistindo no Postgres, publicação no Outbox/RabbitMQ, bloqueio `401`/`403` BOLA. |
| **Frontend** | Unidade / Integração | `Vitest` + `Testing Library` + `MSW` | Renderização de dados, validação de formulário Zod, tratamento de erro 400/500 da API. |
| **End-to-End** | E2E | `Playwright` | Jornada crítica completa do usuário. |

---

## 12. Validação do Definition of Ready (DoR)
- [ ] Todos os itens de `docs/pipeline/DEFINITION_OF_READY.md` foram conferidos e aprovados.

