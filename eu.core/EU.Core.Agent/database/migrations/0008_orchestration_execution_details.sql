/*
P7 orchestration execution-detail migration handoff.

IMPORTANT
---------
This file is documentation-only. EU.Core.Agent.Api does not execute it and no
external database was contacted while it was prepared. The application
currently creates the equivalent local SQLite tables idempotently:

  orchestration_run_details
  orchestration_node_attempts
  orchestration_tool_calls

Review and translate the following logical model into the owner-approved
EU.Core schema/table naming, SqlSugar entities, collation, and operations
standards before manually applying it.

Logical keys and relationships
------------------------------
1. orchestration_run_details
   - primary key: run_id
   - owner: orchestration_id
   - permanent content: input_text, output_text

2. orchestration_node_attempts
   - primary key: (run_id, node_id, attempt)
   - ordered read index: (run_id, sequence)
   - permanent content: input_text, output_text
   - audit: agent_run_id, hashes, status, UTC timestamps, error_code

3. orchestration_tool_calls
   - primary key: tool_call_id
   - parent: (run_id, node_id, attempt)
   - ordered read index: (run_id, node_id, attempt, sequence)
   - permanent content: arguments_json, result_content
   - audit: Agent/tool-version IDs, status, result hash/length, UTC timestamps,
     error_code

Provider mapping
----------------
SQLite TEXT content:
  - MySQL 8: LONGTEXT using utf8mb4; use a case-sensitive/binary collation for
    identifiers where EU.Core identifier rules require it.
  - SQL Server: nvarchar(max).

SQLite identifier TEXT:
  - MySQL 8: char(36) ASCII/binary collation, or binary(16) only if the import
    process performs an explicit and verified GUID conversion.
  - SQL Server: uniqueidentifier.

SQLite INTEGER:
  - MySQL 8: int; result character counts may be bigint if operations requires.
  - SQL Server: int; use bigint only under the same reviewed convention.

SQLite ISO-8601 UTC TEXT:
  - MySQL 8: datetime(6), normalized to UTC by the importer.
  - SQL Server: datetimeoffset(7).

Import sequence
---------------
1. Freeze writes or take a consistent SQLite snapshot.
2. Import run details.
3. Import node attempts in sequence order.
4. Import tool calls in sequence order.
5. Verify row counts, all primary/parent keys, SHA-256 values, payload lengths,
   UTC timestamps, and representative UTF-8/Chinese payloads.
6. Point a staging Host at the reviewed provider implementation and compare the
   details API response with SQLite before any production cutover.

Security and retention
----------------------
- The application redacts credential-shaped JSON fields before persistence.
- API keys, Authorization values, passwords, secrets, tokens, credentials, and
  connection strings must never be imported if discovered in historical data.
- P7 execution details have permanent retention by product decision. Do not add
  automatic deletion without a separately approved retention specification.
- Access to full inputs, outputs, MCP arguments, and MCP results must be limited
  to the same operator boundary as the Agent management Host.

No executable SQL is supplied until the owner confirms the final EU.Core
SqlSugar and operations conventions.
*/
