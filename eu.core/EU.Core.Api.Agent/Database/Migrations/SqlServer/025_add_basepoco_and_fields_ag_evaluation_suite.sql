-- Prepare AgEvaluationSuite for BasePoco and normalized suite fields.
-- DocumentJson remains until the generated data script and Data/027 complete.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgEvaluationSuite', N'U') IS NULL
    THROW 51500, N'dbo.AgEvaluationSuite does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdName SYSNAME, @IdType SYSNAME, @PkName SYSNAME, @PkType NVARCHAR(20), @Sql NVARCHAR(MAX);
    SELECT @IdName = columns.name, @IdType = types.name
    FROM sys.columns AS columns
    INNER JOIN sys.types AS types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgEvaluationSuite') AND UPPER(columns.name) = N'ID';
    IF @IdName IS NULL THROW 51501, N'AgEvaluationSuite.Id is missing.', 1;

    DECLARE @TenantCodeConstraint SYSNAME;
    SELECT @TenantCodeConstraint = constraints.name
    FROM sys.key_constraints AS constraints
    WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgEvaluationSuite')
      AND constraints.[type] = N'UQ';
    IF @TenantCodeConstraint IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.AgEvaluationSuite DROP CONSTRAINT ' + QUOTENAME(@TenantCodeConstraint) + N';';
        EXEC sys.sp_executesql @Sql;
    END;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationSuite') AND name = N'ix_ag_evaluation_suite_tenant_code')
        DROP INDEX ix_ag_evaluation_suite_tenant_code ON dbo.AgEvaluationSuite;

    IF @IdType <> N'uniqueidentifier'
    BEGIN
        IF @IdType NOT IN (N'char', N'varchar', N'nchar', N'nvarchar')
            THROW 51502, N'AgEvaluationSuite.Id must be a character GUID or UNIQUEIDENTIFIER.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AgEvaluationSuite WHERE TRY_CONVERT(UNIQUEIDENTIFIER, Id) IS NULL)
            THROW 51503, N'AgEvaluationSuite contains an invalid GUID Id.', 1;
        SELECT @PkName = constraints.name,
               @PkType = CASE WHEN indexes.type = 1 THEN N'CLUSTERED' ELSE N'NONCLUSTERED' END
        FROM sys.key_constraints AS constraints
        INNER JOIN sys.indexes AS indexes
          ON indexes.object_id = constraints.parent_object_id
         AND indexes.index_id = constraints.unique_index_id
        WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgEvaluationSuite')
          AND constraints.[type] = N'PK';
        IF @PkName IS NULL THROW 51504, N'AgEvaluationSuite primary key is missing.', 1;
        SET @Sql = N'ALTER TABLE dbo.AgEvaluationSuite DROP CONSTRAINT ' + QUOTENAME(@PkName) + N';';
        EXEC sys.sp_executesql @Sql;
        ALTER TABLE dbo.AgEvaluationSuite ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL;
        SET @Sql = N'ALTER TABLE dbo.AgEvaluationSuite ADD CONSTRAINT ' + QUOTENAME(@PkName)
            + N' PRIMARY KEY ' + @PkType + N' (Id);';
        EXEC sys.sp_executesql @Sql;
    END;
    IF @IdName COLLATE Latin1_General_100_BIN2 <> N'ID'
        EXEC sys.sp_rename N'dbo.AgEvaluationSuite.Id', N'ID', N'COLUMN';

    IF EXISTS (
        SELECT 1 FROM dbo.AgEvaluationSuite
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), TenantId)))
              <> CONVERT(VARBINARY(MAX), TenantId)
           OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), Code)))
              <> CONVERT(VARBINARY(MAX), Code))
        THROW 51505, N'Evaluation Suite identity text cannot be represented by VARCHAR under the current database collation.', 1;

    ALTER TABLE dbo.AgEvaluationSuite ALTER COLUMN TenantId VARCHAR(128) NOT NULL;
    ALTER TABLE dbo.AgEvaluationSuite ALTER COLUMN Code VARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL;

    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'IsDeleted') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD IsDeleted BIT NOT NULL CONSTRAINT DF_AgEvaluationSuite_IsDeleted DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'IsActive') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD IsActive BIT NULL CONSTRAINT DF_AgEvaluationSuite_IsActive DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'ImportDataId') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD ImportDataId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'ModificationNum') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD ModificationNum INT NULL CONSTRAINT DF_AgEvaluationSuite_ModificationNum DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'Tag') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD Tag INT NULL CONSTRAINT DF_AgEvaluationSuite_Tag DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'GroupId') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD GroupId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'CompanyId') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD CompanyId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'AuditStatus') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgEvaluationSuite_AuditStatus DEFAULT ('Add') WITH VALUES;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'CurrentNode') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD CurrentNode VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'CreatedBy') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD CreatedBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'CreatedTime') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD CreatedTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'UpdateBy') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD UpdateBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'UpdateTime') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD UpdateTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'Name') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD Name VARCHAR(256) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'Description') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD Description VARCHAR(MAX) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'Status') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD Status VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'CreatedAtUtc') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD CreatedAtUtc DATETIME2(7) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'UpdatedAtUtc') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD UpdatedAtUtc DATETIME2(7) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'CreatedByUserId') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD CreatedByUserId VARCHAR(256) NULL;
    IF COL_LENGTH(N'dbo.AgEvaluationSuite', N'UpdatedByUserId') IS NULL ALTER TABLE dbo.AgEvaluationSuite ADD UpdatedByUserId VARCHAR(256) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationSuite') AND name = N'ux_ag_evaluation_suite_tenant_code')
        EXEC sys.sp_executesql N'CREATE UNIQUE INDEX ux_ag_evaluation_suite_tenant_code ON dbo.AgEvaluationSuite(TenantId, Code) WHERE IsDeleted = 0;';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationSuite') AND name = N'ix_ag_evaluation_suite_is_deleted')
        EXEC sys.sp_executesql N'CREATE INDEX ix_ag_evaluation_suite_is_deleted ON dbo.AgEvaluationSuite(IsDeleted);';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationSuite') AND name = N'ix_ag_evaluation_suite_is_active')
        EXEC sys.sp_executesql N'CREATE INDEX ix_ag_evaluation_suite_is_active ON dbo.AgEvaluationSuite(IsActive);';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
