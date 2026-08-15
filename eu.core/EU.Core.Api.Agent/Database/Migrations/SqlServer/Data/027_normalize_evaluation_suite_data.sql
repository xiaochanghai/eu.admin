-- Validate staged Evaluation Suite normalization and remove DocumentJson.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgEvaluationSuite', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgEvaluationSuiteVersion', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgEvaluationCase', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgEvaluationCaseRule', N'U') IS NULL
    THROW 51520, N'Evaluation Suite normalized tables are missing.', 1;
IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'DocumentJson') IS NULL
BEGIN
    PRINT N'DocumentJson is already absent; the Evaluation Suite cutover was previously finalized.';
    RETURN;
END;
IF OBJECT_ID(N'dbo.AgEvaluationSuiteNormalizationCheckpoint', N'U') IS NULL
    THROW 51521, N'Evaluation Suite normalization data script has not completed.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationSuite suite
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.AgEvaluationSuiteNormalizationCheckpoint migrationCheckpoint
            WHERE migrationCheckpoint.SuiteId = suite.ID))
        THROW 51522, N'One or more Evaluation Suites were not staged.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationSuite
        WHERE Name IS NULL OR Description IS NULL OR Status IS NULL
           OR LogicalRevision IS NULL OR CreatedAtUtc IS NULL OR UpdatedAtUtc IS NULL
           OR CreatedByUserId IS NULL OR UpdatedByUserId IS NULL)
        THROW 51523, N'Evaluation Suite fields are incomplete.', 1;
    IF EXISTS (
        SELECT suite.ID FROM dbo.AgEvaluationSuite suite
        LEFT JOIN dbo.AgEvaluationSuiteVersion version
          ON version.SuiteId = suite.ID AND version.IsDraft = 1 AND version.IsDeleted = 0
        GROUP BY suite.ID HAVING COUNT(version.ID) <> 1)
        THROW 51524, N'Each Evaluation Suite must contain exactly one draft version.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationSuiteVersion
        WHERE SuiteId IS NULL OR Ordinal IS NULL OR Label IS NULL OR IsDraft IS NULL
           OR ContentSha256 IS NULL OR PublishedByUserId IS NULL)
        THROW 51525, N'Evaluation Suite version fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationSuiteVersion
        WHERE IsDraft = 0 AND PublishedAtUtc IS NULL)
        THROW 51526, N'Published Evaluation Suite versions require a publish time.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationCase
        WHERE SuiteId IS NULL OR VersionId IS NULL OR Ordinal IS NULL OR CaseId IS NULL
           OR Name IS NULL OR Input IS NULL OR TargetAgentId IS NULL OR TargetAgentVersionId IS NULL)
        THROW 51527, N'Evaluation Case fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationCaseRule
        WHERE SuiteId IS NULL OR VersionId IS NULL OR EvaluationCaseId IS NULL
           OR RuleType IS NULL OR Ordinal IS NULL OR Value IS NULL)
        THROW 51528, N'Evaluation Case rule fields are incomplete.', 1;

    ALTER TABLE dbo.AgEvaluationSuite DROP COLUMN DocumentJson;
    DROP TABLE dbo.AgEvaluationSuiteNormalizationCheckpoint;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
