import { describe, it, expect, vi, afterEach } from "vitest";
import { isAndroid } from "./device";

function setUserAgent(ua: string) {
  vi.stubGlobal("navigator", { userAgent: ua });
}

describe("isAndroid", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns true for an Android user agent", () => {
    setUserAgent(
      "Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.6778.135 Mobile Safari/537.36",
    );
    expect(isAndroid()).toBe(true);
  });

  it("returns false for a desktop Chrome user agent", () => {
    setUserAgent(
      "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
    );
    expect(isAndroid()).toBe(false);
  });

  it("returns false when navigator is undefined", () => {
    vi.stubGlobal("navigator", undefined);
    expect(isAndroid()).toBe(false);
  });
});
