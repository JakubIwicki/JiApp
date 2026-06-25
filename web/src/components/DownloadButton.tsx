import { APK_URL } from "../config";
import styles from "./DownloadButton.module.css";

/** Big primary CTA — a plain `<a download>` so the download works without JS. */
export function DownloadButton() {
  return (
    <a href={APK_URL} download className={styles.button}>
      Download for Android
    </a>
  );
}
