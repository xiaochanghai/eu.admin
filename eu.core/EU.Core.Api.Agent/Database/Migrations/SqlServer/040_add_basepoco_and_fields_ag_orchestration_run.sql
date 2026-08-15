-- Prepare AgOrchestrationRun for BasePoco and normalized run summary fields.
-- Existing CHAR(36) identifier columns are intentionally preserved.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationRun', N'U') IS NULL
    THROW 51800, N'dbo.AgOrchestrationRun does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationRun') AND name = N'ix_ag_orchestration_run_owner')
        DROP INDEX ix_ag_orchestration_run_owner ON dbo.AgOrchestrationRun;

    IF EXISTS (SELECT 1 FROM dbo.AgOrchestrationRun WHERE TRY_CONVERT(UNIQUEIDENTIFIER, ID) IS NULL)
        THROW 51801, N'AgOrchestrationRun contains an invalid run GUID.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgOrchestrationRun WHERE TRY_CONVERT(UNIQUEIDENTIFIER, OrchestrationId) IS NULL)
        THROW 51802, N'AgOrchestrationRun contains an invalid orchestration GUID.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgOrchestrationRun WHERE TRY_CONVERT(DATETIMEOFFSET(7), StartedAtUtc, 127) IS NULL)
        THROW 51803, N'AgOrchestrationRun.StartedAtUtc contains an invalid timestamp.', 1;

    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'StartedAtUtcValue') IS NULL
        ALTER TABLE dbo.AgOrchestrationRun ADD StartedAtUtcValue DATETIME2(7) NULL;
    EXEC sys.sp_executesql N'
        UPDATE dbo.AgOrchestrationRun
        SET StartedAtUtcValue = CONVERT(DATETIME2(7), TRY_CONVERT(DATETIMEOFFSET(7), StartedAtUtc, 127))
        WHERE StartedAtUtcValue IS NULL;
        IF EXISTS (SELECT 1 FROM dbo.AgOrchestrationRun WHERE StartedAtUtcValue IS NULL)
            THROW 51804, N''AgOrchestrationRun.StartedAtUtc conversion failed.'', 1;';
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'StartedAtUtc') IS NOT NULL
        ALTER TABLE dbo.AgOrchestrationRun DROP COLUMN StartedAtUtc;
    EXEC sys.sp_rename N'dbo.AgOrchestrationRun.StartedAtUtcValue', N'StartedAtUtc', N'COLUMN';
    ALTER TABLE dbo.AgOrchestrationRun ALTER COLUMN StartedAtUtc DATETIME2(7) NOT NULL;

    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'IsDeleted') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD IsDeleted BIT NOT NULL DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'IsActive') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD IsActive BIT NULL DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'ImportDataId') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD ImportDataId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'ModificationNum') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD ModificationNum INT NULL DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'Tag') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD Tag INT NULL DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'GroupId') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD GroupId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'CompanyId') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD CompanyId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'AuditStatus') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD AuditStatus VARCHAR(32) NULL DEFAULT ('Add') WITH VALUES;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'CurrentNode') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD CurrentNode VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'CreatedBy') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD CreatedBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'CreatedTime') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD CreatedTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'UpdateBy') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD UpdateBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'UpdateTime') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD UpdateTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'OrchestrationVersionId') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD OrchestrationVersionId CHAR(36) NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'OrchestrationCode') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD OrchestrationCode VARCHAR(128) NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'Status') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD Status VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'FinishedAtUtc') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD FinishedAtUtc DATETIME2(7) NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'InputSha256') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD InputSha256 VARCHAR(64) NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'ErrorCode') IS NULL ALTER TABLE dbo.AgOrchestrationRun ADD ErrorCode VARCHAR(128) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationRun') AND name = N'ix_ag_orchestration_run_owner')
        CREATE INDEX ix_ag_orchestration_run_owner ON dbo.AgOrchestrationRun(OrchestrationId, StartedAtUtc DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationRun') AND name = N'ix_ag_orchestration_run_status')
        CREATE INDEX ix_ag_orchestration_run_status ON dbo.AgOrchestrationRun(Status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationRun') AND name = N'ix_ag_orchestration_run_is_deleted')
        CREATE INDEX ix_ag_orchestration_run_is_deleted ON dbo.AgOrchestrationRun(IsDeleted);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationRun') AND name = N'ix_ag_orchestration_run_is_active')
        CREATE INDEX ix_ag_orchestration_run_is_active ON dbo.AgOrchestrationRun(IsActive);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
