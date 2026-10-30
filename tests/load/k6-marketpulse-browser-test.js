import { browser } from 'k6/browser';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    ui_and_api_browser_journey: {
      executor: 'shared-iterations',
      iterations: 1,
      maxDuration: '2m',
      options: {
        browser: {
          type: 'chromium',
        },
      },
    },
  },
  thresholds: {
    checks: ['rate==1.0'],
    browser_web_vital_fcp: ['p(95) < 2000'],
    browser_web_vital_lcp: ['p(95) < 3000'],
  },
};

export default async function () {
  const baseUrl = __ENV.TARGET_URL || 'https://agentic-pipeline-lab-api.onrender.com';
  const page = await browser.newPage();

  try {
    // Step 1: Healthcheck verification
    const healthResponse = await page.goto(`${baseUrl}/health/live`, {
      waitUntil: 'networkidle',
    });

    check(healthResponse, {
      'healthcheck status is 200': (r) => r && r.status() === 200,
    });

    const healthContent = await page.textContent('body');
    check(healthContent, {
      'health status is Healthy': (t) => t && t.includes('Healthy'),
    });

    // Step 2: Live FX Rates JSON endpoint verification in browser
    const ratesResponse = await page.goto(`${baseUrl}/api/v1/rates/USD?symbols=BRL,EUR,GBP`, {
      waitUntil: 'networkidle',
    });

    check(ratesResponse, {
      'rates endpoint status is 200': (r) => r && r.status() === 200,
    });

    const ratesContent = await page.textContent('body');
    check(ratesContent, {
      'payload contains base USD': (t) => t && t.includes('USD'),
      'payload contains BRL quote': (t) => t && t.includes('BRL'),
      'payload contains EUR quote': (t) => t && t.includes('EUR'),
    });

    // Optional short pause simulating human user interaction
    sleep(1);
  } finally {
    await page.close();
  }
}

