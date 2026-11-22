# Gemini pre-command guard. Policy lives in safety-policy.ps1 (shared with the Claude hook).
# Fails closed: an unparseable payload is denied. Gemini has no "ask" decision, so ask verdicts deny.

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. "$PSScriptRoot/safety-policy.ps1"

function Write-Decision([string]$decision, [string]$reason) {
    @{ decision = $decision; reason = $reason } | ConvertTo-Json -Compress
    exit 0
}

$inputJson = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($inputJson)) { Write-Decision "allow" "" }

try {
    $command = [string]($inputJson | ConvertFrom-Json -ErrorAction Stop).toolCall.args.CommandLine
} catch {
    Write-Decision "deny" "Safety guard could not parse the hook payload."
}

$verdict = Get-CommandVerdict $command
if ($verdict.Decision -eq "allow") { Write-Decision "allow" "" }
Write-Decision "deny" $verdict.Reason
