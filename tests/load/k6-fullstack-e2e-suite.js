import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Trend, Counter, Rate } from 'k6/metrics';

// Custom Metrics
const webPageLatency = new Trend('k6_web_page_duration_ms');
const ratesLatency = new Trend('k6_rates_api_duration_ms');
const watchlistCreateLatency = new Trend('k6_watchlist_create_duration_ms');
const watchlistQueryLatency = new Trend('k6_watchlist_query_duration_ms');
const aiChatLatency = new Trend('k6_ai_chat_duration_ms');
const rateLimitHits = new Counter('k6_rate_limit_429_total');
const successfulRequests = new Counter('k6_successful_requests_total');
const failureRate = new Rate('k6_failure_rate');

export const options = {
  scenarios: {
    // Multi-user journey exercising frontend and all backend API routes
    multi_user_journey: {
      executor: 'ramping-vus',
      startVUs: 1,
      stages: [
        { duration: '10s', target: 5 },   // Warm-up ramp
        { duration: '25s', target: 10 },  // Sustained load across distinct users
        { duration: '10s', target: 0 },   // Cool-down
      ],
      gracefulRampDown: '5s',
    },
  },
  thresholds: {
    'http_req_duration': ['p(95)<2500'], // Free tier Render cold/warm threshold
    'k6_failure_rate': ['rate<0.10'],    // <10% failure (allowing rate-limit 429 backoff)
  },
};

const WEB_URL = __ENV.WEB_URL || 'https://agentic-pipeline-lab-web.onrender.com';
const API_URL = __ENV.API_URL || 'https://agentic-pipeline-lab-api.onrender.com';

// Generate consistent distinct user UUIDs for each virtual user
function getUserHeaders(vuId) {
  // Deterministic user GUID based on VU ID to validate multi-tenancy & BOLA
  const paddedId = String(vuId).padStart(4, '0');
  const userGuid = `10000000-0000-0000-0000-00000000${paddedId}`;
  return {
    'Content-Type': 'application/json',
    'X-Demo-User-Id': userGuid,
    'Accept': 'application/json',
  };
}

export default function () {
  const vuId = __VU;
  const userHeaders = getUserHeaders(vuId);

  // =========================================================================
  // 1. FRONTEND WEB JOURNEY (Next.js 14 App Router)
  // =========================================================================
  group('01_Web_Frontend_Navigation', () => {
    const webRes = http.get(WEB_URL, {
      headers: { 'Accept': 'text/html,application/xhtml+xml' },
      tags: { name: 'GET_Frontend_Home' },
    });

    webPageLatency.add(webRes.timings.duration);
    const webOk = check(webRes, {
      'Web homepage status is 200': (r) => r.status === 200,
      'Web contains MarketPulse / Agentic FX title': (r) =>
        r.body && (r.body.includes('Agentic FX') || r.body.includes('Real-Time FX Rates')),
    });

    if (webOk) successfulRequests.add(1);
    else failureRate.add(1);
  });

  sleep(0.5);

  // =========================================================================
  // 2. BACKEND API: HEALTH & CATALOG
  // =========================================================================
  group('02_API_Health_And_Catalog', () => {
    const healthRes = http.get(`${API_URL}/health/live`, {
      tags: { name: 'GET_Health_Live' },
    });
    check(healthRes, {
      'API /health/live status is 200': (r) => r.status === 200,
    });

    const catalogRes = http.get(`${API_URL}/`, {
      tags: { name: 'GET_API_Catalog' },
    });
    check(catalogRes, {
      'API Catalog root status is 200': (r) => r.status === 200,
    });
  });

  sleep(0.5);

  // =========================================================================
  // 3. BACKEND API: LIVE FX RATES (European Central Bank + HybridCache)
  // =========================================================================
  group('03_API_Live_Rates_HybridCache', () => {
    const currencies = ['USD', 'EUR', 'GBP', 'BRL'];
    const base = currencies[__ITER % currencies.length];
    const ratesRes = http.get(`${API_URL}/api/v1/rates/${base}?symbols=BRL,EUR,GBP,JPY,CAD,CHF,AUD`, {
      headers: userHeaders,
      tags: { name: 'GET_Rates_Live' },
    });

    ratesLatency.add(ratesRes.timings.duration);
    const ratesOk = check(ratesRes, {
      'Rates endpoint returns 200': (r) => r.status === 200,
      'Rates payload contains base currency': (r) => r.body && r.body.includes(base),
      'Rates payload contains quotes map': (r) => r.body && r.body.includes('rates'),
    });

    if (ratesOk) successfulRequests.add(1);
    else if (ratesRes.status === 429) rateLimitHits.add(1);
    else failureRate.add(1);
  });

  sleep(0.5);

  // =========================================================================
  // 4. BACKEND API: WATCHLIST ALERTS (PostgreSQL + RabbitMQ Outbox)
  // =========================================================================
  group('04_API_Watchlist_Transactional_Outbox', () => {
    // 4A. Query existing user alerts
    const listRes = http.get(`${API_URL}/api/v1/watchlist?page=1&pageSize=10`, {
      headers: userHeaders,
      tags: { name: 'GET_Watchlist_List' },
    });

    watchlistQueryLatency.add(listRes.timings.duration);
    check(listRes, {
      'Watchlist list returns 200': (r) => r.status === 200,
    });

    // 4B. Create a new threshold alert for this user
    // Rotate currency pairs per iteration to test uniqueness
    const pairs = [
      { base: 'USD', target: 'EUR', threshold: 0.95 },
      { base: 'USD', target: 'BRL', threshold: 5.60 },
      { base: 'EUR', target: 'GBP', threshold: 0.85 },
      { base: 'GBP', target: 'USD', threshold: 1.30 },
    ];
    const pair = pairs[__ITER % pairs.length];
    const payload = JSON.stringify({
      baseCurrency: pair.base,
      targetCurrency: pair.target,
      thresholdRate: pair.threshold + (vuId * 0.001),
      direction: 'Above',
      notificationChannels: ['RabbitMq', 'Email'],
    });

    const createRes = http.post(`${API_URL}/api/v1/watchlist`, payload, {
      headers: userHeaders,
      tags: { name: 'POST_Watchlist_Create' },
    });

    watchlistCreateLatency.add(createRes.timings.duration);
    const createOk = check(createRes, {
      'Watchlist creation returns 201 Created or 409 Conflict': (r) =>
        r.status === 201 || r.status === 409 || r.status === 429,
    });

    if (createRes.status === 201) successfulRequests.add(1);
    else if (createRes.status === 429) rateLimitHits.add(1);
  });

  sleep(0.5);

  // =========================================================================
  // 5. BACKEND API: AI CONTEXTUAL ADVISOR (Google Gemini 2.5 Flash)
  // =========================================================================
  group('05_API_AI_Advisor_Chat', () => {
    // Test AI chat with domain-grounded query
    const aiPayload = JSON.stringify({
      prompt: 'Explain how MassTransit Outbox dispatches currency alert notifications.',
      history: [],
    });

    const aiRes = http.post(`${API_URL}/api/v1/ai/chat`, aiPayload, {
      headers: userHeaders,
      tags: { name: 'POST_AI_Chat' },
    });

    aiChatLatency.add(aiRes.timings.duration);
    check(aiRes, {
      'AI Advisor chat returns 200 or 429 (rate-limited)': (r) =>
        r.status === 200 || r.status === 429,
      'AI Advisor response contains model or answer': (r) =>
        r.status !== 200 || (r.body && r.body.includes('message')),
    });

    if (aiRes.status === 200) successfulRequests.add(1);
    else if (aiRes.status === 429) rateLimitHits.add(1);
  });

  sleep(1);
}

