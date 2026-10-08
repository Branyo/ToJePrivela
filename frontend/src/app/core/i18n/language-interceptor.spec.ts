import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { LanguageCode, LanguageService } from './language';
import { languageInterceptor } from './language-interceptor';

describe('languageInterceptor', () => {
  const language = signal<LanguageCode>('sk');
  let http: HttpTestingController;
  let client: HttpClient;

  beforeEach(() => {
    language.set('sk');
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([languageInterceptor])),
        provideHttpClientTesting(),
        { provide: LanguageService, useValue: { language } },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
  });

  afterEach(() => http.verify());

  async function send(url: string): Promise<string | null> {
    const response = firstValueFrom(client.get(url));
    const request = http.expectOne(url);
    request.flush({});
    await response;
    return request.request.headers.get('Accept-Language');
  }

  it('asks the API for the shown language, following a switch', async () => {
    expect(await send('/api/question-categories')).toBe('sk');

    language.set('en');

    expect(await send('/api/questions/1')).toBe('en');
  });

  it('leaves other requests alone', async () => {
    expect(await send('/i18n/en.json')).toBeNull();
  });
});
