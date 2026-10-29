---
name: sre-observability-telemetry
description: >-
  Guia executável de SRE (Site Reliability Engineering), Observabilidade (Logs, Métricas e Traces com OpenTelemetry),
  Monitoramento (Grafana, Mimir, Loki, Tempo e Aspire Dashboard), Investigação de Incidentes, Debugging Estruturado e RCA.
---

# Engenharia de Confiabilidade (SRE) e Observabilidade com OpenTelemetry & Grafana

Esta skill estabelece os padrões técnicos e operacionais de **SRE (Site Reliability Engineering)**, **Observabilidade Moderna (Logs, Métricas e Traces)** e **Gestão de Incidentes** para aplicações corporativas com zero aprisionamento tecnológico (Vendor-Neutral).

Consulte também o runbook detalhado em [`docs/sre/INCIDENT_INVESTIGATION_RUNBOOK.md`](file:///d:/pipeline-dev-automatizador/docs/sre/INCIDENT_INVESTIGATION_RUNBOOK.md).

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
  - Spans personalizados marcam tags de negócio usando `activity?.SetTag("key", "value")`.

### 2.2. Métricas de Negócio e Infraestrutura (Metrics)
- **Métricas de Infraestrutura**: Coletadas automaticamente via `OpenTelemetry.Instrumentation.AspNetCore` e `OpenTelemetry.Instrumentation.Http`.
- **Métricas de Negócio**: Instrumentadas via `System.Diagnostics.Metrics.Meter`:
  - `marketpulse.rates.requests_total`: volume de cotações consultadas.
  - `marketpulse.watchlist.created_total`: alertas cambiais criados.
  - `marketpulse.watchlist.deleted_total`: alertas removidos.

### 2.3. Logs Estruturados com Escopo de Correlação (Logging)
- **Headers de Rastreamento**:
  - `X-Correlation-Id`: Identificador único de transação (repassado pelo cliente ou gerado na borda).
  - `X-Trace-Id`: Trace ID do padrão OpenTelemetry W3C.
- **Logging Scope**: O middleware `RequestTelemetryMiddleware` anexa via `logger.BeginScope`:
  - `CorrelationId`, `TraceId`, `SpanId`, `RequestMethod`, `RequestPath`.
- **Exportação via OTLP**: Logs estruturados são enviados via OTLP diretamente para o Grafana Loki ou Aspire Dashboard com `IncludeFormattedMessage = true` e `IncludeScopes = true`.

---

## 3. Cheat Sheet de Consultas para Investigação de Incidentes

### A. Consultas de Métricas no Grafana Mimir / Prometheus (`grafanacloud-bigveranda1683-prom`)
- **Taxa de Erros 5xx**:
  ```promql
  (sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])) / sum(rate(http_server_request_duration_seconds_count[5m]))) or vector(0)
  ```
- **Latência P95 por Rota**:
  ```promql
  histogram_quantile(0.95, sum by (le, http_route) (rate(http_server_request_duration_seconds_bucket[5m])))
  ```
- **Throughput por Rota**:
  ```promql
  sum by (http_route, http_request_method) (rate(http_server_request_duration_seconds_count[1m]))
  ```

### B. Consultas de Logs no Grafana Loki (`grafanacloud-bigveranda1683-logs`)
- **Buscar Erros e Falhas**:
  ```logql
  {service_name="agentic-pipeline-lab-api"} |~ "(?i)fail|error|exception|status 5"
  ```
- **Buscar por CorrelationId**:
  ```logql
  {service_name="agentic-pipeline-lab-api"} |= "CorrelationId: <ID_AQUI>"
  ```
- **Buscar por TraceId**:
  ```logql
  {service_name="agentic-pipeline-lab-api"} |= "<TRACE_ID_AQUI>"
  ```

### C. Consultas de Tracing no Grafana Tempo (`grafanacloud-bigveranda1683-traces`)
- **Traces com Erro**:
  ```traceql
  { status = error }
  ```
- **Traces Lentos (> 500ms)**:
  ```traceql
  { duration > 500ms }
  ```

---

## 4. Como Conectar ao Grafana Cloud (ex: bigveranda1683.grafana.net)

Para enviar telemetria diretamente da aplicação (.NET 9) ou do Docker/Render para a sua conta no Grafana Cloud, configure:

```bash
OTEL_SERVICE_NAME="agentic-pipeline-lab-api"
OTEL_EXPORTER_OTLP_ENDPOINT="https://otlp-gateway-prod-sa-east-1.grafana.net/otlp"
OTEL_EXPORTER_OTLP_PROTOCOL="http/protobuf"
OTEL_EXPORTER_OTLP_HEADERS="Authorization=Basic <TOKEN_BASE64>"
```

### Importante sobre o Protocolo HTTP Protobuf no .NET:
O SDK .NET com `OtlpExportProtocol.HttpProtobuf` requer que cada sinal anexe seu respectivo sufixo de rota:
- Traces: `{Endpoint}/v1/traces`
- Métricas: `{Endpoint}/v1/metrics`
- Logs: `{Endpoint}/v1/logs`
Essa regra está implementada e garantida em [`Program.cs`](file:///d:/pipeline-dev-automatizador/src/backend/src/App.Api/Program.cs).

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
   {service_name="agentic-pipeline-lab-api"} |= "CorrelationId: <ID_AQUI>"
   ```
3. Abra a aba **Traces** (Tempo) e cole o `TraceId`.
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
