# ADR-001: Arquitetura Hexagonal com Integração Frankfurter Public API, HybridCache e Deploy Resiliente em Containers no Render

- **Status**: `Aceito`
- **Data**: `2026-10-28`
- **Decisores**: `domain-architect-planner`

---

## 1. Contexto e Problema
Precisamos construir e implantar o **MarketPulse Lab** no repositório `https://github.com/poferrari/agentic-pipeline-lab` com deploy em containers no **Render** (`prj-d6rakhshg0os73a5ahqg` / `evm-d6rakhshg0os73a5ahr0`), consumindo uma API pública real, aplicando Arquitetura Hexagonal, DDD, Cache, Mensageria e suíte completa de testes automatizados com custo zero de MVP.

## 2. Decisão
1. **Integração Pública Desacoplada**: Utilizar a API oficial **Frankfurter (`https://api.frankfurter.app`)** (taxas de câmbio do Banco Central Europeu) encapsulada no Driven Adapter `FrankfurterExchangeAdapter` implementando a porta `IMarketExchangeProvider`, com `HybridCache` e fallback resiliente.
2. **Infraestrutura Adaptativa para Cloud Containers**: Suportar Npgsql PostgreSQL + MassTransit RabbitMQ quando `ConnectionStrings:Database` e `ConnectionStrings:RabbitMq` estiverem definidas, com fallback automático para EF Core In-Memory + MassTransit In-Memory quando executado em container standalone no Render Free Tier ou em `WebApplicationFactory`.
3. **Containerização Segura**: `Dockerfile` multi-stage expondo a porta `8080` com usuário não-root e healthchecks `/health/live` e `/health/ready`.
