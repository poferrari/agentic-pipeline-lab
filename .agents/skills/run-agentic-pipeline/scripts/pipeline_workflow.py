# Multi-Agent Orchestration Pipeline (run_workflow)
# This script can be invoked via `run_workflow` to orchestrate
# Discovery, Architecture, Implementation, and QA/OWASP Review phases.

feature_request = args.get("feature", "Analyze workspace structure and plan MVP implementation")

phase("1. Proactive Discovery, Requirements (FR/NFR), and Risk Forecasting")
discovery_schema = {
    "type": "object",
    "properties": {
        "spec_path": {"type": "string"},
        "open_questions": {"type": "array", "items": {"type": "string"}},
        "risks_identified": {"type": "array", "items": {"type": "string"}},
        "dor_business_ready": {"type": "boolean"}
    },
    "required": ["spec_path", "open_questions", "risks_identified", "dor_business_ready"]
}

discovery = await agent(
    f"""Act as product-requirements-analyst using the discovery-and-spec skill.
    Context/Demand: {feature_request}
    1. Analyze project files.
    2. Gather all Functional Requirements (FR), Non-Functional Requirements (NFR), Open Questions, and Risks with solutions.
    3. Write or update the Spec in docs/specs/.
    Return the Spec path, open questions, and mapped risks.""",
    schema=discovery_schema,
    role="Product Requirements Analyst",
    type_name="product-requirements-analyst"
)
log(f"Spec generated at: {discovery['spec_path']}")

phase("2. DDD/Hexagonal Architecture, REST/RabbitMQ Contracts, and Tasks (DoR Gate)")
arch_schema = {
    "type": "object",
    "properties": {
        "tasks_path": {"type": "string"},
        "dor_passed": {"type": "boolean"},
        "summary": {"type": "string"}
    },
    "required": ["tasks_path", "dor_passed", "summary"]
}

architecture = await agent(
    f"""Act as domain-architect-planner.
    Complete technical modeling in Spec {discovery['spec_path']} (DDD Aggregates, Value Objects, Minimal API REST, RabbitMQ Outbox, PostgreSQL, HybridCache, Frontend TanStack Query)
    and generate the atomic Tasks file following docs/templates/TASKS_TEMPLATE.md.
    Validate 100% of Definition of Ready (docs/pipeline/DEFINITION_OF_READY.md).""",
    schema=arch_schema,
    role="Domain Architect & Planner",
    type_name="domain-architect-planner"
)
log(f"Tasks generated at: {architecture['tasks_path']} (DoR Passed: {architecture['dor_passed']})")

phase("3. Backend C# & Frontend TypeScript Implementation")
dev_schema = {
    "type": "object",
    "properties": {
        "files_changed": {"type": "array", "items": {"type": "string"}},
        "tests_passed": {"type": "boolean"},
        "test_summary": {"type": "string"}
    },
    "required": ["files_changed", "tests_passed", "test_summary"]
}

backend_res = await agent(
    f"""Act as backend-csharp-developer following Spec {discovery['spec_path']} and Tasks {architecture['tasks_path']}.
    Implement Backend C# (.NET Minimal API, Hexagonal Architecture, DDD, EF Core Postgres, MassTransit RabbitMQ, HybridCache, Supabase JWT)
    and execute automated tests (Unit, Arch, Integration).""",
    schema=dev_schema,
    role="Backend C# Developer",
    type_name="backend-csharp-developer"
)

frontend_res = await agent(
    f"""Act as frontend-web-developer following Spec {discovery['spec_path']} and Tasks {architecture['tasks_path']}.
    Implement Frontend (React/Next.js, TypeScript, TanStack Query, Zod, Supabase Auth)
    and execute automated tests (Vitest, Testing Library, MSW).""",
    schema=dev_schema,
    role="Frontend Web Developer",
    type_name="frontend-web-developer"
)

phase("4. QA Audit, OWASP Top 10 Security, and Pull Request Report (DoD Gate)")
qa_schema = {
    "type": "object",
    "properties": {
        "dod_passed": {"type": "boolean"},
        "owasp_verified": {"type": "boolean"},
        "blockers": {"type": "array", "items": {"type": "string"}},
        "pr_report_path": {"type": "string"}
    },
    "required": ["dod_passed", "owasp_verified", "blockers", "pr_report_path"]
}

qa_res = await agent(
    f"""Act as qa-security-reviewer using owasp-quality-gate and pr-delivery-automation skills.
    Audit generated code against Spec {discovery['spec_path']} and docs/pipeline/DEFINITION_OF_DONE.md.
    Generate the full Pull Request report including test execution evidence.""",
    schema=qa_schema,
    role="QA & OWASP Security Reviewer",
    type_name="qa-security-reviewer"
)
log(f"Pipeline finished! DoD: {qa_res['dod_passed']} | PR Report: {qa_res['pr_report_path']}")
