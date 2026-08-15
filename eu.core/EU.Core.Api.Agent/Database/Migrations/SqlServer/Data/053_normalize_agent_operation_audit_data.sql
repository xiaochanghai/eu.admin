-- Validate staged Agent operation audit normalization and remove DocumentJson.
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgAgentOperationAudit', N'U') IS NULL
    THROW 52020, N'AgAgentOperationAudit is missing.', 1;
IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'DocumentJson') IS NULL
BEGIN
    PRINT N'DocumentJson is already absent; the operation audit cutover was previously finalized.';
    RETURN;
END;
IF OBJECT_ID(N'dbo.AgAgentOperationAuditNormalizationCheckpoint', N'U') IS NULL
    THROW 52021, N'Agent operation audit normalization data script has not completed.', 1;
GO

IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'DocumentJson') IS NULL RETURN;
BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (
        SELECT 1 FROM dbo.AgAgentOperationAudit auditRow
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.AgAgentOperationAuditNormalizationCheckpoint checkpointRow
            WHERE checkpointRow.ID = auditRow.ID))
        THROW 52022, N'One or more Agent operation audits were not staged.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgAgentOperationAudit
        WHERE TenantId IS NULL OR UserId IS NULL OR CorrelationId IS NULL OR Policy IS NULL
           OR Method IS NULL OR Path IS NULL OR StatusCode IS NULL OR Outcome IS NULL
           OR DurationMilliseconds IS NULL OR OccurredAtUtc IS NULL)
        THROW 52023, N'Agent operation audit normalized fields are incomplete.', 1;

    ALTER TABLE dbo.AgAgentOperationAudit DROP COLUMN DocumentJson;
    DROP TABLE dbo.AgAgentOperationAuditNormalizationCheckpoint;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
