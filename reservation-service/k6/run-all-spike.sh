#!/usr/bin/env bash
set -uo pipefail

cd "$(dirname "$0")"
mkdir -p results

k6 run \
  -e MAIN_SERVICE_URL="${MAIN_SERVICE_URL:-http://localhost:5000}" \
  -e RESERVATION_SERVICE_URL="${RESERVATION_SERVICE_URL:-http://localhost:8081}" \
  -e MEMBER_EMAIL="${MEMBER_EMAIL:-usuario@saas.com}" \
  -e MEMBER_PASSWORD="${MEMBER_PASSWORD:-Password1}" \
  -e API_KEY_HEADER_NAME="${API_KEY_HEADER_NAME:-X-API-Key}" \
  tests/spike/reservations/reservations-history.spike.js
