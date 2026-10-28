---
name: run-agentic-pipeline
description: >-
  Use esta skill quando o usuário pedir para rodar a esteira agêntica completa sobre uma ideia,
  estrutura de projeto ou requisito. Coordena o fluxo autônomo em múltiplos estágios:
  (1) Discovery, RF/RNF, Riscos e Spec -> (2) Arquitetura DDD/Hexagonal e Tasks (DoR) ->
  (3) Implementação Backend C# e Frontend -> (4) Auditoria QA/OWASP e Pull Request (DoD).
---

# Execução da Esteira de Desenvolvimento Agêntico (`run-agentic-pipeline`)

Esta skill orquestra todo o ciclo de vida de engenharia de software de forma autônoma, reativa e padronizada.

---

## Como Executar a Esteira Completa

### Etapa 1: Bootstrap da Estrutura (Se o código ainda não existir)
Se a pasta atual contiver apenas a descrição do projeto ou regras de negócio e ainda não possuir a estrutura de código em `src/backend` e `src/frontend`:
1. Execute o script de scaffold da arquitetura base (ou crie a estrutura seguindo `docs/architecture/STACK_AND_DECISIONS.md`):
   ```powershell
   pwsh -File scripts/scaffold-monolith.ps1
   ```

### Etapa 2: Discovery, Previsão de Problemas e Spec (`Gate 1 & Gate 2 - DoR`)
1. Ative a skill `discovery-and-spec` (ou delegue para `product-requirements-analyst` e `domain-architect-planner`).
2. Analise profundamente tudo que o usuário forneceu na pasta.
3. Identifique **Open Questions**. Se houver bloqueios reais de negócio, pergunte ao usuário de forma objetiva antes de codificar.
4. Gere:
   - `docs/specs/<feature>.md` (Especificação completa com RF, RNF, Riscos previstos + Soluções, DDD, Contratos REST, Eventos RabbitMQ, Cache e SQL).
   - `docs/specs/<feature>-tasks.md` (Tasks sequenciais atômicas com comandos de verificação).
5. Valide 100% da [DEFINITION_OF_READY.md](../../docs/pipeline/DEFINITION_OF_READY.md).

### Etapa 3: Construção Paralela ou Sequencial Guiada por Testes (`Gate 3`)
Com o contrato REST OpenAPI e a Spec congelados na Etapa 2:
1. **Backend (`backend-csharp-developer`)**:
   - Executa `TASK-01` (Domain + Unit Tests) -> `TASK-02` (Application + Validators + Unit Tests) -> `TASK-03` (Infra EF Core Postgres + MassTransit RabbitMQ Outbox + HybridCache + Supabase Auth) -> `TASK-04` (Minimal API + ArchTests + IntegrationTests).
2. **Frontend (`frontend-web-developer`)**:
   - Executa `TASK-05` (Sincronização de tipos OpenAPI, Cliente HTTP com Supabase Auth, Hooks TanStack Query, Componentes UI + Formulários Zod + Testes Vitest/MSW).

### Etapa 4: Auditoria Implacável de Qualidade, OWASP e Pull Request (`Gate 4 - DoD`)
1. Delegue para `qa-security-reviewer` (ou execute `owasp-quality-gate` + `pr-delivery-automation`).
2. Rode `pwsh -File scripts/validate-all.ps1`.
3. Se houver qualquer falha de compilação, linter, teste ou segurança OWASP, corrija imediatamente e re-execute até obter **100% de aprovação**.
4. Gere o artefato final de revisão do Pull Request com todos os detalhes do que foi feito e os resultados dos testes executados.

