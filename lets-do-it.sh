#!/bin/bash

# --- Configuration & Parameters ---
# Usage: ./lets-do-it.sh [max_iterations] [plan_file] [process_file]
MAX_ITERATIONS=${1:-10}
PLAN_FILE=${2:-"PLAN.md"}
PROCESS_FILE=${3:-"PROCESS.md"}

# Internal State
ITERATION_COUNT=0
PREVIOUS_CRITIQUE="None"

echo "🚀 Pipeline 'lets-do-it' initiated"
echo "📍 Max Iterations: $MAX_ITERATIONS"
echo "📍 Plan File:      $PLAN_FILE"
echo "📍 Process File:   $PROCESS_FILE"
echo "------------------------------------------------"

while [ $ITERATION_COUNT -lt $MAX_ITERATIONS ]; do
    ITERATION_COUNT=$((ITERATION_COUNT + 1))
    
    echo -e "\n--- 🔄 Loop $ITERATION_COUNT of $MAX_ITERATIONS: EXECUTION ---"

    # STEP 1: The Doer
    # We call this directly so its output streams to the terminal in real-time.
    claude -p --dangerously-skip-permissions "/using-superpowers @$PLAN_FILE @$PROCESS_FILE
              PREVIOUS_CRITIQUE: \"$PREVIOUS_CRITIQUE\"
              
              INSTRUCTIONS:
              1. If PREVIOUS_CRITIQUE is NOT 'None', your ONLY goal is to fix those issues. 
                 Do NOT start any new tasks from $PLAN_FILE.
              2. If PREVIOUS_CRITIQUE is 'None', pick exactly ONE simple task from @$PLAN_FILE, 
                 mark it as [/] in the file, and implement it.
              3. Use /test-driven-development and @\"backend-coder\" or @\"mobile-coder\" for the implementation.
              4. DO NOT commit. Finish after code is written and tests pass."

    echo -e "\n--- 🧐 Loop $ITERATION_COUNT: CRITIQUE ---"

    # STEP 2: The Critic
    # We use 'tee /dev/tty' to print the output to the screen while capturing it in the variable.
    CURRENT_CRITIQUE=$(claude -p --dangerously-skip-permissions "/using-superpowers @$PLAN_FILE @$PROCESS_FILE
                                 INSTRUCTIONS:
                                 1. Review the code changes made in the last step.
                                 2. Run /code-review and /simplify.
                                 3. If the code is simple, correct, and tests pass, reply ONLY with 'APPROVED'.
                                 4. If there are any issues, list them clearly as bullet points for the next dev loop." | tee /dev/tty)

    # STEP 3: Logic Gate & Persistence
    if [[ "$CURRENT_CRITIQUE" == *"APPROVED"* ]]; then
        echo -e "\n✅ STATUS: APPROVED! Finalizing Task..."
        
        # STEP 4: Finalize
        claude -p --dangerously-skip-permissions "The Critic has APPROVED the work.
                  1. Update @$PLAN_FILE: Change the current [/] task to [x].
                  2. Update @$PROCESS_FILE: Note the completion of this task and any key technical decisions.
                  3. Commit the changes with a clear message.
                  4. Finish."

        PREVIOUS_CRITIQUE="None" 

        # Check if any tasks remain
        if ! grep -q "\[ \]" "$PLAN_FILE"; then
            echo -e "\n🏁 ALL TASKS COMPLETE. Pipeline exiting successfully."
            exit 0
        fi
    else
        echo -e "\n❌ STATUS: REJECTED. Issues detected."
        PREVIOUS_CRITIQUE="$CURRENT_CRITIQUE"
        
        # Log failure to PROCESS.md
        echo "Iteration $ITERATION_COUNT: Critique failed. Issues: $PREVIOUS_CRITIQUE" >> "$PROCESS_FILE"
    fi

    sleep 2
done

if [ $ITERATION_COUNT -eq $MAX_ITERATIONS ]; then
    echo -e "\n⚠️  Max iterations reached. Pipeline paused for human intervention."
fi