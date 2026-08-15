-- Prepare AgEvaluationBatch for BasePoco and normalized batch fields.
-- DocumentJson remains until the generated data script and Data/032 complete.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgEvaluationBatch', N'U') IS NULL
    THROW 51600, N'dbo.AgEvaluationBatch does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationBatch') AND name = N'ix_ag_evaluation_batch_suite_started')
        DROP INDEX ix_ag_evaluation_batch_suite_started ON dbo.AgEvaluationBatch;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationBatch') AND name = N'ix_ag_evaluation_batch_status')
        DROP INDEX ix_ag_evaluation_batch_status ON dbo.AgEvaluationBatch;

    DECLARE @IdName SYSNAME, @IdType SYSNAME, @PkName SYSNAME, @PkType NVARCHAR(20), @Sql NVARCHAR(MAX);
    SELECT @IdName = columns.name, @IdType = types.name
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgEvaluationBatch') AND UPPER(columns.name) = N'ID';
    IF @IdType IS NULL THROW 51601, N'AgEvaluationBatch.ID is missing.', 1;

    IF @IdType <> N'uniqueidentifier'
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.AgEvaluationBatch WHERE TRY_CONVERT(UNIQUEIDENTIFIER, ID) IS NULL)
            THROW 51602, N'AgEvaluationBatch contains an invalid GUID ID.', 1;
        SELECT @PkName = constraints.name,
               @PkType = CASE WHEN indexes.type = 1 THEN N'CLUSTERED' ELSE N'NONCLUSTERED' END
        FROM sys.key_constraints constraints
        INNER JOIN sys.indexes indexes
          ON indexes.object_id = constraints.parent_object_id
         AND indexes.index_id = constraints.unique_index_id
        WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgEvaluationBatch')
          AND constraints.[type] = N'PK';
        IF @PkName IS NULL THROW 51603, N'AgEvaluationBatch primary key is missing.', 1;
        SET @Sql = N'ALTER TABLE dbo.AgEvaluationBatch DROP CONSTRAINT ' + QUOTENAME(@PkName) + N';';
        EXEC sys.sp_executesql @Sql;
        ALTER TABLE dbo.AgEvaluationBatch ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL;
        SET @Sql = N'ALTER TABLE dbo.AgEvaluationBatch ADD CONSTRAINT ' + QUOTENAME(@PkName)
            + N' PRIMARY KEY ' + @PkType + N' (' + QUOTENAME(@IdName) + N');';
        EXEC sys.sp_executesql @Sql;
    END;
    IF @IdName COLLATE Latin1_General_100_BIN2 <> N'ID'
        EXEC sys.sp_rename N'dbo.AgEvaluationBatch.Id', N'ID', N'COLUMN';

    IF EXISTS (SELECT 1 FROM dbo.AgEvaluationBatch WHERE TRY_CONVERT(UNIQUEIDENTIFIER, SuiteId) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER, SuiteVersionId) IS NULL)
        THROW 51604, N'AgEvaluationBatch contains an invalid Suite GUID.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationBatch
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), TenantId)))
              <> CONVERT(VARBINARY(MAX), TenantId))
        THROW 51605, N'AgEvaluationBatch.TenantId cannot be represented by VARCHAR under the current database collation.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgEvaluationBatch WHERE TRY_CONVERT(DATETIMEOFFSET(7), StartedAtUtc, 127) IS NULL)
        THROW 51606, N'AgEvaluationBatch.StartedAtUtc contains an invalid timestamp.', 1;

    ALTER TABLE dbo.AgEvaluationBatch ALTER COLUMN TenantId VARCHAR(128) NOT NULL;
    ALTER TABLE dbo.AgEvaluationBatch ALTER COLUMN SuiteId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.AgEvaluationBatch ALTER COLUMN SuiteVersionId UNIQUEIDENTIFIER NOT NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'StartedAtUtcValue') IS NULL
        ALTER TABLE dbo.AgEvaluationBatch ADD StartedAtUtcValue DATETIME2(7) NULL;
    EXEC sys.sp_executesql N'
        UPDATE dbo.AgEvaluationBatch
        SET StartedAtUtcValue = CONVERT(DATETIME2(7), TRY_CONVERT(DATETIMEOFFSET(7), StartedAtUtc, 127))
        WHERE StartedAtUtcValue IS NULL;
        IF EXISTS (SELECT 1 FROM dbo.AgEvaluationBatch WHERE StartedAtUtcValue IS NULL)
            THROW 51607, N''AgEvaluationBatch.StartedAtUtc conversion failed.'', 1;';
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'StartedAtUtc') IS NOT NULL
        ALTER TABLE dbo.AgEvaluationBatch DROP COLUMN StartedAtUtc;
    EXEC sys.sp_rename N'dbo.AgEvaluationBatch.StartedAtUtcValue', N'StartedAtUtc', N'COLUMN';
    ALTER TABLE dbo.AgEvaluationBatch ALTER COLUMN StartedAtUtc DATETIME2(7) NOT NULL;

    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'IsDeleted') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD IsDeleted BIT NOT NULL CONSTRAINT DF_AgEvaluationBatch_IsDeleted DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'IsActive') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD IsActive BIT NULL CONSTRAINT DF_AgEvaluationBatch_IsActive DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'ImportDataId') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD ImportDataId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'ModificationNum') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD ModificationNum INT NULL CONSTRAINT DF_AgEvaluationBatch_ModificationNum DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'Tag') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD Tag INT NULL CONSTRAINT DF_AgEvaluationBatch_Tag DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'GroupId') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD GroupId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'CompanyId') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD CompanyId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'AuditStatus') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgEvaluationBatch_AuditStatus DEFAULT ('Add') WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'CurrentNode') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD CurrentNode VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'CreatedBy') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD CreatedBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'CreatedTime') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD CreatedTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'UpdateBy') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD UpdateBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'UpdateTime') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD UpdateTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'RequestedByUserId') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD RequestedByUserId VARCHAR(256) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'SuiteVersionContentSha256') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD SuiteVersionContentSha256 VARCHAR(64) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'FinishedAtUtc') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD FinishedAtUtc DATETIME2(7) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationBatch', N'ErrorCode') IS NULL ALTER TABLE dbo.AgEvaluationBatch ADD ErrorCode VARCHAR(128) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationBatch') AND name = N'ix_ag_evaluation_batch_suite_started')
        EXEC sys.sp_executesql N'CREATE INDEX ix_ag_evaluation_batch_suite_started ON dbo.AgEvaluationBatch(TenantId, SuiteId, StartedAtUtc DESC);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationBatch') AND name = N'ix_ag_evaluation_batch_status')
        EXEC sys.sp_executesql N'CREATE INDEX ix_ag_evaluation_batch_status ON dbo.AgEvaluationBatch(Status);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationBatch') AND name = N'ix_ag_evaluation_batch_is_deleted')
        EXEC sys.sp_executesql N'CREATE INDEX ix_ag_evaluation_batch_is_deleted ON dbo.AgEvaluationBatch(IsDeleted);';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
