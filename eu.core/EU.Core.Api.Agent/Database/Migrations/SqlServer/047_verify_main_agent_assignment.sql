-- Verify normalized Main Agent assignment structure and character types.

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgMainAgentAssignment', N'U') IS NULL
    THROW 51910, N'AgMainAgentAssignment is missing.', 1;
IF EXISTS (SELECT 1 FROM dbo.AgMainAgentAssignment WHERE AssignmentKey <> 'platform-main-agent')
    THROW 51911, N'AgMainAgentAssignment contains an unsupported assignment key.', 1;
IF EXISTS (SELECT 1 FROM dbo.AgMainAgentAssignment WHERE ID IS NULL OR AgentId IS NULL OR AgentVersionId IS NULL OR LogicalRevision IS NULL OR UpdatedAtUtc IS NULL)
    THROW 51912, N'AgMainAgentAssignment contains incomplete normalized data.', 1;
IF EXISTS (
    SELECT 1
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgMainAgentAssignment')
      AND types.name IN (N'nchar', N'nvarchar', N'ntext'))
    THROW 51913, N'AgMainAgentAssignment still contains an NVARCHAR-family column.', 1;
IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints constraints
    INNER JOIN sys.index_columns indexColumns
      ON indexColumns.object_id = constraints.parent_object_id
     AND indexColumns.index_id = constraints.unique_index_id
    INNER JOIN sys.columns columns
      ON columns.object_id = indexColumns.object_id
     AND columns.column_id = indexColumns.column_id
    WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgMainAgentAssignment')
      AND constraints.[type] = N'PK'
      AND columns.name = N'ID')
    THROW 51914, N'AgMainAgentAssignment.ID is not the primary key.', 1;

PRINT N'Main Agent assignment normalization verified.';
GO
