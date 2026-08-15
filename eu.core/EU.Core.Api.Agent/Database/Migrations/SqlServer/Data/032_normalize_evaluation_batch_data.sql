-- Validate staged Evaluation Batch normalization and remove DocumentJson.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgEvaluationBatch', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgEvaluationBatchCase', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgEvaluationBatchCheck', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgEvaluationBatchObservation', N'U') IS NULL
    THROW 51620, N'Evaluation Batch normalized tables are missing.', 1;
IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'DocumentJson') IS NULL
BEGIN
    PRINT N'DocumentJson is already absent; the Evaluation Batch cutover was previously finalized.';
    RETURN;
END;
IF OBJECT_ID(N'dbo.AgEvaluationBatchNormalizationCheckpoint', N'U') IS NULL
    THROW 51621, N'Evaluation Batch normalization data script has not completed.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationBatch batch
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.AgEvaluationBatchNormalizationCheckpoint migrationCheckpoint
            WHERE migrationCheckpoint.BatchId = batch.ID))
        THROW 51622, N'One or more Evaluation Batches were not staged.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationBatch
        WHERE RequestedByUserId IS NULL OR SuiteVersionContentSha256 IS NULL
           OR Status IS NULL OR LogicalRevision IS NULL OR StartedAtUtc IS NULL OR ErrorCode IS NULL)
        THROW 51623, N'Evaluation Batch fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationBatchCase
        WHERE BatchId IS NULL OR Ordinal IS NULL OR CaseId IS NULL OR CaseName IS NULL
           OR TargetAgentId IS NULL OR TargetAgentVersionId IS NULL OR Status IS NULL
           OR UnifiedRunStatus IS NULL OR ErrorCode IS NULL OR ToolCallCount IS NULL
           OR OutputSha256 IS NULL)
        THROW 51624, N'Evaluation Batch Case fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationBatchCase
        WHERE ReportEvaluatedAtUtc IS NOT NULL
          AND (UnifiedRunId IS NULL OR ReportPassed IS NULL OR ReportScore IS NULL
               OR OutputUtf8Bytes IS NULL))
        THROW 51625, N'Evaluation Batch report fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationBatchCheck
        WHERE BatchId IS NULL OR BatchCaseId IS NULL OR Ordinal IS NULL OR Code IS NULL
           OR Passed IS NULL OR Expected IS NULL OR Actual IS NULL)
        THROW 51626, N'Evaluation Batch Check fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationBatchObservation
        WHERE BatchId IS NULL OR BatchCaseId IS NULL OR ObservationType IS NULL
           OR Ordinal IS NULL OR Value IS NULL)
        THROW 51627, N'Evaluation Batch Observation fields are incomplete.', 1;

    ALTER TABLE dbo.AgEvaluationBatch DROP COLUMN DocumentJson;
    DROP TABLE dbo.AgEvaluationBatchNormalizationCheckpoint;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
