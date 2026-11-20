#!/usr/bin/env bash
set -uo pipefail

cd "$(dirname "$0")"

if [ -f .env ]; then
  set -a
  source .env
  set +a
fi

mkdir -p results

tests=(
  "tests/spike/health/health-check.spike.js"
  "tests/spike/rooms/rooms-top.spike.js"
)

passed=()
failed=()

echo "======================================"
echo "Ejecutando spike tests con k6"
echo "BASE_URL=${BASE_URL:-http://localhost:5000}"
echo "SPIKE_START_RATE=${SPIKE_START_RATE:-}"
echo "SPIKE_RATE=${SPIKE_RATE:-}"
echo "SPIKE_RAMP_UP=${SPIKE_RAMP_UP:-}"
echo "SPIKE_HOLD=${SPIKE_HOLD:-}"
echo "SPIKE_RAMP_DOWN=${SPIKE_RAMP_DOWN:-}"
echo "SPIKE_COOLDOWN=${SPIKE_COOLDOWN:-}"
echo "======================================"
echo ""

for test_file in "${tests[@]}"; do
  echo ""
  echo "======================================"
  echo "▶ Ejecutando: $test_file"
  echo "======================================"

  if k6 run \
    -e BASE_URL="${BASE_URL:-http://localhost:5000}" \
    -e ADMIN_EMAIL="${ADMIN_EMAIL:-admin@saas.com}" \
    -e ADMIN_PASSWORD="${ADMIN_PASSWORD:-Password1}" \
    -e MEMBER_EMAIL="${MEMBER_EMAIL:-usuario@saas.com}" \
    -e MEMBER_PASSWORD="${MEMBER_PASSWORD:-Password1}" \
    -e COMPANY_ID="${COMPANY_ID:-}" \
    -e INVITATION_COMPANY_ID="${INVITATION_COMPANY_ID:-${COMPANY_ID}}" \
    -e CREATE_USER_ROLE="${CREATE_USER_ROLE:-Member}" \
    -e INVITATION_EMAIL_DOMAIN="${INVITATION_EMAIL_DOMAIN:-workly.test}" \
    -e API_KEY_HEADER_NAME="${API_KEY_HEADER_NAME:-X-API-Key}" \
    -e SPIKE_START_RATE="${SPIKE_START_RATE:-}" \
    -e SPIKE_RATE="${SPIKE_RATE:-}" \
    -e SPIKE_PRE_ALLOCATED_VUS="${SPIKE_PRE_ALLOCATED_VUS:-}" \
    -e SPIKE_MAX_VUS="${SPIKE_MAX_VUS:-}" \
    -e SPIKE_RAMP_UP="${SPIKE_RAMP_UP:-}" \
    -e SPIKE_HOLD="${SPIKE_HOLD:-}" \
    -e SPIKE_RAMP_DOWN="${SPIKE_RAMP_DOWN:-}" \
    -e SPIKE_COOLDOWN="${SPIKE_COOLDOWN:-}" \
    "$test_file"; then
    passed+=("$test_file")
    echo "✅ PASÓ: $test_file"
  else
    failed+=("$test_file")
    echo "❌ FALLÓ: $test_file"
  fi
done

echo ""
echo "======================================"
echo "Resumen final"
echo "======================================"

echo ""
echo "✅ Pasaron: ${#passed[@]}"
for test_file in "${passed[@]}"; do
  echo "  - $test_file"
done

echo ""
echo "❌ Fallaron: ${#failed[@]}"
for test_file in "${failed[@]}"; do
  echo "  - $test_file"
done

echo ""

if [ "${#failed[@]}" -gt 0 ]; then
  exit 1
fi

exit 0
