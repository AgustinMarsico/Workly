import { BrevoClient } from '@getbrevo/brevo';
import nodemailer from 'nodemailer';
import { config } from '../config.js';

export function createEmailService(settings, {
  createBrevoClient = (options) => new BrevoClient(options),
  createTransport = (options) => nodemailer.createTransport(options),
  logger = console,
} = {}) {
  const mode = settings.brevo.deliveryMode;
  if (!['brevo', 'smtp', 'log'].includes(mode)) {
    throw new Error('Unsupported email delivery mode');
  }

  const brevoClient = mode === 'brevo'
    ? createBrevoClient({ apiKey: settings.brevo.apiKey })
    : null;
  const smtpTransport = mode === 'smtp'
    ? createTransport({
        host: settings.smtp.host,
        port: settings.smtp.port,
        secure: settings.smtp.port === 465,
        requireTLS: settings.smtp.useSsl,
        ...(settings.smtp.username && settings.smtp.password
          ? { auth: { user: settings.smtp.username, pass: settings.smtp.password } }
          : {}),
      })
    : null;

  return async function sendEmail({ to, subject, htmlContent, textContent }) {
    const text = textContent ?? stripHtml(htmlContent);

    if (mode === 'log') {
      // No registrar contenido: puede incluir tokens de invitación de un solo uso.
      logger.log('Email delivery skipped in log mode', { to, subject });
      return { skipped: true, mode: 'log' };
    }

    if (mode === 'smtp') {
      const result = await smtpTransport.sendMail({
        from: {
          address: settings.smtp.senderEmail,
          name: settings.smtp.senderName,
        },
        to,
        subject,
        html: htmlContent,
        text,
      });
      logger.log('SMTP success', { messageId: result.messageId });
      return result;
    }

    try {
      const result = await brevoClient.transactionalEmails.sendTransacEmail({
        sender: {
          email: settings.brevo.senderEmail,
          name: settings.brevo.senderName,
        },
        to: [{ email: to }],
        subject,
        htmlContent,
        textContent: text,
      });
      logger.log('Brevo success', { messageId: result.messageId });
      return result;
    } catch (error) {
      // No volcar objetos HTTP que puedan contener headers o cuerpos privados.
      logger.error('Brevo delivery failed', { statusCode: error.statusCode });
      throw error;
    }
  };
}

function stripHtml(html) {
  return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
}

export const sendEmail = createEmailService(config);
