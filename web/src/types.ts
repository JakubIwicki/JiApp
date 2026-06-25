/** A curated project displayed in the portfolio card grid. */
export interface Project {
  name: string;
  description: string;
  tech: string[];
  githubUrl: string;
  demoUrl?: string;
}

/** Re-exported from the Zod boundary so types.ts stays the single type index. */
export type { ApkMetadata } from "./lib/apkMetadata";
