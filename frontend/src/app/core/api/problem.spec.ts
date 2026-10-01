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

  it('reports an unreachable server', () => {
    expect(toProblem(new HttpErrorResponse({ status: 0 })).message).toEqual({ key: 'errors.unreachable' });
  });
});
