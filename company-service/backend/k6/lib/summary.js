export function buildSummary(data, testName, endpointTag, expectations = {}) {
  const latencyThresholdMs = expectations.latencyThresholdMs ?? 300;
  const maxErrorRate = expectations.maxErrorRate ?? 0.01;
  const durationMetricName = `http_req_duration{endpoint:${endpointTag}}`;
  const failedMetricName = `http_req_failed{endpoint:${endpointTag}}`;
  const reqsMetricName = `http_reqs{endpoint:${endpointTag}}`;

  const durationMetric = data.metrics[durationMetricName];
  const failedMetric = data.metrics[failedMetricName];
  const reqsMetric = data.metrics[reqsMetricName];
  const checksMetric = data.metrics.checks;

  const p95 = durationMetric?.values?.['p(95)'];
  const avg = durationMetric?.values?.avg;
  const max = durationMetric?.values?.max;
  const errorRate = failedMetric?.values?.rate;
  const totalRequests = reqsMetric?.values?.count ?? 0;
  const checksRate = checksMetric?.values?.rate;

  const hasRequests = totalRequests > 0;
  const hasDurationMetric = p95 !== undefined && p95 !== null;
  const hasErrorMetric = errorRate !== undefined && errorRate !== null;

  const passedP95 = hasDurationMetric && p95 < latencyThresholdMs;
  const passedErrors = hasErrorMetric && errorRate < maxErrorRate;
  const passed = hasRequests && passedP95 && passedErrors;

  const result = {
    testName,
    endpointTag,
    passed,
    thresholds: {
      hasEndpointRequests: hasRequests,
      p95Under300ms: passedP95,
      errorRateUnder1Percent: passedErrors,
      p95WithinThreshold: passedP95,
      errorRateWithinThreshold: passedErrors,
      latencyThresholdMs,
      maxErrorRatePercent: round(maxErrorRate * 100),
    },
    metrics: {
      p95Ms: round(p95),
      avgMs: round(avg),
      maxMs: round(max),
      errorRatePercent: hasErrorMetric ? round(errorRate * 100) : null,
      totalRequests,
      checksRatePercent: checksRate !== undefined ? round(checksRate * 100) : null,
    },
  };

  const icon = passed ? '✅' : '❌';

  const text = `
${icon} ${testName} ${passed ? 'PASÓ' : 'NO PASÓ'}

Endpoint tag: ${endpointTag}
p95: ${result.metrics.p95Ms} ms
avg: ${result.metrics.avgMs} ms
max: ${result.metrics.maxMs} ms
error rate: ${result.metrics.errorRatePercent}%
requests: ${result.metrics.totalRequests}
checks: ${result.metrics.checksRatePercent}%

Criterios:
- hubo requests reales al endpoint: ${hasRequests ? 'OK' : 'FAIL'}
- p95 < ${latencyThresholdMs} ms: ${passedP95 ? 'OK' : 'FAIL'}
- error rate < ${round(maxErrorRate * 100)}%: ${passedErrors ? 'OK' : 'FAIL'}
`;

  return {
    stdout: text,
    [`results/${testName}.summary.json`]: JSON.stringify(result, null, 2),
    [`results/${testName}.summary.txt`]: text,
  };
}

function round(value) {
  if (value === undefined || value === null || Number.isNaN(value)) {
    return null;
  }

  return Math.round(value * 100) / 100;
}
