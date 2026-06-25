import type { Project } from "../types";
import styles from "./ProjectCard.module.css";

interface ProjectCardProps {
  project: Project;
}

export function ProjectCard({ project }: ProjectCardProps) {
  return (
    <article className={styles.card}>
      <div className={styles.content}>
        <h3 className={styles.name}>{project.name}</h3>
        <p className={styles.description}>{project.description}</p>
        <ul className={styles.tags} aria-label="Technologies used">
          {project.tech.map((t) => (
            <li key={t} className={styles.tag}>
              {t}
            </li>
          ))}
        </ul>
      </div>
      <div className={styles.links}>
        <a
          href={project.githubUrl}
          target="_blank"
          rel="noopener noreferrer"
          className={styles.link}
          aria-label={`${project.name} on GitHub (opens in new tab)`}
        >
          GitHub
          <span className={styles.arrow} aria-hidden="true">
            ↗
          </span>
        </a>
        {project.demoUrl && (
          <a
            href={project.demoUrl}
            target="_blank"
            rel="noopener noreferrer"
            className={styles.link}
            aria-label={`${project.name} live demo (opens in new tab)`}
          >
            Demo
            <span className={styles.arrow} aria-hidden="true">
              ↗
            </span>
          </a>
        )}
      </div>
    </article>
  );
}
