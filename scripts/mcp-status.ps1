# Painel Rapido de Gerenciamento de MCPs (mcp-status.ps1)
# Uso:
#   powershell -File scripts/mcp-status.ps1                  -> Lista todos os MCPs e status
#   powershell -File scripts/mcp-status.ps1 -Enable supabase -> Ativa um MCP
#   powershell -File scripts/mcp-status.ps1 -Disable github  -> Desativa um MCP

param(
    [string]$Enable = "",
    [string]$Disable = ""
)

$agyExe = "C:\Users\Usuario\.gemini\bin\agy.exe"
$configPath = "C:\Users\Usuario\.gemini\config\mcp_config.json"

if ($Enable) {
    Write-Host "Ativando MCP '$Enable'..." -ForegroundColor Cyan
    & $agyExe mcp enable $Enable
}

if ($Disable) {
    Write-Host "Desativando MCP '$Disable'..." -ForegroundColor Yellow
    & $agyExe mcp disable $Disable
}

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host " [MCP PANEL] SERVIDORES MCP CONFIGURADOS NO SEU ANTIGRAVITY" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
& $agyExe mcp list
Write-Host "-----------------------------------------------------------------" -ForegroundColor DarkGray
Write-Host "Arquivo de Configuracao Global: $configPath" -ForegroundColor Gray
Write-Host "Comandos Rapidos:" -ForegroundColor Gray
Write-Host "  - Ver lista:      agy mcp list"
Write-Host "  - Ativar MCP:     agy mcp enable NOME"
Write-Host "  - Desativar MCP:  agy mcp disable NOME"
Write-Host "  - Adicionar MCP:  agy mcp add NOME URL_OU_COMANDO"
Write-Host "=================================================================`n" -ForegroundColor Cyan
