import * as brevo from '@getbrevo/brevo';
import nodemailer from 'nodemailer';
import { config } from '../config.js';

const apiInstance = new brevo.TransactionalEmailsApi();
if (config.brevo.deliveryMode === 'brevo') {
  apiInstance.setApiKey(
    brevo.TransactionalEmailsApiApiKeys.apiKey,
    config.brevo.apiKey
  );
}

const smtpTransport =
  config.brevo.deliveryMode === 'smtp'
    ? nodemailer.createTransport({
        host: config.smtp.host,
        port: config.smtp.port,
        secure: config.smtp.port === 465,
        requireTLS: config.smtp.useSsl,
        auth: {
          user: config.smtp.username,
          pass: config.smtp.password,
        },
      })
    : null;

export async function sendEmail({ to, subject, htmlContent, textContent }) {
  if (config.brevo.deliveryMode === 'log') {
    console.log('Email delivery skipped in log mode', {
      to,
      subject,
      textContent: textContent ?? stripHtml(htmlContent),
    });
    return { skipped: true, mode: 'log' };
  }

  if (config.brevo.deliveryMode === 'smtp') {
    const result = await smtpTransport.sendMail({
      from: {
        address: config.smtp.senderEmail,
        name: config.smtp.senderName,
      },
      to,
      subject,
      html: htmlContent,
      text: textContent ?? stripHtml(htmlContent),
    });

    console.log('SMTP success', {
      messageId: result.messageId,
      accepted: result.accepted,
      rejected: result.rejected,
    });

    return result;
  }

  try {	
  const sendSmtpEmail = new brevo.SendSmtpEmail();
  sendSmtpEmail.sender = {
    email: config.brevo.senderEmail,
    name: config.brevo.senderName,
  };
  sendSmtpEmail.to = [{ email: to }];
  sendSmtpEmail.subject = subject;
  sendSmtpEmail.htmlContent = htmlContent;
  sendSmtpEmail.textContent = textContent ?? stripHtml(htmlContent);

  const result = await apiInstance.sendTransacEmail(sendSmtpEmail);

  console.log('Brevo success', result);

    return result;
  } catch (error) {
    console.error('Brevo error details', {
      message: error.message,
      status: error.status,
      code: error.code,
      response: error.response,
      body: error.body,
    });

    throw error;
  }
}

function stripHtml(html) {
  return html.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
}
