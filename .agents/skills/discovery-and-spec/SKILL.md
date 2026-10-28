---
name: discovery-and-spec
description: >-
  Use esta skill no início de qualquer novo projeto, épico ou feature para analisar a estrutura da pasta,
  levantar Requisitos Funcionais e Não Funcionais (RF/RNF), prever problemas e soluções, resolver Open Questions,
  desenhar a Spec técnica completa (DDD, Hexagonal, RabbitMQ, PostgreSQL, REST API, Frontend) e validar a
  Definition of Ready (DoR) e o plano de Tasks antes de iniciar a codificação.
---

# Discovery Proativo, Especificação Técnica e Decomposição de Tasks (`discovery-and-spec`)

Esta skill transforma qualquer estrutura de projeto, briefing ou solicitação em um **Plano de Engenharia Completo e Inequívoco**, garantindo que o próximo agente codifique sem dúvidas, sem retrabalho e sem bugs de arquitetura.

---

## Passo 1: Varredura de Contexto e Análise Preventiva (Pre-Mortem)

1. **Inspecione o Repositório**:
   - Leia os arquivos existentes na pasta do projeto (documentos, diagramas, schemas, código legado ou rascunhos).
2. **Aplique a Matriz de Previsão de Problemas (Proactive Problem Predictor)**:
   Sempre verifique e responda na Spec como o sistema lidará com os 8 pontos críticos abaixo:
   - **Concorrência e Duplicidade**: E se o usuário clicar 2 vezes ou a rede reenviar o POST? *(Solução padrão: `Idempotency-Key` no header ou constraint `UNIQUE` no PostgreSQL + captura de conflito `409`)*.
   - **Falha de Dual-Write (DB + RabbitMQ)**: E se o commit no PostgreSQL funcionar, mas o RabbitMQ cair? *(Solução padrão: **Transactional Outbox Pattern** via MassTransit EF Core Outbox)*.
   - **Mensagem Duplicada ou Venenosa no RabbitMQ**: E se o consumidor receber o mesmo evento 2 vezes ou falhar 3 vezes seguidas? *(Solução padrão: Consumidor idempotente checando estado/ID + Retry exponencial 3x -> Dead-Letter Queue `_error`)*.
   - **Segurança Multi-Usuário (BOLA/IDOR)**: E se o usuário A passar o `Guid` de um registro do usuário B na URL? *(Solução padrão: Sempre filtrar no repositório por `WHERE id = @id AND owner_id = @currentUserId` extraído do JWT do Supabase)*.
   - **Explosão de Leitura / Banco Lento**: E se uma listagem tiver 100.000 itens ou for chamada 500x por segundo? *(Solução padrão: Paginação obrigatória `pageSize <= 100`, índice composto no PostgreSQL e cache `HybridCache` com TTL curto)*.
   - **Evolução para App Mobile**: Os endpoints retornam dados agnósticos de plataforma? *(Solução padrão: REST JSON puro com OpenAPI 3.1, sem acoplamento a sessão de cookie exclusivo de browser; suporte a header `Authorization: Bearer <token>`)*.
   - **Custo de Infraestrutura (MVP FinOps)**: Há algum serviço pago desnecessário sendo proposto? *(Solução padrão: Substituir por Supabase Auth Free Tier, PostgreSQL unificado, `HybridCache` In-Memory L1 e RabbitMQ container/CloudAMQP free)*.

---

## Passo 2: Resolução de Open Questions

1. Liste todas as perguntas em aberto (`Open Questions`).
2. Para decisões puramente técnicas, adote imediatamente o padrão de mercado documentado em `docs/architecture/STACK_AND_DECISIONS.md` e registre como `RESOLVED`.
3. Se houver alguma dúvida de **regra de negócio exclusiva do domínio do usuário** que impeça uma especificação exata, utilize a ferramenta `ask_question` apresentando opções claras e recomendando a opção mais enxuta para MVP (`(Recommended)`).

---

## Passo 3: Geração da Spec Completa e Plano de Tasks

1. Crie o arquivo `docs/specs/<slug-da-feature>.md` preenchendo **todas as 12 seções** de [SPEC_TEMPLATE.md](../../docs/templates/SPEC_TEMPLATE.md).
   - Não deixe placeholders genéricos (`TODO`, `A definir`). Especifique nomes exatos de classes, propriedades, tipos, rotas HTTP, status codes, eventos RabbitMQ e índices SQL.
2. Crie o arquivo `docs/specs/<slug-da-feature>-tasks.md` preenchendo [TASKS_TEMPLATE.md](../../docs/templates/TASKS_TEMPLATE.md).
3. Confira rigorosamente cada item de [DEFINITION_OF_READY.md](../../docs/pipeline/DEFINITION_OF_READY.md). Somente libere para codificação quando 100% do checklist estiver marcado (`[x]`).

