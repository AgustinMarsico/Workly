# Fill in the values from setup-aws.sh output

AWS_REGION="us-east-1"
TOPIC_ARN=""
QUEUE_URL=""
INVITATION_TOPIC_ARN=""
INVITATION_QUEUE_URL=""

echo "Region: $AWS_REGION"
echo ""

echo "Deleting reservation SNS topic..."
aws sns delete-topic \
  --topic-arn "$TOPIC_ARN" \
  --region "$AWS_REGION"
echo ""

echo "Deleting reservation SQS queue..."
aws sqs delete-queue \
  --queue-url "$QUEUE_URL" \
  --region "$AWS_REGION"
echo ""

echo "Deleting invitation SNS topic..."
aws sns delete-topic \
  --topic-arn "$INVITATION_TOPIC_ARN" \
  --region "$AWS_REGION"
echo ""

echo "Deleting invitation SQS queue..."
aws sqs delete-queue \
  --queue-url "$INVITATION_QUEUE_URL" \
  --region "$AWS_REGION"
echo ""

echo "=============================================="
echo "Messaging infrastructure deleted"
echo "=============================================="
