import type { SkillCategory } from "../types";

/**
 * Skill categories rendered in the skills grid.
 * Each category groups related technologies and optionally links to
 * projects in data/projects.ts that demonstrate them.
 */
export const skillCategories: SkillCategory[] = [
  {
    name: "Backend & Architecture",
    featured: true,
    seenIn: ["JjChat", "JiApp", "ParkingFlow"],
    skills: [
      ".NET 10 / C# 14",
      "ASP.NET Core Minimal APIs",
      "Vertical Slice Architecture",
      "CQRS + mediator",
      "DDD boundaries",
      "FluentValidation",
      "OpenIddict (OAuth2/OIDC)",
      "JWT + claim-based RBAC",
      "Rate limiting",
      "Result types",
      "Problem Details",
    ],
  },
  {
    name: "Messaging & Resilience",
    seenIn: ["JjChat", "JiApp"],
    skills: [
      "MassTransit",
      "RabbitMQ",
      "Transactional outbox",
      "Async command pipelines",
      "Polly (retry/backoff/jitter)",
      "Idempotency",
      "Optimistic concurrency",
      "Background workers",
    ],
  },
  {
    name: "Data & Persistence",
    seenIn: ["JjChat", "ParkingFlow", "CRM_Pro"],
    skills: [
      "PostgreSQL",
      "EF Core",
      "Migrations",
      "SQLite",
      "RavenDB",
      "Prisma",
    ],
  },
  {
    name: "Cloud & DevOps",
    seenIn: ["JiApp", "JjChat"],
    skills: [
      "AWS EC2",
      "S3",
      "Lambda",
      "API Gateway",
      "CloudFormation",
      "Docker",
      "Docker Compose",
      "nginx",
      "GitHub Actions",
      "systemd",
    ],
  },
  {
    name: "Testing & Quality",
    seenIn: ["JjChat", "JiApp"],
    skills: [
      "xUnit",
      "FluentAssertions",
      "Moq",
      "Testcontainers",
      "Integration testing",
      "Architecture fitness tests",
      "Vitest",
      "Testing Library",
      "MSW",
    ],
  },
  {
    name: "Languages",
    skills: [
      "C#",
      "TypeScript",
      "Python",
      "SQL",
      "Bash",
      "PowerShell",
      "JavaScript",
    ],
  },
  {
    name: "Frontend",
    note: "Comfortable shipping UI when the product needs it.",
    seenIn: ["JiApp", "CRM_Pro", "ParkingFlow", "JjChat"],
    skills: [
      "React",
      "React Native",
      "Vite",
      "Zod",
      "TailwindCSS",
      "CSS Modules",
      "React Router",
      "Storybook",
    ],
  },
  {
    name: "ML & Data Science",
    seenIn: ["FruityClassify", "SlopBot"],
    skills: [
      "TensorFlow/Keras",
      "CNNs",
      "Transfer learning",
      "XGBoost",
      "pandas",
      "Jupyter",
    ],
  },
];
