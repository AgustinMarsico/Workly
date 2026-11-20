#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

if [ -f .env ]; then
  set -a
  source .env
  set +a
fi

SCRIPT="${1:-tests/load/auth/auth-login.load.js}"

k6 run "$SCRIPT"