import { createMockFn } from '../../test/createMockFn';

// ── Internal state ─────────────────────────────────────────────────────────

let _previewUrl: string | null = null;
let _previewHeaders: Record<string, string> = {
  Authorization: 'Bearer mock-token',
};

// ── Mock functions ─────────────────────────────────────────────────────────

export const getPreviewUrl = createMockFn((videoId: string): string => {
  if (_previewUrl) return _previewUrl;
  return `https://example.com/yt/preview/${videoId}`;
});

export const getPreviewHeaders = createMockFn(
  async (): Promise<Record<string, string>> => _previewHeaders,
);

// ── Fluent scenario builders (.withX()) ────────────────────────────────────

export function withPreviewUrl(url: string): string {
  _previewUrl = url;
  return url;
}

export function withPreviewHeaders(
  headers: Record<string, string>,
): Record<string, string> {
  _previewHeaders = headers;
  return headers;
}

// ── Reset ──────────────────────────────────────────────────────────────────

export function reset(): void {
  _previewUrl = null;
  _previewHeaders = { Authorization: 'Bearer mock-token' };

  if (typeof jest !== 'undefined') {
    getPreviewUrl.mockClear();
    getPreviewHeaders.mockClear();
  }
}
