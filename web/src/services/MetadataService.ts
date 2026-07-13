import { AbstractService } from "./AbstractService";
import { ApkMetadataSchema, type ApkMetadata } from "../lib/apkMetadata";

/**
 * Fetches and validates APK metadata from a sidecar JSON URL.
 * Never throws — returns null on any failure (network, non-ok,
 * invalid JSON, or Zod validation failure).
 */
export class MetadataService extends AbstractService {
  async getMetadata(url: string): Promise<ApkMetadata | null> {
    try {
      return await this.getJson<ApkMetadata | null>(url, (data) => {
        const result = ApkMetadataSchema.safeParse(data);
        return result.success ? result.data : null;
      });
    } catch {
      return null;
    }
  }
}
