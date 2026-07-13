import {
  getErrorMessage,
  getFriendlyErrorMessage,
  getDownloadErrorMessage,
} from '../errorUtils';

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

describe('getFriendlyErrorMessage', () => {
  it('returns network message for axios error with no response', () => {
    const err = {
      isAxiosError: true,
      response: undefined,
      code: 'ERR_NETWORK',
    };
    expect(getFriendlyErrorMessage(err, 'Fallback')).toBe(
      'Connection failed — check your network',
    );
  });

  it('returns network message for axios error with ECONNABORTED', () => {
    const err = {
      isAxiosError: true,
      response: undefined,
      code: 'ECONNABORTED',
    };
    expect(getFriendlyErrorMessage(err, 'Fallback')).toBe(
      'Connection failed — check your network',
    );
  });

  it('returns session expired for 401', () => {
    const err = {
      isAxiosError: true,
      response: { status: 401, data: {} },
    };
    expect(getFriendlyErrorMessage(err, 'Fallback')).toBe(
      'Your session expired — please sign in again',
    );
  });

  it('returns permission denied for 403', () => {
    const err = {
      isAxiosError: true,
      response: { status: 403, data: {} },
    };
    expect(getFriendlyErrorMessage(err, 'Fallback')).toBe(
      "You don't have permission to do that",
    );
  });

  it('returns server error for 500', () => {
    const err = {
      isAxiosError: true,
      response: { status: 500, data: { error: 'Internal error' } },
    };
    expect(getFriendlyErrorMessage(err, 'Fallback')).toBe(
      'Server error — please try again later',
    );
  });

  it('returns server error for 503', () => {
    const err = {
      isAxiosError: true,
      response: { status: 503, data: {} },
    };
    expect(getFriendlyErrorMessage(err, 'Fallback')).toBe(
      'Server error — please try again later',
    );
  });

  it('returns _serverError for 4xx with server message', () => {
    const err = {
      isAxiosError: true,
      response: { status: 400, data: { error: 'Username already taken' } },
      _serverError: 'Username already taken',
    };
    expect(getFriendlyErrorMessage(err, 'Fallback')).toBe(
      'Username already taken',
    );
  });

  it('returns _serverError for 422 with server message', () => {
    const err = {
      isAxiosError: true,
      response: { status: 422, data: { error: 'Cannot delete own account' } },
      _serverError: 'Cannot delete own account',
    };
    expect(getFriendlyErrorMessage(err, 'Fallback')).toBe(
      'Cannot delete own account',
    );
  });

  it('returns fallback for plain Error (non-axios)', () => {
    const err = new Error('Raw technical string');
    expect(getFriendlyErrorMessage(err, 'Default fallback')).toBe(
      'Default fallback',
    );
  });

  it('returns fallback for non-Error values', () => {
    expect(getFriendlyErrorMessage(null, 'Null fallback')).toBe(
      'Null fallback',
    );
    expect(getFriendlyErrorMessage(undefined, 'Undefined fallback')).toBe(
      'Undefined fallback',
    );
    expect(getFriendlyErrorMessage('string error', 'String fallback')).toBe(
      'String fallback',
    );
  });

  it('returns fallback for axios 404 without _serverError', () => {
    const err = {
      isAxiosError: true,
      response: { status: 404, data: {} },
    };
    expect(getFriendlyErrorMessage(err, 'Not found fallback')).toBe(
      'Not found fallback',
    );
  });

  it('returns _serverError for axios 404 with server error string', () => {
    const err = {
      isAxiosError: true,
      response: { status: 404, data: { error: 'User not found' } },
      _serverError: 'User not found',
    };
    expect(getFriendlyErrorMessage(err, 'Fallback')).toBe('User not found');
  });
});

describe('getDownloadErrorMessage', () => {
  it('returns server error message for 502 Bad Gateway (yt-dlp failure)', () => {
    const err = {
      isAxiosError: true,
      response: {
        status: 502,
        data: { error: 'Failed to download video: Video unavailable' },
      },
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
    expect(getDownloadErrorMessage(err)).toBe(
      'Server error — please try again later',
    );
  });

  it('returns connection error for network failures (ERR_NETWORK)', () => {
    const err = {
      isAxiosError: true,
      code: 'ERR_NETWORK',
      response: undefined,
      message: 'Network Error',
    };
    expect(getDownloadErrorMessage(err)).toBe(
      'Connection failed — check your network',
    );
  });

  it('returns connection error for network failures (no response)', () => {
    const err = {
      isAxiosError: true,
      response: undefined,
      code: 'ECONNABORTED',
    };
    expect(getDownloadErrorMessage(err)).toBe(
      'Connection failed — check your network',
    );
  });

  it('returns the error message for generic Error instances', () => {
    const err = new Error('Something went wrong');
    expect(getDownloadErrorMessage(err)).toBe('Something went wrong');
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
    expect(getDownloadErrorMessage(err)).toBe(
      'Server error — please try again later',
    );
  });
});
