# Definition of Ready (DoR) — Critérios de Prontidão para Desenvolvimento

Nenhuma Task ou Feature pode avançar para a etapa de **Codificação (`backend-csharp-developer` / `frontend-web-developer`)** sem que **100% dos critérios abaixo** estejam marcados e validados na Spec Técnica.

O objetivo do **DoR** é eliminar completamente o retrabalho, a ambiguidade e o improviso durante a escrita do código.

---

## 1. Clareza de Negócio e Escopo (Zero Ambiguidade)
- [ ] **Objetivo e Valor de Negócio**: Está claro *por que* esta funcionalidade existe e qual problema do usuário (MVP) ela resolve.
- [ ] **Requisitos Funcionais (RF)**: Todos os fluxos principais (Happy Path), fluxos alternativos e regras de negócio estão enumerados com identificadores únicos (`RF-001`, `RF-002`).
- [ ] **Open Questions Resolvidas**: Nenhuma dúvida funcional ou de regra de negócio permanece em aberto (`[OPEN QUESTION]` = 0). Se uma decisão foi assumida proativamente, ela está registrada como Premissa Validada ou ADR.
- [ ] **Critérios de Aceite (BDD / Gherkin)**: Cada requisito possui cenários claros no formato `Dado (Given) / Quando (When) / Então (Then)`, incluindo casos de erro.

## 2. Arquitetura e Domínio (DDD + Hexagonal + Event-Driven)
- [ ] **Linguagem Ubíqua**: Termos do domínio (Entidades, Aggregates, Value Objects, Eventos) estão nomeados de forma padronizada e sem sinônimos conflitantes.
- [ ] **Invariantes de Domínio**: Regras que nunca podem ser violadas dentro do Aggregate Root estão explícitas (ex.: "Um pedido pago não pode ter itens removidos").
- [ ] **Portas de Entrada e Saída (Hexagonal)**:
  - Quais Commands/Queries na camada `Application` serão criados?
  - Quais interfaces de saída (`IRepository`, `ICache`, `IMessageBus`) serão acionadas?
- [ ] **Eventos e Mensageria (RabbitMQ)**:
  - Se houver comunicação assíncrona, o nome do evento (`<Context>.<Entity><Action>Event`), payload JSON, fila/exchange, política de **Idempotência**, **Outbox Pattern** e **Dead-Letter Queue (DLQ)** estão definidos.

## 3. Contratos de API REST (Web & Futuro Mobile) e Banco de Dados
- [ ] **Contrato HTTP (OpenAPI)**:
  - Método (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`), rota versionada (`/api/v1/...`), Request Body, Query Params e Response DTOs especificados.
  - Códigos de retorno HTTP mapeados (`200`, `201`, `204`, `400 ProblemDetails`, `401`, `403`, `404`, `409`, `422`, `429`).
- [ ] **Validações de Input**: Regras exatas do `FluentValidation` (backend) e `Zod` (frontend) listadas (tamanhos máximos, formatos, obrigatoriedade).
- [ ] **Modelagem de Dados (PostgreSQL)**:
  - Tabelas, colunas, tipos de dados, chaves primárias (`UUIDv7` / `Guid`), chaves estrangeiras e **índices de performance** definidos.
  - Impacto em migrations avaliado (compatibilidade retroativa).

## 4. Requisitos Não Funcionais (RNF), Custo e Segurança (OWASP)
- [ ] **Autenticação e Autorização (Supabase Auth)**:
  - Quais roles/claims do JWT do Supabase têm permissão para acessar este recurso?
  - Como é garantida a proteção contra **BOLA / IDOR** (ex.: validar que o `sub` / `user_id` do token é dono do recurso)?
- [ ] **Estratégia de Cache**:
  - Os dados serão cacheados? Qual a chave de cache, TTL (Time-To-Live) e qual evento/ação invalida esse cache?
- [ ] **Previsão de Falhas e Mitigação**:
  - O que acontece se o banco cair, se o RabbitMQ atrasar, se o usuário clicar duas vezes (concorrência otimista / `xmin` / `Idempotency-Key`)?
- [ ] **Alinhamento de Custo MVP**: A solução utiliza recursos nativos da stack ou free-tier aprovado sem introduzir serviços pagos desnecessários.

## 5. Plano de Testes e Decomposição de Tasks
- [ ] **Plano de Testes Mapeado**:
  - Lista dos testes de unidade (`Domain` e `Application` handlers/validators) que serão escritos.
  - Lista dos testes de integração (`Api` + `PostgreSQL` + `RabbitMQ` / `MSW` no frontend) que serão escritos.
- [ ] **Tasks Atômicas e Independentes**: O trabalho foi fatiado em tasks pequenas e sequenciais, onde cada task tem escopo claro de arquivos a criar/modificar e comando exato de verificação.
- [ ] **Padrão de Idioma Técnico (EN-US)**: Toda a especificação técnica (`docs/specs/`), histórias de usuário, critérios de aceitação, termos de linguagem ubíqua e descrição de tasks foram redigidos estritamente em inglês (EN-US).

