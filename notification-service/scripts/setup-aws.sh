AWS_REGION="us-east-1"
SNS_TOPIC_NAME="notification-topic"
SQS_QUEUE_NAME="notification-queue"
SNS_INVITATION_TOPIC_NAME="invitation-topic"
SQS_INVITATION_QUEUE_NAME="invitation-queue"

echo "Region: $AWS_REGION"
echo "Reservation SNS Topic: $SNS_TOPIC_NAME"
echo "Reservation SQS Queue: $SQS_QUEUE_NAME"
echo "Invitation SNS Topic: $SNS_INVITATION_TOPIC_NAME"
echo "Invitation SQS Queue: $SQS_INVITATION_QUEUE_NAME"
echo ""

create_topic_and_queue() {
  local topic_name="$1"
  local queue_name="$2"
  local topic_arn_var="$3"
  local queue_url_var="$4"
  local queue_arn_var="$5"
  local subscription_arn_var="$6"

  echo "Creating SNS Topic: $topic_name"
  local topic_arn
  topic_arn=$(aws sns create-topic \
    --name "$topic_name" \
    --region "$AWS_REGION" \
    --query "TopicArn" \
    --output text)
  echo "  TOPIC_ARN=$topic_arn"

  echo "Creating SQS Queue: $queue_name"
  local queue_url
  queue_url=$(aws sqs create-queue \
    --queue-name "$queue_name" \
    --region "$AWS_REGION" \
    --query "QueueUrl" \
    --output text)
  echo "  QUEUE_URL=$queue_url"

  local queue_arn
  queue_arn=$(aws sqs get-queue-attributes \
    --queue-url "$queue_url" \
    --attribute-names QueueArn \
    --region "$AWS_REGION" \
    --query "Attributes.QueueArn" \
    --output text)
  echo "  QUEUE_ARN=$queue_arn"

  echo "Allowing SNS to publish to SQS..."
  cat > "/tmp/sqs-attributes-${queue_name}.json" << EOF
{
  "Policy": "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Sid\":\"AllowSNSToSendMessage\",\"Effect\":\"Allow\",\"Principal\":{\"Service\":\"sns.amazonaws.com\"},\"Action\":\"sqs:SendMessage\",\"Resource\":\"${queue_arn}\",\"Condition\":{\"ArnEquals\":{\"aws:SourceArn\":\"${topic_arn}\"}}}]}"
}
EOF

  aws sqs set-queue-attributes \
    --queue-url "$queue_url" \
    --region "$AWS_REGION" \
    --attributes file:///tmp/sqs-attributes-${queue_name}.json
  echo "  SQS policy applied"

  echo "Subscribing SQS to SNS..."
  local subscription_arn
  subscription_arn=$(aws sns subscribe \
    --topic-arn "$topic_arn" \
    --protocol sqs \
    --notification-endpoint "$queue_arn" \
    --region "$AWS_REGION" \
    --return-subscription-arn \
    --query "SubscriptionArn" \
    --output text)
  echo "  SUBSCRIPTION_ARN=$subscription_arn"
  echo ""

  eval "$topic_arn_var='$topic_arn'"
  eval "$queue_url_var='$queue_url'"
  eval "$queue_arn_var='$queue_arn'"
  eval "$subscription_arn_var='$subscription_arn'"
}

create_topic_and_queue \
  "$SNS_TOPIC_NAME" \
  "$SQS_QUEUE_NAME" \
  TOPIC_ARN \
  QUEUE_URL \
  QUEUE_ARN \
  SUBSCRIPTION_ARN

create_topic_and_queue \
  "$SNS_INVITATION_TOPIC_NAME" \
  "$SQS_INVITATION_QUEUE_NAME" \
  INVITATION_TOPIC_ARN \
  INVITATION_QUEUE_URL \
  INVITATION_QUEUE_ARN \
  INVITATION_SUBSCRIPTION_ARN

echo "=============================================="
echo "Infrastructure created successfully"
echo "=============================================="
echo "AWS_REGION=$AWS_REGION"
echo "SNS_TOPIC_ARN=$TOPIC_ARN"
echo "SQS_QUEUE_URL=$QUEUE_URL"
echo "SQS_QUEUE_ARN=$QUEUE_ARN"
echo "SNS_SUBSCRIPTION_ARN=$SUBSCRIPTION_ARN"
echo "SNS_INVITATION_TOPIC_ARN=$INVITATION_TOPIC_ARN"
echo "SQS_INVITATION_QUEUE_URL=$INVITATION_QUEUE_URL"
echo "SQS_INVITATION_QUEUE_ARN=$INVITATION_QUEUE_ARN"
echo "SNS_INVITATION_SUBSCRIPTION_ARN=$INVITATION_SUBSCRIPTION_ARN"
echo "=============================================="
echo ""
echo "Use these values in your worker environment:"
echo "AWS_REGION=$AWS_REGION"
echo "SQS_QUEUE_URL=$QUEUE_URL"
echo "SQS_INVITATION_QUEUE_URL=$INVITATION_QUEUE_URL"
