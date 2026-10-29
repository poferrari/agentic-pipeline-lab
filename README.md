# 📈 MarketPulse Lab: Intelligent Currency & Market Watchlist Hub

[![CI Validation](https://github.com/poferrari/agentic-pipeline-lab/actions/workflows/ci.yml/badge.svg)](https://github.com/poferrari/agentic-pipeline-lab/actions/workflows/ci.yml)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Hexagonal%20%2F%20DDD-blue.svg)](docs/architecture/ADR-001-hexagonal-public-api-and-render-containers.md)
[![Security](https://img.shields.io/badge/OWASP-API%20Top%2010-green.svg)](docs/specs/market-pulse-lab-qa-report.md)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> **Pipeline Validation Lab**: This repository represents the live testbed and final delivered product generated through the **Agentic Enterprise Pipeline** — validating autonomous end-to-end software delivery from Discovery to Production-Ready Cloud Container.

An enterprise-grade, high-performance **Modular Hexagonal Web API** built with **C# (.NET 9 Minimal APIs)**, **Domain-Driven Design (DDD)**, and **Event-Driven Architecture (EDA)**. 

The application integrates the official **Frankfurter European Central Bank Exchange Rates API**, provides resilient two-tier caching via **.NET `HybridCache`**, event publishing with **MassTransit & RabbitMQ**, and multi-tenant authentication and authorization backed by **Supabase Auth JWT**.

---

## 🏛️ Architecture Overview

The system strictly adheres to **Hexagonal Architecture (Ports and Adapters)**, ensuring the domain model remains completely decoupled from frameworks, transport layers, and persistence mechanisms:

```text
├── src/backend/
│   ├── MarketPulse.sln                # Solution configuration with strict Roslyn analyzer rules
│   ├── Directory.Build.props          # TreatWarningsAsErrors=true & Nullable=enable
│   ├── src/
│   │   ├── App.Domain/                # Pure Core: Entities, Value Objects, Domain Events, Output Ports
│   │   ├── App.Application/           # Use Cases: Commands, Queries, DTOs, FluentValidation, Result Pattern
│   │   ├── App.Infrastructure/        # Driven Adapters: Frankfurter Public API, EF Core Postgres, MassTransit RabbitMQ, HybridCache
│   │   └── App.Api/                   # Driving Adapter: ASP.NET Core Minimal APIs, Security Middleware, ProblemDetails RFC 7807
│   └── tests/
│       ├── App.UnitTests/             # 17 Unit Tests (Domain Invariants & Use Cases via NSubstitute/FluentAssertions)
│       ├── App.ArchTests/             # 3 Architecture Tests (Hexagonal layer dependency purity via NetArchTest)
│       └── App.IntegrationTests/      # 5 Integration Tests (WebApplicationFactory, Live Frankfurter API, BOLA/IDOR protection)
```

---

## ⚡ Key Features

- **Public Currency Rates Integration**: Live official European Central Bank rates via `https://api.frankfurter.app` with timeout resilience and circuit protection.
- **Two-Tier Caching**: High-efficiency `.NET HybridCache` combining in-memory L1 cache (with stampede protection) and extensible L2 persistence.
- **Currency Watchlist Hub**: Registered watch items (`CurrencyWatchItem` Aggregate Root) enriched with live spreads, target rate alerts, and trigger status.
- **OWASP API Security Top 10 Compliant**:
  - **BOLA / IDOR Protection**: Database queries strictly partitioned by authenticated user `OwnerId`.
  - **Mass Assignment Defense**: Immutable `record` request models preventing unauthorized field modifications.
  - **Security Headers & Error Hygiene**: Standard RFC 7807 `ProblemDetails` output; strict headers (`nosniff`, `DENY`, `strict-origin`).
  - **Rate Limiting**: Built-in fixed-window rate limiter preventing resource exhaustion (`60 req/min`).
- **Resilient Cloud & Container Deployment**:
  - Multi-stage Alpine container running under non-root `$APP_UID` on port `8080`.
  - Self-healing fallback: Runs in full PostgreSQL + RabbitMQ mode when connection strings are present, and seamlessly falls back to In-Memory storage and bus when deployed on single-instance free tiers (e.g., Render Free Tier).

---

## 🧪 Automated Test Suite (26 Tests Passing with Testcontainers)

All tests are executed via strict automated pipelines (`-warnaserror`):

```powershell
# Run complete validation script (Restore, Lint, Strict Build, Unit, Architecture, and Integration Tests)
powershell -ExecutionPolicy Bypass -File scripts/validate-all.ps1
```

| Suite | Projects / Frameworks | Coverage Scope | Status |
| :--- | :--- | :--- | :---: |
| **Unit Tests** | `App.UnitTests` (xUnit, FluentAssertions, NSubstitute) | Value objects, domain invariants, use case handlers | ✅ **17 Passed** |
| **Architecture Tests** | `App.ArchTests` (NetArchTest.Rules) | Hexagonal isolation (Domain has 0 external references) | ✅ **3 Passed** |
| **Integration Tests** | `App.IntegrationTests` (WebApplicationFactory + Testcontainers) | End-to-end REST endpoints, PostgreSQL 17, RabbitMQ 4, live rates, BOLA/IDOR | ✅ **6 Passed** |
| **Total** | | | ✅ **26 Passed (0 Failures, 0 Warnings)** |

---

## 🚀 Running Locally

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker & Docker Compose](https://www.docker.com/) (optional, for local PostgreSQL and RabbitMQ)

### 1. Build and Run API
```bash
cd src/backend
dotnet restore
dotnet build --configuration Release -warnaserror
dotnet run --project src/App.Api
```
The API starts on `http://localhost:8080`. Explore the interactive documentation:
- **Swagger / OpenAPI**: `http://localhost:8080/swagger`
- **Health Probes**: `http://localhost:8080/health/live` & `http://localhost:8080/health/ready`

### 2. Run with Docker Compose
To spin up the API alongside PostgreSQL 17 and RabbitMQ 4:
```bash
docker compose up -d --build
```

---

## 🌐 API Contract Reference

| Method | Endpoint | Description | Auth Required |
| :--- | :--- | :--- | :---: |
| `GET` | `/api/v1/rates/{baseCurrency}` | Returns exchange rates for a given ISO base currency | No |
| `POST` | `/api/v1/watchlist` | Creates a new currency alert item | **Yes** |
| `GET` | `/api/v1/watchlist` | Lists paginated watchlist items for the authenticated user | **Yes** |
| `GET` | `/api/v1/watchlist/{id}` | Retrieves a single watchlist item by ID | **Yes** |
| `DELETE` | `/api/v1/watchlist/{id}` | Deletes a watchlist item by ID | **Yes** |
| `GET` | `/health/live` | Container liveness check | No |
| `GET` | `/health/ready` | Container readiness check | No |

---

## ☁️ Deployment on Render

This project includes a native Render Blueprint specification ([`render.yaml`](render.yaml)):
- **Runtime**: Docker (`Dockerfile`)
- **Port**: `8080`
- **Health Check Path**: `/health/live`
- **Region**: Oregon (`oregon`)

Deploying is as simple as connecting your GitHub repository to Render with the blueprint enabled.
