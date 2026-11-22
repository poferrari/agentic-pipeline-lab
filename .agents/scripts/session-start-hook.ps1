[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "SilentlyContinue"
$output = @{
  injectSteps = @(
    @{
      ephemeralMessage = "Agentic pipeline active. Follow AGENTS.md (Discovery -> Spec/DoR -> Hexagonal DDD C# Minimal API + RabbitMQ + Postgres + Supabase Auth + React/TanStack Query -> Tests + OWASP/DoD -> PR). Repository artifacts are English (EN-US); chat with the user is Portuguese."
    }
  )
}
$output | ConvertTo-Json -Depth 5 -Compress

