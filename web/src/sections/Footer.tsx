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
          <a
            href="https://www.linkedin.com/in/jakub-iwicki/"
            target="_blank"
            rel="noopener noreferrer"
            className={styles.link}
          >
            LinkedIn
          </a>
          <a href="mailto:jakubiwicki.aj@gmail.com" className={styles.link}>
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
