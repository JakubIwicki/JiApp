import { QRCodeSVG } from "qrcode.react";
import { APK_URL } from "../config";
import styles from "./QrCode.module.css";

/** Renders a QR code encoding the APK download URL, for desktop visitors. */
export function QrCode() {
  return (
    <div className={styles.container}>
      <div className={styles.qrWrapper}>
        <QRCodeSVG
          value={APK_URL}
          size={200}
          bgColor="transparent"
          fgColor="currentColor"
          level="M"
          className={styles.qr}
        />
      </div>
      <p className={styles.caption}>Scan to download on your phone</p>
    </div>
  );
}
