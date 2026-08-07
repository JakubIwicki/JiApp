import { isUpdateRequired } from '../version';

describe('isUpdateRequired', () => {
  it('returns true when the installed version is below the minimum', () => {
    expect(isUpdateRequired(64, 65)).toBe(true);
  });

  it('returns false when the installed version equals the minimum', () => {
    expect(isUpdateRequired(65, 65)).toBe(false);
  });

  it('returns false when the installed version is above the minimum', () => {
    expect(isUpdateRequired(66, 65)).toBe(false);
  });

  it('returns false when the server reports a zero minimum (gate dormant)', () => {
    expect(isUpdateRequired(65, 0)).toBe(false);
  });
});
