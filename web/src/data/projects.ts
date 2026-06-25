import type { Project } from "../types";

/**
 * Curated project list — edit freely to add, remove, or reorder.
 * Each project links to its public GitHub repo under https://github.com/JakubIwicki.
 */
export const projects: Project[] = [
  {
    name: "SlopBot",
    description:
      "An algorithmic trading engine for Capital.com — streaming market data, ML-driven price predictions, and automated position management with risk controls.",
    tech: ["C#", ".NET", "Machine Learning", "Trading"],
    githubUrl: "https://github.com/JakubIwicki/SlopBot",
  },
  {
    name: "JiApp",
    description:
      "A full-stack YouTube-to-MP3 download app — a .NET microservices backend on AWS with a React Native Android client.",
    tech: ["C#", ".NET", "React Native", "AWS"],
    githubUrl: "https://github.com/JakubIwicki/JiApp",
  },
  {
    name: "FruityClassify",
    description:
      "A deep-learning image classifier that benchmarks custom CNN, EfficientNetB0, MobileNetV3Large, and ensemble models across 10 fruit classes.",
    tech: ["Python", "TensorFlow/Keras", "Deep Learning"],
    githubUrl: "https://github.com/JakubIwicki/FruityClassify",
  },
  {
    name: "ParkingFlow",
    description:
      "A full-stack parking management system for defining parking areas, calculating fees, and tracking payments with multi-currency support.",
    tech: ["ASP.NET Core", "React", "TypeScript", "RavenDB"],
    githubUrl: "https://github.com/JakubIwicki/ParkingFlow",
  },
  {
    name: "CRM_Pro",
    description:
      "A full-stack CRM for managing clients, orders, products, and services, with a REST API backend and a React dashboard.",
    tech: ["Node.js", "React", "TypeScript", "Prisma"],
    githubUrl: "https://github.com/JakubIwicki/CRM_Pro",
  },
  {
    name: "Bawario-Game",
    description:
      "A low-poly Unity arena slasher with infinite levels, an in-game shop, and escalating waves of enemies.",
    tech: ["C#", "Unity", "Game"],
    githubUrl: "https://github.com/JakubIwicki/Bawario-Game",
  },
];
