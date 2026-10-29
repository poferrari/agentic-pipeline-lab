$baseUrl = "https://agentic-pipeline-lab-api.onrender.com"
$userA = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
$userB = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  LOAD AND TELEMETRY TRAFFIC GENERATOR: MARKETPULSE -> GRAFANA" -ForegroundColor Cyan
Write-Host "  Target: $baseUrl" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Health probes
Write-Host "`n[1/5] Running Health Probes (/health/live and /health/ready)..." -ForegroundColor Yellow
1..3 | ForEach-Object {
    $resLive = Invoke-WebRequest -Uri "$baseUrl/health/live" -UseBasicParsing
    $resReady = Invoke-WebRequest -Uri "$baseUrl/health/ready" -UseBasicParsing
    Write-Host " -> Health Probe: Live=$($resLive.StatusCode), Ready=$($resReady.StatusCode), TraceId=$($resLive.Headers['X-Trace-Id'])"
}

# 2. Currency Rates Queries (Frankfurter ECB API + HybridCache)
Write-Host "`n[2/5] Generating currency exchange rate queries (Business Traffic)..." -ForegroundColor Yellow
$currencies = @("USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF")
foreach ($curr in $currencies) {
    try {
        $url = "$baseUrl/api/v1/rates/$curr`?symbols=BRL,EUR,USD,GBP"
        $res = Invoke-RestMethod -Uri $url -Method Get -UseBasicParsing
        Write-Host " -> Rate $curr : BRL = $($res.rates.BRL) | EUR = $($res.rates.EUR)"
    } catch {
        Write-Host " -> Error querying $curr : $_" -ForegroundColor Red
    }
}

# 3. Validation and Client Error Testing (400 Bad Request and 401 Unauthorized)
Write-Host "`n[3/5] Generating client error metrics (400 and 401)..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$baseUrl/api/v1/rates/XYZ123" -Method Get -UseBasicParsing
} catch {
    Write-Host " -> [OK 400 Expected] Invalid currency code rejected with status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Green
}

try {
    Invoke-RestMethod -Uri "$baseUrl/api/v1/watchlist" -Method Get -UseBasicParsing
} catch {
    Write-Host " -> [OK 401 Expected] Unauthenticated request rejected with status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Green
}

# 4. Watchlist CRUD Lifecycle (BOLA/IDOR + Create + List + Delete)
Write-Host "`n[4/5] Running watchlist lifecycle (Create -> List -> Delete)..." -ForegroundColor Yellow
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
    notes = "GBP Alert Grafana Cloud"
} | ConvertTo-Json

try {
    $created = Invoke-RestMethod -Uri "$baseUrl/api/v1/watchlist" -Method Post -Headers $headersUserA -Body $body -UseBasicParsing
    $itemId = $created.id
    Write-Host " -> [201 Created] Alert item created with ID: $itemId" -ForegroundColor Green

    # Attempt to retrieve with User B (BOLA Protection -> 404)
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/v1/watchlist/$itemId" -Method Get -Headers $headersUserB -UseBasicParsing
    } catch {
        Write-Host " -> [OK 404 BOLA] User B prevented from reading User A item: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Green
    }

    # List with User A
    $list = Invoke-RestMethod -Uri "$baseUrl/api/v1/watchlist?page=1&pageSize=10" -Method Get -Headers $headersUserA -UseBasicParsing
    Write-Host " -> [200 OK] User A listed watchlist: $($list.totalCount) alert(s) registered."

    # Delete with User A
    $del = Invoke-WebRequest -Uri "$baseUrl/api/v1/watchlist/$itemId" -Method Delete -Headers $headersUserA -UseBasicParsing
    Write-Host " -> [204 NoContent] Alert $itemId successfully deleted by owner." -ForegroundColor Green
} catch {
    Write-Host " -> Error in watchlist lifecycle: $_" -ForegroundColor Red
}

# 5. Traffic Burst for Latency Percentiles and Throughput Graphs
Write-Host "`n[5/5] Sending traffic burst for Latency Percentiles and RPS metrics..." -ForegroundColor Yellow
1..10 | ForEach-Object {
    $c = $_
    $t1 = [System.Diagnostics.Stopwatch]::StartNew()
    $r1 = Invoke-WebRequest -Uri "$baseUrl/api/v1/rates/USD?symbols=BRL,EUR" -UseBasicParsing
    $t1.Stop()

    $t2 = [System.Diagnostics.Stopwatch]::StartNew()
    $r2 = Invoke-WebRequest -Uri "$baseUrl/api/v1/rates/EUR?symbols=BRL,USD" -UseBasicParsing
    $t2.Stop()

    Write-Host " -> Batch $c/10: USD in $($t1.ElapsedMilliseconds)ms (status $($r1.StatusCode)), EUR in $($t2.ElapsedMilliseconds)ms (status $($r2.StatusCode))"
}

Write-Host "`n=================================================================" -ForegroundColor Cyan
Write-Host " [SUCCESS] Telemetry traffic battery completed successfully!" -ForegroundColor Green
Write-Host " Open your Grafana Cloud dashboard to inspect updated data:" -ForegroundColor Cyan
Write-Host " https://bigveranda1683.grafana.net/d/pomrnx9/9de3a08" -ForegroundColor Yellow
Write-Host "=================================================================" -ForegroundColor Cyan
