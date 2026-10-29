# Modular Hexagonal Monolith Architecture Bootstrap Script (.NET C# + Frontend)
# Creates standard directory topologies and build props for rapid development.

param(
    [string]$SolutionName = "App"
)

$ErrorActionPreference = "Stop"

Write-Host "[BOOTSTRAP] Initializing Hexagonal DDD structure for '$SolutionName'..." -ForegroundColor Cyan

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

Write-Host "[OK] File src/backend/Directory.Build.props configured (Nullable=enable, TreatWarningsAsErrors=true)." -ForegroundColor Green
Write-Host "[INFO] When ready to create .NET projects (Domain, Application, Infrastructure, Api, and Tests), the 'backend-csharp-developer' agent will run commands per the approved Spec." -ForegroundColor Yellow
