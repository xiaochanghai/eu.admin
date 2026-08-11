# Agent database migrations

These baseline scripts create the Agent module in the shared EU.Core database.

- MySQL: 8.0.13 or later
- SQL Server: 2016 or later
- Table names use the EU.Core `Ag` module prefix.
- Column names use PascalCase. SQLite source columns use snake_case.
- The scripts create schema objects only; they do not copy SQLite rows.

## Table mapping

| SQLite source | EU.Core target |
|---|---|
| `agent_definitions` | `AgAgentDefinition` |
| `skill_definitions` | `AgSkillDefinition` |
| `mcp_server_definitions` | `AgMcpServerDefinition` |
| `knowledge_base_definitions` | `AgKnowledgeBaseDefinition` |
| `agent_run_audits` | `AgAgentRunAudit` |
| `agent_operation_audits` | `AgAgentOperationAudit` |
| `orchestration_definitions` | `AgOrchestrationDefinition` |
| `orchestration_runs` | `AgOrchestrationRun` |
| `orchestration_run_details` | `AgOrchestrationRunDetail` |
| `orchestration_node_attempts` | `AgOrchestrationNodeAttempt` |
| `orchestration_tool_calls` | `AgOrchestrationToolCall` |
| `chat_conversations` | `AgChatConversation` |
| `chat_messages` | `AgChatMessage` |
| `unified_entry_runs` | `AgUnifiedEntryRun` |
| `unified_agent_runs` | `AgUnifiedAgentRun` |
| `unified_orchestration_links` | `AgUnifiedOrchestrationLink` |
| `unified_tool_calls` | `AgUnifiedToolCall` |
| `unified_run_events` | `AgUnifiedRunEvent` |
| `main_agent_assignment` | `AgMainAgentAssignment` |
| `tool_approval_requests` | `AgToolApprovalRequest` |
| `tool_approval_payloads` | `AgToolApprovalPayload` |
| `tool_approval_decisions` | `AgToolApprovalDecision` |
| `tool_approval_execution_results` | `AgToolApprovalExecutionResult` |
| `evaluation_suites` | `AgEvaluationSuite` |
| `evaluation_batches` | `AgEvaluationBatch` |
| `evaluation_model_judgements` | `AgEvaluationModelJudgement` |
| `api_idempotency` | `AgApiIdempotency` |

Column mapping is mechanical snake_case to PascalCase, with acronym casing such as
`id` to `Id`, `mcp` to `Mcp`, `json` to `Json`, `sha256` to `Sha256`, `utf8` to
`Utf8`, and `utc` to `Utc`.

## Existing SQLite data

The generated SQL Server snapshot import is located at:

`SqlServer/Data/001_import_from_sqlite.sql`

This file contains live application data and is intentionally ignored by Git. Treat it
as sensitive migration material and remove it after the database cutover is accepted.

Run `SqlServer/001_initial_schema.sql` first, select the same target database, and then
run the data script. The data script requires all 27 target tables to be empty, imports
inside one transaction, and validates every target row count before commit.

To regenerate the data script from a newer SQLite snapshot, run from this directory:

```powershell
py -3 .\Tools\export_sqlite_to_sqlserver.py `
  ..\..\data\eu-core-agent.db `
  .\SqlServer\Data\001_import_from_sqlite.sql
```

For the final copy, stop writes to the Agent API and keep a backup of
`data/eu-core-agent.db`. Import parent rows before dependent rows:

1. Import definition, audit, orchestration, evaluation, assignment, and idempotency tables.
2. Import `AgChatConversation`.
3. Import `AgChatMessage` and `AgUnifiedEntryRun`.
4. Import the four `AgUnified*` child tables.
5. Import `AgToolApprovalRequest`.
6. Import the three tool-approval child tables.

After import, compare row counts and primary-key sets for all 27 tables before switching
the application storage provider. The application supports SQLite, SQL Server, and InMemory. After validation, switch with
`AgentStorage:Provider=SqlServer` and configure
`AgentStorage:ConnectionStringAlias=alias:agent-storage`. Store the real value only
as `AGENT_STORAGE_CONNECTION_AGENT_STORAGE` in the ignored `.env` or process
environment. The connection string is resolved outside `IConfiguration` so the
existing secret-leak validation remains effective.
