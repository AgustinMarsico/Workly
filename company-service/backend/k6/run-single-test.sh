#!/usr/bin/env bash
# Script para ejecutar un test de k6 individual con las variables de entorno cargadas

set -uo pipefail

cd "$(dirname "$0")"

# Cargar variables de entorno desde .env
if [ -f .env ]; then
  set -a
  source .env
  set +a
else
  echo "❌ Error: archivo .env no encontrado en $(pwd)"
  exit 1
fi

# Verificar que se pasó el archivo de test
if [ $# -eq 0 ]; then
  echo "❌ Uso: $0 <path/to/test.js>"
  echo ""
  echo "Ejemplos:"
  echo "  $0 tests/load/health/health-check.load.js"
  echo "  $0 tests/spike/health/health-check.spike.js"
  echo "  $0 tests/load/invitations/invitations-get.load.js"
  echo ""
  echo "Variables de entorno cargadas desde .env:"
  echo "  BASE_URL=${BASE_URL:-no configurado}"
  echo "  COMPANY_ID=${COMPANY_ID:-no configurado}"
  echo "  INVITATION_COMPANY_ID=${INVITATION_COMPANY_ID:-no configurado}"
  exit 1
fi

test_file="$1"

if [ ! -f "$test_file" ]; then
  echo "❌ Error: archivo de test no encontrado: $test_file"
  exit 1
fi

# Pasar las variables de entorno a k6
k6 run \
  -e BASE_URL="${BASE_URL}" \
  -e ADMIN_EMAIL="${ADMIN_EMAIL:-}" \
  -e ADMIN_PASSWORD="${ADMIN_PASSWORD:-}" \
  -e MEMBER_EMAIL="${MEMBER_EMAIL:-}" \
  -e MEMBER_PASSWORD="${MEMBER_PASSWORD:-}" \
  -e COMPANY_ID="${COMPANY_ID:-}" \
  -e INVITATION_COMPANY_ID="${INVITATION_COMPANY_ID:-${COMPANY_ID}}" \
  -e CREATE_USER_ROLE="${CREATE_USER_ROLE:-Member}" \
  -e INVITATION_EMAIL_DOMAIN="${INVITATION_EMAIL_DOMAIN:-workly.test}" \
  -e API_KEY_HEADER_NAME="${API_KEY_HEADER_NAME:-}" \
  -e RATE="${RATE:-}" \
  -e TEST_DURATION="${TEST_DURATION:-}" \
  -e SPIKE_START_RATE="${SPIKE_START_RATE:-}" \
  -e SPIKE_RATE="${SPIKE_RATE:-}" \
  -e SPIKE_PRE_ALLOCATED_VUS="${SPIKE_PRE_ALLOCATED_VUS:-}" \
  -e SPIKE_MAX_VUS="${SPIKE_MAX_VUS:-}" \
  -e SPIKE_RAMP_UP="${SPIKE_RAMP_UP:-}" \
  -e SPIKE_HOLD="${SPIKE_HOLD:-}" \
  -e SPIKE_RAMP_DOWN="${SPIKE_RAMP_DOWN:-}" \
  -e SPIKE_COOLDOWN="${SPIKE_COOLDOWN:-}" \
  "$test_file"
