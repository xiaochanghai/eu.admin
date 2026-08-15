-- Validate staged Orchestration Run normalization and remove DocumentJson.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationRun', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationRunNode', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationRunDetail', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationNodeAttempt', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationToolCall', N'U') IS NULL
    THROW 51830, N'Orchestration Run normalized tables are missing.', 1;
IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'DocumentJson') IS NULL
BEGIN
    PRINT N'DocumentJson is already absent; the Orchestration Run cutover was previously finalized.';
    RETURN;
END;
IF OBJECT_ID(N'dbo.AgOrchestrationRunNormalizationCheckpoint', N'U') IS NULL
    THROW 51831, N'Orchestration Run normalization data script has not completed.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationRun run
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint checkpointRow
            WHERE checkpointRow.RunId = run.ID))
        THROW 51832, N'One or more Orchestration Runs were not staged.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationRun
        WHERE OrchestrationVersionId IS NULL OR OrchestrationCode IS NULL OR Status IS NULL
           OR StartedAtUtc IS NULL OR InputSha256 IS NULL OR ErrorCode IS NULL)
        THROW 51833, N'Orchestration Run summary fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationRunNode
        WHERE RunId IS NULL OR Ordinal IS NULL OR NodeId IS NULL OR NodeName IS NULL
           OR AgentId IS NULL OR AgentVersionId IS NULL OR Status IS NULL OR Attempts IS NULL
           OR OutputCharacters IS NULL OR InputSha256 IS NULL OR ErrorCode IS NULL)
        THROW 51834, N'Orchestration Run node fields are incomplete.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunDetail WHERE ID IS NULL)
        THROW 51835, N'Orchestration Run detail identity is incomplete.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgOrchestrationNodeAttempt WHERE ID IS NULL)
        THROW 51836, N'Orchestration Node Attempt identity is incomplete.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgOrchestrationToolCall WHERE ID IS NULL)
        THROW 51837, N'Orchestration Tool Call identity is incomplete.', 1;

    ALTER TABLE dbo.AgOrchestrationRun DROP COLUMN DocumentJson;
    DROP TABLE dbo.AgOrchestrationRunNormalizationCheckpoint;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
