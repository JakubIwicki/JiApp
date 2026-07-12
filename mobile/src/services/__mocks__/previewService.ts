import { createMockFn } from '../../test/createMockFn';

export const getPreviewUrl = createMockFn(
  (videoId: string): string => `https://example.com/yt/preview/${videoId}`,
);

export const getPreviewHeaders = createMockFn(
  async (): Promise<Record<string, string>> => ({
    Authorization: 'Bearer mock-token',
  }),
);

export function reset(): void {
  if (typeof jest !== 'undefined') {
    getPreviewUrl.mockClear();
    getPreviewHeaders.mockClear();
  }
}
