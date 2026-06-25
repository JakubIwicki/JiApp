import { GITHUB_URL } from "../config";
import styles from "./Footer.module.css";

export function Footer() {
  return (
    <footer className={styles.footer}>
      <div className={styles.container}>
        <div className={styles.links}>
          <a
            href={GITHUB_URL}
            target="_blank"
            rel="noopener noreferrer"
            className={styles.link}
          >
            GitHub
          </a>
          {/* TODO(jakub): replace with real LinkedIn URL */}
          <a
            href="https://linkedin.com/in/REPLACE_ME"
            target="_blank"
            rel="noopener noreferrer"
            className={`${styles.link} ${styles.muted}`}
          >
            LinkedIn
          </a>
          {/* TODO(jakub): replace with real email */}
          <a
            href="mailto:REPLACE_ME@example.com"
            className={`${styles.link} ${styles.muted}`}
          >
            Email
          </a>
        </div>
        <p className={styles.copy}>
          &copy; {new Date().getFullYear()} Jakub Iwicki. Built with React +
          Vite.
        </p>
      </div>
    </footer>
  );
}
