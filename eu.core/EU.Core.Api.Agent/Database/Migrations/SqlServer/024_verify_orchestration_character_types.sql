-- Verify that normalized Orchestration definition tables contain VARCHAR only.
-- Run after Data/022 and 023. SQL Server 2014+.

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationDefinition', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationVersion', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationNode', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationEdge', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationAgentBinding', N'U') IS NULL
    THROW 51429, N'Orchestration normalized tables are missing.', 1;
IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'DocumentJson') IS NOT NULL
    THROW 51430, N'Orchestration normalization is not finalized. Run the generated data script and Data/022 first.', 1;

IF EXISTS (
    SELECT 1
    FROM sys.tables AS tableObject
    INNER JOIN sys.schemas AS schemaObject ON schemaObject.schema_id = tableObject.schema_id
    INNER JOIN sys.columns AS columnObject ON columnObject.object_id = tableObject.object_id
    INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
    WHERE schemaObject.name = N'dbo'
      AND tableObject.name IN (
          N'AgOrchestrationDefinition', N'AgOrchestrationVersion', N'AgOrchestrationNode',
          N'AgOrchestrationEdge', N'AgOrchestrationAgentBinding')
      AND typeObject.name IN (N'char', N'nchar', N'nvarchar'))
    THROW 51431, N'One or more Orchestration character columns are not VARCHAR.', 1;

PRINT N'All normalized Orchestration character columns use VARCHAR.';
GO
