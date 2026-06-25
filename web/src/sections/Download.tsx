import { useEffect, useState } from "react";
import { APK_METADATA_URL } from "../config";
import { isAndroid } from "../lib/device";
import { fetchApkMetadata, type ApkMetadata } from "../lib/apkMetadata";
import { DownloadButton } from "../components/DownloadButton";
import { QrCode } from "../components/QrCode";
import styles from "./Download.module.css";

type MetadataState =
  | { status: "loading" }
  | { status: "ready"; data: ApkMetadata }
  | { status: "unavailable" };

function formatSize(bytes: number): string {
  const mb = bytes / 1_048_576;
  return `${mb.toFixed(1)} MB`;
}

export function Download() {
  const [metadata, setMetadata] = useState<MetadataState>({
    status: "loading",
  });
  const android = isAndroid();

  useEffect(() => {
    let cancelled = false;
    fetchApkMetadata(APK_METADATA_URL).then((data) => {
      if (cancelled) return;
      setMetadata(data ? { status: "ready", data } : { status: "unavailable" });
    });
    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <section id="download" className={styles.section}>
      <div className={styles.container}>
        <h2 className={styles.heading}>Download the app</h2>

        {/* ---- Metadata line ---- */}
        {metadata.status === "loading" && (
          <p className={styles.meta}>Loading version info&hellip;</p>
        )}
        {metadata.status === "ready" && (
          <p className={styles.meta}>
            JiApp v{metadata.data.version} &middot;{" "}
            {formatSize(metadata.data.sizeBytes)} &middot;{" "}
            {metadata.data.releaseDate}
          </p>
        )}
        {/* metadata.status === 'unavailable' → silent; download still works */}

        {/* ---- Device branch ---- */}
        <div className={styles.action}>
          {android ? <DownloadButton /> : <QrCode />}
        </div>

        {/* ---- Install steps ---- */}
        <details className={styles.details}>
          <summary className={styles.summary}>How to install</summary>
          <ol className={styles.steps}>
            <li>
              Open your device <strong>Settings</strong> and enable{" "}
              <em>Install unknown apps</em> (or <em>Allow from this source</em>)
              for your browser.
            </li>
            <li>
              After downloading, open the APK file from your notifications or
              downloads folder.
            </li>
            <li>
              Tap <strong>Install</strong> and confirm any prompts.
            </li>
          </ol>
        </details>

        {/* ---- Notes ---- */}
        <div className={styles.notes}>
          <p>
            This APK is <strong>self-signed</strong> and distributed outside
            Google Play, so Android will show a security warning during
            installation. This is expected.
          </p>
          <p>
            The app backend runs on a server that{" "}
            <strong>sleeps when idle</strong> and wakes on demand — the first
            launch may take a moment while the server starts up.
          </p>
        </div>
      </div>
    </section>
  );
}
