import { describe, it, expect } from "vitest";
import { skillCategories } from "./skills";
import { projects } from "./projects";

describe("skillCategories", () => {
  it("every category has a non-empty trimmed name and at least 3 skills", () => {
    for (const cat of skillCategories) {
      expect(cat.name.trim()).not.toBe("");
      expect(cat.skills.length).toBeGreaterThanOrEqual(3);
    }
  });

  it("every seenIn reference matches an existing project name", () => {
    const projectNames = new Set(projects.map((p) => p.name));

    for (const cat of skillCategories) {
      if (!cat.seenIn) continue;
      for (const ref of cat.seenIn) {
        expect(projectNames.has(ref)).toBe(true);
      }
    }
  });

  it("exactly one category is featured", () => {
    const featured = skillCategories.filter((c) => c.featured === true);
    expect(featured.length).toBe(1);
  });

  it("category names are unique", () => {
    const names = skillCategories.map((c) => c.name);
    expect(new Set(names).size).toBe(names.length);
  });
});
