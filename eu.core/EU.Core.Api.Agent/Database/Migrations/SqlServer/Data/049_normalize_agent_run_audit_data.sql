-- Validate staged Agent run audit normalization and remove DocumentJson.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgAgentRunAudit', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgAgentToolCallAudit', N'U') IS NULL
    THROW 51930, N'Agent run audit normalized tables are missing.', 1;
IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'DocumentJson') IS NULL
BEGIN
    PRINT N'DocumentJson is already absent; the Agent run audit cutover was previously finalized.';
    RETURN;
END;
IF OBJECT_ID(N'dbo.AgAgentRunAuditNormalizationCheckpoint', N'U') IS NULL
    THROW 51931, N'Agent run audit normalization data script has not completed.', 1;
GO

IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'DocumentJson') IS NULL
    RETURN;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1 FROM dbo.AgAgentRunAudit auditRow
        WHERE NOT EXISTS (
            SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint checkpointRow
            WHERE checkpointRow.RunId = auditRow.ID))
        THROW 51932, N'One or more Agent run audits were not staged.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgAgentRunAudit
        WHERE AgentId IS NULL OR AgentVersionId IS NULL OR AgentCode IS NULL OR Status IS NULL
           OR StartedAtUtc IS NULL OR InputSha256 IS NULL OR OutputCharacters IS NULL
           OR ToolCallCount IS NULL OR ErrorCode IS NULL)
        THROW 51933, N'Agent run audit summary fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgAgentToolCallAudit
        WHERE RunId IS NULL OR Ordinal IS NULL OR ToolVersionId IS NULL OR ToolName IS NULL
           OR Risk IS NULL OR Status IS NULL OR StartedAtUtc IS NULL OR FinishedAtUtc IS NULL
           OR ErrorCode IS NULL)
        THROW 51934, N'Agent tool-call audit fields are incomplete.', 1;
    IF EXISTS (
        SELECT 1
        FROM dbo.AgAgentRunAudit auditRow
        OUTER APPLY (
            SELECT COUNT_BIG(*) AS ActualCount
            FROM dbo.AgAgentToolCallAudit toolCall
            WHERE toolCall.RunId = auditRow.ID AND toolCall.IsDeleted = 0) counts
        WHERE CONVERT(BIGINT, auditRow.ToolCallCount) <> counts.ActualCount)
        THROW 51935, N'Agent run audit tool-call counts do not match normalized rows.', 1;

    ALTER TABLE dbo.AgAgentRunAudit DROP COLUMN DocumentJson;
    DROP TABLE dbo.AgAgentRunAuditNormalizationCheckpoint;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
