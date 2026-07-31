import http from 'k6/http';
import { check, sleep } from 'k6';
export const options = {
  scenarios: { release_baseline: { executor: 'constant-vus', vus: 20, duration: '60s' } },
  thresholds: { http_req_failed: ['rate<0.01'], http_req_duration: ['p(95)<800', 'p(99)<1500'], checks: ['rate>0.99'] },
};
const baseUrl = __ENV.BASE_URL || 'http://localhost:8088';
export default function () {
  const live = http.get(`${baseUrl}/health/live`);
  check(live, { 'live is 200': (response) => response.status === 200 });
  const ready = http.get(`${baseUrl}/health/ready`);
  check(ready, { 'ready is 200': (response) => response.status === 200 });
  const nearby = http.get(`${baseUrl}/api/v1/complaints/nearby?latitude=19.076&longitude=72.8777&radiusKm=5&limit=10`);
  check(nearby, { 'public nearby is successful': (response) => response.status >= 200 && response.status < 400 });
  sleep(1);
}
