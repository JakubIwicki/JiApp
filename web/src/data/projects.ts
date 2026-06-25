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
    name: "ki",
    description:
      "A modern fuzzy finder and file-navigation tool for the terminal, written in Rust.",
    tech: ["Rust", "CLI", "Fuzzy Matching"],
    githubUrl: "https://github.com/JakubIwicki/ki",
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
    name: "beesness",
    description:
      "A playful business-simulation game where you manage a bee colony and compete in honey markets.",
    tech: ["TypeScript", "Game", "Web"],
    githubUrl: "https://github.com/JakubIwicki/beesness",
  },
  {
    name: "permafrost",
    description:
      "Climate-data tooling for permafrost modelling — processing pipelines and interactive visualizations.",
    tech: ["Python", "Climate", "Data Viz"],
    githubUrl: "https://github.com/JakubIwicki/permafrost",
  },
];
