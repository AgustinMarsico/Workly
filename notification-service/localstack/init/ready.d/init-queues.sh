#!/bin/bash
set -euo pipefail

REGION="${AWS_DEFAULT_REGION:-us-east-1}"

create_topic_and_queue() {
  local topic_name="$1"
  local queue_name="$2"

  local topic_arn
  topic_arn="$(awslocal sns create-topic --name "$topic_name" --region "$REGION" --query TopicArn --output text)"

  local queue_url
  queue_url="$(awslocal sqs create-queue --queue-name "$queue_name" --region "$REGION" --query QueueUrl --output text)"

  local queue_arn
  queue_arn="$(awslocal sqs get-queue-attributes \
    --queue-url "$queue_url" \
    --attribute-names QueueArn \
    --region "$REGION" \
    --query 'Attributes.QueueArn' \
    --output text)"

  awslocal sqs set-queue-attributes \
    --queue-url "$queue_url" \
    --region "$REGION" \
    --attributes "{\"Policy\":\"{\\\"Version\\\":\\\"2012-10-17\\\",\\\"Statement\\\":[{\\\"Effect\\\":\\\"Allow\\\",\\\"Principal\\\":{\\\"Service\\\":\\\"sns.amazonaws.com\\\"},\\\"Action\\\":\\\"sqs:SendMessage\\\",\\\"Resource\\\":\\\"${queue_arn}\\\",\\\"Condition\\\":{\\\"ArnEquals\\\":{\\\"aws:SourceArn\\\":\\\"${topic_arn}\\\"}}}]}\"}"

  awslocal sns subscribe \
    --topic-arn "$topic_arn" \
    --protocol sqs \
    --notification-endpoint "$queue_arn" \
    --region "$REGION" \
    --attributes RawMessageDelivery=true \
    >/dev/null

  echo "Created ${topic_name} -> ${queue_name}"
}

awslocal sqs create-queue \
  --queue-name audit-events-queue.fifo \
  --region "$REGION" \
  --attributes FifoQueue=true,ContentBasedDeduplication=true \
  >/dev/null

create_topic_and_queue notification-topic notification-queue
create_topic_and_queue invitation-topic invitation-queue

echo "LocalStack queues and topics initialized."
