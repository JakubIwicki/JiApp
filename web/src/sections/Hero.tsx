import { GITHUB_URL } from "../config";
import styles from "./Hero.module.css";

export function Hero() {
  return (
    <section id="about" className={styles.hero}>
      <div className={styles.container}>
        {/* Avatar slot — replace the placeholder div with an <img> when a real photo is ready.
            <img src="/avatar.jpg" alt="Jakub Iwicki" className={styles.avatar} /> */}
        <div
          className={styles.avatar}
          aria-label="Jakub Iwicki initials avatar"
        >
          <span className={styles.avatarInitials}>JI</span>
        </div>

        <h1 className={styles.name}>Jakub Iwicki</h1>
        <p className={styles.role}>Software Engineer</p>

        <div className={styles.bio}>
          <p>
            I build full-stack web applications, mobile apps, and backend
            services — from architecture to deployment. I&rsquo;m comfortable
            across the stack, with a focus on React, TypeScript, .NET, and cloud
            infrastructure on AWS.
          </p>
          <p>
            I enjoy solving real problems with clean, maintainable code and
            thoughtful system design. When I&rsquo;m not shipping features,
            I&rsquo;m exploring new tools, contributing to open-source projects,
            or tinkering with game prototypes and CLI utilities.
          </p>
        </div>

        <div className={styles.ctas}>
          <a
            href={GITHUB_URL}
            target="_blank"
            rel="noopener noreferrer"
            className={styles.ctaPrimary}
          >
            View GitHub
          </a>
          <a href="#download" className={styles.ctaSecondary}>
            Download the app
          </a>
        </div>
      </div>
    </section>
  );
}
