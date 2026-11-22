# End-to-end tests of the hook entry points: payload on stdin -> decision on stdout.
$scripts = Split-Path -Parent $PSScriptRoot
$failures = 0
$total = 0

function Invoke-Hook([string]$script, [string]$payload) {
    $shell = if (Get-Command pwsh -ErrorAction SilentlyContinue) { "pwsh" } else { "powershell" }
    $output = $payload | & $shell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $scripts $script)
    return ($output -join "")
}

function Assert-Contains([string]$name, [string]$actual, [string]$expected) {
    $script:total++
    $ok = if ($expected -eq "") { $actual -eq "" } else { $actual -like "*$expected*" }
    if (-not $ok) { Write-Host "FAIL: $name -> expected '$expected' in '$actual'" -ForegroundColor Red; $script:failures++ }
}

$deny = '{"tool_name":"Bash","tool_input":{"command":"git reset --hard"}}'
$ask = '{"tool_name":"Bash","tool_input":{"command":"git push origin main"}}'
$safe = '{"tool_name":"Bash","tool_input":{"command":"git status"}}'
$envWrite = '{"tool_name":"Write","tool_input":{"file_path":"D:\\r\\.env","content":"X=1"}}'
$codeWrite = '{"tool_name":"Edit","tool_input":{"file_path":"src\\a.cs","new_string":"class A {}"}}'

Assert-Contains "claude deny"          (Invoke-Hook "claude-pre-tool-hook.ps1" $deny) '"permissionDecision":"deny"'
Assert-Contains "claude ask"           (Invoke-Hook "claude-pre-tool-hook.ps1" $ask) '"permissionDecision":"ask"'
Assert-Contains "claude allow"         (Invoke-Hook "claude-pre-tool-hook.ps1" $safe) ""
Assert-Contains "claude env write"     (Invoke-Hook "claude-pre-tool-hook.ps1" $envWrite) '"permissionDecision":"deny"'
Assert-Contains "claude code edit"     (Invoke-Hook "claude-pre-tool-hook.ps1" $codeWrite) ""
Assert-Contains "claude fail closed"   (Invoke-Hook "claude-pre-tool-hook.ps1" "not json") '"permissionDecision":"deny"'
Assert-Contains "gemini deny"          (Invoke-Hook "pre-command-hook.ps1" '{"toolCall":{"args":{"CommandLine":"git reset --hard"}}}') '"decision":"deny"'
Assert-Contains "gemini ask is deny"   (Invoke-Hook "pre-command-hook.ps1" '{"toolCall":{"args":{"CommandLine":"git push"}}}') '"decision":"deny"'
Assert-Contains "gemini allow"         (Invoke-Hook "pre-command-hook.ps1" '{"toolCall":{"args":{"CommandLine":"dotnet test"}}}') '"decision":"allow"'
Assert-Contains "gemini fail closed"   (Invoke-Hook "pre-command-hook.ps1" "not json") '"decision":"deny"'
Assert-Contains "stop hook loop guard" (Invoke-Hook "claude-stop-hook.ps1" '{"stop_hook_active":true}') ""

if ($failures -gt 0) { Write-Host "$failures of $total hook test(s) failed." -ForegroundColor Red; exit 1 }
Write-Host "[OK] Hook entry point tests passed ($total cases)." -ForegroundColor Green
