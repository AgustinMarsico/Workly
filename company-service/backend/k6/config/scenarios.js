export const twelveHundredReqPerMinute = {
  executor: 'constant-arrival-rate',
  rate: 20,
  timeUnit: '1s',
  duration: '5m',
  preAllocatedVUs: 50,
  maxVUs: 200,
};

export function buildSpikeScenario(overrides = {}) {
  const startRate = readPositiveNumber(
    overrides.startRate,
    __ENV.SPIKE_START_RATE,
    5
  );
  const peakRate = readPositiveNumber(
    overrides.peakRate,
    __ENV.SPIKE_RATE,
    150
  );
  const preAllocatedVUs = readPositiveNumber(
    overrides.preAllocatedVUs,
    __ENV.SPIKE_PRE_ALLOCATED_VUS,
    100
  );
  const maxVUs = readPositiveNumber(
    overrides.maxVUs,
    __ENV.SPIKE_MAX_VUS,
    400
  );

  return {
    executor: 'ramping-arrival-rate',
    startRate,
    timeUnit: '1s',
    preAllocatedVUs,
    maxVUs,
    stages: [
      {
        target: peakRate,
        duration: overrides.rampUp || __ENV.SPIKE_RAMP_UP || '20s',
      },
      {
        target: peakRate,
        duration: overrides.hold || __ENV.SPIKE_HOLD || '40s',
      },
      {
        target: startRate,
        duration: overrides.rampDown || __ENV.SPIKE_RAMP_DOWN || '20s',
      },
      {
        target: startRate,
        duration: overrides.cooldown || __ENV.SPIKE_COOLDOWN || '30s',
      },
    ],
    gracefulStop: overrides.gracefulStop || '30s',
  };
}

function readPositiveNumber(...values) {
  for (const value of values) {
    const parsed = Number(value);

    if (Number.isFinite(parsed) && parsed > 0) {
      return parsed;
    }
  }

  return 1;
}
