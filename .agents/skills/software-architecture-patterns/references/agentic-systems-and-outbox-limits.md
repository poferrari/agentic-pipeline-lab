# Agentic System Architecture and Outbox Limits

Reference for the `software-architecture-patterns` skill. Load only when the task touches this area.

## 10. Enterprise Agentic System Architecture & Failure Modes (Mercado Libre & Industry Standards)

1. **Taxonomy & Golden Rule of Agentic Workflows**:
   - **Pure LLM**: Passive prompt $\rightarrow$ text inference (zero tools, isolated).
   - **Deterministic AI Workflow (DAG)**: Code-orchestrated sequential/parallel pipeline where LLM only classifies or extracts structured data. Deterministic, low-latency, and predictable. **Covers $\ge 90\%$ of enterprise needs**.
   - **Autonomous AI Agent**: Goal + Scoped Tools + Cognitive Loop (Plan $\rightarrow$ Act $\rightarrow$ Observe $\rightarrow$ Reflect). Probabilistic and non-deterministic.
   - **Architectural Directive**: **NEVER** use an autonomous agent when a deterministic workflow can solve the problem. Agents introduce unbounded latency, token burn, and non-deterministic branching.

2. **Identity-Scoped Tool Architecture**:
   - Prohibit raw database query tools or arbitrary ID parameters passed from LLMs (e.g. `GetUser(userId)` or `ExecuteSql(query)`).
   - Tools must be **Identity-Scoped**: The user identifier must be resolved strictly from the authenticated session context (e.g. Supabase JWT `userContext.UserId`), making privilege escalation via prompt injection mathematically impossible.

3. **Reversible vs. Irreversible Actions (The Ctrl+Z Principle)**:
   - **Reversible Actions**: Autonomous execution allowed (e.g., creating draft tickets, querying state, generating previews).
   - **Irreversible Actions**: Financial transfers, account deletions, mass notifications. Mandatory **Human-in-the-Loop (HITL)**.
   - **Durable Asynchronous Sagas**: Irreversible intents must be published to a message broker (RabbitMQ), generating an approval ticket. The agent's execution state is checkpointed in database storage until the human decision event is received.

4. **Automation Complacency & Canary Safeguards**:
   - High-accuracy agents ($\ge 93\%$) cause human reviewers to slip into superficial rubber-stamping (review time dropping from 30s to 3s).
   - System must inject synthetic canary/honeypot test cases into human review queues to measure and enforce human reviewer vigilance.

5. **Statistical Compounding Failure in Multi-Step Chains**:
   - For an agent performing $N$ sequential probabilistic steps with step accuracy $P$:
     $$P_{\text{total}} = P^N$$
   - A 10-step agent with $95\%$ step accuracy yields only $0.95^{10} \approx 59.8\%$ overall success (over $40\%$ failure rate in production).
   - Architecture must bound $N \le 3-5$ steps and anchor intermediate transitions with deterministic code, typed schema contracts, and domain invariant guards.

6. **FinOps Circuit Breakers & AI-to-AI Infinite Loop Protections**:
   - Enforce hard limits on agent loops (`MaxIterations \le 5`).
   - Implement dollar budget circuit breakers per hour/day per tenant/agent to prevent runaway token billing (e.g., two automated agents negotiating in an infinite loop).

---

## 11. Transactional Outbox Limits, Indeterminate States (`UNKNOWN`), and Distributed Resilience (Dennis Rojas Blueprint)

1. **The Fundamental Boundary of the Outbox Pattern**:
   - The Transactional Outbox guarantees **ONLY** the local boundary: atomic persistence between state mutation and event publishing intent.
   - It does **NOT** guarantee end-to-end distributed transaction success, downstream processing, nor strict global ordering across independent consumers.

2. **The "Outbox in a Separate Database" Anti-Pattern**:
   - Moving the Outbox table to an isolated database instance destroys atomicity and resurrects the *Dual-Write Problem*.
   - Outbox tables **MUST** share the identical local database transaction as the domain aggregate mutation.

3. **Post-Outbox Failure Modes & Mandatory Defenses**:
   - **At-Least-Once Redelivery**: Brokers may redeliver events upon transient network hiccups during ACK. Every downstream consumer must implement **strict business idempotency** (e.g. Inbox pattern or distributed idempotency keys).
   - **Indeterminate Network States (`UNKNOWN` is not `FAILED`)**:
     - Timeouts (`HTTP 504`, socket resets) on external mutating calls (e.g. banking/Pix) mean the remote state is unknown—the money may already have transferred.
     - Blind retries cause catastrophic double-charging. State machines must model `UNKNOWN` / `IN_ANALYSIS` and switch to status queries or manual review.
   - **Dead Letter Queue (DLQ) Traps**: Messages that exceed retry budgets must be routed to DLQs with automated alerting and scheduled operational reconciliation.

4. **Continuous Asynchronous Reconciliation**:
   - Mature distributed architectures do not chase the myth of a magical, all-encompassing ACID transaction across microservices.
   - Design systems to record audit evidence at each state transition and implement background **Reconciliation Engines** continuously aligning internal state with upstream/partner authoritative sources of truth.
