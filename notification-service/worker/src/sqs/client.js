import {
  DeleteMessageCommand,
  GetQueueAttributesCommand,
  ReceiveMessageCommand,
  SQSClient,
} from '@aws-sdk/client-sqs';
import { config } from '../config.js';

const sqsClient = new SQSClient({
  region: config.aws.region,
  ...(config.aws.endpoint
    ? { endpoint: config.aws.endpoint, forcePathStyle: true }
    : {}),
});

export async function receiveMessages(queueUrl) {
  const command = new ReceiveMessageCommand({
    QueueUrl: queueUrl,
    MaxNumberOfMessages: config.aws.maxMessages,
    WaitTimeSeconds: config.aws.waitTimeSeconds,
    VisibilityTimeout: config.aws.visibilityTimeout,
    MessageAttributeNames: ['All'],
  });

  const response = await sqsClient.send(command);
  return response.Messages ?? [];
}

export async function getQueueDepth(queueUrl) {
  const command = new GetQueueAttributesCommand({
    QueueUrl: queueUrl,
    AttributeNames: [
      'ApproximateNumberOfMessages',
      'ApproximateNumberOfMessagesNotVisible',
    ],
  });

  const response = await sqsClient.send(command);
  return {
    pending: Number(response.Attributes?.ApproximateNumberOfMessages ?? 0),
    inFlight: Number(response.Attributes?.ApproximateNumberOfMessagesNotVisible ?? 0),
  };
}

export async function deleteMessage(queueUrl, receiptHandle) {
  const command = new DeleteMessageCommand({
    QueueUrl: queueUrl,
    ReceiptHandle: receiptHandle,
  });

  await sqsClient.send(command);
}
