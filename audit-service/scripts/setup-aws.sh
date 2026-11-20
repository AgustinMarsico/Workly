AWS_REGION="us-east-1"
SQS_AUDIT_QUEUE_NAME="audit-events-queue.fifo"

echo "Region: $AWS_REGION"
echo "Audit SQS Queue: $SQS_AUDIT_QUEUE_NAME"
echo ""

echo "Creating SQS FIFO Queue: $SQS_AUDIT_QUEUE_NAME"
AUDIT_QUEUE_URL=$(aws sqs create-queue \
  --queue-name "$SQS_AUDIT_QUEUE_NAME" \
  --attributes FifoQueue=true,ContentBasedDeduplication=true \
  --region "$AWS_REGION" \
  --query "QueueUrl" \
  --output text)
echo "  SQS_AUDIT_QUEUE_URL=$AUDIT_QUEUE_URL"

AUDIT_QUEUE_ARN=$(aws sqs get-queue-attributes \
  --queue-url "$AUDIT_QUEUE_URL" \
  --attribute-names QueueArn \
  --region "$AWS_REGION" \
  --query "Attributes.QueueArn" \
  --output text)
echo "  SQS_AUDIT_QUEUE_ARN=$AUDIT_QUEUE_ARN"
echo ""

echo "=============================================="
echo "Infrastructure created successfully"
echo "=============================================="
echo "AWS_REGION=$AWS_REGION"
echo "SQS_AUDIT_QUEUE_URL=$AUDIT_QUEUE_URL"
echo "SQS_AUDIT_QUEUE_ARN=$AUDIT_QUEUE_ARN"
echo "=============================================="
echo ""
echo "Use these values in your audit service and business services environment:"
echo "AWS_REGION=$AWS_REGION"
echo "SQS_AUDIT_QUEUE_URL=$AUDIT_QUEUE_URL"
