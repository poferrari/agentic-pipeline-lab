---
name: sre-observability-telemetry
description: >-
  Executable guide for Site Reliability Engineering (SRE), Observability (Logs, Metrics, Traces with OpenTelemetry),
  Monitoring (Grafana, Mimir, Loki, Tempo, Aspire Dashboard), Incident Investigation, Structured Debugging, and RCA.
---

# Site Reliability Engineering (SRE) and Observability with OpenTelemetry & Grafana

This skill establishes the technical and operational standards for **Site Reliability Engineering (SRE)**, **Modern Observability (Logs, Metrics, Traces)**, and **Incident Management** for enterprise applications with zero vendor lock-in.

Consult the incident runbook at [`docs/sre/INCIDENT_INVESTIGATION_RUNBOOK.md`](file:///d:/pipeline-dev-automatizador/docs/sre/INCIDENT_INVESTIGATION_RUNBOOK.md).

---

## 1. SRE Principles and the 4 Golden Signals

Every API and distributed service must be monitored against the **4 Golden Signals** (Google SRE Book):

| Golden Signal | What it Measures | How to Instrument in .NET / OpenTelemetry | Recommended Alert Threshold |
| :--- | :--- | :--- | :--- |
| **Latency** | Time taken to process a request. Must measure p50, p95, and p99 independently of errors. | `http.server.request.duration` (OpenTelemetry Histogram) | p95 > 500ms for 5 min |
| **Traffic** | Demand placed upon the system (requests/sec, consumed messages/sec). | `http.server.active_requests`, `marketpulse.rates.requests_total` | Sudden drop (> 80%) or anomalous spike (> 300%) |
| **Errors** | Rate of requests that fail explicitly (HTTP 5xx) or semantically (business rule/timeout). | HTTP status counter (`http.response.status_code >= 500`) | 5xx error rate > 1% for 3 min |
| **Saturation** | How "full" the service is (CPU, memory, database connection pool, RabbitMQ queue depth). | Active connections in Npgsql/EF Core pool, active HTTP connections (`http.client.open_connections`) | Database pool usage > 85% or RabbitMQ queue growing |

---

## 2. The 3 Pillars of Observability with OpenTelemetry (OTel)

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

### 2.1. Distributed Tracing
- **Protocol**: W3C TraceContext (`traceparent`, `tracestate`).
- **Implementation**:
  - Native .NET `ActivitySource` (`System.Diagnostics`).
  - Each request generates a `TraceId` and a `SpanId`.
  - Outbound HTTP calls (e.g. Frankfurter API) and message broker dispatches (RabbitMQ) automatically propagate the `traceparent` header.
  - Custom spans tag business attributes via `activity?.SetTag("key", "value")`.

### 2.2. Business and Infrastructure Metrics
- **Infrastructure Metrics**: Collected automatically via `OpenTelemetry.Instrumentation.AspNetCore` and `OpenTelemetry.Instrumentation.Http`.
- **Business Metrics**: Instrumented via `System.Diagnostics.Metrics.Meter`:
  - `marketpulse.rates.requests_total`: volume of queried currency rates.
  - `marketpulse.watchlist.created_total`: currency watch alert items created.
  - `marketpulse.watchlist.deleted_total`: currency alerts removed.

### 2.3. Structured Logging with Correlation Scopes
- **Tracing Headers**:
  - `X-Correlation-Id`: Unique transaction identifier (forwarded from client or injected at the edge).
  - `X-Trace-Id`: OpenTelemetry W3C Trace ID.
- **Logging Scope**: The `RequestTelemetryMiddleware` attaches via `logger.BeginScope`:
  - `CorrelationId`, `TraceId`, `SpanId`, `RequestMethod`, `RequestPath`.
- **OTLP Exporting**: Structured logs are streamed via OTLP directly to Grafana Loki or Aspire Dashboard with `IncludeFormattedMessage = true` and `IncludeScopes = true`.

---

## 3. Query Cheat Sheet for Incident Investigation

### A. Metric Queries in Grafana Mimir / Prometheus (`grafanacloud-bigveranda1683-prom`)
- **5xx Error Rate**:
  ```promql
  (sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m])) / sum(rate(http_server_request_duration_seconds_count[5m]))) or vector(0)
  ```
- **P95 Latency by Route**:
  ```promql
  histogram_quantile(0.95, sum by (le, http_route) (rate(http_server_request_duration_seconds_bucket[5m])))
  ```
- **Throughput by Route**:
  ```promql
  sum by (http_route, http_request_method) (rate(http_server_request_duration_seconds_count[1m]))
  ```

### B. Log Queries in Grafana Loki (`grafanacloud-bigveranda1683-logs`)
- **Search Errors and Exceptions**:
  ```logql
  {service_name="agentic-pipeline-lab-api"} |~ "(?i)fail|error|exception|status 5"
  ```
- **Filter by CorrelationId**:
  ```logql
  {service_name="agentic-pipeline-lab-api"} |= "CorrelationId: <TARGET_ID>"
  ```
- **Filter by TraceId**:
  ```logql
  {service_name="agentic-pipeline-lab-api"} |= "<TARGET_TRACE_ID>"
  ```

### C. Tracing Queries in Grafana Tempo (`grafanacloud-bigveranda1683-traces`)
- **Traces with Errors**:
  ```traceql
  { status = error }
  ```
- **Slow Traces (> 500ms)**:
  ```traceql
  { duration > 500ms }
  ```

---

## 4. Connecting to Grafana Cloud (e.g. bigveranda1683.grafana.net)

To ship telemetry directly from the .NET 9 API, Docker, or Render to Grafana Cloud:

```bash
OTEL_SERVICE_NAME="agentic-pipeline-lab-api"
OTEL_EXPORTER_OTLP_ENDPOINT="https://otlp-gateway-prod-sa-east-1.grafana.net/otlp"
OTEL_EXPORTER_OTLP_PROTOCOL="http/protobuf"
OTEL_EXPORTER_OTLP_HEADERS="Authorization=Basic <BASE64_TOKEN>"
```

### Important regarding HTTP Protobuf in .NET:
The .NET SDK with `OtlpExportProtocol.HttpProtobuf` requires each telemetry signal to append its respective route suffix:
- Traces: `{Endpoint}/v1/traces`
- Metrics: `{Endpoint}/v1/metrics`
- Logs: `{Endpoint}/v1/logs`
This is handled dynamically in [`Program.cs`](file:///d:/pipeline-dev-automatizador/src/backend/src/App.Api/Program.cs).

---

## 5. Incident Investigation Playbook, Debugging, and RCA

When an alert triggers or an anomaly is reported in production:

### Step 1: Triage and Blast Radius Assessment
1. Open the **Golden Signals** dashboard:
   - Which endpoint is failing? What HTTP status is returned (429, 500, 503)?
   - Did error volume correlate with a recent deploy or database migration?
   - What percentage of users are impacted?

### Step 2: Correlation via CorrelationId or TraceId
1. Obtain the `X-Correlation-Id` or `X-Trace-Id` from HTTP response headers or client telemetry.
2. In Grafana Loki or Aspire Dashboard, filter:
   ```logql
   {service_name="agentic-pipeline-lab-api"} |= "CorrelationId: <TARGET_ID>"
   ```
3. Open the **Traces** tab (Tempo) and paste the `TraceId`.
4. Analyze the waterfall trace:
   - Which span consumed the most time (CPU bottleneck, I/O, lock contention)?
   - Did failure originate in PostgreSQL (Npgsql), external HTTP (Frankfurter), or RabbitMQ?

### Step 3: Immediate Mitigation
- Resource saturation: scale replicas horizontally or adjust Rate Limiter partitions.
- External dependency failure: verify `HybridCache` fallback activation.
- Critical bug in recent commit: trigger instant rollback via Render or Docker.

### Step 4: Blameless Root Cause Analysis (RCA)
Document in Markdown at `docs/incidents/YYYY-MM-DD-rca.md`:
1. **Incident Summary**: Detection time, start time, resolution time (MTTD, MTTR).
2. **Technical Root Cause**: 5 Whys analysis.
3. **Corrective Action Items**: New automated regression integration test, alert tuning, and threshold adjustments.
