#!/usr/bin/env bash
set -euo pipefail

OPEN_REPORT=false

if [[ "${1:-}" == "--open" ]]; then
  OPEN_REPORT=true
elif [[ "${1:-}" != "" ]]; then
  echo "Unknown argument: $1"
  echo "Usage: $0 [--open]"
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

COVERAGE_DIR="/tmp/opencode/rooms-coverage"
REPORT_DIR="/tmp/opencode/rooms-coverage-report-rnf6"

export PATH="$PATH:$HOME/.dotnet/tools"

if ! command -v reportgenerator >/dev/null 2>&1; then
  echo "reportgenerator is not installed or not available in PATH."
  echo "Install it with:"
  echo "  dotnet tool install -g dotnet-reportgenerator-globaltool"
  echo "Or update it with:"
  echo "  dotnet tool update -g dotnet-reportgenerator-globaltool"
  exit 1
fi

cd "$REPO_ROOT"

rm -rf "$COVERAGE_DIR" "$REPORT_DIR"
mkdir -p "$COVERAGE_DIR"

dotnet test "RoomsService.ApiTest/RoomsService.ApiTest.csproj" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$COVERAGE_DIR/api"

dotnet test "BusinessLogicTest/BusinessLogicTest.csproj" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$COVERAGE_DIR/business"

dotnet test "DataAccessTest/DataAccessTest.csproj" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$COVERAGE_DIR/data"

reportgenerator \
  -reports:"$COVERAGE_DIR/**/coverage.cobertura.xml" \
  -targetdir:"$REPORT_DIR" \
  -reporttypes:"Html;TextSummary" \
  -filefilters:"+*RoomsController.cs;+*RoomService.cs;+*RoomRepository.cs;+*ReservationServiceAuthContext.cs;+*RoomTypeFactory.cs;+*RoomType.cs;+*ApiErrors.cs;+*CustomException.cs"

echo "Coverage report generated:"
echo "  $REPORT_DIR/index.html"
echo
echo "Text summary:"
echo "  $REPORT_DIR/Summary.txt"
echo
echo "Open it with:"
echo "  xdg-open $REPORT_DIR/index.html"

if [[ "$OPEN_REPORT" == true ]]; then
  xdg-open "$REPORT_DIR/index.html"
fi
