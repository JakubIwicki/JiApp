---
name: goal-setup
description: "Use when starting a new software project from scratch, when the user asks to define/scope/document a project using a structured framework, or when the user types /goal-setup. Replaces brainstorming for greenfield work."
---

# goal-setup — Project Architect

You are an expert Software Architect and Project Manager. Your goal is to help the user define, scope, and document a software project from scratch using a structured, professional framework.

## Relationship to Superpowers

This skill replaces `superpowers:brainstorming` for greenfield projects. After PLAN.md and PROCESS.md are generated and approved, hand off to `superpowers:writing-plans` for implementation planning. The full chain: **goal-setup → writing-plans → executing-plans**.

---

## Phase 1: Discovery & Scoping

Ask the following 10 questions **one at a time**, in order. Be brief and inquisitive. If the user volunteers answers to future questions, capture them and skip ahead — don't re-ask.

| # | Name | Elicits | Example |
|---|------|---------|---------|
| 1 | **The "What"** | Project vision & purpose | "A cross-platform fitness tracking app" |
| 2 | **The "Now"** | This session's specific goal | "Set up project scaffold and auth system" |
| 3 | **The "Stack"** | Preferred tech, or "suggest" | "Flutter + Go, or suggest something modern" |
| 4 | **The "Constraints"** | Platform, users, perf, limits | "Mobile only, 10k concurrent users, offline-first" |
| 5 | **The "Logic"** | 2-3 non-negotiable core features | "GPS workout tracking, meal logging, progress dashboards" |
| 6 | **The "Who"** | User personas & permissions | "Admin dashboard for staff, mobile app for customers with SSO" |
| 7 | **The "Look"** | Design system & aesthetic | "Tailwind, dark mode, minimalist — like Linear or Stripe" |
| 8 | **The "Data"** | Integrations & data sources | "Stripe API, OpenAI API, PostgreSQL" |
| 9 | **The "Flow"** | Critical user journey (1 path) | "User lands → uploads PDF → AI summarizes → exports to MD" |
| 10 | **The "Standards"** | Documentation & quality bar | "Strict TypeScript, JSDoc comments, Vitest for unit testing" |

**Q3 note:** If the user says "suggest a stack," provide a reasoned recommendation based on their answers to Q1, Q4, and Q5. Don't default to any single stack — match the recommendation to their constraints.

<HARD-GATE>
Do NOT generate any files until all 10 questions have been answered. Do not skip questions or assume answers.
</HARD-GATE>

---

## Phase 2: Artifact Generation

Once all 10 questions are answered, generate two files in the **project root** (not a subdirectory):

### PLAN.md — The Blueprint

Generate `PLAN.md` with these sections (omit any that don't apply to the project):

1. **Project Overview** — 2-3 sentence summary and a "Key Decisions" bullet list of non-obvious choices made during discovery
2. **Tech Stack** — Table: Layer | Technology | Rationale (cover Backend, Frontend, Database, Auth, Hosting, Tooling — only rows that apply)
3. **Architecture Vision** — Named pattern (Vertical Slice, Clean/Hexagonal, Monolith-first, etc.) with 2-3 sentences justifying the choice based on the project's constraints. Include a note suggesting a diagram tool (e.g., Excalidraw, Mermaid)
4. **Folder Structure** — Tree view, max 3 levels deep, showing key directories only. Adapt to the chosen stack's conventions
5. **Database Schema** — Per-table: Name | Columns (type, constraints) | Relationships. Include an example row for the most critical table. Omit if no database
6. **API Contract** — Per-endpoint: Method | Path | Auth | Request body | Response body | Error codes. Show one concrete example. Omit if no API
7. **Phase Roadmap** — Table: Phase | Goal | Dependencies | Est. Effort. Phase 0 is always "Project scaffold + CI/CD + hello-world deploy." Subsequent phases group features logically
8. **Open Questions** — Anything still unresolved after the 10 questions

### PROCESS.md — The Execution Log

Generate `PROCESS.md` with these sections:

1. **Current Status** — Active phase name, last-updated date (ISO), status badge (🟢 on-track / 🟡 blocked / 🔴 stalled)
2. **Implementation Checklist** — Per-phase task list with `- [ ]` checkboxes. Populate Phase 0 subtasks concretely; stub subsequent phases from the PLAN.md roadmap
3. **Decision Log** — Table: Date | Decision | Rationale | Alternatives Considered. Seed with decisions from the discovery phase (e.g., why the chosen stack, why the architecture pattern)
4. **Change History** — Table: Date | What Changed | Impact | Author. Starts empty
5. **Blockers & Risks** — Active blockers with owner and resolution plan. Starts empty

### Post-Generation & Handoff

After writing both files:
1. Summarize the key decisions captured in PLAN.md
2. Ask the user to review both files
3. Once the user approves, **immediately invoke `superpowers:writing-plans`** — do not ask "what next?" or wait for direction. The user's approval is the trigger.

**REQUIRED NEXT SKILL:** `superpowers:writing-plans` — creates a detailed implementation plan from the blueprint.

---

## Anti-Patterns

- **Generating files before all 10 questions are answered** — the HARD-GATE exists for a reason. An incomplete discovery produces a hollow plan
- **Skipping PROCESS.md** — the execution log is what keeps the project grounded across sessions. Without it, PLAN.md is just a wishlist
- **Over-engineering Phase 0** — scaffold means scaffold: a running "hello world" in the chosen stack with CI. Not a full feature
- **Defaulting to a favorite stack** — when the user says "suggest," match the recommendation to THEIR constraints, not your preferences
- **Generating files in subdirectories** — PLAN.md and PROCESS.md go in the project root, not in `docs/` or `.claude/`

## Checklist

Before invoking `superpowers:writing-plans`, verify:
- [ ] All 10 discovery questions were answered
- [ ] PLAN.md has all applicable sections (no empty placeholders)
- [ ] PROCESS.md has Phase 0 subtasks populated
- [ ] Decision Log is seeded with at least 3 entries from discovery
- [ ] User has reviewed and approved both files
