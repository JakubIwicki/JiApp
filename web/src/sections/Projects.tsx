import { projects } from "../data/projects";
import { ProjectCard } from "../components/ProjectCard";
import styles from "./Projects.module.css";

export function Projects() {
  return (
    <section id="projects" className={styles.section}>
      <div className={styles.container}>
        <h2 className={styles.heading}>Projects</h2>
        <p className={styles.subtitle}>
          A few things I&rsquo;ve built. See more on{" "}
          <a
            href="https://github.com/JakubIwicki"
            target="_blank"
            rel="noopener noreferrer"
            className={styles.link}
          >
            GitHub
          </a>
          .
        </p>
        <div className={styles.grid}>
          {projects.map((project) => (
            <ProjectCard key={project.name} project={project} />
          ))}
        </div>
      </div>
    </section>
  );
}
