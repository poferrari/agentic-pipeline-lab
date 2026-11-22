# Shared safety policy for the Claude Code and Gemini hooks (single source of truth).
# Dot-source this file: . "$PSScriptRoot/safety-policy.ps1"
# Verdicts are hashtables: @{ Decision = "allow" | "deny" | "ask"; Reason = "..." }

$script:DenyCommandPatterns = [ordered]@{
    "force push (use --force-with-lease and ask the user)" = 'git\s+push\b.*\s(--force(?!-with-lease)|-[a-zA-Z]*f[a-zA-Z]*)(\s|$)'
    "git reset --hard discards work"                       = 'git\s+reset\s+--hard'
    "git clean -f deletes untracked files"                 = 'git\s+clean\b.*\s-[a-zA-Z]*f'
    "discarding all working tree changes"                  = 'git\s+(checkout|restore)\s+(--\s+)?\.(\s|$)'
    "recursive delete of root, home, or cwd"               = '\brm\s+-\w*[rR]\w*\s+(--\s+)?["'']?(/|~|\$HOME|\.|\.\.|\*)["'']?/?\*?(\s|$)'
    "recursive delete of a drive root"                     = 'Remove-Item\b.*\s["'']?[A-Za-z]:[\\/]?\*?["'']?(\s|$)'
    "recursive delete of cwd or parent"                    = 'Remove-Item\b(?=.*-Recurse)(?=.*\s["'']?(\.|\.\.|\*)["'']?(\s|$))'
    "destructive SQL (DROP/TRUNCATE)"                      = '\b(DROP\s+(DATABASE|SCHEMA|TABLE)|TRUNCATE\s+TABLE)\b'
    "remote script piped into a shell"                     = '\b(curl|wget|iwr|Invoke-WebRequest)\b[^|]*\|\s*(sh|bash|zsh|iex|Invoke-Expression|pwsh|powershell)\b'
    "remote script executed with iex"                      = '\b(iex|Invoke-Expression)\b.*\b(iwr|Invoke-WebRequest|DownloadString)\b'
    "volume format"                                        = '\bFormat-Volume\b'
}

$script:AskCommandPatterns = [ordered]@{
    "git push publishes work (human gate)"                 = '\bgit\s+push\b'
    "GitHub PR/release action (human gate)"                = '\bgh\s+(pr\s+(create|merge|review|close)|release\s+(create|delete))\b'
    "GitHub API write (human gate)"                        = '\bgh\s+api\b.*-X\s*(POST|PUT|PATCH|DELETE)\b'
}

$script:SecretContentPatterns = [ordered]@{
    "private key material"      = '-----BEGIN [A-Z ]*PRIVATE KEY-----'
    "AWS access key id"         = '\bAKIA[0-9A-Z]{16}\b'
    "Supabase secret key"       = '\bsb_secret_[A-Za-z0-9_-]{20,}'
    "service_role JWT"          = 'service_role["'']?\s*[:=]\s*["'']?eyJ[A-Za-z0-9_-]{20,}'
}

function New-Verdict([string]$decision, [string]$reason) {
    return @{ Decision = $decision; Reason = $reason }
}

function Get-CommandVerdict([string]$command) {
    if ([string]::IsNullOrWhiteSpace($command)) { return New-Verdict "allow" "" }
    foreach ($entry in $script:DenyCommandPatterns.GetEnumerator()) {
        if ($command -match "(?i)$($entry.Value)") { return New-Verdict "deny" "Blocked by safety guard: $($entry.Key)." }
    }
    foreach ($entry in $script:AskCommandPatterns.GetEnumerator()) {
        if ($command -match "(?i)$($entry.Value)") { return New-Verdict "ask" "Requires explicit user approval: $($entry.Key)." }
    }
    return New-Verdict "allow" ""
}

function Get-WriteVerdict([string]$filePath, [string]$content) {
    $isEnvFile = $filePath -match '(^|[\\/])\.env(\.[^\\/]*)?$' -and $filePath -notmatch '\.example$'
    if ($isEnvFile -or $filePath -match 'appsettings\.(Development|Local)\.json$') {
        return New-Verdict "deny" "Blocked by safety guard: writing a local secrets file ($filePath). Use .env.example with placeholders."
    }
    foreach ($entry in $script:SecretContentPatterns.GetEnumerator()) {
        if ($content -match $entry.Value) { return New-Verdict "deny" "Blocked by safety guard: content looks like $($entry.Key)." }
    }
    return New-Verdict "allow" ""
}

# Fingerprint of modified and untracked files under src/ and tests/. validate-all.ps1 stores it on
# success; the Stop hook compares it to detect code changed after the last green gate.
function Get-WorkspaceFingerprint([string]$repoRoot) {
    Push-Location $repoRoot
    try {
        $files = @(git ls-files -m -o --exclude-standard -- src tests 2>$null | Sort-Object -Unique)
        if ($files.Count -eq 0) { return "" }
        $parts = foreach ($file in $files) {
            if (Test-Path -LiteralPath $file) { "$file=" + (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash } else { "$file=deleted" }
        }
        $bytes = [System.Text.Encoding]::UTF8.GetBytes(($parts -join "`n"))
        return [System.BitConverter]::ToString([System.Security.Cryptography.SHA256]::Create().ComputeHash($bytes)).Replace("-", "")
    } finally { Pop-Location }
}
