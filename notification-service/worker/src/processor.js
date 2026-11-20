import { sendEmail } from './services/emailService.js';
import {
  buildInvitationHtml,
  buildInvitationLink,
  buildInvitationSubject,
  buildInvitationText,
  isInvitationPayload,
  validateInvitationPayload,
} from './templates/invitationEmail.js';
import {
  buildReservationHtml,
  buildReservationSubject,
  buildReservationText,
  validateReservationPayload,
} from './templates/reservationEmail.js';
import { config } from './config.js';

export function parseSnsMessage(body) {
  const envelope = JSON.parse(body);

  if (envelope.Type === 'Notification' && envelope.Message) {
    return JSON.parse(envelope.Message);
  }

  return envelope;
}

export async function processMessage(payload) {
  if (isInvitationPayload(payload)) {
    return processInvitation(payload);
  }

  return processReservationNotification(payload);
}

async function processReservationNotification(payload) {
  const notification = validateReservationPayload(payload);
  const { emails } = notification;

  if ((payload.emailSubscribed ?? payload.EmailSubscribed) === false) {
    return {
      type: 'reservation',
      status: 'skipped',
      reason: 'Recipients are not subscribed to email notifications',
      emails,
      reservationId: notification.reservation.id,
      action: notification.action,
    };
  }

  const subject = buildReservationSubject(notification);
  const htmlContent = buildReservationHtml(notification);
  const textContent = buildReservationText(notification);

  await Promise.all(
    emails.map((email) =>
      sendEmail({
        to: email,
        subject,
        htmlContent,
        textContent,
      }),
    ),
  );

  return {
    type: 'reservation',
    status: 'sent',
    emails,
    userId: notification.user.id,
    reservationId: notification.reservation.id,
    action: notification.action,
  };
}

async function processInvitation(payload) {
  const invitation = validateInvitationPayload(payload);
  const invitationLink = buildInvitationLink(
    config.invitation.baseUrl,
    invitation.invitationId,
    invitation.plainToken,
  );

  await sendEmail({
    to: invitation.userEmail,
    subject: buildInvitationSubject(),
    htmlContent: buildInvitationHtml({
      companyName: invitation.companyName,
      areaName: invitation.areaName,
      invitationLink,
    }),
    textContent: buildInvitationText({
      companyName: invitation.companyName,
      areaName: invitation.areaName,
      invitationLink,
    }),
  });

  return {
    type: 'invitation',
    status: 'sent',
    email: invitation.userEmail,
    invitationId: invitation.invitationId,
    companyId: invitation.companyId,
  };
}
