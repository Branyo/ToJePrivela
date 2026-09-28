import { compareNames, isLanguage, pluralKey } from './language';

describe('pluralKey', () => {
  it('picks the Slovak plural form', () => {
    const forms = [0, 1, 2, 4, 5, 12].map((n) => pluralKey('common.cards', n, 'sk-SK'));

    expect(forms).toEqual([
      'common.cards.other',
      'common.cards.one',
      'common.cards.few',
      'common.cards.few',
      'common.cards.other',
      'common.cards.other',
    ]);
  });

  it('picks the English plural form', () => {
    expect([0, 1, 2].map((n) => pluralKey('common.cards', n, 'en-GB'))).toEqual([
      'common.cards.other',
      'common.cards.one',
      'common.cards.other',
    ]);
  });
});

describe('compareNames', () => {
  it('orders Slovak letters after their base letter', () => {
    expect(['Zuzana', 'Čaba', 'Cyril'].sort((a, b) => compareNames(a, b, 'sk-SK'))).toEqual(['Cyril', 'Čaba', 'Zuzana']);
  });
});

describe('isLanguage', () => {
  it('accepts only supported languages', () => {
    expect(['sk', 'en', 'de', null].map(isLanguage)).toEqual([true, true, false, false]);
  });
});
