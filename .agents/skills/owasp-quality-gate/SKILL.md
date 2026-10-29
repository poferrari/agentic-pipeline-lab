---
name: owasp-quality-gate
description: >-
  Use esta skill antes de concluir qualquer implementação ou durante a revisão de código para auditar
  segurança (OWASP Top 10 e OWASP API Security Top 10), Clean Code, KISS, Arquitetura Hexagonal,
  cobertura de testes e validar a Definition of Done (DoD).
---

# Auditoria de Qualidade e Segurança OWASP (`owasp-quality-gate`)

Esta skill atua como o **Quality Gate Automatizado** da esteira. Execute todas as verificações abaixo antes de abrir ou aprovar um Pull Request.

---

## 1. Checklist de Segurança OWASP API Security Top 10 (Verificação no Código)

| Código OWASP | Vulnerabilidade | O que verificar obrigatoriamente no Backend C# e Frontend TS |
| :--- | :--- | :--- |
| **API1:2023** | **Broken Object Level Authorization (BOLA / IDOR)** | Toda rota com `{id}` (`/api/v1/orders/{id}`) passa o `currentUser.Id` (do JWT Supabase) para a query do repositório? É impossível ler/alterar dados de outro usuário trocando o UUID na URL? |
| **API2:2023** | **Broken Authentication** | O `JwtBearer` valida `Issuer`, `Audience`, `Lifetime` e assinatura JWKS do Supabase? O Frontend usa `@supabase/ssr` sem expor tokens em URLs? |
| **API3:2023** | **Broken Object Property Level Authorization (Mass Assignment)** | Os endpoints Minimal API recebem `sealed record` Request DTOs específicos? Nenhuma entidade de domínio ou modelo EF Core é exposto diretamente no binding HTTP? |
| **API4:2023** | **Unrestricted Resource Consumption** | Todas as queries de listagem têm `PageSize` limitado (ex.: `RuleFor(x => x.PageSize).InclusiveBetween(1, 100)`)? Rate Limiting (`UseRateLimiter`) e tamanho máximo de payload estão ativos? |
| **API5:2023** | **Broken Function Level Authorization** | Todos os grupos de endpoints exigem `.RequireAuthorization()` por padrão, exceto rotas explicitamente públicas e documentadas? |
| **API6:2023** | **Unrestricted Access to Sensitive Business Flows** | Fluxos críticos (pagamentos, reservas, criação de pedidos) exigem `Idempotency-Key` e proteção contra automação abusiva? |
| **API7:2023** | **Server Side Request Forgery (SSRF)** | Se o backend faz chamadas HTTP externas baseadas em input, a URL passa por allowlist estrita de domínios e bloqueio de IPs privados/loopback? |
| **API8:2023** | **Security Misconfiguration** | `CORS` restrito às origens configuradas (sem `AllowAnyOrigin()` com credenciais)? `ProblemDetails` oculta StackTrace quando `IsProduction()`? Security Headers (`X-Content-Type-Options`, `Strict-Transport-Security`) ativos? |
| **API9:2023** | **Improper Inventory Management** | Rotas estão versionadas (`/api/v1/...`) e documentadas no OpenAPI? Endpoints de debug/teste estão desabilitados em produção? |
| **API10:2023** | **Unsafe Consumption of APIs** | Integrações com terceiros e consumidores RabbitMQ validam o payload recebido, têm `Timeout`, `Retry` e `CircuitBreaker` (`Microsoft.Extensions.Http.Resilience`)? |

---

## 2. Checklist de Clean Code, KISS e Arquitetura Hexagonal

1. **Zero Dependências Indevidas**: `App.Domain` não possui `using` de infraestrutura.
2. **Sem Exceções para Fluxo de Negócio**: Handlers retornam `Result<T>`.
3. **Sem Segredos no Git**: Verifique com `git status` e `git diff` se nenhum arquivo `.env`, senha de banco ou chave privada foi incluído.
4. **Queries Seguras e Performáticas**: Sem concatenação de strings em SQL (prevenção total de SQL Injection via EF Core / Dapper parametrizado), uso de `.AsNoTracking()` em leituras.

---

## 3. Execução do Script Unificado de Validação

Sempre execute o script de validação da esteira para comprovar que build, lint, testes de unidade, arquitetura e integração estão passando:

```powershell
pwsh -File scripts/validate-all.ps1
```
Se qualquer etapa falhar, corrija a causa raiz antes de prosseguir.

---

## 4. Defesa em Profundidade e Bateria Pentest Automatizada

Para arquitetura de defesa perimétrica, mitigação de origin bypass no Render e testes defensivos automatizados, consulte:
* Documento mestre: `docs/security/DEFENSE_IN_DEPTH_AND_HARDENING_GUIDE.md`
* Bateria de testes de segurança: `src/backend/tests/App.IntegrationTests/Security/SecurityHardeningAndPentestVerificationTests.cs`

