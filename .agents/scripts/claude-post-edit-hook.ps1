# Claude Code PostToolUse hook for Write|Edit: formats C# files with dotnet format. Never blocks.

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ErrorActionPreference = "SilentlyContinue"

try { $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json -ErrorAction Stop } catch { exit 0 }
$filePath = [string]$payload.tool_input.file_path
if ($filePath -notmatch '\.cs$' -or -not (Test-Path -LiteralPath $filePath)) { exit 0 }

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$solution = Join-Path $repoRoot "src/backend/MarketPulse.sln"
if (-not (Test-Path $solution) -or -not (Get-Command dotnet -ErrorAction SilentlyContinue)) { exit 0 }

dotnet format $solution --include $filePath --no-restore *> $null
exit 0
