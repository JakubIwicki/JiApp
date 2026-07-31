/** A curated project displayed in the portfolio card grid. */
export interface Project {
  name: string;
  description: string;
  tech: string[];
  githubUrl: string;
  demoUrl?: string;
}

/** A technology grouping rendered as a card in the skills grid. */
export interface SkillCategory {
  /** Stable key and heading text, e.g. "Backend & Architecture". */
  name: string;
  /** Technology chips, ordered most-representative first. */
  skills: string[];
  /** Short framing line rendered above the chips. */
  note?: string;
  /** Project names from data/projects.ts that demonstrate this category. */
  seenIn?: string[];
  /** Renders double-width on wide screens. Exactly one category sets this. */
  featured?: boolean;
}

/** Re-exported from the Zod boundary so types.ts stays the single type index. */
export type { ApkMetadata } from "./lib/apkMetadata";
