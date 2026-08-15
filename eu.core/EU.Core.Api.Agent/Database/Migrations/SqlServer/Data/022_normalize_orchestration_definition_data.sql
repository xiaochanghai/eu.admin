-- Validate the staged Orchestration normalization and finalize the DocumentJson cutover.
-- Run 020, 021, and orchestration_normalized_data.generated.sql first. SQL Server 2014+.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationDefinition', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationVersion', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationNode', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationEdge', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationAgentBinding', N'U') IS NULL
    THROW 51420, N'Orchestration normalized tables are missing.', 1;
IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'DocumentJson') IS NULL
BEGIN
    PRINT N'DocumentJson is already absent; the Orchestration cutover was previously finalized.';
    RETURN;
END;
IF OBJECT_ID(N'dbo.AgOrchestrationNormalizationCheckpoint', N'U') IS NULL
    THROW 51421, N'Orchestration normalization data script has not completed.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationDefinition definition
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.AgOrchestrationNormalizationCheckpoint migrationCheckpoint
            WHERE migrationCheckpoint.OrchestrationId = definition.ID))
        THROW 51422, N'One or more Orchestration definitions were not staged.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationDefinition definition
        WHERE definition.Name IS NULL OR definition.Description IS NULL
           OR definition.Status IS NULL OR definition.LogicalRevision IS NULL)
        THROW 51423, N'Orchestration definition fields are incomplete.', 1;

    IF EXISTS (
        SELECT definition.ID
        FROM dbo.AgOrchestrationDefinition definition
        LEFT JOIN dbo.AgOrchestrationVersion version
          ON version.OrchestrationId = definition.ID
         AND version.IsDraft = 1
         AND version.IsDeleted = 0
        GROUP BY definition.ID
        HAVING COUNT(version.ID) <> 1)
        THROW 51424, N'Each Orchestration definition must contain exactly one draft version.', 1;

    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationVersion version
        WHERE version.OrchestrationId IS NULL OR version.Ordinal IS NULL
           OR version.Label IS NULL OR version.IsDraft IS NULL OR version.StartNodeId IS NULL)
        THROW 51425, N'Orchestration version fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationNode node
        WHERE node.OrchestrationId IS NULL OR node.VersionId IS NULL OR node.Ordinal IS NULL
           OR node.NodeId IS NULL OR node.Name IS NULL OR node.AgentId IS NULL
           OR node.InputMode IS NULL OR node.InputTemplate IS NULL
           OR node.MaximumRetries IS NULL OR node.TimeoutSeconds IS NULL)
        THROW 51426, N'Orchestration node fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationEdge edge
        WHERE edge.OrchestrationId IS NULL OR edge.VersionId IS NULL OR edge.Ordinal IS NULL
           OR edge.FromNodeId IS NULL OR edge.ToNodeId IS NULL OR edge.Condition IS NULL
           OR edge.ConditionValue IS NULL OR edge.SortOrder IS NULL)
        THROW 51427, N'Orchestration edge fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationAgentBinding binding
        WHERE binding.OrchestrationId IS NULL OR binding.VersionId IS NULL
           OR binding.Ordinal IS NULL OR binding.AgentId IS NULL OR binding.AgentVersionId IS NULL)
        THROW 51428, N'Orchestration Agent binding fields are incomplete.', 1;

    ALTER TABLE dbo.AgOrchestrationDefinition DROP COLUMN DocumentJson;
    DROP TABLE dbo.AgOrchestrationNormalizationCheckpoint;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
