import { HttpErrorResponse } from '@angular/common/http';
import { toProblem } from './problem';

describe('toProblem', () => {
  it('translates by code and keeps the backend text as fallback', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { status: 400, code: 'Request.Invalid', detail: 'Player name should have from 2 to 50 characters.' },
    });

    expect(toProblem(error)).toEqual({
      status: 400,
      code: 'Request.Invalid',
      message: { key: 'errors.api.Request.Invalid', fallback: 'Player name should have from 2 to 50 characters.' },
    });
  });

  it('shows the backend text when there is no code', () => {
    const error = new HttpErrorResponse({ status: 500, error: { title: 'Unexpected error' } });

    expect(toProblem(error).message).toEqual({ text: 'Unexpected error' });
  });

  it('says how long to wait when a rate limit refuses the request', () => {
    const error = new HttpErrorResponse({
      status: 429,
      error: { code: 'RateLimit.Exceeded', detail: 'Too many attempts.', retryAfterSeconds: 42 },
    });

    expect(toProblem(error).message).toEqual({
      key: 'errors.api.RateLimit.Exceeded',
      params: { seconds: 42 },
      fallback: 'Too many attempts.',
    });
  });

  it('still explains a rate limit that does not say how long to wait', () => {
    const error = new HttpErrorResponse({ status: 429, error: { code: 'RateLimit.Exceeded' } });

    expect(toProblem(error).message).toEqual({ key: 'errors.rateLimited', fallback: undefined });
  });

  it('reports an unreachable server', () => {
    expect(toProblem(new HttpErrorResponse({ status: 0 })).message).toEqual({ key: 'errors.unreachable' });
  });
});
