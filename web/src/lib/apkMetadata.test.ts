import { describe, it, expect, vi } from "vitest";
import { fetchApkMetadata } from "./apkMetadata";

const VALID_PAYLOAD = {
  version: "1.2.3",
  versionCode: 42,
  sizeBytes: 98_765_432,
  releaseDate: "2026-06-24",
  sha256: "abc123def456",
};

function stubFetch(
  response: Partial<{ ok: boolean; status: number; json: unknown }>,
) {
  vi.stubGlobal(
    "fetch",
    vi.fn().mockResolvedValue({
      ok: response.ok ?? true,
      status: response.status ?? 200,
      json: vi.fn().mockResolvedValue(response.json),
    }),
  );
}

describe("fetchApkMetadata", () => {
  it("returns parsed object for a valid payload", async () => {
    stubFetch({ json: VALID_PAYLOAD });
    const result = await fetchApkMetadata("https://example.com/metadata.json");
    expect(result).toEqual(VALID_PAYLOAD);
  });

  it("returns null when a required field is missing", async () => {
    stubFetch({ json: { ...VALID_PAYLOAD, version: undefined } });
    const result = await fetchApkMetadata("https://example.com/metadata.json");
    expect(result).toBeNull();
  });

  it("returns null when a field has the wrong type", async () => {
    stubFetch({ json: { ...VALID_PAYLOAD, versionCode: "not-a-number" } });
    const result = await fetchApkMetadata("https://example.com/metadata.json");
    expect(result).toBeNull();
  });

  it("returns null when releaseDate is not a valid date string", async () => {
    stubFetch({ json: { ...VALID_PAYLOAD, releaseDate: "not-a-date" } });
    const result = await fetchApkMetadata("https://example.com/metadata.json");
    expect(result).toBeNull();
  });

  it("returns null on a non-ok response", async () => {
    stubFetch({ ok: false, status: 404, json: {} });
    const result = await fetchApkMetadata("https://example.com/metadata.json");
    expect(result).toBeNull();
  });

  it("returns null when fetch throws", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockRejectedValue(new Error("Network down")),
    );
    const result = await fetchApkMetadata("https://example.com/metadata.json");
    expect(result).toBeNull();
  });
});
