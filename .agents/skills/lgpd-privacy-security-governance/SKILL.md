---
name: lgpd-privacy-security-governance
description: Technical guidelines, automated checklists, PII redaction patterns, and architectural standards for Brazilian LGPD (Lei 13.709/2018), GDPR, privacy-by-design, data minimization, right to be forgotten, and legal risk governance.
---

# Technical Skill: LGPD, Privacy & Sensitive Data Governance (`lgpd-privacy-security-governance`)

Use this skill whenever architecting, creating, or reviewing database schemas, backend logging pipelines, API contracts, telemetry exporters, or frontend forms to enforce strict compliance with **Brazilian Data Protection Law (LGPD - Lei Federal nº 13.709/2018)** and international privacy standards.

---

## 1. Concrete PII Classification & Technical Controls

| Category | Data Elements | Classification | Mandatory Technical Controls |
| :--- | :--- | :--- | :--- |
| **Personal Identifiers** | Full Name, CPF, RG, Voter ID, Passport | **High (PII)** | Never log in plaintext; index with HMAC-SHA256 if searchable; RLS tenant isolation |
| **Contact Data** | Email, Phone Number, WhatsApp, Home Address | **High (PII)** | Anonymize in telemetry; mask in UI (`po****@gmail.com`); validate consent |
| **Credentials & Auth** | Passwords, API Secrets, Session Tokens, MFA Keys | **Critical (Secret)** | Never store custom; delegate to Supabase Auth; zero client bundle exposure |
| **Financial Data** | Bank Account, Credit Card Number, CVV, PIX Key | **Critical (Sensitive)** | Masking (last 4 digits only); tokenize via PCI-DSS compliant providers |
| **Telemetry / SRE** | IP Address, User Agent, Machine ID | **Medium (Indirect PII)** | Truncate IP in logs (`192.168.1.***`); partition by anonymized `UserId` UUID |

---

## 2. Technical Recipes for Privacy by Design

### 2.1. C# Backend Logging & Telemetry Scrubbing
Never pass entity objects containing PII directly to `ILogger` or OpenTelemetry:
```csharp
// ❌ VIOLATION: Leaks customer email and raw IP into Grafana / Loki logs
logger.LogInformation("Processing alert for user {Email} from {Ip}", user.Email, ipAddress);

// ✅ COMPLIANT: Use opaque surrogate UUID or SHA-256 hash
logger.LogInformation("Processing alert for user ID {UserId}", user.Id);
```

### 2.2. Supabase / PostgreSQL Row-Level Security (RLS)
Every table holding customer-owned data must have RLS enabled with explicit tenant isolation:
```sql
-- Ensure RLS is active
ALTER TABLE public.currency_watch_items ENABLE ROW LEVEL SECURITY;

-- Isolate queries to the authenticated Supabase user
CREATE POLICY "Users can only read own watch items"
  ON public.currency_watch_items
  FOR SELECT
  USING (auth.uid()::text = user_id);

CREATE POLICY "Users can only insert own watch items"
  ON public.currency_watch_items
  FOR INSERT
  WITH CHECK (auth.uid()::text = user_id);

CREATE POLICY "Users can only delete own watch items"
  ON public.currency_watch_items
  FOR DELETE
  USING (auth.uid()::text = user_id);
```

### 2.3. Right to be Forgotten (Direito ao Esquecimento - Art. 18, VI)
Provide transactional cascaded deletion or irreversible anonymization:
- When a user requests account deletion, all dependent records (watch items, notifications, logs containing identifiers) must be permanently purged or pseudonimized.

---

## 3. LGPD Audit Checklist for Code Review

1. [ ] **No PII in URLs**: Query strings must not contain emails, names, or CPFs (use HTTP Request Body or headers).
2. [ ] **No Plaintext Passwords**: Password management delegated 100% to Supabase Auth.
3. [ ] **BOLA / IDOR Verification**: Endpoints taking an `{id}` in the path verify that `item.UserId == authenticatedUser.Id`.
4. [ ] **Data Minimization (Art. 6, III)**: Only attributes essential for feature delivery are stored in PostgreSQL.
5. [ ] **Consent & Purpose (Art. 6, I)**: Clearly inform users why their data is processed.
6. [ ] **Audit Logs**: Administrative or security-sensitive actions generate immutable audit events without logging the sensitive data itself.
