# =============================================================================
# Publish test reservation message to SNS
# =============================================================================
#
# Set SNS_TOPIC_ARN and RECIPIENT_EMAILS in your private environment before running.
# Example recipient format: first@example.com,second@example.com
#
# =============================================================================

set -eu

AWS_REGION="${AWS_REGION:-us-east-1}"
: "${SNS_TOPIC_ARN:?Set SNS_TOPIC_ARN for the target environment}"
: "${RECIPIENT_EMAILS:?Set RECIPIENT_EMAILS with test recipients}"
ACTION="${ACTION:-updated}"

echo "Region: $AWS_REGION"
echo "Action: $ACTION"
echo ""

export AWS_REGION SNS_TOPIC_ARN RECIPIENT_EMAILS ACTION

MESSAGE=$(python3 << 'PYSCRIPT'
import json
import os
from datetime import datetime, timezone

action = os.environ["ACTION"]
emails = [
    email.strip()
    for email in os.environ["RECIPIENT_EMAILS"].split(",")
    if email.strip()
]

if not emails:
    raise SystemExit("RECIPIENT_EMAILS must include at least one email")

base = {
    "emails": emails,
    "emailSubscribed": True,
    "action": action,
    "actionAt": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
    "reservation": {
        "id": "res-1001",
        "space": {"id": "space-a", "name": "Conference Room A"},
        "startAt": "2026-06-15T11:00:00Z",
        "endAt": "2026-06-15T13:00:00Z",
    },
    "user": {
        "id": "user-1",
        "name": "Demo User",
        "email": emails[0],
    },
}

if action == "created":
    base["reservation"]["startAt"] = "2026-06-15T10:00:00Z"
    base["reservation"]["endAt"] = "2026-06-15T12:00:00Z"
elif action == "updated":
    base["changes"] = [
        {
            "field": "startAt",
            "previous": "2026-06-15T10:00:00Z",
            "current": "2026-06-15T11:00:00Z",
        },
        {
            "field": "endAt",
            "previous": "2026-06-15T12:00:00Z",
            "current": "2026-06-15T13:00:00Z",
        },
    ]
elif action != "cancelled":
    raise SystemExit("ACTION must be one of: created, updated, cancelled")

print(json.dumps(base))
PYSCRIPT
)

aws sns publish \
  --region "$AWS_REGION" \
  --topic-arn "$SNS_TOPIC_ARN" \
  --message "$MESSAGE"

echo ""
echo "Reservation '$ACTION' test message published."
