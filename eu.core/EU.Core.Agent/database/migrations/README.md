# Manual database migrations

These files are hand-applied SQL Server migrations for SQL Server 2014 and
databases using compatibility level 120. The Host does not connect to a
database to apply them: no script is automatically executed by the Host. The
scripts are not automatically executed during Host startup or by any Host path.

## Prerequisites

- Obtain change approval and a backup or restore point appropriate to the
  target database.
- Use an operator account authorized to create the `agent` schema. No Agent
  object belongs in `dbo`.
- Make sure one operator owns the migration window. Checksum validation,
  concurrency serialization, and execution evidence are operator
  responsibilities until a later runner is explicitly authorized.
- Review the exact script bytes that will be applied. Do not edit a script
  after its SHA-256 value has been recorded.

## Required manual order

1. Calculate the SHA-256 for `0000_migration_history.sql`.
2. Apply `0000_migration_history.sql`.
3. Calculate the SHA-256 for `0001_agent_schema.sql`.
4. Apply `0001_agent_schema.sql`.
5. Record the hashes and execution evidence in the operator-controlled evidence record.

`0000` is an operator-owned placeholder only. This codebase does not define
migration-record storage, metadata, or any other database structure for the
operator.

## SHA-256 recording procedure

From the directory containing the exact files to be applied, calculate each
hash before execution:

```powershell
Get-FileHash -Algorithm SHA256 .\0000_migration_history.sql
Get-FileHash -Algorithm SHA256 .\0001_agent_schema.sql
```

Compare each result with the operator-controlled evidence record before
execution. After a successful application, record the exact hash, approval
reference, UTC execution time, operator identity, and verification-query
results in that record. The operator, not this codebase, owns the storage and
validation rules for those records.

## Verification queries

After applying `0001`, verify the schema placement:

```sql
SELECT [name]
FROM sys.schemas
WHERE [name] = N'agent';

SELECT [schema].[name] AS [SchemaName], [object].[name] AS [ObjectName], [object].[type_desc] AS [ObjectType]
FROM sys.objects AS [object]
INNER JOIN sys.schemas AS [schema] ON [schema].[schema_id] = [object].[schema_id]
WHERE [schema].[name] = N'agent'
ORDER BY [object].[name];
```

The first query must return `agent`. The second query must show Agent objects
only under `agent`; this P1 foundation creates no domain objects beyond the
schema itself.

## Rollback cautions

`down/0001_agent_schema.down.sql` is deliberately a non-destructive
operator-owned placeholder. It changes nothing. Review later migration
ownership, use the verification queries, and perform any destructive database
change only through a separately approved and rehearsed procedure.
