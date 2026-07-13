import { useState, useEffect } from "react";
import { useServices } from "../services/ServiceProvider";
import type { ApkMetadata } from "../lib/apkMetadata";
import { APK_METADATA_URL } from "../config";

export type MetadataState =
  | { status: "loading" }
  | { status: "ready"; data: ApkMetadata }
  | { status: "unavailable" };

/**
 * Fetches live APK metadata from S3 on mount.
 * Never throws — the "unavailable" variant covers all failure modes
 * so the Download section can still render the download action.
 */
export function useApkMetadata(): MetadataState {
  const { metadataService } = useServices();
  const [state, setState] = useState<MetadataState>({ status: "loading" });

  useEffect(() => {
    let cancelled = false;
    metadataService.getMetadata(APK_METADATA_URL).then((data) => {
      if (cancelled) return;
      setState(data ? { status: "ready", data } : { status: "unavailable" });
    });
    return () => {
      cancelled = true;
    };
  }, [metadataService]);

  return state;
}
