---
name: sre-observability-telemetry
description: >-
  Guia executável de SRE (Site Reliability Engineering), Observabilidade (Logs, Métricas e Traces com OpenTelemetry),
  Monitoramento (Grafana, Mimir, Loki, Tempo e Aspire Dashboard), Investigação de Incidentes, Debugging Estruturado e RCA.
---

# Engenharia de Confiabilidade (SRE) e Observabilidade com OpenTelemetry & Grafana

Esta skill estabelece os padrões técnicos e operacionais de **SRE (Site Reliability Engineering)**, **Observabilidade Moderna (Logs, Métricas e Traces)** e **Gestão de Incidentes** para aplicações corporativas com zero aprisionamento tecnológico (Vendor-Neutral).

---

## 1. Princípios de SRE e os 4 Sinais de Ouro (Golden Signals)

Toda API e serviço distribuído deve ser monitorado com base nos **4 Golden Signals** (Google SRE Book):

| Sinal de Ouro | O que mede | Como instrumentar no .NET / OpenTelemetry | Alerta Recomendado |
| :--- | :--- | :--- | :--- |
| **Latência (Latency)** | Tempo que uma requisição leva para ser processada. Deve-se medir p50, p95 e p99 separadamente dos erros. | `http.server.request.duration` (Histograma OpenTelemetry) | p95 > 500ms por 5 min |
| **Tráfego (Traffic)** | Demanda imposta ao sistema (requests/segundo, mensagens consumidas/segundo). | `http.server.active_requests`, `marketpulse.rates.requests_total` | Queda abrupta (> 80%) ou pico anômalo (> 300%) |
| **Erros (Errors)** | Taxa de requisições que falharam explicitamente (HTTP 5xx) ou semanticamente (falha de negócio/timeout). | Contador de status HTTP (`http.response.status_code >= 500`) | Taxa de erros 5xx > 1% por 3 min |
| **Saturação (Saturation)** | Quão "cheio" está o serviço (CPU, memória, conexões de banco de dados, tamanho de fila RabbitMQ). | Conexões ativas no pool Npgsql/EF Core, conexões HTTP ativas (`http.client.open_connections`) | Uso de pool de banco > 85% ou fila RabbitMQ acumulando |

---

## 2. Os 3 Pilares da Observabilidade com OpenTelemetry (OTel)

```
        ┌──────────────────────────────────────────────────┐
        │             OpenTelemetry Standard               │
        │        (W3C TraceContext, OTLP gRPC/HTTP)        │
        └────────────────────────┬─────────────────────────┘
                                 │
         ┌───────────────────────┼───────────────────────┐
         ▼                       ▼                       ▼
   [ Tracing ]              [ Metrics ]              [ Logging ]
 • ActivitySource         • Meter / Counters       • ILogger Scope
 • Distributed Spans      • Histograms             • CorrelationId
 • TraceId / SpanId       • Connection Pools       • ProblemDetails
```

### 2.1. Distributed Tracing (Rastreamento Distribuído)
- **Protocolo**: W3C TraceContext (`traceparent`, `tracestate`).
- **Implementação**:
  - `ActivitySource` nativo do .NET (`System.Diagnostics`).
  - Cada requisição gera um `TraceId` e um `SpanId`.
  - Chamadas HTTP externas (ex: Frankfurter API) e mensagens do broker (RabbitMQ) propagam automaticamente o cabeçalho `traceparent`.
  - Spans personalizados devem marcar tags de negócio usando `activity?.SetTag("key", "value")`.

### 2.2. Métricas de Negócio e Infraestrutura (Metrics)
- **Métricas de Infraestrutura**: Coletadas automaticamente via `OpenTelemetry.Instrumentation.AspNetCore` e `OpenTelemetry.Instrumentation.Http`.
- **Métricas de Negócio**: Instrumentadas via `System.Diagnostics.Metrics.Meter`:
  - `marketpulse.rates.requests_total`: volume de cotações consultadas.
  - `marketpulse.watchlist.created_total`: alertas cambiais criados.
  - `marketpulse.watchlist.deleted_total`: alertas removidos.

### 2.3. Logs Estruturados com Escopo de Correlação
- **Headers de Rastreamento**:
  - `X-Correlation-Id`: Identificador único de transação (repassado pelo cliente ou gerado na borda).
  - `X-Trace-Id`: Trace ID do padrão OpenTelemetry W3C.
- **Logging Scope**: O middleware `RequestTelemetryMiddleware` anexa via `logger.BeginScope`:
  - `CorrelationId`, `TraceId`, `SpanId`, `RequestMethod`, `RequestPath`.
- **Regra**: Nunca use concatenação de strings para logs (`logger.LogInformation("user " + id)`). Sempre utilize logging semântico estruturado (`logger.LogInformation("Order {OrderId} processed for user {UserId}", orderId, userId)`).

---

## 3. Stack de Observabilidade Recomendada (Open Source & Custos)

### Análise de Custos e Ferramentas:

1. **Grafana**:
   - **Auto-hospedado (Self-Hosted / Docker)**: 100% Open Source e Gratuito (licença AGPLv3). Você pode subir via Docker Compose usando o bundle `grafana/otel-lgtm` (Loki, Grafana, Tempo, Mimir) sem custo de licenciamento.
   - **Grafana Cloud**: Possui um **Free Tier Permanente** bastante generoso que atende MVPs e startups em estágio inicial:
     - 10.000 séries ativas de métricas (Prometheus/Mimir).
     - 50 GB de logs (Loki) com 14 dias de retenção.
     - 50 GB de traces (Tempo) com 14 dias de retenção.
     - 3 usuários e alertas ilimitados.
     - Não exige cartão de crédito no plano gratuito.

2. **.NET Aspire Dashboard (Desenvolvimento e Ambientes Locais / Staging)**:
   - Container oficial, 100% open-source e gratuito da Microsoft (`mcr.microsoft.com/dotnet/aspire-dashboard:9.0`).
   - Não requer nenhum banco de dados ou configuração complexa. Basta iniciar o container na porta 18888 (UI) e 4317 (OTLP gRPC).
   - Exibe visualmente traces em cascata (waterfall), métricas em tempo real e logs filtráveis por CorrelationId e TraceId.

3. **OpenTelemetry Collector**:
   - Agente agnóstico e de alta performance da CNCF que recebe telemetria da aplicação e roteia simultaneamente para Grafana, Aspire Dashboard, Prometheus, Datadog ou qualquer outro backend sem alterar uma única linha do código da aplicação.

---

## 4. Guia Rápido de Execução Local da Observabilidade

### Opção A: Subir com .NET Aspire Dashboard (Incluído no Docker Compose)
```bash
docker compose up -d
```
Acesse o painel web em:
- **Dashboard de Traces, Métricas e Logs**: `http://localhost:18888`
- **API MarketPulse**: `http://localhost:8080`

### Opção B: Subir com Grafana LGTM Stack (Loki + Grafana + Tempo + Mimir)
```bash
docker run --name grafana-lgtm -p 3000:3000 -p 4317:4317 -p 4318:4318 --rm -ti grafana/otel-lgtm
```
- Acesse o Grafana em `http://localhost:3000` (admin/admin).
- O backend C# enviará métricas, traces e logs diretamente para a porta OTLP 4317.

---

## 5. Playbook de Investigação de Incidentes, Debugging e RCA (Root Cause Analysis)

Quando um alerta disparar ou um erro for reportado em produção, siga rigorosamente este roteiro:

### Passo 1: Triagem e Isolamento do Impacto
1. Acesse o dashboard de **Golden Signals**:
   - Qual endpoint está falhando? Qual é o código de status retornado (429, 500, 503)?
   - O volume de erros começou após algum deploy recente ou alteração de banco?
   - Qual a porcentagem de usuários afetados?

### Passo 2: Localização por CorrelationId ou TraceId
1. Obtenha o `X-Correlation-Id` ou `X-Trace-Id` do cabeçalho da resposta HTTP ou do relatório do usuário.
2. No Grafana Loki ou Aspire Dashboard, filtre:
   ```logql
   {service="MarketPulse.Api"} |= "CorrelationId: <ID_AQUI>"
   ```
3. Abra a aba **Traces** e cole o `TraceId`.
4. Analise a árvore de spans em cascata:
   - Qual span demorou mais tempo (gargalo de CPU, I/O ou lock)?
   - O erro ocorreu no banco PostgreSQL (Npgsql), na chamada externa (Frankfurter HTTP) ou no barramento RabbitMQ?

### Passo 3: Mitigação Imediata
- Se for gargalo de recursos: escale a réplica horizontalmente ou ajuste o Rate Limiter.
- Se for falha de terceiro com cota esgotada: ative o fallback de cache no HybridCache.
- Se for bug crítico introduzido no último commit: execute o Rollback via Render/Docker com um clique.

### Passo 4: Post-Mortem Sem Culpa (Blameless RCA)
Documente em um arquivo Markdown no repositório (`docs/incidents/YYYY-MM-DD-rca.md`):
1. **Resumo do Incidente**: Horário de início, detecção e resolução (MTTD e MTTR).
2. **Causa Raiz Técnica**: Por que aconteceu (análise dos 5 Porquês).
3. **Ações Corretivas**: Criação de novo teste automatizado de integração para prevenir regressão, ajuste de alertas e melhoria no threshold de monitoramento.
