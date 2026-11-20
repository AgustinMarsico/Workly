const ACTION_LABELS = {
  created: 'Reservation created',
  updated: 'Reservation updated',
  cancelled: 'Reservation cancelled',
};

export function validateReservationPayload(payload) {
  const reservationRaw = payload.reservation ?? payload.Reservation;
  if (!reservationRaw) {
    throw new Error('Reservation data is required');
  }

  const userRaw = payload.user ?? payload.User;
  if (!userRaw) {
    throw new Error('User associated with the reservation is required');
  }

  const reservation = normalizeReservation(reservationRaw);
  const user = normalizeUser(userRaw, payload);
  const action = normalizeAction(payload.action ?? payload.Action);
  const actionAt =
    payload.actionAt ?? payload.action_at ?? payload.ActionAt;

  if (!reservation.id) {
    throw new Error('Reservation ID is required');
  }

  if (!reservation.spaceName) {
    throw new Error('Reserved space is missing');
  }

  if (!reservation.startAt || !reservation.endAt) {
    throw new Error('Reservation start and end date/time are required');
  }

  if (!user.id) {
    throw new Error('User ID is required');
  }

  if (!user.name) {
    throw new Error('User name is required');
  }

  const emails = normalizeEmails(payload);
  if (emails.length === 0) {
    throw new Error('At least one recipient email is required in emails');
  }

  if (!action) {
    throw new Error('Action must be one of: created, updated, cancelled');
  }

  if (!actionAt) {
    throw new Error('Action date and time are required');
  }

  const changes = normalizeChanges(payload.changes ?? payload.Changes);
  if (action === 'updated' && changes.length === 0) {
    throw new Error('Change details are required for updated reservations');
  }

  return { reservation, user, action, actionAt, changes, emails };
}

export function buildReservationSubject({ reservation, action }) {
  const label = ACTION_LABELS[action] ?? 'Reservation notification';
  return `${label} - ${reservation.spaceName}`;
}

export function buildReservationHtml({
  reservation,
  user,
  action,
  actionAt,
  changes,
}) {
  const actionLabel = ACTION_LABELS[action] ?? action;
  const changesHtml = buildChangesSectionHtml(changes, action);

  return `
    <html>
      <body style="font-family: Arial, sans-serif; color: #222;">
        <h3>Reservation details</h3>
        <ul>
          <li><strong>Space:</strong> ${escapeHtml(reservation.spaceName)}</li>
          <li><strong>Start:</strong> ${escapeHtml(formatDateTime(reservation.startAt))}</li>
          <li><strong>End:</strong> ${escapeHtml(formatDateTime(reservation.endAt))}</li>
        </ul>

        <h3>Associated user</h3>
        <ul>
          <li><strong>Name:</strong> ${escapeHtml(user.name ?? 'N/A')}</li>
          <li><strong>Email:</strong> ${escapeHtml(user.email ?? 'N/A')}</li>
        </ul>

        <h3>Action</h3>
        <ul>
          <li><strong>Type:</strong> ${escapeHtml(actionLabel)}</li>
          <li><strong>Performed at:</strong> ${escapeHtml(formatDateTime(actionAt))}</li>
          ${changesHtml}
        </ul>
      </body>
    </html>
  `.trim();
}

export function buildReservationText({
  reservation,
  user,
  action,
  actionAt,
  changes,
}) {
  const lines = [
    'Reservation details:',
    `- Space: ${reservation.spaceName}`,
    `- Start: ${formatDateTime(reservation.startAt)}`,
    `- End: ${formatDateTime(reservation.endAt)}`,
    '',
    'Associated user:',
    `- Name: ${user.name ?? 'N/A'}`,
    `- Email: ${user.email ?? 'N/A'}`,
    '',
    'Action:',
    `- Type: ${ACTION_LABELS[action] ?? action}`,
    `- Performed at: ${formatDateTime(actionAt)}`,
  ];

  lines.push(...buildChangesSectionText(changes, action));

  return lines.join('\n');
}

function buildChangesSectionHtml(changes, action) {
  if (action !== 'updated' || changes.length === 0) {
    return '';
  }

  const items = changes
    .map(
      (change) => `
            <li>${escapeHtml(formatFieldLabel(change.field))}: ${escapeHtml(formatChangeValue(change.field, change.previous))} → ${escapeHtml(formatChangeValue(change.field, change.current))}</li>`,
    )
    .join('');

  return `
          <li><strong>Changes:</strong>
            <ul>${items}
            </ul>
          </li>`;
}

function buildChangesSectionText(changes, action) {
  if (action !== 'updated' || changes.length === 0) {
    return [];
  }

  return [
    '- Changes:',
    ...changes.map(
      (change) =>
        `  - ${formatFieldLabel(change.field)}: ${formatChangeValue(change.field, change.previous)} -> ${formatChangeValue(change.field, change.current)}`,
    ),
  ];
}

function normalizeReservation(reservation) {
  const space = reservation.space ?? reservation.Space;

  return {
    id:
      reservation.id ??
      reservation.Id ??
      reservation.reservationId ??
      reservation.reservation_id,
    spaceName:
      space?.name ??
      space?.Name ??
      reservation.spaceName ??
      reservation.SpaceName ??
      reservation.space_name ??
      (typeof space === 'string' ? space : null),
    startAt:
      reservation.startAt ??
      reservation.StartAt ??
      reservation.start_at ??
      reservation.startDateTime ??
      reservation.start_date_time,
    endAt:
      reservation.endAt ??
      reservation.EndAt ??
      reservation.end_at ??
      reservation.endDateTime ??
      reservation.end_date_time,
  };
}

function normalizeEmails(payload) {
  const raw =
    payload.emails ??
    payload.Emails ??
    payload.recipientEmails ??
    payload.recipient_emails;

  if (Array.isArray(raw)) {
    return [...new Set(raw.map((email) => String(email).trim().toLowerCase()).filter(Boolean))];
  }

  return [];
}

function normalizeUser(user, payload) {
  return {
    id:
      user.id ??
      user.Id ??
      user.userId ??
      user.UserId ??
      payload.userId ??
      payload.UserId ??
      payload.user_id,
    name:
      user.name ??
      user.Name ??
      user.fullName ??
      user.FullName ??
      user.full_name ??
      payload.name ??
      payload.Name,
    email:
      user.email ??
      user.Email ??
      payload.email ??
      payload.Email ??
      payload.to,
  };
}

function normalizeAction(action) {
  const value = String(action ?? '').toLowerCase();

  if (['created', 'create', 'creation'].includes(value)) {
    return 'created';
  }

  if (['updated', 'update', 'modified', 'modification', 'modify'].includes(value)) {
    return 'updated';
  }

  if (['cancelled', 'canceled', 'cancel', 'cancellation'].includes(value)) {
    return 'cancelled';
  }

  return null;
}

function normalizeChanges(changes) {
  if (!Array.isArray(changes)) {
    return [];
  }

  return changes
    .map((change) => ({
      field: change.field ?? change.Field ?? change.name ?? change.Name,
      previous:
        change.previous ??
        change.Previous ??
        change.oldValue ??
        change.OldValue ??
        change.old_value ??
        change.from ??
        change.From,
      current:
        change.current ??
        change.Current ??
        change.newValue ??
        change.NewValue ??
        change.new_value ??
        change.to ??
        change.To,
    }))
    .filter((change) => change.field);
}

function formatFieldLabel(field) {
  const labels = {
    startAt: 'Start date/time',
    start_at: 'Start date/time',
    endAt: 'End date/time',
    end_at: 'End date/time',
    space: 'Space',
    spaceName: 'Space',
    space_name: 'Space',
  };

  return labels[field] ?? field;
}

function formatChangeValue(field, value) {
  if (value == null) {
    return 'N/A';
  }

  if (
    ['startAt', 'start_at', 'endAt', 'end_at', 'actionAt', 'action_at'].includes(
      field,
    )
  ) {
    return formatDateTime(value);
  }

  return String(value);
}

function formatDateTime(value) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return String(value);
  }

  return date.toLocaleString('en-US', {
    dateStyle: 'medium',
    timeStyle: 'short',
  });
}

function escapeHtml(value) {
  return String(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}
