-- Verify normalized Agent API operation audit persistence.
SET NOCOUNT ON;
GO
IF OBJECT_ID(N'dbo.AgAgentOperationAudit', N'U') IS NULL
    THROW 52030, N'AgAgentOperationAudit is missing.', 1;
IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'DocumentJson') IS NOT NULL
    THROW 52031, N'AgAgentOperationAudit.DocumentJson still exists.', 1;
IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'AuditId') IS NOT NULL OR COL_LENGTH(N'dbo.AgAgentOperationAudit', N'ID') IS NULL
    THROW 52032, N'AgAgentOperationAudit identity was not normalized to ID.', 1;
IF NOT EXISTS (
    SELECT 1 FROM sys.key_constraints constraints
    INNER JOIN sys.index_columns indexColumns
      ON indexColumns.object_id = constraints.parent_object_id
     AND indexColumns.index_id = constraints.unique_index_id
    INNER JOIN sys.columns columns
      ON columns.object_id = indexColumns.object_id
     AND columns.column_id = indexColumns.column_id
    WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgAgentOperationAudit')
      AND constraints.[type] = N'PK' AND columns.name = N'ID')
    THROW 52036, N'AgAgentOperationAudit.ID is not the primary key.', 1;
IF EXISTS (
    SELECT 1 FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgAgentOperationAudit')
      AND types.name IN (N'nchar', N'nvarchar', N'ntext'))
    THROW 52033, N'AgAgentOperationAudit still contains an NVARCHAR-family column.', 1;
IF EXISTS (
    SELECT 1 FROM dbo.AgAgentOperationAudit
    WHERE TenantId IS NULL OR UserId IS NULL OR CorrelationId IS NULL OR Policy IS NULL
       OR Method IS NULL OR Path IS NULL OR StatusCode IS NULL OR Outcome IS NULL
       OR DurationMilliseconds IS NULL OR OccurredAtUtc IS NULL)
    THROW 52034, N'AgAgentOperationAudit contains incomplete rows.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentOperationAudit') AND name = N'ix_ag_agent_operation_audit_tenant_time')
    THROW 52035, N'AgAgentOperationAudit tenant/time index is missing.', 1;
PRINT N'Agent operation audit normalization verified.';
GO
