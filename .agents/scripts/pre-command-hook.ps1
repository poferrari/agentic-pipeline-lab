[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "SilentlyContinue"
$inputJson = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($inputJson)) {
    Write-Output '{"decision":"allow"}'
    exit 0
}

try {
    $payload = $inputJson | ConvertFrom-Json
    $cmd = $payload.toolCall.args.CommandLine
    if ($cmd -match "(?i)(git\s+push\s+.*--force|rm\s+-rf\s+/|DROP\s+DATABASE|Format-Volume)") {
        $deny = @{
            decision = "deny"
            reason = "Blocked by Safety Guardrail: destructive command detected."
        }
        $deny | ConvertTo-Json -Compress
        exit 0
    }
} catch {}

Write-Output '{"decision":"allow"}'

