# =============================================================================
# AWS Academy Lab - Publish test invitation message to SNS
# =============================================================================
#
# 1. Edit the configuration below
# 2. Paste into CloudShell (or upload and run: bash publish-test-invitation.sh)
#
# =============================================================================

# --- Configuration (edit before running) ---
AWS_REGION="us-east-1"
SNS_INVITATION_TOPIC_ARN="arn:aws:sns:us-east-1:ACCOUNT_ID:invitation-topic"
USER_EMAIL="your-email@example.com"
INVITATION_ID="00000000-0000-0000-0000-000000000001"
COMPANY_ID="00000000-0000-0000-0000-000000000002"
COMPANY_NAME="Demo Company"
AREA_NAME="Engineering"
PLAIN_TOKEN="demo-invitation-token"

echo "Region: $AWS_REGION"
echo "Topic: $SNS_INVITATION_TOPIC_ARN"
echo "Recipient: $USER_EMAIL"
echo ""

export AWS_REGION SNS_INVITATION_TOPIC_ARN USER_EMAIL INVITATION_ID
export COMPANY_ID COMPANY_NAME AREA_NAME PLAIN_TOKEN

MESSAGE=$(python3 << 'PYSCRIPT'
import json
import os

print(json.dumps({
    "InvitationId": os.environ["INVITATION_ID"],
    "UserEmail": os.environ["USER_EMAIL"],
    "CompanyId": os.environ["COMPANY_ID"],
    "CompanyName": os.environ["COMPANY_NAME"],
    "AreaName": os.environ["AREA_NAME"],
    "PlainToken": os.environ["PLAIN_TOKEN"],
}))
PYSCRIPT
)

aws sns publish \
  --region "$AWS_REGION" \
  --topic-arn "$SNS_INVITATION_TOPIC_ARN" \
  --message "$MESSAGE"

echo ""
echo "Invitation test message published to $SNS_INVITATION_TOPIC_ARN"
