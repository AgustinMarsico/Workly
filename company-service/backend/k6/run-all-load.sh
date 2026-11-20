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
  # Health
  "tests/load/health/health-check.load.js"

  # Autenticación
  "tests/load/auth/auth-login.load.js"

  # API Keys
  "tests/load/apikey/apikey-create.load.js"
  "tests/load/apikey/apikey-list.load.js"
  "tests/load/apikey/apikey-revoke.load.js"

  # Empresas
  "tests/load/companies/companies-get-all.load.js"
  "tests/load/companies/companies-get-by-id.load.js"
  "tests/load/companies/companies-create.load.js"

  # Invitaciones
  "tests/load/invitations/invitations-get.load.js"
  "tests/load/invitations/invitations-accept.load.js"
  # queda comentado ya que procederia a enviar 1200 mails
  #"tests/load/invitations/invitations-create.load.js"

  # Salas
  "tests/load/rooms/rooms-get-types.load.js"
  "tests/load/rooms/rooms-get-all.load.js"
  "tests/load/rooms/rooms-get-by-id.load.js"
  "tests/load/rooms/rooms-create.load.js"
  "tests/load/rooms/rooms-update.load.js"
  "tests/load/rooms/rooms-delete.load.js"
  "tests/load/rooms/rooms-top.load.js"

  # Usuarios
  "tests/load/users/users-get-all.load.js"
  "tests/load/users/users-get-by-id.load.js"
  "tests/load/users/users-create.load.js"

)

passed=()
failed=()

echo "======================================"
echo "Ejecutando pruebas de carga con k6"
echo "BASE_URL=${BASE_URL:-http://localhost:5000}"
echo "RATE=${RATE:-20} req/s"
echo "TEST_DURATION=${TEST_DURATION:-1m}"
echo "======================================"
echo ""

for test_file in "${tests[@]}"; do
  test_name="$(basename "$test_file" .load.js)"

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
    -e RATE="${RATE:-}" \
    -e TEST_DURATION="${TEST_DURATION:-}" \
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
