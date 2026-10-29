import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate, Counter } from 'k6/metrics';

const reqDuration = new Trend('stress_req_duration_ms');
const errorRate = new Rate('stress_error_rate');
const rateLimitCount = new Counter('stress_rate_limit_hits');

export const options = {
    scenarios: {
        capacity_ramp: {
            executor: 'ramping-arrival-rate',
            startRate: 20,
            timeUnit: '1s',
            preAllocatedVUs: 50,
            maxVUs: 300,
            stages: [
                { duration: '20s', target: 50 },   // 50 req/sec
                { duration: '30s', target: 150 },  // 150 req/sec
                { duration: '30s', target: 300 },  // 300 req/sec (Stress test)
                { duration: '20s', target: 500 },  // 500 req/sec (Saturation search)
                { duration: '20s', target: 50 },   // Recovery
            ],
        },
    },
    thresholds: {
        'http_req_duration': ['p(95)<300'],
        'stress_error_rate': ['rate<0.10'],
    },
};

const BASE_URL = __ENV.TARGET_URL || 'http://localhost:8080';

export default function () {
    const vuId = __VU || 1;
    const clientIp = `192.168.${Math.floor(vuId / 250)}.${(vuId % 250) + 1}`;
    const headers = {
        'Accept': 'application/json',
        'CF-Connecting-IP': clientIp,
    };

    // Alternate between cache read and health check
    const url = (__ITER % 2 === 0)
        ? `${BASE_URL}/api/v1/rates/USD?symbols=BRL,EUR,GBP`
        : `${BASE_URL}/health/live`;

    const res = http.get(url, { headers });
    reqDuration.add(res.timings.duration);

    if (res.status === 429) {
        rateLimitCount.add(1);
    } else {
        const ok = check(res, {
            'status is 200': (r) => r.status === 200,
        });
        errorRate.add(!ok);
    }
}

