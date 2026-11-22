# Claude Code Stop hook: blocks ending the turn once if code under src/ or tests/ changed after the
# last green scripts/validate-all.ps1 run. Cheap: compares fingerprints, never runs the gate itself.

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. "$PSScriptRoot/safety-policy.ps1"

$inputJson = [Console]::In.ReadToEnd()
try { $payload = $inputJson | ConvertFrom-Json -ErrorAction Stop } catch { exit 0 }
if ($payload.stop_hook_active) { exit 0 }

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$current = Get-WorkspaceFingerprint $repoRoot
if ([string]::IsNullOrEmpty($current)) { exit 0 }

$markerPath = Join-Path $repoRoot ".agents/.state/validate-all.ok"
$stored = if (Test-Path $markerPath) { (Get-Content $markerPath -Raw).Trim() } else { "" }
if ($stored -eq $current) { exit 0 }

@{
    decision = "block"
    reason   = "Code under src/ or tests/ changed since the last green quality gate. Run scripts/validate-all.ps1 and report its real result, or state explicitly that the gate was NOT RUN and why."
} | ConvertTo-Json -Compress
exit 0
