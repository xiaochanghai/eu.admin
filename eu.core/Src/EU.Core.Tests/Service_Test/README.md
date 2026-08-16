# Agent persistence tests

The tests in this directory cover the Agent persistence services that use
SqlSugar through `IBaseRepository<T>`.

The regular tests use an in-memory SQLite database and do not access shared
infrastructure. Run them with:

```powershell
dotnet test Src\EU.Core.Tests\EU.Core.Tests.csproj --no-restore `
  --filter "FullyQualifiedName~AgChatConversationPersistence_Should|FullyQualifiedName~AgApiIdempotencyPersistence_Should|FullyQualifiedName~AgToolApprovalPersistence_Should"
```

`AgSqlServerPersistence_Should` is an opt-in integration test. It writes rows
with random identifiers to the configured Agent tables and deletes them in a
`finally` block. Pass the connection string only through the process
environment; do not add credentials to test source or tracked configuration.

```powershell
$env:EUCORE_AGENT_SQLSERVER_INTEGRATION = '<SQL Server connection string>'
dotnet test Src\EU.Core.Tests\EU.Core.Tests.csproj --no-restore `
  --filter "Category=SqlServerIntegration"
```

Run the SQL Server test only against an isolated development database whose
normalized Agent migrations have already completed.
