# Claude Code PreToolUse guard for Bash, PowerShell, Write, and Edit.
# Reads the hook payload from stdin; policy lives in safety-policy.ps1.
# Fails closed: a payload that cannot be parsed is denied, never silently allowed.

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
. "$PSScriptRoot/safety-policy.ps1"

function Write-Verdict($verdict) {
    if ($verdict.Decision -eq "allow") { exit 0 }
    @{
        hookSpecificOutput = @{
            hookEventName            = "PreToolUse"
            permissionDecision       = $verdict.Decision
            permissionDecisionReason = $verdict.Reason
        }
    } | ConvertTo-Json -Depth 5 -Compress
    exit 0
}

$inputJson = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($inputJson)) { exit 0 }

try {
    $payload = $inputJson | ConvertFrom-Json -ErrorAction Stop
} catch {
    Write-Verdict (New-Verdict "deny" "Safety guard could not parse the hook payload.")
}

switch ($payload.tool_name) {
    { $_ -in @("Bash", "PowerShell") } { Write-Verdict (Get-CommandVerdict ([string]$payload.tool_input.command)) }
    { $_ -in @("Write", "Edit") } {
        $content = [string]$payload.tool_input.content + [string]$payload.tool_input.new_string
        Write-Verdict (Get-WriteVerdict ([string]$payload.tool_input.file_path) $content)
    }
}
exit 0
