import { HttpErrorResponse } from '@angular/common/http';
import { Message } from '../i18n/language';

/** The backend answers errors with ProblemDetails carrying a machine-readable `code`. */
export interface Problem {
  status: number;
  code: string | null;
  /** Translated by `code` (`errors.api.<code>`); unknown codes fall back to the backend's own text. */
  message: Message;
}

export function toProblem(error: unknown): Problem {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, code: null, message: { key: 'errors.generic' } };
  }

  if (error.status === 0) {
    return { status: 0, code: null, message: { key: 'errors.unreachable' } };
  }

  const body = error.error as { detail?: string; title?: string; code?: string; retryAfterSeconds?: number } | null;
  const code = body?.code ?? null;
  const text = body?.detail ?? body?.title;

  return {
    status: error.status,
    code,
    message: code ? apiMessage(code, text, body) : text ? { text } : { key: 'errors.generic' },
  };
}

function apiMessage(code: string, fallback: string | undefined, body: { retryAfterSeconds?: number } | null): Message {
  // A refused request (429) says how long to wait; without that number the text would show a bare placeholder.
  if (code === 'RateLimit.Exceeded') {
    const seconds = body?.retryAfterSeconds;
    return typeof seconds === 'number'
      ? { key: 'errors.api.RateLimit.Exceeded', params: { seconds }, fallback }
      : { key: 'errors.rateLimited', fallback };
  }
  return { key: `errors.api.${code}`, fallback };
}
