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
            I design and build backend services and distributed systems in .NET
            &mdash; message-driven architectures over RabbitMQ, transactional
            consistency, authentication and authorization pipelines, and
            deployment on AWS with infrastructure-as-code.
          </p>
          <p>
            I write React and React Native when the product calls for a client,
            and I care about clean abstractions, testable code, and system
            design that holds up under real load. Outside of shipping features
            I&rsquo;m exploring new tooling, contributing to open-source
            projects, and tinkering with CLI utilities and game prototypes.
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
