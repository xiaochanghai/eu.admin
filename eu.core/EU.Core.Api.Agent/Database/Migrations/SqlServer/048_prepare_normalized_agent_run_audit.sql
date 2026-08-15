-- Prepare Agent run audit persistence for BasePoco and normalized tool-call rows.
-- Existing CHAR(36) identifier columns are intentionally preserved.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgAgentRunAudit', N'U') IS NULL
    THROW 51910, N'dbo.AgAgentRunAudit does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'ID') IS NULL
       AND COL_LENGTH(N'dbo.AgAgentRunAudit', N'RunId') IS NOT NULL
        EXEC sys.sp_rename N'dbo.AgAgentRunAudit.RunId', N'ID', N'COLUMN';
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'ID') IS NULL
        THROW 51911, N'AgAgentRunAudit run identity column is missing.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.AgAgentRunAudit WHERE TRY_CONVERT(UNIQUEIDENTIFIER, ID) IS NULL)
        THROW 51912, N'AgAgentRunAudit contains an invalid run GUID.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgAgentRunAudit WHERE TRY_CONVERT(UNIQUEIDENTIFIER, AgentId) IS NULL)
        THROW 51913, N'AgAgentRunAudit contains an invalid Agent GUID.', 1;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentRunAudit') AND name = N'ix_ag_agent_run_audit_agent_started')
        DROP INDEX ix_ag_agent_run_audit_agent_started ON dbo.AgAgentRunAudit;

    DECLARE @StartedAtType SYSNAME;
    SELECT @StartedAtType = types.name
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgAgentRunAudit')
      AND columns.name = N'StartedAtUtc';
    IF @StartedAtType <> N'datetime2'
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.AgAgentRunAudit WHERE TRY_CONVERT(DATETIMEOFFSET(7), StartedAtUtc, 127) IS NULL)
            THROW 51914, N'AgAgentRunAudit.StartedAtUtc contains an invalid timestamp.', 1;
        IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'StartedAtUtcValue') IS NULL
            ALTER TABLE dbo.AgAgentRunAudit ADD StartedAtUtcValue DATETIME2(7) NULL;
        EXEC sys.sp_executesql N'
            UPDATE dbo.AgAgentRunAudit
            SET StartedAtUtcValue = CONVERT(DATETIME2(7), TRY_CONVERT(DATETIMEOFFSET(7), StartedAtUtc, 127))
            WHERE StartedAtUtcValue IS NULL;
            IF EXISTS (SELECT 1 FROM dbo.AgAgentRunAudit WHERE StartedAtUtcValue IS NULL)
                THROW 51915, N''AgAgentRunAudit.StartedAtUtc conversion failed.'', 1;';
        ALTER TABLE dbo.AgAgentRunAudit DROP COLUMN StartedAtUtc;
        EXEC sys.sp_rename N'dbo.AgAgentRunAudit.StartedAtUtcValue', N'StartedAtUtc', N'COLUMN';
        ALTER TABLE dbo.AgAgentRunAudit ALTER COLUMN StartedAtUtc DATETIME2(7) NOT NULL;
    END;

    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'IsDeleted') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD IsDeleted BIT NOT NULL DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'IsActive') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD IsActive BIT NULL DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'ImportDataId') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD ImportDataId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'ModificationNum') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD ModificationNum INT NULL DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'Tag') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD Tag INT NULL DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'GroupId') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD GroupId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'CompanyId') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD CompanyId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'AuditStatus') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD AuditStatus VARCHAR(32) NULL DEFAULT ('Add') WITH VALUES;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'CurrentNode') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD CurrentNode VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'CreatedBy') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD CreatedBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'CreatedTime') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD CreatedTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'UpdateBy') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD UpdateBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'UpdateTime') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD UpdateTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'AgentVersionId') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD AgentVersionId CHAR(36) NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'AgentCode') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD AgentCode VARCHAR(128) NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'FinishedAtUtc') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD FinishedAtUtc DATETIME2(7) NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'InputSha256') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD InputSha256 VARCHAR(64) NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'OutputCharacters') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD OutputCharacters INT NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'ToolCallCount') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD ToolCallCount INT NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'ErrorCode') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD ErrorCode VARCHAR(128) NULL;

    IF OBJECT_ID(N'dbo.AgAgentToolCallAudit', N'U') IS NULL
        CREATE TABLE dbo.AgAgentToolCallAudit (
            ID UNIQUEIDENTIFIER NOT NULL CONSTRAINT pk_ag_agent_tool_call_audit PRIMARY KEY,
            RunId CHAR(36) NOT NULL,
            Ordinal INT NOT NULL,
            ToolVersionId CHAR(36) NOT NULL,
            ToolName VARCHAR(256) NOT NULL,
            Risk VARCHAR(32) NOT NULL,
            Status VARCHAR(32) NOT NULL,
            StartedAtUtc DATETIME2(7) NOT NULL,
            FinishedAtUtc DATETIME2(7) NOT NULL,
            ErrorCode VARCHAR(128) NOT NULL,
            IsDeleted BIT NOT NULL CONSTRAINT df_ag_agent_tool_call_audit_is_deleted DEFAULT (0),
            IsActive BIT NULL CONSTRAINT df_ag_agent_tool_call_audit_is_active DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL CONSTRAINT df_ag_agent_tool_call_audit_modification_num DEFAULT (0),
            Tag INT NULL CONSTRAINT df_ag_agent_tool_call_audit_tag DEFAULT (1),
            GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL,
            AuditStatus VARCHAR(32) NULL CONSTRAINT df_ag_agent_tool_call_audit_audit_status DEFAULT ('Add'),
            CurrentNode VARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL,
            CreatedTime DATETIME NULL,
            UpdateBy UNIQUEIDENTIFIER NULL,
            UpdateTime DATETIME NULL,
            CONSTRAINT fk_ag_agent_tool_call_audit_run FOREIGN KEY (RunId) REFERENCES dbo.AgAgentRunAudit(ID) ON DELETE CASCADE,
            CONSTRAINT ux_ag_agent_tool_call_audit_order UNIQUE (RunId, Ordinal));

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentRunAudit') AND name = N'ix_ag_agent_run_audit_agent_started')
        CREATE INDEX ix_ag_agent_run_audit_agent_started ON dbo.AgAgentRunAudit(AgentId, StartedAtUtc DESC, ID DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentRunAudit') AND name = N'ix_ag_agent_run_audit_is_deleted')
        CREATE INDEX ix_ag_agent_run_audit_is_deleted ON dbo.AgAgentRunAudit(IsDeleted);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentRunAudit') AND name = N'ix_ag_agent_run_audit_is_active')
        CREATE INDEX ix_ag_agent_run_audit_is_active ON dbo.AgAgentRunAudit(IsActive);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentToolCallAudit') AND name = N'ix_ag_agent_tool_call_audit_run')
        CREATE INDEX ix_ag_agent_tool_call_audit_run ON dbo.AgAgentToolCallAudit(RunId, Ordinal);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentToolCallAudit') AND name = N'ix_ag_agent_tool_call_audit_is_deleted')
        CREATE INDEX ix_ag_agent_tool_call_audit_is_deleted ON dbo.AgAgentToolCallAudit(IsDeleted);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentToolCallAudit') AND name = N'ix_ag_agent_tool_call_audit_is_active')
        CREATE INDEX ix_ag_agent_tool_call_audit_is_active ON dbo.AgAgentToolCallAudit(IsActive);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
