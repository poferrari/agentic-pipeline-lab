# Especificação Técnica e Funcional (Spec): `MarketPulse Lab (Intelligent Currency & Market Watchlist Hub)`

> **Status**: `READY_FOR_DEV`
> **Data**: `2026-10-28`
> **Autor(es)**: `product-requirements-analyst` & `domain-architect-planner`
> **Repositório Alvo**: `https://github.com/poferrari/agentic-pipeline-lab`
> **Ambiente Cloud / Deploy**: `Render Containers (prj-d6rakhshg0os73a5ahqg / evm-d6rakhshg0os73a5ahr0)`

---

## 1. Contexto, Problema e Objetivo de Negócio (MVP)
- **Problema**: Investidores, importadores, nômades digitais e operadores financeiros precisam monitorar taxas de câmbio oficiais (ex.: `USD/BRL`, `EUR/BRL`, `EUR/USD`, `GBP/BRL`) em tempo real com base no Banco Central Europeu (BCE) e cadastrar alertas personalizados de cotação-alvo (`TargetRate`) com cálculo automático de spread/variação percentual e disparo de eventos assíncronos quando a meta é atingida, sem pagar licenças caras de terminais financeiros proprietários.
- **Objetivo do MVP**: Entregar uma plataforma **REST API + Backend Monolito Modular Hexagonal em C# (.NET 9 Minimal API)** que:
  1. Consuma em tempo real a **API Pública Real sem chave paga Frankfurter Exchange Rates API** (`https://api.frankfurter.app`) através de um Driven Adapter resiliente (`IMarketExchangeProvider` -> `FrankfurterExchangeAdapter`) com `HttpClient`, Timeout, Retry exponencial e cache em duas camadas (`HybridCache` L1/L2).
  2. Permita gerenciar **Watchlists/Alertas de Câmbio (`CurrencyWatchItem` Aggregate Root)** por usuário autenticado (proteção estrita contra BOLA/IDOR via `OwnerId`), enriquecendo a listagem e consulta com a cotação ao vivo, diferença percentual (`SpreadPercentage`) e status de atingimento da meta (`IsTriggered`).
  3. Dispare **Domain Events** (`WatchItemCreatedDomainEvent`, `TargetRateTriggeredDomainEvent`) e publique **Integration Events** (`WatchItemCreatedIntegrationEvent`) via **MassTransit + RabbitMQ** (com fallback automático e gracioso para In-Memory/Outbox quando executado em container single-instance gratuito no Render).
  4. Exponha contratos OpenAPI v1 (`/api/v1/rates/{baseCurrency}`, `/api/v1/watchlist`, `/health/live`, `/health/ready`) com `TypedResults`, `FluentValidation`, `ProblemDetails (RFC 7807)`, Rate Limiting e Security Headers OWASP.
- **Fora de Escopo (Non-Goals / YAGNI)**:
  - Execução real de ordens de compra/venda de câmbio ou integração bancária PIX/SWIFT.
  - Cotações de criptomoedas de alta frequência via WebSockets (foco exclusivo em moedas fiduciárias ISO-4217 suportadas pelo BCE/Frankfurter).
  - Cobrança de assinatura/billing via Stripe no MVP.

---

## 2. Open Questions, Premissas e Decisões
> **Regra**: Nenhuma task pode iniciar desenvolvimento se houver `[OPEN QUESTION]` bloqueante sem resposta.

| ID | Pergunta / Ponto de Atenção | Status (`OPEN` / `RESOLVED`) | Decisão / Resposta Acordada |
| :--- | :--- | :--- | :--- |
| `OQ-01` | Qual API pública externa será consumida para cotações reais sem risco de bloqueio por falta de API Key paga? | `RESOLVED` | **Frankfurter Public API (`https://api.frankfurter.app/latest`)**, gratuita, open-source, sem necessidade de API key, alimentada pelas taxas oficiais do Banco Central Europeu. |
| `OQ-02` | Como o sistema se comporta se a API pública `api.frankfurter.app` oscilar ou ficar temporariamente indisponível durante testes ou em produção no Render? | `RESOLVED` | O `FrankfurterExchangeAdapter` utiliza `HybridCache` (TTL L1 = 5 min, L2 = 15 min), Timeout de 5s e fallback resiliente com taxas de referência oficiais cacheadas caso o endpoint externo retorne erro de rede/5xx, evitando indisponibilidade do core. |
| `OQ-03` | Como funcionará a autenticação e proteção BOLA/IDOR no ambiente de validação/demo no Render e nos testes automatizados se um token JWT real do Supabase não for enviado? | `RESOLVED` | Suporte completo a **Supabase Auth JWT Bearer** (`ClaimTypes.NameIdentifier` / `sub`) + esquema de autenticação de demonstração/teste controlado por configuração (`Auth:AllowDemoHeader = true` lendo `X-Demo-User-Id`), mantendo bloqueio `401 Unauthorized` para requisições anônimas em rotas protegidas e `404` contra acesso cruzado entre `OwnerId` distintos (BOLA). |
| `OQ-04` | Como operar banco de dados e mensageria em container single-instance gratuito no Render (`prj-d6rakhshg0os73a5ahqg`) quando variáveis externas de Postgres/RabbitMQ não estiverem provisionadas? | `RESOLVED` | Registrado na `ADR-001`: A camada `Infrastructure` suporta PostgreSQL (`Npgsql`) + RabbitMQ (`MassTransit`) por padrão via ConnectionStrings e aplica fallback gracioso para EF Core InMemory + MassTransit In-Memory Bus quando em modo container standalone no Render Free Tier, mantendo 100% dos mesmos Ports & Adapters. |
| `OQ-05` | Quais moedas ISO-4217 são válidas no domínio? | `RESOLVED` | Código alfabético de 3 letras maiúsculas (`USD`, `EUR`, `BRL`, `GBP`, `JPY`, `CAD`, `AUD`, `CHF`, `CNY`, `MXN`). `BaseCurrency` e `TargetCurrency` não podem ser iguais no mesmo `CurrencyWatchItem`. |

---

## 3. Requisitos Funcionais (RF) e Critérios de Aceite (BDD)

### `RF-001`: Consulta de Cotações de Câmbio em Tempo Real (`GET /api/v1/rates/{baseCurrency}`)
- **Descrição**: O sistema deve consultar e retornar as taxas de câmbio mais recentes para uma moeda base ISO-4217 (ex.: `USD`, `EUR`, `BRL`), opcionalmente filtrada por moedas de destino (`symbols`), utilizando cache `HybridCache` e integração com a API pública Frankfurter.
- **Atores / Permissões**: `Anônimo / Público` (`.AllowAnonymous()`, protegido por Rate Limiting).
- **Critérios de Aceite (Gherkin)**:
  - **Cenário 1 (Caminho Feliz - Consulta de Cotação Válida)**:
    - **Dado** que um cliente solicita `GET /api/v1/rates/USD?symbols=BRL,EUR`
    - **Quando** a requisição é processada pelo `GetExchangeRatesQueryHandler`
    - **Então** deve retornar status `200 OK` contendo `baseCurrency: "USD"`, `referenceDate`, `provider` e o dicionário `rates` com valores numéricos maiores que zero para `BRL` e `EUR`.
  - **Cenário 2 (Moeda Base Inválida)**:
    - **Dado** que um cliente solicita `GET /api/v1/rates/INVALID123`
    - **Quando** o `GetExchangeRatesQueryValidator` valida o input
    - **Então** deve retornar status `400 Bad Request` no formato `ProblemDetails` (RFC 7807) com código `Currency.InvalidCode`.

### `RF-002`: Criação de Alerta/Watchlist de Câmbio (`POST /api/v1/watchlist`)
- **Descrição**: O usuário autenticado deve poder cadastrar um par de moedas (`BaseCurrency` -> `TargetCurrency`), uma taxa-alvo (`TargetRate > 0`), condição de disparo (`NotifyWhenAbove`: `true` se cotação atual >= alvo, `false` se cotação atual <= alvo) e notas opcionais (`Notes`). No momento da criação, o sistema consulta a taxa ao vivo, avalia se a meta já foi atingida e dispara os eventos de domínio e integração.
- **Atores / Permissões (Supabase Auth)**: `Usuário Autenticado` (`RequireAuthorization`).
- **Critérios de Aceite (Gherkin)**:
  - **Cenário 1 (Caminho Feliz - Criação de WatchItem)**:
    - **Dado** que o usuário autenticado (`OwnerId = u1`) envia `POST /api/v1/watchlist` com `{ "baseCurrency": "USD", "targetCurrency": "BRL", "targetRate": 5.50, "notifyWhenAbove": true, "notes": "Alerta de hedge cambial" }`
    - **Quando** não existe outro alerta ativo idêntico (`USD` -> `BRL`) para este `OwnerId`
    - **Então** o sistema cria o `CurrencyWatchItem`, calcula a cotação atual via `IMarketExchangeProvider`, registra `WatchItemCreatedDomainEvent` (e `TargetRateTriggeredDomainEvent` se a cotação atual já satisfizer a meta), publica `WatchItemCreatedIntegrationEvent` e retorna `201 Created` com header `Location: /api/v1/watchlist/{id}`.
  - **Cenário 2 (Regra de Negócio - Moedas Iguais ou Taxa Inválida)**:
    - **Dado** que o usuário envia `baseCurrency: "BRL"` e `targetCurrency: "BRL"` (ou `targetRate: -1.0`)
    - **Quando** o comando é validado
    - **Então** deve retornar status `400 Bad Request` (`ProblemDetails`) com código `WatchItem.SameCurrencyNotAllowed` ou `WatchItem.InvalidTargetRate`.
  - **Cenário 3 (Conflito de Duplicidade por Usuário)**:
    - **Dado** que o usuário já possui um alerta ativo para o par `USD` -> `BRL`
    - **Quando** tenta cadastrar novamente o mesmo par `USD` -> `BRL`
    - **Então** deve retornar status `409 Conflict` (`ProblemDetails`) com código `WatchItem.DuplicatePair`.

### `RF-003`: Listagem Paginada e Enriquecida de Watchlist do Usuário (`GET /api/v1/watchlist`)
- **Descrição**: O sistema deve listar de forma paginada (`page`, `pageSize`) apenas os itens da watchlist pertencentes ao `OwnerId` autenticado, enriquecendo cada item com a cotação atual (`CurrentRate`), variação percentual em relação à meta (`SpreadPercentage`) e indicador `IsTriggered`.
- **Atores / Permissões (Supabase Auth)**: `Usuário Autenticado` (`RequireAuthorization`).

### `RF-004`: Consulta por ID e Exclusão Segura com Proteção BOLA/IDOR (`GET` e `DELETE /api/v1/watchlist/{id}`)
- **Descrição**: O usuário autenticado pode consultar os detalhes atualizados de um `CurrencyWatchItem` específico por `id` ou removê-lo de sua watchlist. Se outro usuário tentar acessar ou remover o `id`, retorna `404 Not Found`.

---

## 4. Requisitos Não Funcionais (RNF) e Restrições
- **Performance & Latência**: `p95 < 150ms` para leituras cacheadas no `HybridCache`.
- **Escalabilidade & Compatibilidade**: API stateless pronta para consumo por Web e App Mobile via JSON + OpenAPI v1; container Docker multi-stage na porta `8080` (padrão Render).
- **Segurança (OWASP API Top 10)**: Proteção BOLA/IDOR por `OwnerId`, DTOs imutáveis contra Mass Assignment, Paginação limitada (`MaxPageSize = 100`), Rate Limiting (`60 req/min`) e Security Headers.

---

## 5. Análise Preventiva de Riscos, Edge Cases e Soluções

| ID | Cenário de Falha / Edge Case Previsto | Impacto | Solução Técnica Incorporada na Spec |
| :--- | :--- | :--- | :--- |
| `RISK-01` | Duplo clique do usuário ou retry de rede no `POST /api/v1/watchlist` gerando alertas duplicados | Alto | Índice único composto `(OwnerId, BaseCurrency, TargetCurrency)` + verificação no Handler retornando `409 Conflict`. |
| `RISK-02` | Indisponibilidade momentânea da API pública `api.frankfurter.app` | Alto | `HybridCache` (L1 5 min / L2 15 min) + fallback determinístico seguro no `FrankfurterExchangeAdapter`. |
| `RISK-03` | Usuário tenta ler ou deletar `WatchItem` de outro usuário passando `Guid` na rota (BOLA/IDOR) | Crítico | Todas as assinaturas de `ICurrencyWatchItemRepository` exigem `(Guid id, Guid ownerId, CancellationToken ct)`. |
| `RISK-04` | Falha de conexão com RabbitMQ ou PostgreSQL externo em container standalone no Render | Alto | Configuração adaptativa na `Infrastructure`: PostgreSQL + RabbitMQ quando `ConnectionStrings` estão definidas, ou EF Core InMemory + MassTransit InMemory quando standalone. |

---

## 6. Modelagem de Domínio (DDD) e Arquitetura Hexagonal
- **Contexto**: `MarketPulse`
- **Aggregate Root**: `CurrencyWatchItem`
- **Value Objects**: `CurrencyCode` (`readonly record struct`), `TargetRate` (`readonly record struct`)
- **Domain Events**: `WatchItemCreatedDomainEvent`, `TargetRateTriggeredDomainEvent`
- **Driven Ports**: `ICurrencyWatchItemRepository`, `IUnitOfWork`, `IMarketExchangeProvider`, `ICurrentUserProvider`, `IEventBusPublisher`

---

## 7. Contratos de API REST (Web & Mobile)
- `GET /api/v1/rates/{baseCurrency}?symbols=BRL,EUR` -> `200 OK` / `400 ProblemDetails`
- `POST /api/v1/watchlist` -> `201 Created` / `400` / `401` / `409 ProblemDetails`
- `GET /api/v1/watchlist?page=1&pageSize=20` -> `200 OK` (`PagedResult<WatchItemDto>`)
- `GET /api/v1/watchlist/{id:guid}` -> `200 OK` / `404 ProblemDetails`
- `DELETE /api/v1/watchlist/{id:guid}` -> `204 No Content` / `404 ProblemDetails`
- `GET /health/live` & `GET /health/ready` -> `200 OK`

---

## 8. Mensageria Assíncrona (RabbitMQ) e Cache
- **Integration Event**: `WatchItemCreatedIntegrationEvent` consumido por `WatchItemCreatedConsumer` (idempotente por `EventId`).
- **Cache**: `HybridCache` com chave `"marketpulse:rates:{baseCurrency}:{symbols}"`.

---

## 9. Modelo de Dados (PostgreSQL)
Tabela `currency_watch_items` com chave primária `UUID`, índice único `(owner_id, base_currency, target_currency)` e índice `(owner_id, created_at_utc DESC)`.

---

## 10. Validação do Definition of Ready (DoR)
- [x] Todos os itens de `docs/pipeline/DEFINITION_OF_READY.md` foram conferidos e aprovados (`100% READY_FOR_DEV`).
