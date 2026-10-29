# Unified Quality Gate for Agentic Pipeline (validate-all.ps1)
# Executes Linting, Strict Compilation (-warnaserror), Security Audit, and Automated Tests

$ErrorActionPreference = "Stop"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " [QUALITY GATE] STARTING AGENTIC PIPELINE VALIDATION" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Verification of Secrets in Git
Write-Host "`n[1/4] Verifying security hygiene and sensitive tracked files..." -ForegroundColor Yellow
if (Test-Path ".git") {
    $trackedEnv = git ls-files ".env" "appsettings.Local.json"
    if ($trackedEnv) {
        Write-Error "OWASP BLOCKER: Sensitive configuration file ($trackedEnv) is tracked by Git!"
        exit 1
    }
}
Write-Host "[OK] No sensitive .env or local configuration files tracked." -ForegroundColor Green

# 2. Backend Validation (.NET C# Minimal API + Hexagonal + DDD)
Write-Host "`n[2/4] Validating Backend C# (.NET)..." -ForegroundColor Yellow
$backendCandidates = @("src/backend")
if (Test-Path "sandbox") {
    $sandboxBackends = Get-ChildItem -Path "sandbox" -Directory | ForEach-Object { Join-Path $_.FullName "src/backend" } | Where-Object { Test-Path $_ }
    $backendCandidates += $sandboxBackends
}

$validatedAnyBackend = $false
foreach ($backendDir in $backendCandidates) {
    if (Test-Path $backendDir) {
        $slnFiles = @(Get-ChildItem -Path $backendDir -Include "*.sln", "*.slnx", "*.csproj" -Recurse -Depth 2 -ErrorAction SilentlyContinue)
        if ($slnFiles.Count -gt 0) {
            $validatedAnyBackend = $true
            Write-Host " -> Validating solution in: $backendDir" -ForegroundColor Cyan
            Push-Location $backendDir
            try {
                Write-Host " -> Running dotnet restore..."
                dotnet restore
                if ($LASTEXITCODE -ne 0) { throw "Failed dotnet restore ($backendDir)" }

                Write-Host " -> Verifying code formatting and style (dotnet format)..."
                dotnet format --verify-no-changes --no-restore
                if ($LASTEXITCODE -ne 0) { throw "Failed dotnet format ($backendDir)." }

                Write-Host " -> Compiling in strict mode (Zero Warnings = -warnaserror)..."
                dotnet build --configuration Release --no-restore -warnaserror
                if ($LASTEXITCODE -ne 0) { throw "Failed strict backend build ($backendDir)." }

                Write-Host " -> Running Automated Tests (Unit, Architecture, and Integration)..."
                dotnet test --configuration Release --no-build --logger "console;verbosity=normal"
                if ($LASTEXITCODE -ne 0) { throw "Failed automated backend tests ($backendDir)." }

                Write-Host "[OK] Backend C# ($backendDir) approved with 0 errors and 0 warnings!" -ForegroundColor Green
            } finally {
                Pop-Location
            }
        }
    }
}

if (-not $validatedAnyBackend) {
    Write-Host "[INFO] No backend solution found in src/backend or sandbox/*/src/backend." -ForegroundColor DarkGray
}

# 3. Frontend Validation (React / Next.js + TypeScript + TanStack Query + Vitest)
Write-Host "`n[3/4] Validating Frontend (TypeScript / React)..." -ForegroundColor Yellow
if (Test-Path "src/frontend/package.json") {
    Push-Location "src/frontend"
    try {
        Write-Host " -> Running Linter (npm run lint)..."
        npm run lint
        if ($LASTEXITCODE -ne 0) { throw "Failed Frontend ESLint." }

        Write-Host " -> Running strict Typecheck (npm run typecheck)..."
        npm run typecheck
        if ($LASTEXITCODE -ne 0) { throw "Failed TypeScript type checking." }

        Write-Host " -> Running Automated Tests (npm run test:run)..."
        npm run test:run
        if ($LASTEXITCODE -ne 0) { throw "Failed Frontend Vitest/MSW tests." }

        Write-Host "[OK] Frontend approved with 0 errors!" -ForegroundColor Green
    } finally {
        Pop-Location
    }
} else {
    Write-Host "[INFO] Frontend (src/frontend/package.json) not yet initialized." -ForegroundColor DarkGray
}

# 4. Pipeline Governance Documentation Integrity
Write-Host "`n[4/4] Verifying Quality Gates governance integrity (DoR / DoD / AGENTS.md)..." -ForegroundColor Yellow
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
        Write-Error "Required pipeline governance file missing: $file"
        exit 1
    }
}
Write-Host "[OK] All pipeline governance artifacts are present and intact!" -ForegroundColor Green

Write-Host "`n=================================================================" -ForegroundColor Green
Write-Host " [SUCCESS] QUALITY GATE COMPLETED WITH 100% APPROVAL!" -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
