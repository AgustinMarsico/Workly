export function buildSummary(data, testName, endpointTag, expectations = {}) {
  const latencyThresholdMs = expectations.latencyThresholdMs ?? 300;
  const maxErrorRate = expectations.maxErrorRate ?? 0.01;
  const durationMetricName = `http_req_duration{endpoint:${endpointTag}}`;
  const failedMetricName = `http_req_failed{endpoint:${endpointTag}}`;
  const reqsMetricName = `http_reqs{endpoint:${endpointTag}}`;

  const durationMetric = data.metrics[durationMetricName];
  const failedMetric = data.metrics[failedMetricName];
  const reqsMetric = data.metrics[reqsMetricName];
  const p95 = durationMetric?.values?.['p(95)'];
  const errorRate = failedMetric?.values?.rate;
  const totalRequests = reqsMetric?.values?.count ?? 0;
  const passed = totalRequests > 0 && p95 < latencyThresholdMs && errorRate < maxErrorRate;
  const icon = passed ? 'OK' : 'FAIL';
  const text = `${icon} ${testName}\nEndpoint tag: ${endpointTag}\np95: ${p95 ?? 'n/a'} ms\nerror rate: ${errorRate ?? 'n/a'}\nrequests: ${totalRequests}\n`;

  return {
    stdout: text,
    [`results/${testName}.summary.json`]: JSON.stringify({ testName, endpointTag, passed }, null, 2),
    [`results/${testName}.summary.txt`]: text,
  };
}
