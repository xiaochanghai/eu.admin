-- Verify normalized Orchestration Run tables and character types.

SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'DocumentJson') IS NOT NULL
    THROW 51840, N'AgOrchestrationRun.DocumentJson still exists.', 1;

IF EXISTS (
    SELECT required.TableName
    FROM (VALUES
        (N'AgOrchestrationRun'),
        (N'AgOrchestrationRunNode'),
        (N'AgOrchestrationRunDetail'),
        (N'AgOrchestrationNodeAttempt'),
        (N'AgOrchestrationToolCall')) required(TableName)
    WHERE OBJECT_ID(N'dbo.' + required.TableName, N'U') IS NULL)
    THROW 51841, N'An Orchestration Run normalized table is missing.', 1;

IF EXISTS (
    SELECT 1
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id IN (
        OBJECT_ID(N'dbo.AgOrchestrationRun'),
        OBJECT_ID(N'dbo.AgOrchestrationRunNode'),
        OBJECT_ID(N'dbo.AgOrchestrationRunDetail'),
        OBJECT_ID(N'dbo.AgOrchestrationNodeAttempt'),
        OBJECT_ID(N'dbo.AgOrchestrationToolCall'))
      AND types.name IN (N'nchar', N'nvarchar', N'ntext'))
    THROW 51842, N'An Orchestration Run character column still uses an NVARCHAR-family type.', 1;

PRINT N'Orchestration Run normalized character types verified.';
GO
