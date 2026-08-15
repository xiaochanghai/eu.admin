-- Verify normalized Agent run audit tables and character types.

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgAgentRunAudit', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgAgentToolCallAudit', N'U') IS NULL
    THROW 51940, N'An Agent run audit normalized table is missing.', 1;
IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'DocumentJson') IS NOT NULL
    THROW 51941, N'AgAgentRunAudit.DocumentJson still exists.', 1;
IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'RunId') IS NOT NULL
   OR COL_LENGTH(N'dbo.AgAgentRunAudit', N'ID') IS NULL
    THROW 51942, N'AgAgentRunAudit primary identity was not normalized to ID.', 1;
IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints constraints
    INNER JOIN sys.index_columns indexColumns
      ON indexColumns.object_id = constraints.parent_object_id
     AND indexColumns.index_id = constraints.unique_index_id
    INNER JOIN sys.columns columns
      ON columns.object_id = indexColumns.object_id
     AND columns.column_id = indexColumns.column_id
    WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgAgentRunAudit')
      AND constraints.[type] = N'PK'
      AND columns.name = N'ID')
    THROW 51943, N'AgAgentRunAudit.ID is not the primary key.', 1;
IF EXISTS (
    SELECT 1
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id IN (
        OBJECT_ID(N'dbo.AgAgentRunAudit'),
        OBJECT_ID(N'dbo.AgAgentToolCallAudit'))
      AND types.name IN (N'nchar', N'nvarchar', N'ntext'))
    THROW 51944, N'An Agent run audit character column still uses an NVARCHAR-family type.', 1;
IF EXISTS (
    SELECT 1 FROM dbo.AgAgentRunAudit auditRow
    OUTER APPLY (
        SELECT COUNT_BIG(*) AS ActualCount
        FROM dbo.AgAgentToolCallAudit toolCall
        WHERE toolCall.RunId = auditRow.ID AND toolCall.IsDeleted = 0) counts
    WHERE CONVERT(BIGINT, auditRow.ToolCallCount) <> counts.ActualCount)
    THROW 51945, N'Agent run audit tool-call counts do not match normalized rows.', 1;

PRINT N'Agent run audit normalization verified.';
GO
