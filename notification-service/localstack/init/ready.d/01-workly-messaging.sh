#!/bin/sh
set -e

export AWS_DEFAULT_REGION="${AWS_DEFAULT_REGION:-us-east-1}"

create_topic_queue_subscription() {
  topic_name="$1"
  queue_name="$2"

  topic_arn="$(awslocal sns create-topic \
    --name "$topic_name" \
    --query TopicArn \
    --output text)"

  queue_url="$(awslocal sqs create-queue \
    --queue-name "$queue_name" \
    --query QueueUrl \
    --output text)"

  queue_arn="$(awslocal sqs get-queue-attributes \
    --queue-url "$queue_url" \
    --attribute-names QueueArn \
    --query 'Attributes.QueueArn' \
    --output text)"

  awslocal sns subscribe \
    --topic-arn "$topic_arn" \
    --protocol sqs \
    --notification-endpoint "$queue_arn" \
    --return-subscription-arn >/dev/null

  echo "Created $topic_name -> $queue_name"
}

create_topic_queue_subscription notification-topic notification-queue
create_topic_queue_subscription invitation-topic invitation-queue

awslocal sqs create-queue \
  --queue-name audit-events-queue.fifo \
  --attributes FifoQueue=true,ContentBasedDeduplication=true >/dev/null

echo "Created audit-events-queue.fifo"
