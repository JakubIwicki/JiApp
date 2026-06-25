import { z } from "zod";

/**
 * Schema for the apk-metadata.json sidecar served from the public S3 bucket.
 * This is the SINGLE boundary that parses and validates the JSON — nothing
 * else in the site touches the raw fetch response.
 */
export const ApkMetadataSchema = z.object({
  version: z.string(),
  versionCode: z.number().int(),
  sizeBytes: z.number().int().positive(),
  releaseDate: z.string().date(),
  sha256: z.string(),
});

export type ApkMetadata = z.infer<typeof ApkMetadataSchema>;

/**
 * Fetch and validate APK metadata from a URL.
 * Returns the parsed object on success, or null on ANY failure
 * (network error, non-ok response, invalid JSON, or Zod validation failure).
 * This function NEVER throws.
 */
export async function fetchApkMetadata(
  url: string,
): Promise<ApkMetadata | null> {
  try {
    const res = await fetch(url);
    if (!res.ok) return null;

    const json: unknown = await res.json();
    const result = ApkMetadataSchema.safeParse(json);
    return result.success ? result.data : null;
  } catch {
    return null;
  }
}
