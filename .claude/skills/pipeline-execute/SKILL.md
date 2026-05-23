---
name: pipeline-execute
description: Execute a single phase from PLAN.md — dispatches coder and auditor agents, loops until tasks pass review. Invoked inside claude -p sessions by lets-do-it.sh orchestrator.
type: flexible
---

# pipeline-execute — Phase Executor

You execute a single phase of an implementation plan. You are invoked inside a `claude -p` session by the `lets-do-it.sh` orchestrator. There is no interactive user — you work autonomously until the phase is done or blocked.

## Input

You receive the phase name, plan file, and process file from the prompt that spawned you. Extract them from the prompt text (they appear as `phase="..."`, `plan_file=...`, `process_file=...`).

If you cannot find these parameters, search the conversation for the most recent `PHASE:`, `PLAN_FILE:`, `PROCESS_FILE:` markers or similar.

## Execution Flow

### Step 1: Load context
- Read `plan_file` (the full PLAN.md)
- Read `process_file` (the full PROCESS.md)
- Identify the section for `phase` in PLAN.md

### Step 2: Detect project stack
Check PROCESS.md's Decision Log for the tech stack. Look for keywords:
- **C# / .NET / ASP.NET** → use `csharp-coder` agent and `csharp-coder.md` conventions
- **React Native / TypeScript / RN** → use `react-native-coder` agent
- If unclear from PROCESS.md, scan the project files: `.csproj` → C#, `package.json` with `react-native` → RN

### Step 3: Extract tasks
From the phase section in PLAN.md, extract every `- [ ]` task. If none, print `PHASE_RESULT: COMPLETE` and stop.

### Step 4: Execute each task (inner loop)

Custom agent types (csharp-coder, react-native-coder, smart-auditor) are defined in `.claude/agents/` and auto-discovered by Claude Code. Use them directly.

For each task:

**4a. Dispatch coder agent:**
```
Agent(
  subagent_type: "<stack>-coder",  // "csharp-coder" or "react-native-coder"
  description: "Implement <task description>",
  prompt: "Implement this task: <task description>

  Context from PLAN.md: <phase context>
  Context from PROCESS.md: <relevant decision log entries>
  Previous critique (if any): <critique from auditor>

  Follow TDD: write a failing test first, make it pass, refactor.
  Write the actual files — do not just describe them.
  When done, report what files you changed and what tests pass."
)
```

**4b. Dispatch auditor agent:**
```
Agent(
  subagent_type: "smart-auditor",
  description: "Review <task description>",
  prompt: "Review the implementation of: <task description>

  Files changed: <list from coder output>
  Project conventions from PROCESS.md: <relevant entries>

  Check: protocol compliance, test quality (AAA pattern), code simplicity.
  Start your response with APPROVED if the code is correct.
  If issues exist, list them as actionable bullet points."
)
```

**4c. Decide:**
- If auditor response starts with APPROVED → mark task done, move to next task
- If auditor found issues → go back to 4a with the critique (max 3 attempts per task)
- If 3 attempts exceeded → stop all work and print BLOCKED marker

**4d. Commit after each approved task** with a descriptive message.

### Step 5: Finalize phase
When all tasks are approved:
1. Update `plan_file`: change each `- [ ]` for this phase to `- [x]`
2. Append a completion entry to `process_file` documenting what was done, files changed, and key decisions
3. Print the machine-readable marker:

```
PHASE_RESULT: APPROVED
COMPLETED_TASKS: <comma-separated task descriptions>
NEXT_PHASE: <next phase name from PLAN.md, or "None">
```

## Blocked Output

If a task fails after 3 auditor rejections, or if you encounter an unrecoverable error:

```
PHASE_RESULT: BLOCKED
COMPLETED_TASKS: <tasks that passed>
FAILED_TASKS: <task that is stuck>
CRITIQUE: <last auditor feedback on the failing task>
```

## Agent Fallback

Agent dispatch has been verified to work in `claude -p` mode. If Agent dispatch unexpectedly fails (e.g., agent type not found, timeout), fall back to implementing the task yourself by reading the relevant coder protocol from `.claude/agents/<stack>-coder.md` and the auditor protocol from `.claude/agents/smart-auditor.md`, then applying those rules directly. The quality bar is the same.

## Rules

- **Never skip the auditor.** Every task goes through coder → auditor → decide.
- **One task at a time.** Complete and commit each task before moving to the next.
- **Commit often.** One commit per approved task with a descriptive message.
- **Don't modify PROCESS.md structure.** Only append entries; don't reorganize.
- **The marker MUST be the last thing you output.** The bash script parses it.
