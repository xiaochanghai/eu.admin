-- Finalize MCP normalized storage without comparing or rebuilding existing rows.
-- Populate the normalized columns and child tables before running this script.
-- Stop EU.Core.Api.Agent and back up the database first. SQL Server 2014+.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
IF OBJECT_ID(N'dbo.AgMcpServerDefinition', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgMcpServerArgument', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgMcpToolVersion', N'U') IS NULL
    THROW 51220, N'MCP normalized tables are missing. Run 010 and 011 first.', 1;
IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'DocumentJson') IS NULL
    THROW 51221, N'DocumentJson is absent; the MCP cutover was already finalized.', 1;
IF OBJECT_ID(N'dbo.AgMcpNormalizationCheckpoint', N'U') IS NULL
    THROW 51223, N'MCP normalization data script has not completed.', 1;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.AgMcpNormalizationCheckpoint WHERE ID = 1)
    THROW 51223, N'MCP normalization data checkpoint is missing.', 1;
GO
BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1
        FROM dbo.AgMcpServerDefinition
        WHERE Name IS NULL OR Description IS NULL OR Transport IS NULL
           OR Endpoint IS NULL OR Command IS NULL OR CredentialAlias IS NULL
           OR Enabled IS NULL OR Status IS NULL OR LastError IS NULL
    )
        THROW 51222, N'MCP normalized columns are incomplete. Generate and run the MCP normalization data script before Data/012.', 1;

    ALTER TABLE dbo.AgMcpServerDefinition ALTER COLUMN Name NVARCHAR(256) NOT NULL;
    ALTER TABLE dbo.AgMcpServerDefinition ALTER COLUMN Description NVARCHAR(MAX) NOT NULL;
    ALTER TABLE dbo.AgMcpServerDefinition ALTER COLUMN Transport VARCHAR(32) NOT NULL;
    ALTER TABLE dbo.AgMcpServerDefinition ALTER COLUMN Endpoint NVARCHAR(2048) NOT NULL;
    ALTER TABLE dbo.AgMcpServerDefinition ALTER COLUMN Command NVARCHAR(512) NOT NULL;
    ALTER TABLE dbo.AgMcpServerDefinition ALTER COLUMN CredentialAlias NVARCHAR(200) NOT NULL;
    ALTER TABLE dbo.AgMcpServerDefinition ALTER COLUMN Enabled BIT NOT NULL;
    ALTER TABLE dbo.AgMcpServerDefinition ALTER COLUMN Status VARCHAR(32) NOT NULL;
    ALTER TABLE dbo.AgMcpServerDefinition ALTER COLUMN LastError NVARCHAR(MAX) NOT NULL;

    IF OBJECT_ID(N'dbo.ck_ag_mcp_server_transport', N'C') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD CONSTRAINT ck_ag_mcp_server_transport CHECK (Transport IN ('StreamableHttp', 'Sse', 'Stdio'));
    IF OBJECT_ID(N'dbo.ck_ag_mcp_server_status', N'C') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD CONSTRAINT ck_ag_mcp_server_status CHECK (Status IN ('NotSynced', 'Healthy', 'Unhealthy', 'Disabled', 'Archived'));

    DECLARE @DropChecks NVARCHAR(MAX) = N'';
    SELECT @DropChecks = @DropChecks + N'ALTER TABLE dbo.AgMcpServerDefinition DROP CONSTRAINT ' + QUOTENAME(name) + N';'
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.AgMcpServerDefinition') AND definition LIKE N'%DocumentJson%';
    IF @DropChecks <> N'' EXEC sys.sp_executesql @DropChecks;
    ALTER TABLE dbo.AgMcpServerDefinition DROP COLUMN DocumentJson;
    DROP TABLE dbo.AgMcpNormalizationCheckpoint;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
