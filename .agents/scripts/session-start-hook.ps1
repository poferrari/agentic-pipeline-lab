[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "SilentlyContinue"
$output = @{
  injectSteps = @(
    @{
      ephemeralMessage = "Esteira Agêntica Ativa: Respeite AGENTS.md (Discovery -> Spec/DoR -> Hexagonal DDD C# Minimal API + RabbitMQ + Postgres + Supabase Auth + React/TanStack Query -> Testes + OWASP/DoD -> PR)."
    }
  )
}
$output | ConvertTo-Json -Depth 5 -Compress

