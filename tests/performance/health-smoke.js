import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.CIVICHERO_API_URL || 'http://host.docker.internal:5180';

export const options = {
  vus: 5,
  duration: '20s',
  thresholds: {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<500'],
    checks: ['rate>0.99'],
  },
};

export default function () {
  const response = http.get(`${baseUrl}/health/live`, { tags: { endpoint: 'health-live' } });
  check(response, {
    'health returns 200': (result) => result.status === 200,
    'security header is present': (result) => result.headers['X-Content-Type-Options'] === 'nosniff',
  });
  sleep(0.5);
}
