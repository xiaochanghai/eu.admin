# P7 Agent orchestration acceptance summary

Date: 2026-07-29  
Status: **ACCEPTED**

## Delivered

- Controller-based orchestration Draft, publish, run, polling, history, and
  cancellation APIs.
- Bounded directed acyclic graph validation with reachable nodes and ordered
  conditional edges (`Always`, `Succeeded`, `Failed`, `OutputContains`).
- Agent nodes with initial-input, previous-output, or controlled-template
  mapping; templates expose only `{{input}}` and `{{previous}}`.
- Per-node 0–3 retry policy and 5–600 second timeout.
- Publish snapshots freeze current enabled Agent version IDs.
- Runtime rejects disabled, missing, or superseded Agent versions.
- Background execution with explicit cancellation and interrupted-Host
  recovery.
- Compact run-list audit retains SHA-256 hashes, output character counts,
  attempts, status, timing, and error codes.
- Normalized SQLite details permanently retain orchestration input/final output,
  every node-attempt input/Agent output, and MCP arguments/raw results.
- Credential-shaped JSON properties are recursively redacted before
  persistence; oversized content fails without truncation using
  `ORCHESTRATION_PAYLOAD_LIMIT_EXCEEDED`.
- SQLite and in-memory definition/run repositories.
- Operator flow editor and expandable node execution timeline with Attempt and
  MCP call details rendered without unsafe HTML.
- The output and details endpoints read persisted repository content and
  therefore survive Host reconstruction.
- SQL Server 0007 and provider-mapping 0008 forward/down files are
  documentation-only placeholders.
- Scheduling remains disabled and is assigned to P8.

## Acceptance boundary

- Release build: **0 warnings, 0 errors**.
- Main suite: **223 passed, 0 failed, 0 skipped**.
- Compatibility suite with real-model gates deliberately disabled:
  **98 passed, 0 failed, 12 environment-gated/skipped**.
- JavaScript syntax and unsafe-rendering scans passed.
- Automated real-provider gates remain environment-controlled; operator
  acceptance used the configured development model and MCP Server.
- Operator acceptance confirmed that the P7 orchestration, node output, MCP
  arguments, and MCP raw results render normally.
- No SQL Server connection, schema migration, or query is performed.
- PDF remains deferred.
