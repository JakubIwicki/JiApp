import { GITHUB_URL } from "../config";
import styles from "./Nav.module.css";

const LINKS = [
  { href: "#about", label: "About" },
  { href: "#projects", label: "Projects" },
  { href: "#download", label: "Download" },
] as const;

export function Nav() {
  return (
    <header className={styles.header}>
      <nav className={styles.nav} aria-label="Site navigation">
        <a href="#about" className={styles.logo}>
          JI
        </a>
        <ul className={styles.links}>
          {LINKS.map(({ href, label }) => (
            <li key={href}>
              <a href={href} className={styles.link}>
                {label}
              </a>
            </li>
          ))}
          <li>
            <a
              href={GITHUB_URL}
              target="_blank"
              rel="noopener noreferrer"
              className={styles.link}
              aria-label="GitHub profile (opens in new tab)"
            >
              GitHub
            </a>
          </li>
        </ul>
      </nav>
    </header>
  );
}
