-- Finalize Evaluation Model Judgement normalization after generated data migration.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'DocumentJson') IS NULL
    THROW 51720, N'DocumentJson is absent; the Evaluation Model Judgement cutover was already finalized.', 1;
IF OBJECT_ID(N'dbo.AgEvaluationModelJudgementNormalizationCheckpoint', N'U') IS NULL
    THROW 51721, N'Evaluation Model Judgement normalization data script has not completed.', 1;
IF EXISTS (
    SELECT 1 FROM dbo.AgEvaluationModelJudgement source
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.AgEvaluationModelJudgementNormalizationCheckpoint checkpointRow
        WHERE checkpointRow.JudgementId = source.ID))
    THROW 51722, N'Evaluation Model Judgement normalization data script has not completed for every source row.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.AgEvaluationModelJudgement WHERE
        RequestedByUserId IS NULL OR SuiteId IS NULL OR SuiteVersionId IS NULL OR
        SuiteVersionContentSha256 IS NULL OR Provider IS NULL OR PackageVersion IS NULL OR
        ModelProfileId IS NULL OR PromptVersion IS NULL OR FinishedAtUtc IS NULL OR AdvisoryPassed IS NULL)
        THROW 51723, N'Evaluation Model Judgement normalized scalar data is incomplete.', 1;

    ALTER TABLE dbo.AgEvaluationModelJudgement DROP COLUMN DocumentJson;
    DROP TABLE dbo.AgEvaluationModelJudgementNormalizationCheckpoint;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
