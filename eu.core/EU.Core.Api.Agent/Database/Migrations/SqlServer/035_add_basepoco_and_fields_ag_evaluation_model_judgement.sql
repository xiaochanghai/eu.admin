-- Prepare AgEvaluationModelJudgement for BasePoco and normalized report fields.
-- DocumentJson remains until the generated data script and Data/037 complete.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgEvaluationModelJudgement', N'U') IS NULL
    THROW 51700, N'dbo.AgEvaluationModelJudgement does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgement') AND name = N'ix_ag_evaluation_model_judgement_batch_started')
        DROP INDEX ix_ag_evaluation_model_judgement_batch_started ON dbo.AgEvaluationModelJudgement;

    DECLARE @IdName SYSNAME, @IdType SYSNAME, @PkName SYSNAME, @PkType NVARCHAR(20),
            @UniqueName SYSNAME, @Sql NVARCHAR(MAX);
    SELECT @IdName = columns.name, @IdType = types.name
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgement') AND UPPER(columns.name) = N'ID';
    IF @IdType IS NULL THROW 51701, N'AgEvaluationModelJudgement.ID is missing.', 1;

    SELECT @UniqueName = constraints.name
    FROM sys.key_constraints constraints
    WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgement')
      AND constraints.[type] = N'UQ';
    IF @UniqueName IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.AgEvaluationModelJudgement DROP CONSTRAINT ' + QUOTENAME(@UniqueName) + N';';
        EXEC sys.sp_executesql @Sql;
    END;

    IF @IdType <> N'uniqueidentifier'
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.AgEvaluationModelJudgement WHERE TRY_CONVERT(UNIQUEIDENTIFIER, Id) IS NULL)
            THROW 51702, N'AgEvaluationModelJudgement contains an invalid GUID ID.', 1;
        SELECT @PkName = constraints.name,
               @PkType = CASE WHEN indexes.type = 1 THEN N'CLUSTERED' ELSE N'NONCLUSTERED' END
        FROM sys.key_constraints constraints
        INNER JOIN sys.indexes indexes
          ON indexes.object_id = constraints.parent_object_id
         AND indexes.index_id = constraints.unique_index_id
        WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgement')
          AND constraints.[type] = N'PK';
        IF @PkName IS NULL THROW 51703, N'AgEvaluationModelJudgement primary key is missing.', 1;
        SET @Sql = N'ALTER TABLE dbo.AgEvaluationModelJudgement DROP CONSTRAINT ' + QUOTENAME(@PkName) + N';';
        EXEC sys.sp_executesql @Sql;
        ALTER TABLE dbo.AgEvaluationModelJudgement ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL;
        SET @Sql = N'ALTER TABLE dbo.AgEvaluationModelJudgement ADD CONSTRAINT ' + QUOTENAME(@PkName)
            + N' PRIMARY KEY ' + @PkType + N' (' + QUOTENAME(@IdName) + N');';
        EXEC sys.sp_executesql @Sql;
    END;
    IF @IdName COLLATE Latin1_General_100_BIN2 <> N'ID'
        EXEC sys.sp_rename N'dbo.AgEvaluationModelJudgement.Id', N'ID', N'COLUMN';

    IF EXISTS (SELECT 1 FROM dbo.AgEvaluationModelJudgement WHERE TRY_CONVERT(UNIQUEIDENTIFIER, BatchId) IS NULL)
        THROW 51704, N'AgEvaluationModelJudgement contains an invalid Batch GUID.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationModelJudgement
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), TenantId)))
              <> CONVERT(VARBINARY(MAX), TenantId))
        THROW 51705, N'AgEvaluationModelJudgement.TenantId cannot be represented by VARCHAR under the current database collation.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgEvaluationModelJudgement WHERE TRY_CONVERT(DATETIMEOFFSET(7), StartedAtUtc, 127) IS NULL)
        THROW 51706, N'AgEvaluationModelJudgement.StartedAtUtc contains an invalid timestamp.', 1;

    ALTER TABLE dbo.AgEvaluationModelJudgement ALTER COLUMN TenantId VARCHAR(128) NOT NULL;
    ALTER TABLE dbo.AgEvaluationModelJudgement ALTER COLUMN BatchId UNIQUEIDENTIFIER NOT NULL;
    ALTER TABLE dbo.AgEvaluationModelJudgement ALTER COLUMN ConfigurationSha256 VARCHAR(64) NOT NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'StartedAtUtcValue') IS NULL
        ALTER TABLE dbo.AgEvaluationModelJudgement ADD StartedAtUtcValue DATETIME2(7) NULL;
    EXEC sys.sp_executesql N'
        UPDATE dbo.AgEvaluationModelJudgement
        SET StartedAtUtcValue = CONVERT(DATETIME2(7), TRY_CONVERT(DATETIMEOFFSET(7), StartedAtUtc, 127))
        WHERE StartedAtUtcValue IS NULL;
        IF EXISTS (SELECT 1 FROM dbo.AgEvaluationModelJudgement WHERE StartedAtUtcValue IS NULL)
            THROW 51707, N''AgEvaluationModelJudgement.StartedAtUtc conversion failed.'', 1;';
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'StartedAtUtc') IS NOT NULL
        ALTER TABLE dbo.AgEvaluationModelJudgement DROP COLUMN StartedAtUtc;
    EXEC sys.sp_rename N'dbo.AgEvaluationModelJudgement.StartedAtUtcValue', N'StartedAtUtc', N'COLUMN';
    ALTER TABLE dbo.AgEvaluationModelJudgement ALTER COLUMN StartedAtUtc DATETIME2(7) NOT NULL;

    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'IsDeleted') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD IsDeleted BIT NOT NULL CONSTRAINT DF_AgEvaluationModelJudgement_IsDeleted DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'IsActive') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD IsActive BIT NULL CONSTRAINT DF_AgEvaluationModelJudgement_IsActive DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'ImportDataId') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD ImportDataId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'ModificationNum') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD ModificationNum INT NULL CONSTRAINT DF_AgEvaluationModelJudgement_ModificationNum DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'Tag') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD Tag INT NULL CONSTRAINT DF_AgEvaluationModelJudgement_Tag DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'GroupId') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD GroupId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'CompanyId') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD CompanyId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'AuditStatus') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgEvaluationModelJudgement_AuditStatus DEFAULT ('Add') WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'CurrentNode') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD CurrentNode VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'CreatedBy') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD CreatedBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'CreatedTime') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD CreatedTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'UpdateBy') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD UpdateBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'UpdateTime') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD UpdateTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'RequestedByUserId') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD RequestedByUserId VARCHAR(256) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'SuiteId') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD SuiteId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'SuiteVersionId') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD SuiteVersionId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'SuiteVersionContentSha256') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD SuiteVersionContentSha256 VARCHAR(64) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'Provider') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD Provider VARCHAR(256) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'PackageVersion') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD PackageVersion VARCHAR(64) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'ModelProfileId') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD ModelProfileId VARCHAR(256) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'PromptVersion') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD PromptVersion VARCHAR(128) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'FinishedAtUtc') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD FinishedAtUtc DATETIME2(7) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationModelJudgement', N'AdvisoryPassed') IS NULL ALTER TABLE dbo.AgEvaluationModelJudgement ADD AdvisoryPassed BIT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgement') AND name = N'ux_ag_evaluation_model_judgement_configuration')
        ALTER TABLE dbo.AgEvaluationModelJudgement ADD CONSTRAINT ux_ag_evaluation_model_judgement_configuration UNIQUE (TenantId, BatchId, ConfigurationSha256);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgement') AND name = N'ix_ag_evaluation_model_judgement_batch_started')
        EXEC sys.sp_executesql N'CREATE INDEX ix_ag_evaluation_model_judgement_batch_started ON dbo.AgEvaluationModelJudgement(TenantId, BatchId, StartedAtUtc DESC);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgement') AND name = N'ix_ag_evaluation_model_judgement_is_deleted')
        EXEC sys.sp_executesql N'CREATE INDEX ix_ag_evaluation_model_judgement_is_deleted ON dbo.AgEvaluationModelJudgement(IsDeleted);';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
