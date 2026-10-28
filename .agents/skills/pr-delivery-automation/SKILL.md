---
name: pr-delivery-automation
description: >-
  Use esta skill quando o desenvolvimento e os testes de uma feature estiverem concluídos para
  executar a bateria final de validação, organizar commits semânticos (Conventional Commits) e
  gerar ou publicar um Pull Request ultra-detalhado no GitHub contendo arquitetura, testes executados,
  auditoria OWASP e checklist de Definition of Done (DoD).
---

# Automação de Entrega e Pull Request Detalhado (`pr-delivery-automation`)

O objetivo desta skill é garantir que o revisor humano (ou agente revisor) receba no Pull Request **100% do contexto, evidências reais dos testes executados, decisões tomadas e garantias de segurança**, tornando a revisão rápida, transparente e confiável.

---

## Passo 1: Validação Pré-PR Obrigatória

1. Execute `pwsh -File scripts/validate-all.ps1` (ou os comandos de build, lint e test de backend e frontend).
2. Capture o resumo real da execução dos testes:
   - Quantidade de testes de **Unidade Backend** executados e aprovados.
   - Quantidade de testes de **Arquitetura (`NetArchTest`)** executados e aprovados.
   - Quantidade de testes de **Integração Backend (`Testcontainers`)** executados e aprovados.
   - Quantidade de testes **Frontend (`Vitest / MSW / Playwright`)** executados e aprovados.
3. Verifique `git status` e `git diff` para garantir que nenhum arquivo temporário, log ou segredo (`.env`) será commitado.

---

## Passo 2: Padronização de Branch e Commits (Conventional Commits)

- **Nome da Branch**: `feat/<slug-da-feature>`, `fix/<slug-do-bug>`, `chore/<slug>`
- **Formato de Commit**:
  - `feat(domain): add Order aggregate and value objects`
  - `feat(api): expose POST /api/v1/orders minimal api endpoint`
  - `feat(web): implement order creation form with TanStack Query and Zod`
  - `test(integration): add Postgres and RabbitMQ outbox integration tests`

---

## Passo 3: Estrutura Obrigatória da Descrição do Pull Request

Ao criar o Pull Request (via `gh pr create` seguindo a skill `permissioned-github` ou gerando o artefato de PR), preencha **obrigatoriamente** todas as seções de `.github/PULL_REQUEST_TEMPLATE.md`:

1. **Resumo Executivo e Link para a Spec** (`docs/specs/<feature>.md`).
2. **Arquitetura e Alterações por Camada** (`Domain`, `Application`, `Infrastructure`, `Minimal API`, `Frontend`).
3. **Problemas Previstos & Mitigações Aplicadas** (ex.: como foi garantida a idempotência no RabbitMQ, proteção BOLA no Supabase JWT e cache).
4. **Relatório Real de Testes Executados** (Tabela com total de testes passados, falhos, ignorados e tempo de execução).
5. **Checklist de Segurança OWASP Top 10** e **Definition of Done (DoD)** assinalados.
6. **Guia de Deploy, Migrations e Rollback** (se há migrations EF Core ou novas variáveis de ambiente necessárias).

