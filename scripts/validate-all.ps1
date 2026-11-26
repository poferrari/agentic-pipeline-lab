# Unified quality gate for the agentic pipeline. Its exit code is the ONLY accepted proof that checks pass.
#
#   powershell -File scripts/validate-all.ps1             # full gate (needs Docker for integration tests)
#   powershell -File scripts/validate-all.ps1 -Fast       # skips integration tests, CRAP and mutation (reported as NOT RUN)
#   powershell -File scripts/validate-all.ps1 -Mutation   # adds Stryker.NET mutation testing (slow)
#
# Every step ends as PASS, FAIL, or NOT RUN. NOT RUN is never reported as success: the gate exits 1 on any
# FAIL, and 2 when nothing failed but something did not run (only a fully green run is a green gate).
# CI calls the same commands (parity rule, AGENTS.md section 5).

param(
    [switch]$Fast,
    [switch]$Mutation
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot
. (Join-Path $repoRoot ".agents/scripts/safety-policy.ps1")

$results = [ordered]@{}

function Invoke-Step([string]$name, [scriptblock]$action) {
    Write-Host "`n>> $name" -ForegroundColor Yellow
    try {
        $status = & $action
        $results[$name] = if ($status -eq "NOT RUN") { "NOT RUN" } else { "PASS" }
    } catch {
        Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
        $results[$name] = "FAIL"
    }
}

function Assert-ExitCode([string]$what) {
    if ($LASTEXITCODE -ne 0) { throw "$what failed (exit code $LASTEXITCODE)." }
}

function Test-Tool([string]$name) { return [bool](Get-Command $name -ErrorAction SilentlyContinue) }

$backendDir = Join-Path $repoRoot "src/backend"
$frontendDir = Join-Path $repoRoot "src/frontend"

# --- Governance and policy ---------------------------------------------------------------------------
Invoke-Step "Governance artifacts present" {
    $required = @(
        "AGENTS.md", "CLAUDE.md", "docs/pipeline/DEFINITION_OF_READY.md", "docs/pipeline/DEFINITION_OF_DONE.md",
        "docs/templates/SPEC_TEMPLATE.md", "docs/templates/TASKS_TEMPLATE.md", ".github/PULL_REQUEST_TEMPLATE.md",
        ".github/workflows/ci.yml", ".agents/scripts/safety-policy.ps1", ".jscpd.json", "src/backend/stryker-config.json"
    )
    $missing = $required | Where-Object { -not (Test-Path $_) }
    if ($missing) { throw "Missing governance files: $($missing -join ', ')" }
}

Invoke-Step "Safety hook policy tests" {
    & powershell -NoProfile -ExecutionPolicy Bypass -File ".agents/scripts/tests/safety-policy.tests.ps1"; Assert-ExitCode "Safety policy tests"
    & powershell -NoProfile -ExecutionPolicy Bypass -File ".agents/scripts/tests/hooks.tests.ps1"; Assert-ExitCode "Hook entry point tests"
}

Invoke-Step "English-only repository artifacts (EN-US)" {
    if (-not (Test-Tool "python")) { throw "python is required for the English-only check." }
    python -I scripts/quality/check_english_only.py; Assert-ExitCode "English-only check"
}

Invoke-Step "Test hygiene gate (ambient clock, fixed waits, prompt safety)" {
    if (-not (Test-Tool "python")) { throw "python is required for the test hygiene gate." }
    python -I -m unittest discover scripts/quality/tests; Assert-ExitCode "quality gate unit tests"
    python -I scripts/quality/check_test_hygiene.py; Assert-ExitCode "test hygiene gate"
    python -I scripts/quality/check_prompt_safety.py; Assert-ExitCode "prompt safety audit"
}

Invoke-Step "Secrets hygiene" {
    $trackedSecrets = git ls-files ".env" "appsettings.Local.json" "appsettings.Development.json"
    if ($trackedSecrets) { throw "OWASP BLOCKER: sensitive file tracked by Git: $trackedSecrets" }
    if (-not (Test-Tool "gitleaks")) { Write-Host "   gitleaks not installed; tracked-file check only." -ForegroundColor DarkYellow; return "NOT RUN" }
    gitleaks dir . --redact --exit-code 1 --no-banner; Assert-ExitCode "gitleaks"
}

# --- Backend ------------------------------------------------------------------------------------------
$backendSolution = Join-Path $backendDir "MarketPulse.sln"
if (Test-Path $backendSolution) {
    Invoke-Step "Backend restore" { dotnet restore $backendSolution; Assert-ExitCode "dotnet restore" }
    Invoke-Step "Backend format (dotnet format)" { dotnet format $backendSolution --verify-no-changes --no-restore; Assert-ExitCode "dotnet format" }
    Invoke-Step "Backend strict build (-warnaserror)" { dotnet build $backendSolution --configuration Release --no-restore -warnaserror; Assert-ExitCode "dotnet build" }
    Invoke-Step "Backend vulnerable NuGet packages" {
        $output = dotnet list $backendSolution package --vulnerable --include-transitive | Out-String
        Write-Host $output
        if ($output -match "has the following vulnerable packages") { throw "Vulnerable NuGet packages detected." }
    }

    $testResults = Join-Path $backendDir "TestResults"
    if (Test-Path $testResults) { Remove-Item -Recurse -Force $testResults }
    $coverageArgs = @("--collect:XPlat Code Coverage", "--results-directory", $testResults, "--", "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover")

    Invoke-Step "Backend unit and architecture tests" {
        dotnet test (Join-Path $backendDir "tests/App.UnitTests") --configuration Release --no-build @coverageArgs; Assert-ExitCode "unit tests"
        dotnet test (Join-Path $backendDir "tests/App.ArchTests") --configuration Release --no-build @coverageArgs; Assert-ExitCode "architecture tests"
    }

    Invoke-Step "Backend integration tests (Testcontainers)" {
        if ($Fast) { return "NOT RUN" }
        if (-not (Test-Tool "docker")) { throw "Docker is required for integration tests." }
        docker info *> $null
        if ($LASTEXITCODE -ne 0) { throw "Docker daemon is not running. Integration tests would silently fall back to in-memory (see learnedLessons, lesson 12). Start Docker Desktop and rerun." }
        dotnet test (Join-Path $backendDir "tests/App.IntegrationTests") --configuration Release --no-build @coverageArgs; Assert-ExitCode "integration tests"
    }

    Invoke-Step "CRAP < 10 gate (ratchet baseline)" {
        if ($Fast) { return "NOT RUN" }
        python -I scripts/quality/crap_report.py "src/backend/TestResults/**/coverage.opencover.xml"; Assert-ExitCode "CRAP gate"
    }

    Invoke-Step "Mutation testing (Stryker.NET)" {
        if (-not $Mutation) { return "NOT RUN" }
        dotnet tool restore; Assert-ExitCode "dotnet tool restore"
        Push-Location $backendDir
        try { dotnet tool run dotnet-stryker; Assert-ExitCode "Stryker.NET" } finally { Pop-Location }
    }
} else {
    Write-Host "`n[INFO] No backend solution found; backend steps skipped." -ForegroundColor DarkGray
}

# --- Frontend -----------------------------------------------------------------------------------------
if (Test-Path (Join-Path $frontendDir "package.json")) {
    Push-Location $frontendDir
    try {
        Invoke-Step "Frontend lint" { npm run lint; Assert-ExitCode "npm run lint" }
        Invoke-Step "Frontend strict typecheck" { npm run typecheck; Assert-ExitCode "npm run typecheck" }
        Invoke-Step "Frontend tests (Vitest + MSW)" { npm run test; Assert-ExitCode "npm run test" }
        Invoke-Step "Frontend production build" {
            $env:NEXT_PUBLIC_API_URL = "http://localhost:8080"
            $env:NEXT_PUBLIC_SUPABASE_URL = "http://localhost:54321"
            $env:NEXT_PUBLIC_SUPABASE_ANON_KEY = "placeholder-anon-key-for-build-only"
            npm run build; Assert-ExitCode "npm run build"
        }
        Invoke-Step "Frontend vulnerable npm packages (high+)" { npm audit --omit=dev --audit-level=high; Assert-ExitCode "npm audit" }
    } finally {
        Pop-Location
    }
}

# --- Cross-cutting ------------------------------------------------------------------------------------
Invoke-Step "Duplication gate (jscpd)" {
    if (-not (Test-Tool "npx")) { return "NOT RUN" }
    npx --yes jscpd@5.4.3 --config .jscpd.json; Assert-ExitCode "jscpd"
}

# --- Summary ------------------------------------------------------------------------------------------
Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host " QUALITY GATE SUMMARY" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
foreach ($entry in $results.GetEnumerator()) {
    $color = switch ($entry.Value) { "PASS" { "Green" } "FAIL" { "Red" } default { "DarkYellow" } }
    Write-Host ("{0,-9} {1}" -f $entry.Value, $entry.Key) -ForegroundColor $color
}

$failed = @($results.Values | Where-Object { $_ -eq "FAIL" }).Count
$notRun = @($results.Values | Where-Object { $_ -eq "NOT RUN" }).Count
$stateDir = Join-Path $repoRoot ".agents/.state"
$marker = Join-Path $stateDir "validate-all.ok"

if ($failed -gt 0) { Write-Host "`nGATE RED: $failed step(s) failed." -ForegroundColor Red; exit 1 }
if ($notRun -gt 0) { Write-Host "`nGATE INCOMPLETE: $notRun step(s) NOT RUN. Not a green gate." -ForegroundColor DarkYellow; exit 2 }

New-Item -ItemType Directory -Force -Path $stateDir | Out-Null
Set-Content -Path $marker -Value (Get-WorkspaceFingerprint $repoRoot) -Encoding ascii
Write-Host "`nGATE GREEN: every step executed and passed." -ForegroundColor Green
exit 0
