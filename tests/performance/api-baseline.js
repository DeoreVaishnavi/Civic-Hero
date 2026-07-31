import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.CIVICHERO_API_URL || 'http://host.docker.internal:5180';

export const options = {
  stages: [
    { duration: '20s', target: 10 },
    { duration: '40s', target: 25 },
    { duration: '20s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{endpoint:health-live}': ['p(95)<500'],
    checks: ['rate>0.99'],
  },
};

export default function () {
  const health = http.get(`${baseUrl}/health/live`, { tags: { endpoint: 'health-live' } });
  check(health, { 'live health is available': (response) => response.status === 200 });

  const anonymous = http.get(`${baseUrl}/api/v1/security/session`, { tags: { endpoint: 'protected-anonymous' } });
  check(anonymous, { 'protected route rejects anonymous': (response) => response.status === 401 });
  sleep(1);
}
