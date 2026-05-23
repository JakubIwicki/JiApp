import { getErrorMessage } from '../errorUtils';

describe('getErrorMessage', () => {
  it('returns the message from an Error instance', () => {
    const err = new Error('Something went wrong');
    expect(getErrorMessage(err, 'Fallback')).toBe('Something went wrong');
  });

  it('returns the fallback string when error is not an Error instance', () => {
    expect(getErrorMessage('string error', 'Fallback message')).toBe(
      'Fallback message',
    );
  });

  it('returns the fallback string when error is null', () => {
    expect(getErrorMessage(null, 'Null fallback')).toBe('Null fallback');
  });

  it('returns the fallback string when error is undefined', () => {
    expect(getErrorMessage(undefined, 'Undefined fallback')).toBe(
      'Undefined fallback',
    );
  });

  it('returns the fallback string when error is a plain object', () => {
    expect(getErrorMessage({ code: 500 }, 'Object fallback')).toBe(
      'Object fallback',
    );
  });

  it('handles Error with empty message', () => {
    const err = new Error('');
    expect(getErrorMessage(err, 'Fallback')).toBe('');
  });
});
