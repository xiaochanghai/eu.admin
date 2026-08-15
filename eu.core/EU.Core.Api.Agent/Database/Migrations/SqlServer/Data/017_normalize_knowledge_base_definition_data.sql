-- Finalize normalized Knowledge Base storage after the generated data script has run.
-- Stop EU.Core.Api.Agent and back up the database first. SQL Server 2014+.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
IF OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgKnowledgeDocument', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgKnowledgeChunk', N'U') IS NULL
    THROW 51320, N'Knowledge normalized tables are missing. Run 015 and 016 first.', 1;
IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'DocumentJson') IS NULL
BEGIN
    PRINT N'Knowledge normalization was already finalized; Data/017 has no work to do.';
    RETURN;
END;
IF OBJECT_ID(N'dbo.AgKnowledgeNormalizationCheckpoint', N'U') IS NULL
    THROW 51323, N'Knowledge normalization data script has not completed.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.AgKnowledgeNormalizationCheckpoint WHERE ID = 1)
    THROW 51323, N'Knowledge normalization data checkpoint is missing.', 1;
BEGIN TRY
    BEGIN TRANSACTION;
    UPDATE dbo.AgKnowledgeBaseDefinition
    SET Status = CASE Status
        WHEN '0' THEN 'Enabled'
        WHEN '1' THEN 'Disabled'
        WHEN '2' THEN 'Archived'
        ELSE Status
    END
    WHERE Status IN ('0', '1', '2');

    IF EXISTS (SELECT 1 FROM dbo.AgKnowledgeBaseDefinition WHERE Name IS NULL OR Description IS NULL OR Status IS NULL)
        THROW 51322, N'Knowledge normalized columns are incomplete. Run the generated normalization data script first.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgKnowledgeBaseDefinition WHERE Status NOT IN ('Enabled', 'Disabled', 'Archived'))
        THROW 51324, N'Knowledge status contains an unsupported value.', 1;

    ALTER TABLE dbo.AgKnowledgeBaseDefinition ALTER COLUMN Name VARCHAR(256) NOT NULL;
    ALTER TABLE dbo.AgKnowledgeBaseDefinition ALTER COLUMN Description VARCHAR(MAX) NOT NULL;
    ALTER TABLE dbo.AgKnowledgeBaseDefinition ALTER COLUMN Status VARCHAR(32) NOT NULL;
    IF OBJECT_ID(N'dbo.ck_ag_knowledge_base_status', N'C') IS NULL
        ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD CONSTRAINT ck_ag_knowledge_base_status CHECK (Status IN ('Enabled', 'Disabled', 'Archived'));

    DECLARE @DropChecks NVARCHAR(MAX) = N'';
    SELECT @DropChecks = @DropChecks + N'ALTER TABLE dbo.AgKnowledgeBaseDefinition DROP CONSTRAINT ' + QUOTENAME(name) + N';'
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition') AND definition LIKE N'%DocumentJson%';
    IF @DropChecks <> N'' EXEC sys.sp_executesql @DropChecks;
    ALTER TABLE dbo.AgKnowledgeBaseDefinition DROP COLUMN DocumentJson;
    DROP TABLE dbo.AgKnowledgeNormalizationCheckpoint;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
