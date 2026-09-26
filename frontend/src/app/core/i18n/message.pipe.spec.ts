import { TestBed } from '@angular/core/testing';
import { TranslateService, provideTranslateService } from '@ngx-translate/core';
import { MessagePipe } from './message.pipe';

describe('MessagePipe', () => {
  let pipe: MessagePipe;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideTranslateService(), MessagePipe] });
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('sk', { errors: { generic: 'Chyba', named: 'Chyba: {{name}}' } });
    translate.use('sk');
    pipe = TestBed.inject(MessagePipe);
  });

  it('translates the key with its parameters', () => {
    expect(pipe.transform({ key: 'errors.named', params: { name: 'X' } })).toBe('Chyba: X');
  });

  it('shows the fallback when the key has no translation', () => {
    expect(pipe.transform({ key: 'errors.api.Unknown.Code', fallback: 'Backend text' })).toBe('Backend text');
  });

  it('shows plain text as it is', () => {
    expect(pipe.transform({ text: 'As is.' })).toBe('As is.');
  });

  it('shows nothing for no message', () => {
    expect(pipe.transform(null)).toBe('');
  });
});
