/*
P7 SQL Server deployment placeholder — never executed by this Host.

Current standalone persistence uses local SQLite aggregate records. Before
EU.Core.sln integration, translate these logical objects into the approved
agent schema and SqlSugar/operations conventions:

  agent.orchestration
  agent.orchestration_version
  agent.orchestration_node
  agent.orchestration_edge
  agent.orchestration_run
  agent.orchestration_node_run

Review row-version, immutable-version, retention, soft-delete, run-recovery,
indexing, and UTC timestamp conventions. Run audit must not store prompt,
intermediate output, final output, credentials, or MCP result content.

No SQL statements are supplied until the owner approves the EU.Core SQL Server
mapping. Creation and migration remain manual.
*/
