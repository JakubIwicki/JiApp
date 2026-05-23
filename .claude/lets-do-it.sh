#!/bin/bash
set -euo pipefail

# --- Configuration & Parameters ---
# Usage: ./lets-do-it.sh [max_retries_per_task] [plan_file] [process_file]
MAX_RETRIES=${1:-3}
PLAN_FILE=${2:-"PLAN.md"}
PROCESS_FILE=${3:-"PROCESS.md"}
WORKTREE_DIR=".claude/worktrees"

ORIGINAL_DIR="$(pwd)"
PIPELINE_STARTED=$(date -Iseconds)

# --- Helpers ---

# Extract phase names from PLAN.md Phase Roadmap table.
# Looks for table rows like: | Phase N — Name | ... |
extract_phases() {
    local plan="$1"
    awk -F'|' '/^\|.*Phase [0-9]+/ {
        gsub(/^[ \t]+|[ \t]+$/, "", $2);
        print $2
    }' "$plan"
}

# Convert a phase name to a filesystem-safe slug.
slugify() {
    echo "$1" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]/-/g' | sed 's/-\+/-/g' | sed 's/^-//;s/-$//'
}

# Build the prompt that gets sent to claude -p for a single phase.
build_prompt() {
    local phase_name="$1"
    local max_retries="$2"
    cat <<PROMPT
You are executing a phase of an implementation plan autonomously in a git worktree.
There is no interactive user. Work until the phase is done or blocked, then stop.

=== PHASE ===
$phase_name

=== FILES ===
Plan file: $PLAN_FILE
Process file: $PROCESS_FILE

=== EXECUTION PROTOCOL ===

STEP 1: Load context
- Read $PLAN_FILE — find the section for "$phase_name"
- Read $PROCESS_FILE — load the decision log and previous handoffs
- Extract every \`- [ ]\` task from the phase section

STEP 2: Detect project stack
Check $PROCESS_FILE Decision Log for tech stack keywords: C#, .NET, ASP.NET, React Native, TypeScript, etc.
Or scan project files: .csproj or \*.cs → C# stack; package.json with react-native → RN stack.
Record your finding: "Stack detected: <stack>"

STEP 3: For each task (loop, max $max_retries attempts per task)

  a. DISPATCH CODER — use the Agent tool:
     Agent(
       subagent_type: "<stack>-coder",  // e.g., "csharp-coder" or "react-native-coder"
       description: "Implement <task description>",
       prompt: "Implement this task: <task description>

         Context from $PLAN_FILE phase section.
         Conventions from $PROCESS_FILE Decision Log.
         Previous critique (if any): <auditor feedback from previous attempt>

         Follow TDD: write a failing test first, make it pass, refactor.
         Write the actual files — do not just describe them.
         When done, report what files you changed and what tests pass."
     )

  b. DISPATCH AUDITOR — use the Agent tool:
     Agent(
       subagent_type: "smart-auditor",
       description: "Review <task description>",
       prompt: "Review the implementation of: <task description>

         Files changed: <list from coder output>
         Project conventions from $PROCESS_FILE: <relevant entries>

         Check: protocol compliance, test quality (AAA), code simplicity.
         Start your response with APPROVED if correct, or list actionable issues."
     )

  c. DECIDE:
     - If auditor says APPROVED → commit, mark task [x] in $PLAN_FILE, move to next task
     - If auditor found issues → go back to 3a with critique (max $max_retries attempts total)
     - If $max_retries attempts exceeded → PHASE_RESULT: BLOCKED

STEP 4: Finalize
When all phase tasks are done:
  1. Update $PLAN_FILE: change \`- [ ]\` to \`- [x]\` for this phase's tasks
  2. Append a completion entry to $PROCESS_FILE documenting:
     - What was done (tasks completed)
     - Files changed (list with descriptions)
     - Key decisions made
  3. Output the APPROVED marker below

=== MARKER ===
Your final output MUST end with exactly one of these markers as the LAST lines:

On success (all tasks done):
PHASE_RESULT: APPROVED
COMPLETED_TASKS: <comma-separated list>
NEXT_PHASE: <next phase name from $PLAN_FILE, or "None">

On failure (task stuck after $max_retries attempts):
PHASE_RESULT: BLOCKED
COMPLETED_TASKS: <tasks that passed>
FAILED_TASKS: <task that is stuck>
CRITIQUE: <what went wrong>
PROMPT
}

# Print a timestamped pipeline log line.
log_step() {
    echo "[$(date '+%H:%M:%S')] $*"
}

# --- Startup ---

echo "================================================"
echo "  Autonomous Pipeline — lets-do-it"
echo "================================================"
echo "  Started:    $PIPELINE_STARTED"
echo "  Plan:       $PLAN_FILE"
echo "  Process:    $PROCESS_FILE"
echo "  Max retries per task: $MAX_RETRIES"
echo "================================================"
echo ""

if [ ! -f "$PLAN_FILE" ]; then
    echo "ERROR: $PLAN_FILE not found in $ORIGINAL_DIR"
    exit 1
fi

if [ ! -f "$PROCESS_FILE" ]; then
    echo "ERROR: $PROCESS_FILE not found in $ORIGINAL_DIR"
    exit 1
fi

# Extract phases from PLAN.md
mapfile -t PHASES < <(extract_phases "$PLAN_FILE")

if [ ${#PHASES[@]} -eq 0 ]; then
    echo "No phases found in $PLAN_FILE Phase Roadmap. Looking for heading-based phases..."
    mapfile -t PHASES < <(grep -oP '^##\s+Phase\s+[^:]+:?\s*\K.*' "$PLAN_FILE" || true)
fi

if [ ${#PHASES[@]} -eq 0 ]; then
    echo "No phases found. Treating entire plan as a single phase."
    PHASES=("Full Plan")
fi

echo "Phases detected: ${#PHASES[@]}"
for i in "${!PHASES[@]}"; do
    echo "  $((i+1)). ${PHASES[$i]}"
done
echo ""

# --- Cleanup handler ---

cleanup() {
    echo ""
    echo "--- Pipeline interrupted ---"
    echo "Active worktrees:"
    git worktree list 2>/dev/null || true
    echo "Worktrees preserved for manual recovery."
    echo "Remove them with: git worktree prune"
    exit 130
}
trap cleanup SIGINT SIGTERM

# --- Main Loop ---

PHASE_COUNT=${#PHASES[@]}
COMPLETED_PHASES=0
BLOCKED=false

for phase_index in "${!PHASES[@]}"; do
    PHASE_NAME="${PHASES[$phase_index]}"
    SLUG="$(slugify "$PHASE_NAME")"
    PHASE_BRANCH="ai/$SLUG"
    PHASE_WORKTREE="$ORIGINAL_DIR/$WORKTREE_DIR/$SLUG"

    log_step "Phase $((phase_index + 1))/$PHASE_COUNT: $PHASE_NAME"
    log_step "  Branch:    $PHASE_BRANCH"
    log_step "  Worktree:  $PHASE_WORKTREE"

    # Create worktree
    mkdir -p "$ORIGINAL_DIR/$WORKTREE_DIR"
    if ! git worktree add -b "$PHASE_BRANCH" "$PHASE_WORKTREE" main 2>/dev/null; then
        log_step "  Branch already exists, creating worktree from existing branch..."
        git worktree add "$PHASE_WORKTREE" "$PHASE_BRANCH" || {
            echo "ERROR: Could not create worktree for phase: $PHASE_NAME"
            exit 1
        }
    fi

    log_step "  Worktree created."

    # Build prompt and run claude -p
    PROMPT=$(build_prompt "$PHASE_NAME" "$MAX_RETRIES")

    log_step "  Dispatching autonomous session..."
    echo "------------------------------------------------"

    # Run in the worktree directory so files are read/written there
    OUTPUT=$(cd "$PHASE_WORKTREE" && claude -p --dangerously-skip-permissions "$PROMPT" 2>&1) || true

    echo "------------------------------------------------"
    log_step "  Session finished. Parsing result..."

    # Parse the PHASE_RESULT marker
    if echo "$OUTPUT" | grep -q "PHASE_RESULT: APPROVED"; then
        COMPLETED_TASKS=$(echo "$OUTPUT" | grep "COMPLETED_TASKS:" | head -1 | sed 's/COMPLETED_TASKS: //')
        log_step "  RESULT: APPROVED"
        log_step "  Tasks: $COMPLETED_TASKS"

        # Commit any remaining changes in the worktree
        cd "$PHASE_WORKTREE"
        if ! git diff --quiet || ! git diff --cached --quiet; then
            git add -A
            git commit -m "chore: finalize $PHASE_NAME

Co-Authored-By: Claude Code Pipeline <noreply@anthropic.com>" || true
        fi

        # Merge back to main
        cd "$ORIGINAL_DIR"
        git merge "$PHASE_BRANCH" --no-edit -m "merge: $PHASE_NAME

Co-Authored-By: Claude Code Pipeline <noreply@anthropic.com>" || {
            log_step "  WARNING: Merge conflict for $PHASE_BRANCH"
            log_step "  Worktree preserved at: $PHASE_WORKTREE"
            log_step "  Resolve conflicts manually, then:"
            log_step "    git worktree remove $PHASE_WORKTREE"
            log_step "    git branch -d $PHASE_BRANCH"
            BLOCKED=true
            break
        }

        # Clean up
        git worktree remove "$PHASE_WORKTREE" 2>/dev/null || {
            log_step "  WARNING: Could not remove worktree (may have leftover files)"
            log_step "  Manual cleanup: rm -rf $PHASE_WORKTREE && git worktree prune"
        }
        git branch -d "$PHASE_BRANCH" 2>/dev/null || true

        COMPLETED_PHASES=$((COMPLETED_PHASES + 1))
        log_step "  Merged to main. Worktree cleaned up."

    elif echo "$OUTPUT" | grep -q "PHASE_RESULT: BLOCKED"; then
        COMPLETED_TASKS=$(echo "$OUTPUT" | grep "COMPLETED_TASKS:" | head -1 | sed 's/COMPLETED_TASKS: //')
        FAILED_TASKS=$(echo "$OUTPUT" | grep "FAILED_TASKS:" | head -1 | sed 's/FAILED_TASKS: //')
        CRITIQUE=$(echo "$OUTPUT" | grep "CRITIQUE:" | head -1 | sed 's/CRITIQUE: //')

        echo ""
        echo "================================================"
        echo "  PHASE BLOCKED"
        echo "================================================"
        echo "  Phase:       $PHASE_NAME"
        echo "  Completed:   $COMPLETED_TASKS"
        echo "  Failed:      $FAILED_TASKS"
        echo "  Reason:      $CRITIQUE"
        echo "  Worktree:    $PHASE_WORKTREE (preserved)"
        echo "  Branch:      $PHASE_BRANCH"
        echo "================================================"

        # Append blocker to PROCESS.md
        echo "" >> "$PROCESS_FILE"
        echo "### Blocked: $PHASE_NAME — $(date -I)" >> "$PROCESS_FILE"
        echo "- **Failed task:** $FAILED_TASKS" >> "$PROCESS_FILE"
        echo "- **Reason:** $CRITIQUE" >> "$PROCESS_FILE"
        echo "- **Worktree:** $PHASE_WORKTREE" >> "$PROCESS_FILE"
        echo "- **Branch:** $PHASE_BRANCH" >> "$PROCESS_FILE"

        BLOCKED=true
        break

    elif echo "$OUTPUT" | grep -q "PHASE_RESULT: COMPLETE"; then
        log_step "  RESULT: COMPLETE (no tasks to do)"
        git worktree remove "$PHASE_WORKTREE" 2>/dev/null || true
        git branch -d "$PHASE_BRANCH" 2>/dev/null || true

    else
        echo ""
        log_step "  WARNING: No PHASE_RESULT marker found in output."
        log_step "  This may indicate a crash or timeout."
        log_step "  Worktree preserved at: $PHASE_WORKTREE"
        echo ""
        echo "Last 20 lines of output:"
        echo "$OUTPUT" | tail -20
        BLOCKED=true
        break
    fi

    echo ""
done

# --- Completion ---

echo ""
echo "================================================"

if [ "$BLOCKED" = true ]; then
    echo "  PIPELINE HALTED (BLOCKED)"
    echo "================================================"
    echo "  Phases completed: $COMPLETED_PHASES / $PHASE_COUNT"
    echo "  Check PROCESS.md for blocker details."
    echo "  Active worktrees:"
    git worktree list 2>/dev/null || true
    exit 1
fi

echo "  PIPELINE COMPLETE"
echo "================================================"
echo "  Phases: $COMPLETED_PHASES/$PHASE_COUNT"
echo "  Branch: main (clean)"
echo "  Started:  $PIPELINE_STARTED"
echo "  Finished: $(date -Iseconds)"
echo ""
echo "  Next steps:"
echo "    Review:   git log --oneline main"
echo "    Create PR: gh pr create --base main"
echo "================================================"
exit 0
