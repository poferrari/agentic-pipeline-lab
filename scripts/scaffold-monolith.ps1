# Script de Bootstrap da Arquitetura Monolito Hexagonal (.NET C# + Frontend)
# Cria a estrutura padrao de pastas e configuracoes para iniciar a codificacao em segundos.

param(
    [string]$SolutionName = "App"
)

$ErrorActionPreference = "Stop"

Write-Host "[BOOTSTRAP] Inicializando estrutura Hexagonal DDD para '$SolutionName'..." -ForegroundColor Cyan

$dirs = @(
    "docs/specs",
    "src/backend/src",
    "src/backend/tests",
    "src/frontend"
)
foreach ($d in $dirs) {
    New-Item -ItemType Directory -Force -Path $d | Out-Null
}

$buildProps = @"
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
  </PropertyGroup>
</Project>
"@
Set-Content -Path "src/backend/Directory.Build.props" -Value $buildProps -Encoding UTF8

Write-Host "[OK] Arquivo src/backend/Directory.Build.props configurado (Nullable=enable, TreatWarningsAsErrors=true)." -ForegroundColor Green
Write-Host "[INFO] Quando desejar criar os projetos .NET (Domain, Application, Infrastructure, Api e Tests), o agente 'backend-csharp-developer' executara os comandos conforme a Spec aprovada." -ForegroundColor Yellow
