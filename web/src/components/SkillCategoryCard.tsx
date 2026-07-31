import type { SkillCategory } from "../types";
import styles from "./SkillCategoryCard.module.css";

interface SkillCategoryCardProps {
  category: SkillCategory;
  className?: string;
}

export function SkillCategoryCard({
  category,
  className,
}: SkillCategoryCardProps) {
  const cardClass = [
    styles.card,
    category.featured ? styles.featured : undefined,
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <article className={cardClass}>
      <h3 className={styles.name}>{category.name}</h3>
      {category.note && <p className={styles.note}>{category.note}</p>}
      <ul className={styles.tags} aria-label="Technologies">
        {category.skills.map((skill) => (
          <li key={skill} className={styles.tag}>
            {skill}
          </li>
        ))}
      </ul>
      {category.seenIn && (
        <p className={styles.seenIn}>Seen in: {category.seenIn.join(", ")}</p>
      )}
    </article>
  );
}
