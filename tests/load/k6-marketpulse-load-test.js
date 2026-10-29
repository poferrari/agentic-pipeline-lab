import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Counter, Rate } from 'k6/metrics';

// Custom Prometheus/k6 Metrics
const cacheHitLatency = new Trend('marketpulse_cache_read_duration_ms');
const outboxWriteLatency = new Trend('marketpulse_outbox_write_duration_ms');
const queryListLatency = new Trend('marketpulse_query_list_duration_ms');
const successfulCreations = new Counter('marketpulse_successful_creations_total');
const duplicateConflicts = new Counter('marketpulse_duplicate_conflicts_total');
const rateLimitHits = new Counter('marketpulse_rate_limit_hits_total');
const errorRate = new Rate('marketpulse_error_rate');

export const options = {
    scenarios: {
        // Scenario 1: High-throughput Cache Read (Validating HybridCache L1/L2)
        cache_reads: {
            executor: 'ramping-vus',
            startVUs: 2,
            stages: [
                { duration: '15s', target: 20 },
                { duration: '30s', target: 50 },
                { duration: '15s', target: 80 },
                { duration: '15s', target: 0 },
            ],
            exec: 'testCacheReads',
        },
        // Scenario 2: Transactional Outbox + DB Writes + Idempotency
        outbox_writes: {
            executor: 'ramping-vus',
            startVUs: 1,
            stages: [
                { duration: '15s', target: 10 },
                { duration: '30s', target: 25 },
                { duration: '15s', target: 40 },
                { duration: '15s', target: 0 },
            ],
            exec: 'testOutboxAndIdempotency',
        },
        // Scenario 3: Watchlist Pagination Queries
        watchlist_queries: {
            executor: 'ramping-vus',
            startVUs: 1,
            stages: [
                { duration: '15s', target: 10 },
                { duration: '30s', target: 30 },
                { duration: '15s', target: 0 },
            ],
            exec: 'testWatchlistQueries',
        },
    },
    thresholds: {
        'http_req_duration{scenario:cache_reads}': ['p(95)<100'], // Cache should be very fast
        'http_req_duration{scenario:outbox_writes}': ['p(95)<500'], // DB + Outbox transaction
        'marketpulse_error_rate': ['rate<0.05'], // Max 5% error tolerance under peak stress
    },
};

const BASE_URL = __ENV.TARGET_URL || 'http://localhost:8080';
const ORIGIN_SECRET = __ENV.ORIGIN_SECRET || '';

function getHeaders(userId, clientIp) {
    const headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
    };
    if (userId) {
        headers['X-Demo-User-Id'] = userId;
    }
    if (clientIp) {
        headers['CF-Connecting-IP'] = clientIp;
    }
    if (ORIGIN_SECRET) {
        headers['X-Origin-Verify'] = ORIGIN_SECRET;
    }
    return headers;
}

// Helper to generate UUIDs in pure JS
function generateUuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = (Math.random() * 16) | 0;
        const v = c === 'x' ? r : (r & 0x3) | 0x8;
        return v.toString(16);
    });
}

const AVAILABLE_CURRENCIES = ['BRL', 'EUR', 'GBP', 'CAD', 'CHF', 'JPY', 'AUD', 'CNY', 'SEK', 'NZD'];

export default function () {
    testCacheReads();
}

export function testCacheReads() {
    const clientIp = `192.168.1.${(__VU % 250) + 1}`;
    const headers = getHeaders(null, clientIp);

    const res = http.get(`${BASE_URL}/api/v1/rates/USD?symbols=BRL,EUR,GBP`, { headers });
    cacheHitLatency.add(res.timings.duration);

    if (res.status === 429) {
        rateLimitHits.add(1);
    } else {
        const isSuccess = check(res, {
            'rates endpoint status is 200': (r) => r.status === 200,
            'rates contains USD baseCurrency': (r) => r.body && r.body.includes('"baseCurrency":"USD"'),
            'rates has valid BRL rate': (r) => r.body && r.body.includes('"BRL"'),
        });
        errorRate.add(!isSuccess);
    }

    sleep(0.1);
}

export function testOutboxAndIdempotency() {
    const userId = generateUuid();
    const clientIp = `10.0.${Math.floor(__VU / 255)}.${(__VU % 250) + 1}`;
    const headers = getHeaders(userId, clientIp);

    const targetCurrency = AVAILABLE_CURRENCIES[Math.floor(Math.random() * AVAILABLE_CURRENCIES.length)];
    const payload = JSON.stringify({
        baseCurrency: 'USD',
        targetCurrency: targetCurrency,
        targetRate: 5.50 + (Math.random() * 0.5),
        notes: `Load test item VU ${__VU} iteration ${__ITER}`,
    });

    // 1. First creation: Should succeed and stage Transactional Outbox event
    const createRes = http.post(`${BASE_URL}/api/v1/watchlist`, payload, { headers });
    outboxWriteLatency.add(createRes.timings.duration);

    if (createRes.status === 429) {
        rateLimitHits.add(1);
    } else {
        const createOk = check(createRes, {
            'watchlist creation status is 201': (r) => r.status === 201,
            'watchlist has valid id': (r) => r.body && r.body.includes('"id"'),
            'watchlist has Active status': (r) => r.body && r.body.includes('"isTriggered":false'),
        });
        if (createOk) {
            successfulCreations.add(1);
        }
        errorRate.add(!createOk);
    }

    // 2. Immediate duplicate creation for same user & pair: Should trigger Idempotency/Conflict 409
    const duplicateRes = http.post(`${BASE_URL}/api/v1/watchlist`, payload, { headers });
    if (duplicateRes.status === 429) {
        rateLimitHits.add(1);
    } else {
        const duplicateOk = check(duplicateRes, {
            'duplicate pair returns 409 Conflict': (r) => r.status === 409,
            'duplicate body has DuplicatePair title': (r) => r.body && r.body.includes('WatchItem.DuplicatePair'),
        });
        if (duplicateOk) {
            duplicateConflicts.add(1);
        }
        errorRate.add(!duplicateOk);
    }

    sleep(0.3);
}

export function testWatchlistQueries() {
    const userId = generateUuid();
    const clientIp = `172.16.${Math.floor(__VU / 255)}.${(__VU % 250) + 1}`;
    const headers = getHeaders(userId, clientIp);

    const page = (__ITER % 5) + 1;
    const res = http.get(`${BASE_URL}/api/v1/watchlist?page=${page}&pageSize=20`, { headers });
    queryListLatency.add(res.timings.duration);

    if (res.status === 429) {
        rateLimitHits.add(1);
    } else {
        const queryOk = check(res, {
            'query watchlist status is 200': (r) => r.status === 200,
            'query watchlist is JSON array': (r) => r.body && r.body.startsWith('['),
        });
        errorRate.add(!queryOk);
    }

    sleep(0.2);
}
