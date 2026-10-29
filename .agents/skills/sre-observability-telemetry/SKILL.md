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

## 3. Stack de Observabilidade Recomendada (Open Source & Custos)

### Análise de Custos e Ferramentas:

1. **Grafana**:
   - **Auto-hospedado (Self-Hosted / Docker)**: 100% Open Source e Gratuito (licença AGPLv3).
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

---

## 4. Como Conectar ao Grafana Cloud (ex: bigveranda1683.grafana.net)

Para enviar telemetria diretamente da aplicação (.NET 9) ou do Docker/Render para a sua conta no Grafana Cloud, siga estes passos:

### Passo 1: Obter as Credenciais OTLP no Grafana Cloud
1. Faça login na sua stack Grafana Cloud (ex: `https://bigveranda1683.grafana.net`).
2. No menu lateral, acesse **Connections** -> **Add new connection**.
3. Pesquise por **OpenTelemetry** e selecione o card.
4. Clique em **Configure OpenTelemetry**.
5. Em **Select your environment / language**, escolha **.NET** ou **Environment Variables**.
6. Clique em **Generate Token** (ele criará um token com permissão de escrita em Traces, Metrics e Logs).
7. O Grafana exibirá três valores prontos para copiar:
   - `OTEL_EXPORTER_OTLP_ENDPOINT` (exemplo: `https://otlp-gateway-prod-us-east-0.grafana.net/otlp`)
   - `OTEL_EXPORTER_OTLP_PROTOCOL`: `http/protobuf`
   - `OTEL_EXPORTER_OTLP_HEADERS`: `Authorization=Basic <TOKEN_GERADO_BASE64>`

### Passo 2: Configurar no Ambiente

#### A. Em Produção no Render:
No painel do serviço no Render:
1. Vá em `Environment` (`https://dashboard.render.com/web/srv-dbh8dc7f3r2c73bv92h0/env`).
2. Adicione as 3 variáveis obtidas no Passo 1:
   - `OTEL_EXPORTER_OTLP_ENDPOINT`: `https://otlp-gateway-prod-us-east-0.grafana.net/otlp`
   - `OTEL_EXPORTER_OTLP_PROTOCOL`: `http/protobuf`
   - `OTEL_EXPORTER_OTLP_HEADERS`: `Authorization=Basic <seu_token>`
3. Salve as alterações. O Render fará o redeploy automático e todas as requisições, métricas e logs começarão a fluir para o Grafana Cloud!

#### B. Localmente com Docker Compose:
No arquivo `.env`:
```env
OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp-gateway-prod-us-east-0.grafana.net/otlp
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
OTEL_EXPORTER_OTLP_HEADERS="Authorization=Basic <seu_token>"
```
Em seguida: `docker compose up -d`.

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
