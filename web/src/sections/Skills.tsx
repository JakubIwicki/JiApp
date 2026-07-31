import { skillCategories } from "../data/skills";
import { SkillCategoryCard } from "../components/SkillCategoryCard";
import styles from "./Skills.module.css";

export function Skills() {
  return (
    <section id="skills" className={styles.section}>
      <div className={styles.container}>
        <h2 className={styles.heading}>Skills</h2>
        <p className={styles.subtitle}>
          Backend-focused full-stack &mdash; the stack I actually ship with.
        </p>
        <div className={styles.grid}>
          {skillCategories.map((c) => (
            <SkillCategoryCard
              key={c.name}
              category={c}
              className={c.featured ? styles.featured : undefined}
            />
          ))}
        </div>
      </div>
    </section>
  );
}
