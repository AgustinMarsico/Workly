#!/usr/bin/env bash
set -uo pipefail

cd "$(dirname "$0")"
mkdir -p results

tests=(
  "tests/load/reservations/reservations-create.load.js"
  "tests/load/reservations/reservations-get-mine.load.js"
  "tests/load/reservations/reservations-get-upcoming.load.js"
  "tests/load/reservations/reservations-get-upcoming-stats.load.js"
  "tests/load/reservations/reservations-search.load.js"
  "tests/load/reservations/reservations-history.load.js"
  "tests/load/reservations/reservations-get-by-id.load.js"
  "tests/load/reservations/reservations-cancel.load.js"
)

failed=()

for test_file in "${tests[@]}"; do
  if ! k6 run \
    -e MAIN_SERVICE_URL="${MAIN_SERVICE_URL:-http://localhost:5000}" \
    -e RESERVATION_SERVICE_URL="${RESERVATION_SERVICE_URL:-http://localhost:8081}" \
    -e MEMBER_EMAIL="${MEMBER_EMAIL:-usuario@saas.com}" \
    -e MEMBER_PASSWORD="${MEMBER_PASSWORD:-Password1}" \
    -e API_KEY_HEADER_NAME="${API_KEY_HEADER_NAME:-X-API-Key}" \
    -e RATE="${RATE:-}" \
    -e TEST_DURATION="${TEST_DURATION:-}" \
    "$test_file"; then
    failed+=("$test_file")
  fi
done

if [ "${#failed[@]}" -gt 0 ]; then
  printf 'Failed tests:\n'
  printf '  - %s\n' "${failed[@]}"
  exit 1
fi

exit 0
