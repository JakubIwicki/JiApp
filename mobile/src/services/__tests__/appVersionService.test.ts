import { fetchAppVersionInfo } from '../appVersionService';

const VALID_INFO = {
  minVersionCode: 65,
  downloadUrl: 'https://example.com/JiAppMobile.apk',
};

const mockFetch = jest.fn();

beforeEach(() => {
  jest.clearAllMocks();
  global.fetch = mockFetch as unknown as typeof fetch;
});

describe('fetchAppVersionInfo', () => {
  it('parses a valid version response via the schema', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => VALID_INFO,
    });

    const result = await fetchAppVersionInfo();

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/app/version'),
      { signal: undefined },
    );
    expect(result).toEqual(VALID_INFO);
  });

  it('throws on a non-2xx response', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 500,
      json: async () => ({}),
    });

    await expect(fetchAppVersionInfo()).rejects.toThrow('Version check failed');
  });

  it('rejects when the network request fails', async () => {
    mockFetch.mockRejectedValueOnce(new Error('Network error'));

    await expect(fetchAppVersionInfo()).rejects.toThrow('Network error');
  });

  it('rejects a malformed payload', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({ minVersionCode: '65', downloadUrl: 123 }),
    });

    await expect(fetchAppVersionInfo()).rejects.toThrow();
  });

  it('passes the abort signal through to fetch', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => VALID_INFO,
    });
    const controller = new AbortController();

    await fetchAppVersionInfo(controller.signal);

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/app/version'),
      { signal: controller.signal },
    );
  });
});
