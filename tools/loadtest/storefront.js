// k6 load test — five hottest storefront endpoints. Target: p95 < 500ms at 50 RPS.
//
// Usage:
//   k6 run -e API=https://localhost:7001 -e STOREFRONT=https://localhost:7100 tools/loadtest/storefront.js
//   k6 run -e API=https://api.example.com -e STOREFRONT=https://shop.example.com --vus 50 --duration 5m ...
//
// The default stages ramp to 50 virtual users (≈50 RPS sustained with ~1 req/s/user)
// and hold for 3 minutes, which is enough for the Aspire dashboard to surface p95 +
// GC + DB-connection-pool pressure.

import http from 'k6/http';
import { check, sleep, group } from 'k6';

export const options = {
    thresholds: {
        http_req_failed:   ['rate<0.01'],        // <1 % errors
        http_req_duration: ['p(95)<500'],        // P7-05 contract
    },
    stages: [
        { duration: '30s', target: 10 },
        { duration: '1m',  target: 50 },
        { duration: '3m',  target: 50 },
        { duration: '30s', target: 0 },
    ],
};

const API        = __ENV.API        || 'https://localhost:7001';
const STOREFRONT = __ENV.STOREFRONT || 'https://localhost:7100';

// Deterministic slugs let the smoke suite ride on SampleData seeded fixtures.
// Adjust when the seed set changes.
const SAMPLE_PRODUCT_SLUG  = __ENV.PRODUCT_SLUG  || 'beats-solo-3';
const SAMPLE_CATEGORY_SLUG = __ENV.CATEGORY_SLUG || 'mens-shoes';

export default function () {
    group('GET /', () => {
        const r = http.get(`${STOREFRONT}/`, { tags: { name: 'Home' } });
        check(r, { 'home 2xx/3xx': x => x.status < 400 });
    });

    group('GET /category/{slug}', () => {
        const r = http.get(`${STOREFRONT}/category/${SAMPLE_CATEGORY_SLUG}`, { tags: { name: 'Category' } });
        check(r, { 'category 2xx/3xx': x => x.status < 400 });
    });

    group('GET /product/{slug}', () => {
        const r = http.get(`${STOREFRONT}/product/${SAMPLE_PRODUCT_SLUG}`, { tags: { name: 'Product' } });
        check(r, { 'product 2xx/3xx': x => x.status < 400 });
    });

    group('GET /api/storefront/catalog/products', () => {
        const r = http.get(`${API}/api/storefront/catalog/products?pageSize=12`, { tags: { name: 'ListProducts' } });
        check(r, { 'list products 200': x => x.status === 200 });
    });

    group('GET /api/storefront/search', () => {
        const r = http.get(`${API}/api/storefront/search?q=red`, { tags: { name: 'Search' } });
        check(r, { 'search 200': x => x.status === 200 });
    });

    sleep(1);
}
