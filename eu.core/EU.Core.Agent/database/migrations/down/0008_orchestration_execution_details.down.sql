/*
P7 orchestration execution-detail rollback handoff.

The forward file is documentation-only and creates no external database
objects, so this file intentionally executes nothing.

When an owner-approved forward migration is added, its reviewed rollback must:
  1. remove orchestration_tool_calls first;
  2. remove orchestration_node_attempts second;
  3. remove orchestration_run_details last;
  4. preserve/export retained execution content before any destructive action.

Never run a destructive rollback automatically.
*/
