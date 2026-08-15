# Agent database migrations

These baseline scripts create the Agent module in the shared EU.Core database.

- MySQL: 8.0.13 or later
- SQL Server: 2014 or later
- Table names use the EU.Core `Ag` module prefix.
- Column names use PascalCase. SQLite source columns use snake_case.
- Skill cutover scripts retain existing Skill definitions and normalize their
  published-version metadata before removing the legacy aggregate document.
- MCP cutover scripts normalize Server configuration, ordered Stdio arguments,
  immutable tool history, and the ordered current-tool set.
- Orchestration cutover scripts normalize definitions, versions, graph nodes and
  edges, published Agent-version bindings, and runtime execution records.

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
10. `010_add_basepoco_and_fields_ag_mcp_server_definition.sql`
11. `011_create_mcp_tool_tables.sql`
12. `Data/012_normalize_mcp_server_definition_data.sql` (finalizes already-populated normalized data)
13. `013_add_mcp_table_descriptions.sql` (adds or updates Chinese table and column descriptions)
14. `014_convert_mcp_nvarchar_to_varchar.sql` (losslessly converts supported MCP `nvarchar` data columns to `varchar`)
15. `015_add_basepoco_and_fields_ag_knowledge_base_definition.sql`
16. `016_create_knowledge_document_tables.sql`
17. Generate and run `Data/knowledge_normalized_data.generated.sql` from the current SQL Server rows
18. `Data/017_normalize_knowledge_base_definition_data.sql`
19. `018_add_knowledge_table_descriptions.sql`
20. `019_convert_knowledge_text_to_varchar.sql` (idempotently converts pre-existing normalized text columns)
21. `020_add_basepoco_and_fields_ag_orchestration_definition.sql`
22. `021_create_orchestration_version_tables.sql`
23. Generate and run `Data/orchestration_normalized_data.generated.sql` from the current SQL Server rows
24. `Data/022_normalize_orchestration_definition_data.sql`
25. `023_add_orchestration_table_descriptions.sql`
26. `024_verify_orchestration_character_types.sql`
27. `025_add_basepoco_and_fields_ag_evaluation_suite.sql`
28. `026_create_evaluation_suite_version_tables.sql`
29. Generate and run `Data/evaluation_suite_normalized_data.generated.sql` from the current SQL Server rows
30. `Data/027_normalize_evaluation_suite_data.sql`
31. `028_add_evaluation_suite_table_descriptions.sql`
32. `029_verify_evaluation_suite_character_types.sql`
33. `030_add_basepoco_and_fields_ag_evaluation_batch.sql`
34. `031_create_evaluation_batch_detail_tables.sql`
35. Generate and run `Data/evaluation_batch_normalized_data.generated.sql` from the current SQL Server rows
36. `Data/032_normalize_evaluation_batch_data.sql`
37. `033_add_evaluation_batch_table_descriptions.sql`
38. `034_verify_evaluation_batch_character_types.sql`
39. `035_add_basepoco_and_fields_ag_evaluation_model_judgement.sql`
40. `036_create_evaluation_model_judgement_detail_tables.sql`
41. Generate and run `Data/evaluation_model_judgement_normalized_data.generated.sql` from the current SQL Server rows
42. `Data/037_normalize_evaluation_model_judgement_data.sql`
43. `038_add_evaluation_model_judgement_table_descriptions.sql`
44. `039_verify_evaluation_model_judgement_character_types.sql`
45. `040_add_basepoco_and_fields_ag_orchestration_run.sql`
46. `041_normalize_orchestration_run_detail_tables.sql`
47. Generate and run `Data/orchestration_run_normalized_data.generated.sql` from the current SQL Server rows
48. `Data/042_normalize_orchestration_run_data.sql`
49. `043_add_orchestration_run_table_descriptions.sql`
50. `044_verify_orchestration_run_character_types.sql`
51. `045_normalize_main_agent_assignment.sql`
52. `046_add_main_agent_assignment_descriptions.sql`
53. `047_verify_main_agent_assignment.sql`
54. `048_prepare_normalized_agent_run_audit.sql`
55. Generate and run `Data/agent_run_audit_normalized_data.generated.sql` from the current SQL Server rows
56. `Data/049_normalize_agent_run_audit_data.sql`
57. `050_add_agent_run_audit_descriptions.sql`
58. `051_verify_agent_run_audit.sql`

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

Apply the MCP normalization only after the Skill flow has been accepted. For SQL
Server, run `010`, then `011`, generate and run the ordinary MCP normalization
`UPDATE`/`INSERT` script shown below, and only then run `Data/012`. SQL Server 2014
cannot expand `DocumentJson` itself. `Data/012` refuses to remove `DocumentJson`
unless the generated normalization script completed its checkpoint and all basic
fields are populated; it never substitutes placeholder MCP configuration. Stop
Agent writes and back up the database for the cutover. Run `013` for idempotent
Chinese table and column descriptions, then run `014` for the final string-type
conversion.

For MySQL, run `007_add_basepoco_and_fields_ag_mcp_server_definition.sql`,
`008_create_mcp_tool_tables.sql`, `009_normalize_mcp_server_definition_data.sql`,
and `010_add_mcp_table_descriptions.sql` in that order. MySQL `009` expands the
existing JSON document directly into the three normalized tables before removing
`DocumentJson`.

After the MCP cutover, `AgMcpServerDefinitionServices` owns lifecycle behavior and
reads and writes `AgMcpServerDefinition`, `AgMcpServerArgument`, and
`AgMcpToolVersion` through SqlSugar. Runtime consumers use the read-only
`IMcpServerDefinitionCatalog`; the MCP repository implementations and
`AgentStorage:Provider` selection no longer participate in MCP-definition storage.

## Table mapping

| SQLite source | EU.Core target |
|---|---|
| `agent_definitions` | `AgAgentDefinition` |
| `skill_definitions` | `AgSkillDefinition` |
| `skill_definitions.publishedVersions` | `AgSkillVersion` |
| `skill_definitions.publishedVersions.files` | `AgSkillVersionFile` |
| `mcp_server_definitions` | `AgMcpServerDefinition` |
| `mcp_server_definitions.arguments` | `AgMcpServerArgument` |
| `mcp_server_definitions.toolVersions` | `AgMcpToolVersion` |
| `knowledge_base_definitions` | `AgKnowledgeBaseDefinition` |
| `agent_run_audits` | `AgAgentRunAudit` |
| `agent_run_audits.toolCalls` | `AgAgentToolCallAudit` |
| `agent_operation_audits` | `AgAgentOperationAudit` |
| `orchestration_definitions` | `AgOrchestrationDefinition` |
| `orchestration_definitions.draft/publishedVersions` | `AgOrchestrationVersion` |
| `orchestration_definitions.*.nodes` | `AgOrchestrationNode` |
| `orchestration_definitions.*.edges` | `AgOrchestrationEdge` |
| `orchestration_definitions.publishedVersions.snapshot.agents` | `AgOrchestrationAgentBinding` |
| `orchestration_runs` | `AgOrchestrationRun` |
| `orchestration_run_details` | `AgOrchestrationRunDetail` |
| `orchestration_runs.nodes` | `AgOrchestrationRunNode` |
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
| `evaluation_suites.draft/publishedVersions` | `AgEvaluationSuiteVersion` |
| `evaluation_suites.*.cases` | `AgEvaluationCase` |
| `evaluation_suites.*.cases.specification rules` | `AgEvaluationCaseRule` |
| `evaluation_batches` | `AgEvaluationBatch` |
| `evaluation_batches.cases` | `AgEvaluationBatchCase` |
| `evaluation_batches.cases.report.checks` | `AgEvaluationBatchCheck` |
| `evaluation_batches.cases.observedEventKinds/observedRoutes` | `AgEvaluationBatchObservation` |
| `evaluation_model_judgements` | `AgEvaluationModelJudgement` |
| `evaluation_model_judgements.evaluators` | `AgEvaluationModelJudgementEvaluator` |
| `evaluation_model_judgements.minimumScores` | `AgEvaluationModelJudgementMinimumScore` |
| `evaluation_model_judgements.cases` | `AgEvaluationModelJudgementCase` |
| `evaluation_model_judgements.cases.metrics` | `AgEvaluationModelJudgementMetric` |
| `evaluation_model_judgements.cases.metrics.diagnosticCodes` | `AgEvaluationModelJudgementDiagnostic` |
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

For an existing SQL Server database, generate only the MCP normalization script:

```powershell
py -3 .\Tools\export_sqlite_mcp_to_sqlserver.py `
  ..\..\data\eu-core-agent.db `
  .\SqlServer\Data\mcp_normalized_data.generated.sql
```

Run the generated `mcp_normalized_data.generated.sql` after `010` and `011`; do not
rerun the initial full-database import. Then run `Data/012`, `013`, and `014`.
The final `014` migration validates the current SQL Server collation and the encoded
byte length of every value before converting supported `nvarchar` columns in the
three MCP tables to their declared `varchar` length. `LastError` uses
`varchar(4096)`. `AgMcpToolVersion.Description` remains `nvarchar(max)` because
SQL Server 2014 code pages cannot losslessly represent every imported Unicode tool
description. The migration rolls back rather than replacing unsupported characters
or truncating data.

For Knowledge Base normalization, run `015` and `016`, stop Agent API writes, and
generate the data-only script from the current SQL Server
`AgKnowledgeBaseDefinition.DocumentJson` rows. Put a read-capable ODBC connection
string in the process environment; never pass or commit it as a command argument:

```powershell
py -3 .\Tools\export_sqlserver_knowledge_to_sqlserver.py `
  .\SqlServer\Data\knowledge_normalized_data.generated.sql
```

The default variable name is `KNOWLEDGE_MIGRATION_SQLSERVER_ODBC`; use
`--connection-env <name>` when the secret manager exposes a different variable.
The exporter maps historical numeric statuses (`0`, `1`, `2`) to `Enabled`,
`Disabled`, and `Archived`, and the generated SQL verifies each definition's code
and logical revision before modifying normalized rows.

The three finalized Knowledge tables use `varchar` for persisted text columns.
Migration `015` checks the legacy JSON and code, while `016` checks any existing
normalized rows, and stops if the current SQL Server collation cannot represent a
value losslessly. `DocumentJson` remains `nvarchar(max)` only during the transition
and is removed by `Data/017`.

Run the generated script and then `Data/017`, followed by `018`. The generated file
contains document content and is intentionally ignored by Git. Keep the Agent API
stopped from generation through `Data/017`, and retain a database backup until list,
document, chunk, search, Agent binding, disable, and archive checks have passed.
Re-running `Data/017` after a completed cutover is a no-op and prints an
already-finalized message.

For Orchestration definition normalization, run `020` and `021`, stop Agent API
writes, and generate the data-only script from the current SQL Server aggregate
rows:

```powershell
py -3 .\Tools\export_sqlserver_orchestration_to_sqlserver.py `
  .\SqlServer\Data\orchestration_normalized_data.generated.sql
```

The default connection environment variable is
`ORCHESTRATION_MIGRATION_SQLSERVER_ODBC`. The exporter maps historical numeric enum
values to contract names and emits deterministic child-row IDs. Run the generated
script, then `Data/022`, `023`, and `024`. Do not run the generated script after
`Data/022` removes `DocumentJson`.

After the cutover, `AgOrchestrationDefinitionServices` implements both
`IOrchestrationRepository` and `IPublishedOrchestrationCatalog` through SqlSugar.
`AgentStorage:Provider` no longer selects Orchestration-definition persistence.

For Evaluation Suite normalization, run `025` and `026`, stop Agent API writes,
and generate the data-only script from the current SQL Server aggregate rows:

```powershell
py -3 .\Tools\export_sqlserver_evaluation_suite_to_sqlserver.py `
  .\SqlServer\Data\evaluation_suite_normalized_data.generated.sql
```

The default connection environment variable is
`EVALUATION_SUITE_MIGRATION_SQLSERVER_ODBC`. Run the generated script, then
`Data/027`, `028`, and `029`. The generated script validates source identity,
logical revision, and lossless conversion to `varchar`. Do not run it after
`Data/027` removes `DocumentJson`.

After this cutover, `AgEvaluationSuiteServices` implements
`IEvaluationSuiteRepository` through SqlSugar. Evaluation cases and their ordered
contains, excludes, and required-event rules are stored in normalized child rows.
The Evaluation Suite cutover itself does not change Evaluation Batch or Model Judge persistence.

For Evaluation Batch normalization, run `030` and `031`, stop Agent API writes,
and generate the data-only script from the current SQL Server aggregate rows:

```powershell
py -3 .\Tools\export_sqlserver_evaluation_batch_to_sqlserver.py `
  .\SqlServer\Data\evaluation_batch_normalized_data.generated.sql
```

The default connection environment variable is
`EVALUATION_BATCH_MIGRATION_SQLSERVER_ODBC`. Run the generated script, then
`Data/032`, `033`, and `034`. After this cutover,
`AgEvaluationBatchServices` implements both `IEvaluationBatchRepository` and
`IEvaluationBatchRecovery` through SqlSugar. Case reports, ordered checks, event
kinds, and routes are stored in normalized child rows. Model Judge persistence
remains unchanged.

For Evaluation Model Judgement normalization, run `035` and `036`, stop Agent API
writes, and generate the data-only script from the current SQL Server aggregate rows:

```powershell
py -3 .\Tools\export_sqlserver_evaluation_model_judgement_to_sqlserver.py `
  .\SqlServer\Data\evaluation_model_judgement_normalized_data.generated.sql
```

The default connection environment variable is
`EVALUATION_MODEL_JUDGEMENT_MIGRATION_SQLSERVER_ODBC`. Run the generated script,
then `Data/037`, `038`, and `039`. The generated script validates source identity
and lossless conversion to `varchar`; do not run it after `Data/037` removes
`DocumentJson`.

After this cutover, `AgEvaluationModelJudgementServices` implements
`IModelJudgeReportRepository` through SqlSugar. Evaluators, minimum scores, cases,
metrics, and diagnostic codes are stored in normalized ordered child rows.

For Orchestration Run normalization, run `040` and `041`, stop Agent API writes,
and generate the data-only script from the current SQL Server run documents:

```powershell
py -3 .\Tools\export_sqlserver_orchestration_run_to_sqlserver.py `
  .\SqlServer\Data\orchestration_run_normalized_data.generated.sql
```

The default connection environment variable is
`ORCHESTRATION_RUN_MIGRATION_SQLSERVER_ODBC`. Existing `CHAR(36)` identifier
columns remain unchanged. Run the generated script, then `Data/042`, `043`, and
`044`. Do not run the generated script after `Data/042` removes `DocumentJson`.

After this cutover, `AgOrchestrationRunServices` implements
`IOrchestrationRunRepository` through SqlSugar. Run summaries and ordered node
summaries are normalized, while execution details, node attempts, and tool calls
continue in their existing dedicated tables with BasePoco fields and `varchar`
text storage. Terminal transitions, conditional detail saves, and interrupted-run
recovery remain transactional.

For Main Agent assignment normalization, stop Agent API writes and run `045`,
`046`, and `047` in order. No generated data script is required because the
existing assignment columns are converted in place. Existing `CHAR(36)` Agent
identifier columns remain unchanged.

After this cutover, `AgMainAgentAssignmentServices` implements
`IMainAgentAssignmentRepository` through SqlSugar. The fixed
`platform-main-agent` business key remains unique, while `LogicalRevision`
continues to provide optimistic concurrency control.

For Agent run audit normalization, run `048`, stop Agent API writes, and generate
the data-only script from the current SQL Server audit documents:

```powershell
py -3 .\Tools\export_sqlserver_agent_run_audit_to_sqlserver.py `
  .\SqlServer\Data\agent_run_audit_normalized_data.generated.sql
```

The default connection environment variable is
`AGENT_RUN_AUDIT_MIGRATION_SQLSERVER_ODBC`. Existing `CHAR(36)` identifier
columns remain unchanged; the run key column is renamed from `RunId` to the
`BasePoco` key name `ID`. Run the generated script, then `Data/049`, `050`, and
`051`. Do not run the generated script after `Data/049` removes `DocumentJson`.

After this cutover, `AgAgentRunAuditServices` implements
`IAgentRunAuditRepository` through SqlSugar. Run summaries remain in
`AgAgentRunAudit`, ordered tool-call audit rows are stored in
`AgAgentToolCallAudit`, and `AgentStorage:Provider` no longer selects Agent run
audit persistence.


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
SQL Server, and InMemory for the remaining Agent platform repositories. Agent and
Orchestration definitions, versions, snapshots, and bindings always use the shared
EU.Core SqlSugar data source.
After validation, switch the remaining repositories with
`AgentStorage:Provider=SqlServer` and configure
`AgentStorage:ConnectionStringAlias=alias:agent-storage`. Store the real value only
as `AGENT_STORAGE_CONNECTION_AGENT_STORAGE` in the ignored `.env` or process
environment. The connection string is resolved outside `IConfiguration` so the
existing secret-leak validation remains effective.
