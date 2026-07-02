import {
  VideoItemSchema,
  SearchHistoryItemSchema,
  DownloadHistoryItemSchema,
  LoginApiRawSchema,
  RefreshResponseSchema,
  MeApiRawSchema,
  UpdateProfileApiRawSchema,
  SearchResponseSchema,
  SearchHistoryResponseSchema,
  HistoryResponseSchema,
  DownloadResponseSchema,
  DownloadHistoryResponseSchema,
} from '../schemas';

// ── VideoItemSchema ────────────────────────────────────────────────────────

describe('VideoItemSchema', () => {
  const valid = {
    videoId: 'dQw4w9WgXcQ',
    title: 'Rick Astley - Never Gonna Give You Up',
    description: 'Classic music video',
    imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
    videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    channelTitle: 'Rick Astley',
  };

  it('parses a valid VideoItem', () => {
    expect(() => VideoItemSchema.parse(valid)).not.toThrow();
  });

  it('fails when videoId is a number instead of string', () => {
    const bad = { ...valid, videoId: 42 };
    expect(VideoItemSchema.safeParse(bad).success).toBe(false);
  });
});

// ── SearchHistoryItemSchema ────────────────────────────────────────────────

describe('SearchHistoryItemSchema', () => {
  const valid = {
    id: 1,
    searchText: 'never gonna give you up',
    searchedAt: '2026-01-01T00:00:00.000Z',
  };

  it('parses a valid SearchHistoryItem', () => {
    expect(() => SearchHistoryItemSchema.parse(valid)).not.toThrow();
  });

  it('fails when id is a string instead of number', () => {
    const bad = { ...valid, id: 'not-a-number' };
    expect(SearchHistoryItemSchema.safeParse(bad).success).toBe(false);
  });
});

// ── DownloadHistoryItemSchema ──────────────────────────────────────────────

describe('DownloadHistoryItemSchema', () => {
  const valid = {
    id: 100,
    videoTitle: 'Rick Astley - Never Gonna Give You Up',
    videoDescription: 'Classic music video',
    videoId: 'dQw4w9WgXcQ',
    videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
    imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
    downloadedAt: '2026-01-01T00:00:00.000Z',
  };

  it('parses a valid DownloadHistoryItem', () => {
    expect(() => DownloadHistoryItemSchema.parse(valid)).not.toThrow();
  });

  it('fails when id is a string instead of number', () => {
    const bad = { ...valid, id: 'not-a-number' };
    expect(DownloadHistoryItemSchema.safeParse(bad).success).toBe(false);
  });
});

// ── LoginApiRawSchema ──────────────────────────────────────────────────────

describe('LoginApiRawSchema', () => {
  const valid = {
    userId: 1,
    displayName: 'John Doe',
    accessToken: 'jwt-token-123',
    refreshToken: 'refresh-token-456',
    expiresIn: 3600,
    roles: ['User'],
    permissions: ['ytdownloader.access', 'scheduler.access'],
  };

  it('parses a valid LoginApiRaw', () => {
    expect(() => LoginApiRawSchema.parse(valid)).not.toThrow();
  });

  it('parses without optional roles and permissions fields', () => {
    const { roles: _, permissions: __, ...withoutOpt } = valid;
    expect(() => LoginApiRawSchema.parse(withoutOpt)).not.toThrow();
  });

  it('parses with displayName: null', () => {
    const withNullName = { ...valid, displayName: null };
    expect(() => LoginApiRawSchema.parse(withNullName)).not.toThrow();
  });

  it('fails when userId is a string instead of number', () => {
    const bad = { ...valid, userId: 'not-a-number' };
    expect(LoginApiRawSchema.safeParse(bad).success).toBe(false);
  });
});

// ── MeApiRawSchema ─────────────────────────────────────────────────────────

describe('MeApiRawSchema', () => {
  const valid = {
    id: 1,
    displayName: 'John Doe',
    username: 'johndoe',
    email: 'john@example.com',
    roles: ['User'],
    permissions: ['ytdownloader.access', 'scheduler.access'],
  };

  it('parses a valid MeApiRaw', () => {
    expect(() => MeApiRawSchema.parse(valid)).not.toThrow();
  });

  it('parses with only required id field', () => {
    expect(() => MeApiRawSchema.parse({ id: 1 })).not.toThrow();
  });

  it('fails when id is a string instead of number', () => {
    const bad = { ...valid, id: 'not-a-number' };
    expect(MeApiRawSchema.safeParse(bad).success).toBe(false);
  });
});

// ── UpdateProfileApiRawSchema ──────────────────────────────────────────────

describe('UpdateProfileApiRawSchema', () => {
  const valid = {
    id: 1,
    displayName: 'John Doe',
    username: 'johndoe',
    email: 'john@example.com',
  };

  it('parses a valid UpdateProfileApiRaw', () => {
    expect(() => UpdateProfileApiRawSchema.parse(valid)).not.toThrow();
  });

  it('parses with only required id field', () => {
    expect(() => UpdateProfileApiRawSchema.parse({ id: 1 })).not.toThrow();
  });

  it('fails when id is a string instead of number', () => {
    const bad = { ...valid, id: 'not-a-number' };
    expect(UpdateProfileApiRawSchema.safeParse(bad).success).toBe(false);
  });
});

// ── SearchResponseSchema ───────────────────────────────────────────────────

describe('SearchResponseSchema', () => {
  const valid = {
    results: [
      {
        videoId: 'dQw4w9WgXcQ',
        title: 'Rick Astley - Never Gonna Give You Up',
        description: 'Classic music video',
        imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
        videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
        channelTitle: 'Rick Astley',
      },
    ],
    hasMore: false,
  };

  it('parses a valid SearchResponse', () => {
    expect(() => SearchResponseSchema.parse(valid)).not.toThrow();
  });

  it('fails when results contains an item with a non-string videoId', () => {
    const bad = {
      results: [{ ...valid.results[0], videoId: 42 }],
    };
    expect(SearchResponseSchema.safeParse(bad).success).toBe(false);
  });
});

// ── SearchHistoryResponseSchema ────────────────────────────────────────────

describe('SearchHistoryResponseSchema', () => {
  const valid = {
    items: [
      {
        id: 1,
        searchText: 'never gonna give you up',
        searchedAt: '2026-01-01T00:00:00.000Z',
      },
    ],
  };

  it('parses a valid search history response', () => {
    expect(() => SearchHistoryResponseSchema.parse(valid)).not.toThrow();
  });

  it('fails when items is not an array', () => {
    const bad = { items: 'not-an-array' };
    expect(SearchHistoryResponseSchema.safeParse(bad).success).toBe(false);
  });
});

// ── HistoryResponseSchema ──────────────────────────────────────────────────

describe('HistoryResponseSchema', () => {
  const valid = {
    searches: [
      {
        id: 1,
        searchText: 'never gonna give you up',
        searchedAt: '2026-01-01T00:00:00.000Z',
      },
    ],
    downloads: [
      {
        id: 100,
        videoTitle: 'Rick Astley - Never Gonna Give You Up',
        videoDescription: 'Classic music video',
        videoId: 'dQw4w9WgXcQ',
        videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
        imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
        downloadedAt: '2026-01-01T00:00:00.000Z',
      },
    ],
  };

  it('parses a valid HistoryResponse', () => {
    expect(() => HistoryResponseSchema.parse(valid)).not.toThrow();
  });

  it('fails when downloads contains an item with a non-number id', () => {
    const bad = {
      ...valid,
      downloads: [{ ...valid.downloads[0], id: 'not-a-number' }],
    };
    expect(HistoryResponseSchema.safeParse(bad).success).toBe(false);
  });
});

// ── DownloadResponseSchema ─────────────────────────────────────────────────

describe('DownloadResponseSchema', () => {
  const valid = { downloadUrl: 'https://example.com/downloads/song.mp3' };

  it('parses a valid DownloadResponse', () => {
    expect(() => DownloadResponseSchema.parse(valid)).not.toThrow();
  });

  it('fails when downloadUrl is missing', () => {
    const bad = {};
    expect(DownloadResponseSchema.safeParse(bad).success).toBe(false);
  });
});

// ── RefreshResponseSchema ───────────────────────────────────────────────────

describe('RefreshResponseSchema', () => {
  const valid = {
    accessToken: 'new-access-token',
    refreshToken: 'new-refresh-token',
    expiresIn: 3600,
  };

  it('parses a valid RefreshResponse', () => {
    expect(() => RefreshResponseSchema.parse(valid)).not.toThrow();
  });

  it('fails when accessToken is missing', () => {
    const bad = { refreshToken: 'rt', expiresIn: 3600 };
    expect(RefreshResponseSchema.safeParse(bad).success).toBe(false);
  });

  it('fails when refreshToken is missing', () => {
    const bad = { accessToken: 'at', expiresIn: 3600 };
    expect(RefreshResponseSchema.safeParse(bad).success).toBe(false);
  });

  it('fails when expiresIn is not a number', () => {
    const bad = { ...valid, expiresIn: '3600' };
    expect(RefreshResponseSchema.safeParse(bad).success).toBe(false);
  });
});

// ── DownloadHistoryResponseSchema ──────────────────────────────────────────

describe('DownloadHistoryResponseSchema', () => {
  const valid = {
    items: [
      {
        id: 100,
        videoTitle: 'Rick Astley - Never Gonna Give You Up',
        videoDescription: 'Classic music video',
        videoId: 'dQw4w9WgXcQ',
        videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
        imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
        downloadedAt: '2026-01-01T00:00:00.000Z',
      },
    ],
  };

  it('parses a valid download history response', () => {
    expect(() => DownloadHistoryResponseSchema.parse(valid)).not.toThrow();
  });

  it('fails when items contains an item with a non-number id', () => {
    const bad = {
      items: [{ ...valid.items[0], id: 'not-a-number' }],
    };
    expect(DownloadHistoryResponseSchema.safeParse(bad).success).toBe(false);
  });
});
