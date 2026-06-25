/**
 * Contract test: the publisher (publish-apk.sh --print-metadata) produces JSON
 * that matches the site's ApkMetadataSchema. This catches producer/consumer
 * drift — e.g. the releaseDate format changing on one side but not the other.
 *
 * Self-skips when web/ is extracted to its own repo (script no longer at ../).
 * Also self-skips when jq is not on PATH (required by the publisher).
 */
import { describe, it, expect } from "vitest";
import { execFileSync } from "node:child_process";
import { existsSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";
import { writeFileSync, unlinkSync } from "node:fs";
import { randomBytes } from "node:crypto";
import { ApkMetadataSchema } from "./apkMetadata";

function jqAvailable(): boolean {
  try {
    execFileSync("which", ["jq"], { stdio: "ignore" });
    return true;
  } catch {
    return false;
  }
}

describe("apkMetadata contract (publisher ↔ site schema)", () => {
  it("publish-apk.sh --print-metadata output passes ApkMetadataSchema", (ctx) => {
    const scriptPath = resolve(process.cwd(), "..", "publish-apk.sh");

    if (!existsSync(scriptPath)) {
      ctx.skip();
      return;
    }
    if (!jqAvailable()) {
      ctx.skip();
      return;
    }

    const versionCode = 1;
    const apkPath = join(tmpdir(), `JiAppMobile-${versionCode}-release.apk`);

    // Write a non-empty file (~1KB) so sizeBytes is positive
    writeFileSync(apkPath, randomBytes(1024));

    try {
      const stdout = execFileSync(
        "bash",
        [
          scriptPath,
          "--apk",
          apkPath,
          "--version",
          "1.2.3",
          "--print-metadata",
        ],
        { encoding: "utf-8" },
      );

      const json: unknown = JSON.parse(stdout);
      const result = ApkMetadataSchema.safeParse(json);

      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.version).toBe("1.2.3");
        expect(result.data.versionCode).toBe(versionCode);
        expect(result.data.sizeBytes).toBeGreaterThan(0);
        expect(result.data.sha256).toMatch(/^[a-f0-9]{64}$/);
      }
    } finally {
      try {
        unlinkSync(apkPath);
      } catch {
        /* best-effort cleanup */
      }
    }
  });
});
