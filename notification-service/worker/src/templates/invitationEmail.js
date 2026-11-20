export function validateInvitationPayload(payload) {
  const invitationId = payload.InvitationId ?? payload.invitationId;
  const userEmail = payload.UserEmail ?? payload.userEmail ?? payload.email;
  const companyName = payload.CompanyName ?? payload.companyName;
  const areaName = payload.AreaName ?? payload.areaName;
  const plainToken = payload.PlainToken ?? payload.plainToken;

  if (!invitationId) {
    throw new Error('InvitationId is required');
  }

  if (!userEmail) {
    throw new Error('UserEmail is required');
  }

  if (!companyName) {
    throw new Error('CompanyName is required');
  }

  if (!areaName) {
    throw new Error('AreaName is required');
  }

  if (!plainToken) {
    throw new Error('PlainToken is required');
  }

  return {
    invitationId,
    userEmail,
    companyId: payload.CompanyId ?? payload.companyId,
    companyName,
    areaName,
    plainToken,
  };
}

export function isInvitationPayload(payload) {
  return (
    (payload.InvitationId != null || payload.invitationId != null) &&
    (payload.PlainToken != null || payload.plainToken != null)
  );
}

export function buildInvitationSubject() {
  return 'Invitacion a Workly';
}

export function buildInvitationLink(baseUrl, invitationId, plainToken) {
  const trimmedBaseUrl = baseUrl.replace(/\/$/, '');
  const separator = trimmedBaseUrl.includes('?') ? '&' : '?';
  return (
    `${trimmedBaseUrl}${separator}` +
    `invitationId=${encodeURIComponent(invitationId)}&` +
    `token=${encodeURIComponent(plainToken)}`
  );
}

export function buildInvitationText({ companyName, areaName, invitationLink }) {
  return `Fuiste invitado a unirte a la empresa ${companyName} al Area ${areaName} en Workly.

Para completar tu registro, abri este enlace:
${invitationLink}

Este enlace es de un solo uso.`;
}

export function buildInvitationHtml({ companyName, areaName, invitationLink }) {
  return `
    <html>
      <body style="font-family: Arial, sans-serif; color: #222;">
        <p>Fuiste invitado a unirte a la empresa ${escapeHtml(companyName)} al Area ${escapeHtml(areaName)} en Workly.</p>
        <p>Para completar tu registro, abri este enlace:<br>
        <a href="${escapeHtml(invitationLink)}">${escapeHtml(invitationLink)}</a></p>
        <p>Este enlace es de un solo uso.</p>
      </body>
    </html>
  `.trim();
}

function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}
