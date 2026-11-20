export function getHttpErrorMessage(err: any): string | null {
  if (!err) return null;

  if (typeof err === 'string') return err;

  const body = err?.error;

  if (typeof body === 'string') return body;

  const message = body?.message ?? body?.Message ?? body?.title ?? err?.message;

  return typeof message === 'string' && message.trim().length ? message : null;
}
