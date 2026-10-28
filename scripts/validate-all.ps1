# Quality Gate Unificado da Esteira Agentica (validate-all.ps1)
# Executa Lint, Compilacao Estrita (-warnaserror), Auditoria de Seguranca e Testes Automatizados

$ErrorActionPreference = "Stop"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " [QUALITY GATE] INICIANDO VALIDACAO DA ESTEIRA AGENTICA" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Verificacao de Segredos Expostos no Git
Write-Host "`n[1/4] Verificando higiene de seguranca e arquivos sensiveis..." -ForegroundColor Yellow
if (Test-Path ".git") {
    $trackedEnv = git ls-files ".env" "appsettings.Local.json"
    if ($trackedEnv) {
        Write-Error "BLOQUEIO OWASP: Arquivo de variaveis sensiveis ($trackedEnv) esta rastreado pelo Git!"
        exit 1
    }
}
Write-Host "[OK] Nenhum arquivo .env sensivel rastreado." -ForegroundColor Green

# 2. Validacao do Backend (.NET C# Minimal API + Hexagonal + DDD)
Write-Host "`n[2/4] Validando Backend C# (.NET)..." -ForegroundColor Yellow
if (Test-Path "src/backend") {
    $slnFiles = @(Get-ChildItem -Path "src/backend" -Include "*.sln", "*.slnx", "*.csproj" -Recurse -Depth 2 -ErrorAction SilentlyContinue)
    if ($slnFiles.Count -gt 0) {
        Push-Location "src/backend"
        try {
            Write-Host " -> Executando dotnet restore..."
            dotnet restore
            if ($LASTEXITCODE -ne 0) { throw "Falha no dotnet restore" }

            Write-Host " -> Verificando formatacao e estilo (dotnet format)..."
            dotnet format --verify-no-changes --no-restore
            if ($LASTEXITCODE -ne 0) { throw "Falha no dotnet format. Rode 'dotnet format' para corrigir." }

            Write-Host " -> Compilando em modo estrito (Zero Warnings = -warnaserror)..."
            dotnet build --configuration Release --no-restore -warnaserror
            if ($LASTEXITCODE -ne 0) { throw "Falha na compilacao estrita do Backend." }

            Write-Host " -> Executando Testes Automatizados (Unidade, Arquitetura e Integracao)..."
            dotnet test --configuration Release --no-build --logger "console;verbosity=normal"
            if ($LASTEXITCODE -ne 0) { throw "Falha nos testes automatizados do Backend." }

            Write-Host "[OK] Backend C# aprovado com 0 erros e 0 warnings!" -ForegroundColor Green
        } finally {
            Pop-Location
        }
    } else {
        Write-Host "[INFO] Pasta src/backend encontrada, mas sem projetos .csproj/.sln ainda." -ForegroundColor DarkGray
    }
} else {
    Write-Host "[INFO] Pasta src/backend ainda nao inicializada. Use 'scripts/scaffold-monolith.ps1' quando for iniciar a codificacao." -ForegroundColor DarkGray
}

# 3. Validacao do Frontend (React / Next.js + TypeScript + TanStack Query + Vitest)
Write-Host "`n[3/4] Validando Frontend (TypeScript / React)..." -ForegroundColor Yellow
if (Test-Path "src/frontend/package.json") {
    Push-Location "src/frontend"
    try {
        Write-Host " -> Executando Linter (npm run lint)..."
        npm run lint
        if ($LASTEXITCODE -ne 0) { throw "Falha no ESLint do Frontend." }

        Write-Host " -> Executando Typecheck estrito (npm run typecheck)..."
        npm run typecheck
        if ($LASTEXITCODE -ne 0) { throw "Falha na checagem de tipos TypeScript." }

        Write-Host " -> Executando Testes Automatizados (npm run test:run)..."
        npm run test:run
        if ($LASTEXITCODE -ne 0) { throw "Falha nos testes Vitest/MSW do Frontend." }

        Write-Host "[OK] Frontend aprovado com 0 erros!" -ForegroundColor Green
    } finally {
        Pop-Location
    }
} else {
    Write-Host "[INFO] Frontend (src/frontend/package.json) ainda nao inicializado." -ForegroundColor DarkGray
}

# 4. Verificacao da Estrutura Documental da Esteira
Write-Host "`n[4/4] Verificando integridade dos Quality Gates (DoR / DoD / AGENTS.md)..." -ForegroundColor Yellow
$requiredFiles = @(
    "AGENTS.md",
    "docs/pipeline/DEFINITION_OF_READY.md",
    "docs/pipeline/DEFINITION_OF_DONE.md",
    "docs/templates/SPEC_TEMPLATE.md",
    "docs/templates/TASKS_TEMPLATE.md",
    ".github/PULL_REQUEST_TEMPLATE.md",
    ".github/workflows/ci.yml"
)
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        Write-Error "Arquivo obrigatorio da esteira ausente: $file"
        exit 1
    }
}
Write-Host "[OK] Todos os artefatos de governanca da esteira estao presentes e integros!" -ForegroundColor Green

Write-Host "`n=================================================================" -ForegroundColor Green
Write-Host " [SUCESSO] QUALITY GATE CONCLUIDO COM 100% DE APROVACAO!" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
