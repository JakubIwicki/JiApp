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
