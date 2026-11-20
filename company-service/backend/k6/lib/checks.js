import { check } from 'k6';

export function checkOk(res) {
  check(res, {
    'status is 200': r => r.status === 200,
    'response time < 300ms': r => r.timings.duration < 300,
  });
}