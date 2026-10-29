$baseUrl = "https://agentic-pipeline-lab-api.onrender.com"
$userA = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
$userB = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  GERADOR DE CARGA E TELEMETRIA: MARKETPULSE -> GRAFANA CLOUD" -ForegroundColor Cyan
Write-Host "  Alvo: $baseUrl" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Health probes
Write-Host "`n[1/5] Executando Health Probes (/health/live e /health/ready)..." -ForegroundColor Yellow
1..3 | ForEach-Object {
    $resLive = Invoke-WebRequest -Uri "$baseUrl/health/live" -UseBasicParsing
    $resReady = Invoke-WebRequest -Uri "$baseUrl/health/ready" -UseBasicParsing
    Write-Host " -> Health Probe: Live=$($resLive.StatusCode), Ready=$($resReady.StatusCode), TraceId=$($resLive.Headers['X-Trace-Id'])"
}

# 2. Consultas de Cotações com moedas variadas (Frankfurter ECB API + HybridCache)
Write-Host "`n[2/5] Gerando consultas de cotacoes cambiais (Tráfego de Negócio)..." -ForegroundColor Yellow
$currencies = @("USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF")
foreach ($curr in $currencies) {
    try {
        $url = "$baseUrl/api/v1/rates/$curr`?symbols=BRL,EUR,USD,GBP"
        $res = Invoke-RestMethod -Uri $url -Method Get -UseBasicParsing
        Write-Host " -> Cotação $curr : BRL = $($res.rates.BRL) | EUR = $($res.rates.EUR)"
    } catch {
        Write-Host " -> Erro ao consultar $curr : $_" -ForegroundColor Red
    }
}

# 3. Teste de Validações e Erros de Cliente (400 Bad Request e 401 Unauthorized)
Write-Host "`n[3/5] Gerando métricas de erros de validação (400 e 401)..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$baseUrl/api/v1/rates/XYZ123" -Method Get -UseBasicParsing
} catch {
    Write-Host " -> [OK 400 Esperado] Código de moeda inválido rejeitado com status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Green
}

try {
    Invoke-RestMethod -Uri "$baseUrl/api/v1/watchlist" -Method Get -UseBasicParsing
} catch {
    Write-Host " -> [OK 401 Esperado] Acesso não autenticado rejeitado com status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Green
}

# 4. Ciclo de Vida CRUD do Watchlist (BOLA/IDOR + Criar + Listar + Deletar)
Write-Host "`n[4/5] Executando ciclo de vida de alertas cambiais (Criar -> Listar -> Deletar)..." -ForegroundColor Yellow
$headersUserA = @{
    "Content-Type" = "application/json"
    "X-Demo-User-Id" = $userA
}
$headersUserB = @{
    "Content-Type" = "application/json"
    "X-Demo-User-Id" = $userB
}

$body = @{
    baseCurrency = "GBP"
    targetCurrency = "BRL"
    targetRate = 7.10
    notifyWhenAbove = $true
    notes = "Alerta GBP Grafana Cloud"
} | ConvertTo-Json

try {
    $created = Invoke-RestMethod -Uri "$baseUrl/api/v1/watchlist" -Method Post -Headers $headersUserA -Body $body -UseBasicParsing
    $itemId = $created.id
    Write-Host " -> [201 Created] Alerta criado com ID: $itemId" -ForegroundColor Green

    # Tenta obter com User B (BOLA Protection -> 404)
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/v1/watchlist/$itemId" -Method Get -Headers $headersUserB -UseBasicParsing
    } catch {
        Write-Host " -> [OK 404 BOLA] Usuário B impedido de ler item do Usuário A: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Green
    }

    # Listar com User A
    $list = Invoke-RestMethod -Uri "$baseUrl/api/v1/watchlist?page=1&pageSize=10" -Method Get -Headers $headersUserA -UseBasicParsing
    Write-Host " -> [200 OK] Usuário A listou watchlist: $($list.totalCount) alerta(s) cadastrado(s)."

    # Deletar com User A
    $del = Invoke-WebRequest -Uri "$baseUrl/api/v1/watchlist/$itemId" -Method Delete -Headers $headersUserA -UseBasicParsing
    Write-Host " -> [204 NoContent] Alerta $itemId deletado com sucesso pelo proprietário." -ForegroundColor Green
} catch {
    Write-Host " -> Erro no ciclo do watchlist: $_" -ForegroundColor Red
}

# 5. Rajada de tráfego para alimentar os gráficos de RPS e Percentis de Latência
Write-Host "`n[5/5] Enviando rajada de tráfego para alimentar os gráficos de Latência e RPS..." -ForegroundColor Yellow
1..10 | ForEach-Object {
    $c = $_
    $t1 = [System.Diagnostics.Stopwatch]::StartNew()
    $r1 = Invoke-WebRequest -Uri "$baseUrl/api/v1/rates/USD?symbols=BRL,EUR" -UseBasicParsing
    $t1.Stop()

    $t2 = [System.Diagnostics.Stopwatch]::StartNew()
    $r2 = Invoke-WebRequest -Uri "$baseUrl/api/v1/rates/EUR?symbols=BRL,USD" -UseBasicParsing
    $t2.Stop()

    Write-Host " -> Lote $c/10: USD em $($t1.ElapsedMilliseconds)ms (status $($r1.StatusCode)), EUR em $($t2.ElapsedMilliseconds)ms (status $($r2.StatusCode))"
}

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host " [SUCESSO] Bateria de telemetria concluída com sucesso!" -ForegroundColor Green
Write-Host " Acesse seu Grafana Cloud agora para ver os dados atualizados:" -ForegroundColor Cyan
Write-Host " https://bigveranda1683.grafana.net/d/pomrnx9/9de3a08" -ForegroundColor Yellow
Write-Host "=================================================================" -ForegroundColor Cyan
