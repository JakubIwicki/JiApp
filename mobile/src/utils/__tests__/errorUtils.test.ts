import { getErrorMessage, getDownloadErrorMessage } from '../errorUtils';

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

describe('getDownloadErrorMessage', () => {
  it('returns server error message for 502 Bad Gateway (yt-dlp failure)', () => {
    const err = {
      isAxiosError: true,
      response: { status: 502, data: { error: 'Failed to download video: Video unavailable' } },
      _serverError: 'Failed to download video: Video unavailable',
    };
    expect(getDownloadErrorMessage(err)).toBe(
      'YouTube download failed: Failed to download video: Video unavailable',
    );
  });

  it('returns generic message for 500 Internal Server Error', () => {
    const err = {
      isAxiosError: true,
      response: { status: 500, data: { error: 'Internal server error' } },
      _serverError: 'Internal server error',
    };
    expect(getDownloadErrorMessage(err)).toBe('Server error — please try again later');
  });

  it('returns connection error for network failures (ERR_NETWORK)', () => {
    const err = {
      isAxiosError: true,
      code: 'ERR_NETWORK',
      response: undefined,
      message: 'Network Error',
    };
    expect(getDownloadErrorMessage(err)).toBe('Connection failed — check your network');
  });

  it('returns connection error for network failures (no response)', () => {
    const err = {
      isAxiosError: true,
      response: undefined,
      code: 'ECONNABORTED',
    };
    expect(getDownloadErrorMessage(err)).toBe('Connection failed — check your network');
  });

  it('returns fallback for generic Error', () => {
    const err = new Error('Something went wrong');
    expect(getDownloadErrorMessage(err)).toBe('Download failed');
  });

  it('returns fallback for non-Error values', () => {
    expect(getDownloadErrorMessage('string error')).toBe('Download failed');
    expect(getDownloadErrorMessage(null)).toBe('Download failed');
    expect(getDownloadErrorMessage(undefined)).toBe('Download failed');
    expect(getDownloadErrorMessage({ code: 500 })).toBe('Download failed');
  });

  it('handles 502 without _serverError gracefully', () => {
    const err = {
      isAxiosError: true,
      response: { status: 502, data: {} },
    };
    expect(getDownloadErrorMessage(err)).toBe('Download failed');
  });

  it('handles 500 with _serverError but returns generic message', () => {
    const err = {
      isAxiosError: true,
      response: { status: 500, data: { error: 'Something broke' } },
      _serverError: 'Something broke',
    };
    expect(getDownloadErrorMessage(err)).toBe('Server error — please try again later');
  });
});
