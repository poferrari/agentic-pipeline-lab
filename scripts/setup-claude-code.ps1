# Exposes the pipeline agents and skills to Claude Code without duplicating them.
# .agents/ stays the single source of truth; .claude/agents and .claude/skills are
# local directory junctions (git-ignored) pointing at it. Run once per clone.

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

foreach ($name in @("agents", "skills")) {
    $target = Join-Path $repoRoot ".agents/$name"
    $link = Join-Path $repoRoot ".claude/$name"

    if (Test-Path $link) {
        $existing = Get-Item $link -Force
        if ($existing.LinkType -ne "Junction") {
            throw "$link exists and is not a junction. Move it away and run again."
        }
        Write-Host "[OK] $link already linked."
        continue
    }

    New-Item -ItemType Junction -Path $link -Target $target | Out-Null
    Write-Host "[OK] Linked $link -> $target"
}

Write-Host "Restart Claude Code, then check /agents, /skills and /hooks."
