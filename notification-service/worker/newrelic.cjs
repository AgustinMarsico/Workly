'use strict';

exports.config = {
  app_name: [process.env.NEW_RELIC_APP_NAME || 'notification-worker'],
  license_key: process.env.NEW_RELIC_LICENSE_KEY,
  distributed_tracing: {
    enabled: process.env.NEW_RELIC_DISTRIBUTED_TRACING_ENABLED !== 'false',
  },
  application_logging: {
    enabled: true,
    forwarding: {
      enabled: true,
    },
    metrics: {
      enabled: true,
    },
  },
  logging: {
    level: process.env.NEW_RELIC_LOG_LEVEL || 'info',
  },
};
