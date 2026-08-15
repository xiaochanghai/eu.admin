-- Prepare Agent API operation audit persistence for BasePoco and normalized columns.
-- Existing CHAR(36) audit identifiers are intentionally preserved.
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgAgentOperationAudit', N'U') IS NULL
    THROW 52000, N'dbo.AgAgentOperationAudit does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'ID') IS NULL
       AND COL_LENGTH(N'dbo.AgAgentOperationAudit', N'AuditId') IS NOT NULL
        EXEC sys.sp_rename N'dbo.AgAgentOperationAudit.AuditId', N'ID', N'COLUMN';
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'ID') IS NULL
        THROW 52001, N'AgAgentOperationAudit identity column is missing.', 1;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM dbo.AgAgentOperationAudit WHERE TRY_CONVERT(UNIQUEIDENTIFIER, ID) IS NULL)
        THROW 52002, N'AgAgentOperationAudit contains an invalid audit GUID.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgAgentOperationAudit
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(128), TenantId)))
              <> CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), TenantId)))
        THROW 52003, N'AgAgentOperationAudit.TenantId cannot be represented by VARCHAR.', 1;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'DocumentJson') IS NOT NULL
        EXEC sys.sp_executesql N'
            IF EXISTS (
                SELECT 1 FROM dbo.AgAgentOperationAudit
                WHERE DocumentJson IS NOT NULL
                  AND CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), DocumentJson)))
                      <> CONVERT(VARBINARY(MAX), DocumentJson))
                THROW 52006, N''AgAgentOperationAudit.DocumentJson cannot be represented by VARCHAR.'', 1;';

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentOperationAudit') AND name = N'ix_ag_agent_operation_audit_tenant_time')
        DROP INDEX ix_ag_agent_operation_audit_tenant_time ON dbo.AgAgentOperationAudit;

    DECLARE @OccurredAtType SYSNAME;
    SELECT @OccurredAtType = types.name
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgAgentOperationAudit') AND columns.name = N'OccurredAtUtc';
    IF @OccurredAtType <> N'datetime2'
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.AgAgentOperationAudit WHERE TRY_CONVERT(DATETIMEOFFSET(7), OccurredAtUtc, 127) IS NULL)
            THROW 52004, N'AgAgentOperationAudit.OccurredAtUtc contains an invalid timestamp.', 1;
        IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'OccurredAtUtcValue') IS NULL
            ALTER TABLE dbo.AgAgentOperationAudit ADD OccurredAtUtcValue DATETIME2(7) NULL;
        EXEC sys.sp_executesql N'
            UPDATE dbo.AgAgentOperationAudit
            SET OccurredAtUtcValue = CONVERT(DATETIME2(7), TRY_CONVERT(DATETIMEOFFSET(7), OccurredAtUtc, 127));
            IF EXISTS (SELECT 1 FROM dbo.AgAgentOperationAudit WHERE OccurredAtUtcValue IS NULL)
                THROW 52005, N''AgAgentOperationAudit.OccurredAtUtc conversion failed.'', 1;';
        ALTER TABLE dbo.AgAgentOperationAudit DROP COLUMN OccurredAtUtc;
        EXEC sys.sp_rename N'dbo.AgAgentOperationAudit.OccurredAtUtcValue', N'OccurredAtUtc', N'COLUMN';
        ALTER TABLE dbo.AgAgentOperationAudit ALTER COLUMN OccurredAtUtc DATETIME2(7) NOT NULL;
    END;

    ALTER TABLE dbo.AgAgentOperationAudit ALTER COLUMN TenantId VARCHAR(128) NOT NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'UserId') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD UserId VARCHAR(256) NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'CorrelationId') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD CorrelationId VARCHAR(128) NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'Policy') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD Policy VARCHAR(512) NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'Method') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD Method VARCHAR(16) NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'Path') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD Path VARCHAR(2048) NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'StatusCode') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD StatusCode INT NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'ErrorCode') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD ErrorCode VARCHAR(128) NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'DurationMilliseconds') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD DurationMilliseconds BIGINT NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'IsDeleted') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD IsDeleted BIT NOT NULL DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'IsActive') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD IsActive BIT NULL DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'ImportDataId') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD ImportDataId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'ModificationNum') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD ModificationNum INT NULL DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'Tag') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD Tag INT NULL DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'GroupId') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD GroupId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'CompanyId') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD CompanyId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'AuditStatus') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD AuditStatus VARCHAR(32) NULL DEFAULT ('Add') WITH VALUES;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'CurrentNode') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD CurrentNode VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'CreatedBy') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD CreatedBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'CreatedTime') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD CreatedTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'UpdateBy') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD UpdateBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgAgentOperationAudit', N'UpdateTime') IS NULL ALTER TABLE dbo.AgAgentOperationAudit ADD UpdateTime DATETIME NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentOperationAudit') AND name = N'ix_ag_agent_operation_audit_tenant_time')
        CREATE INDEX ix_ag_agent_operation_audit_tenant_time ON dbo.AgAgentOperationAudit(TenantId, OccurredAtUtc DESC, ID DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentOperationAudit') AND name = N'ix_ag_agent_operation_audit_is_deleted')
        CREATE INDEX ix_ag_agent_operation_audit_is_deleted ON dbo.AgAgentOperationAudit(IsDeleted);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentOperationAudit') AND name = N'ix_ag_agent_operation_audit_is_active')
        CREATE INDEX ix_ag_agent_operation_audit_is_active ON dbo.AgAgentOperationAudit(IsActive);
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
