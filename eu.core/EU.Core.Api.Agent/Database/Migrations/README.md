# Agent database migrations

These baseline scripts create the Agent module in the shared EU.Core database.

- MySQL: 8.0.13 or later
- SQL Server: 2014 or later
- Table names use the EU.Core `Ag` module prefix.
- Column names use PascalCase. SQLite source columns use snake_case.
- Skill cutover scripts retain existing Skill definitions and normalize their
  published-version metadata before removing the legacy aggregate document.

## Apply order

For a new SQL Server database, run:

1. `001_initial_schema.sql`
2. `002_add_basepoco_columns_ag_agent_definition.sql`
3. `003_normalize_agent_definition.sql`
4. `Data/004_normalize_agent_definition_data.sql` (generated from the current SQLite snapshot)
5. `005_add_agent_table_descriptions.sql` (optional, adds Chinese table and column descriptions)
6. `006_add_basepoco_and_fields_ag_skill_definition.sql`
7. `007_create_skill_version_tables.sql`
8. `Data/008_normalize_skill_definition_data.sql` (preserves normalized data and finalizes the schema)
9. `009_add_skill_table_descriptions.sql` (adds or updates Chinese table and column descriptions)

For a database where `001_initial_schema.sql` and the SQLite data import have already
been completed, back up the database and run `002`, then `003`. Stop Agent writes
until the migration and application deployment are complete.

The `002` migration is an idempotent pilot for `AgAgentDefinition` only. On SQL
Server it validates and converts a legacy character `Id` to `UNIQUEIDENTIFIER`,
preserving the primary-key name and clustered/nonclustered type, then adds the
missing non-key `BasePoco` columns. It stops before conversion if an invalid GUID,
a conversion collision, a foreign-key dependency, or an additional `Id` index is
found. MySQL keeps `Id CHAR(36)`, its native portable GUID representation.

The SQL Server `003` migration prepares these normalized Agent tables:

| Table | Purpose |
|---|---|
| `AgAgentDefinition` | Agent identity, display data, status, and logical revision |
| `AgAgentVersion` | Draft and published version configuration |
| `AgAgentVersionSnapshot` | Immutable published snapshot values |
| `AgAgentVersionBinding` | Ordered Skill, MCP tool, knowledge, child-Agent, and orchestration references |

All four tables use `ID UNIQUEIDENTIFIER` as the single physical primary key and
match EU.Core `BasePoco`. Business uniqueness such as one snapshot per version and
ordered binding identity is enforced with unique indexes/constraints instead of
using business columns as primary keys.

SQL Server 2014 does not provide `ISJSON`, `JSON_VALUE`, `JSON_QUERY`, or
`OPENJSON`. Therefore `001` does not create database JSON check constraints and
`003` only prepares the relational schema. It deliberately retains `DocumentJson`;
SQL Server 2014 cannot safely expand arbitrary JSON with native T-SQL. JSON validity
remains enforced by the application contracts. Populate the normalized detail
tables with pre-expanded ordinary `INSERT` statements before switching the runtime
catalog; do not use SQL Server 2016 JSON functions on this database.

`Data/004_normalize_agent_definition_data.sql` contains those pre-expanded rows for
the current `EU.Core.Api.Agent/data/eu-core-agent.db` snapshot. It verifies all six
Agent IDs and logical revisions, requires empty normalized detail tables, writes and
validates everything in one transaction, and only then removes `DocumentJson`. If
the SQL Server Agent data no longer matches that snapshot, it stops and rolls back.

After the detail data is populated, `AgAgentDefinitionServices` reads and writes the
four relational Agent-definition tables through the shared SqlSugar data source.
Runtime and lifecycle consumers use `IAgentDefinitionCatalog`; they no longer select
an Agent-definition repository through `AgentStorage:Provider`.
`OutputJsonSchema` remains because JSON Schema is itself a first-class Agent
setting, not an aggregate persistence document.

Apply the Skill normalization only after Agent create, save, publish, archive,
restore, and query flows have passed on the normalized Agent tables.

`CreatedTime` and `UpdateTime` remain nullable without database defaults. Existing
rows retain `NULL`; the migration must not invent historical audit times.

The SQL Server `006` migration prepares `AgSkillDefinition` for the EU.Core
`BasePoco`/SqlSugar model. It safely converts a legacy character `Id` to
`UNIQUEIDENTIFIER`, adds the common columns and the `Name`, `Description`,
`Category`, and `Status` fields, and retains `DocumentJson` so the existing
data remains available until `Data/008` performs the cutover. The SQL Server `007`
migration creates `AgSkillVersion` and `AgSkillVersionFile`. Existing JSON documents
are not expanded by these schema-only migrations because SQL Server 2014 has no
native JSON parser.

`Data/008_normalize_skill_definition_data.sql` assumes the three normalized Skill
tables have already been populated. It does not compare, insert, delete, or rebuild
business rows. It only fills null basic fields before applying required constraints
and removes `DocumentJson` in one SQL Server transaction. Back up the database and
stop Agent writes before running it.

After the data cutover, run `SqlServer/009_add_skill_table_descriptions.sql` to
add or update the Chinese `MS_Description` metadata for all three Skill tables
and every persisted column. The script is idempotent for existing descriptions.

For MySQL, run `003_add_basepoco_and_fields_ag_skill_definition.sql`,
`004_create_skill_version_tables.sql`, and
`005_normalize_skill_definition_data.sql` after `002`, followed by
`006_add_skill_table_descriptions.sql`. The final script adds or updates table and
column `COMMENT` metadata for all three normalized Skill tables. MySQL `005` expands
the existing JSON documents without snapshot identity checks before removing
`DocumentJson`. The schema keeps the portable `Id CHAR(36)` representation.
MySQL Agent normalized detail
tables must still be supplied for a target database before the SqlSugar catalog can
be used there. The checked-in
`SqlServer/003_normalize_agent_definition.sql` migration applies only to SQL Server storage.

After the Skill migration, `AgSkillDefinitionServices` reads and writes
`AgSkillDefinition`, `AgSkillVersion`, and `AgSkillVersionFile` through the shared
SqlSugar data source. `AgentStorage:Provider` no longer selects Skill-definition
storage. Draft Skill file contents remain owned by `ISkillFileStore`; the database
stores only definition metadata and immutable published file manifests.

## Table mapping

| SQLite source | EU.Core target |
|---|---|
| `agent_definitions` | `AgAgentDefinition` |
| `skill_definitions` | `AgSkillDefinition` |
| `skill_definitions.publishedVersions` | `AgSkillVersion` |
| `skill_definitions.publishedVersions.files` | `AgSkillVersionFile` |
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
the application storage provider. `AgentStorage:Provider` continues to support SQLite,
SQL Server, and InMemory for the remaining Agent platform repositories. Agent definitions,
versions, snapshots, and bindings always use the shared EU.Core SqlSugar data source.
After validation, switch the remaining repositories with
`AgentStorage:Provider=SqlServer` and configure
`AgentStorage:ConnectionStringAlias=alias:agent-storage`. Store the real value only
as `AGENT_STORAGE_CONNECTION_AGENT_STORAGE` in the ignored `.env` or process
environment. The connection string is resolved outside `IConfiguration` so the
existing secret-leak validation remains effective.
