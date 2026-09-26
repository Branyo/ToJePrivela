import { formatAnswer, parseCategoryIds } from './play';

describe('parseCategoryIds', () => {
  it('reads a comma separated list', () => {
    expect(parseCategoryIds('1, 2,3')).toEqual([1, 2, 3]);
  });

  it('drops junk and treats a missing value as every category', () => {
    expect(parseCategoryIds('1,abc,-4,0,2.5,7')).toEqual([1, 7]);
    expect(parseCategoryIds(undefined)).toEqual([]);
    expect(parseCategoryIds('')).toEqual([]);
  });
});

describe('formatAnswer', () => {
  it('formats numeric answers the Slovak way by default', () => {
    expect(formatAnswer('1234567')).toBe('1 234 567');
    expect(formatAnswer('3.5')).toBe('3,5');
  });

  it('formats numeric answers for the given locale', () => {
    expect(formatAnswer('1234567', 'en-GB')).toBe('1,234,567');
    expect(formatAnswer('3.5', 'en-GB')).toBe('3.5');
  });

  it('leaves non-numeric text alone', () => {
    expect(formatAnswer('about 5')).toBe('about 5');
  });
});
