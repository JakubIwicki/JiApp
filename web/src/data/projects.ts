import type { Project } from "../types";

/**
 * Curated project list — edit freely to add, remove, or reorder.
 * Each project links to its public GitHub repo under https://github.com/JakubIwicki.
 */
export const projects: Project[] = [
  {
    name: "JiApp",
    description:
      "Full-stack social media management app with a .NET backend on AWS and a React Native Android client.",
    tech: ["C#", ".NET", "React Native", "AWS"],
    githubUrl: "https://github.com/JakubIwicki/JiApp",
  },
  {
    name: "MeSH",
    description:
      "Tools and utilities for working with the Medical Subject Headings (MeSH) thesaurus and biomedical ontologies.",
    tech: ["Python", "Biomedical", "NLP"],
    githubUrl: "https://github.com/JakubIwicki/MeSH",
  },
  {
    name: "trading-api",
    description:
      "Algorithmic trading infrastructure — data pipelines, backtesting harness, and broker API integrations.",
    tech: ["Python", "Finance", "REST APIs"],
    githubUrl: "https://github.com/JakubIwicki/trading-api",
  },
  {
    name: "SlopBot",
    description:
      "A Discord bot with moderation utilities, custom commands, and server-management automation.",
    tech: ["TypeScript", "Discord API", "Node.js"],
    githubUrl: "https://github.com/JakubIwicki/SlopBot",
  },
  {
    name: "FruityClassify",
    description:
      "An image-classification model that identifies fruit varieties from photos, built and trained in Jupyter notebooks.",
    tech: ["Python", "Jupyter", "Machine Learning"],
    githubUrl: "https://github.com/JakubIwicki/FruityClassify",
  },
];
