# Pipeline de Orquestração Multi-Agente (run_workflow)
# Este script pode ser invocado pela ferramenta `run_workflow` para orquestrar
# as fases de Discovery, Arquitetura, Implementação e Revisão QA/OWASP.

feature_request = args.get("feature", "Analisar a estrutura do projeto na pasta atual e planejar o MVP")

phase("1. Discovery Proativo, Requisitos (RF/RNF) e Previsão de Riscos")
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
    f"""Atue como product-requirements-analyst usando a skill discovery-and-spec.
    Contexto/Demanda: {feature_request}
    1. Analise os arquivos do projeto.
    2. Levante todos os Requisitos Funcionais (RF), Não Funcionais (RNF), Open Questions e Riscos com soluções.
    3. Escreva ou atualize a Spec em docs/specs/.
    Retorne o caminho da Spec, questões em aberto e riscos mapeados.""",
    schema=discovery_schema,
    role="Product Requirements Analyst",
    type_name="product-requirements-analyst"
)
log(f"Spec gerada em: {discovery['spec_path']}")

phase("2. Arquitetura DDD/Hexagonal, Contratos REST/RabbitMQ e Tasks (DoR Gate)")
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
    f"""Atue como domain-architect-planner.
    Complete a modelagem técnica na Spec {discovery['spec_path']} (DDD Aggregates, Value Objects, Minimal API REST, RabbitMQ Outbox, PostgreSQL, HybridCache, Frontend TanStack Query)
    e gere o arquivo de Tasks atômicas seguindo docs/templates/TASKS_TEMPLATE.md.
    Valide 100% da Definition of Ready (docs/pipeline/DEFINITION_OF_READY.md).""",
    schema=arch_schema,
    role="Domain Architect & Planner",
    type_name="domain-architect-planner"
)
log(f"Tasks geradas em: {architecture['tasks_path']} (DoR Passed: {architecture['dor_passed']})")

phase("3. Implementação Backend C# & Frontend TypeScript")
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
    f"""Atue como backend-csharp-developer seguindo a Spec {discovery['spec_path']} e Tasks {architecture['tasks_path']}.
    Implemente o Backend C# (.NET Minimal API, Arquitetura Hexagonal, DDD, EF Core Postgres, MassTransit RabbitMQ, HybridCache, Supabase JWT)
    e execute os testes automatizados (Unit, Arch, Integration).""",
    schema=dev_schema,
    role="Backend C# Developer",
    type_name="backend-csharp-developer"
)

frontend_res = await agent(
    f"""Atue como frontend-web-developer seguindo a Spec {discovery['spec_path']} e Tasks {architecture['tasks_path']}.
    Implemente o Frontend (React/Next.js, TypeScript, TanStack Query, Zod, Supabase Auth)
    e execute os testes automatizados (Vitest, Testing Library, MSW).""",
    schema=dev_schema,
    role="Frontend Web Developer",
    type_name="frontend-web-developer"
)

phase("4. Auditoria QA, Segurança OWASP Top 10 e Relatório de Pull Request (DoD Gate)")
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
    f"""Atue como qa-security-reviewer usando as skills owasp-quality-gate e pr-delivery-automation.
    Audite o código produzido contra a Spec {discovery['spec_path']} e docs/pipeline/DEFINITION_OF_DONE.md.
    Gere o relatório completo de Pull Request com o resultado dos testes executados.""",
    schema=qa_schema,
    role="QA & OWASP Security Reviewer",
    type_name="qa-security-reviewer"
)
log(f"Pipeline concluída! DoD: {qa_res['dod_passed']} | Relatório PR: {qa_res['pr_report_path']}")

