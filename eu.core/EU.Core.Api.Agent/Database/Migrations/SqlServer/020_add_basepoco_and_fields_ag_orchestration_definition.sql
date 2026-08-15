-- Prepare AgOrchestrationDefinition for BasePoco and normalized orchestration fields.
-- Existing DocumentJson is retained until the generated data script and Data/022 complete.
-- Stop EU.Core.Api.Agent and back up the database first. SQL Server 2014+.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationDefinition', N'U') IS NULL
    THROW 51400, N'dbo.AgOrchestrationDefinition does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdName SYSNAME, @IdType SYSNAME, @PkName SYSNAME, @PkType NVARCHAR(20), @Sql NVARCHAR(MAX);
    SELECT @IdName = columns.name, @IdType = types.name
    FROM sys.columns AS columns
    INNER JOIN sys.types AS types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgOrchestrationDefinition')
      AND UPPER(columns.name) = N'ID';
    IF @IdName IS NULL THROW 51401, N'AgOrchestrationDefinition.Id is missing.', 1;

    IF @IdType <> N'uniqueidentifier'
    BEGIN
        IF @IdType NOT IN (N'char', N'varchar', N'nchar', N'nvarchar')
            THROW 51402, N'AgOrchestrationDefinition.Id must be a character GUID or UNIQUEIDENTIFIER.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AgOrchestrationDefinition WHERE TRY_CONVERT(UNIQUEIDENTIFIER, Id) IS NULL)
            THROW 51403, N'AgOrchestrationDefinition contains an invalid GUID Id.', 1;

        SELECT @PkName = constraints.name,
               @PkType = CASE WHEN indexes.type = 1 THEN N'CLUSTERED' ELSE N'NONCLUSTERED' END
        FROM sys.key_constraints AS constraints
        INNER JOIN sys.indexes AS indexes
            ON indexes.object_id = constraints.parent_object_id
           AND indexes.index_id = constraints.unique_index_id
        WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgOrchestrationDefinition')
          AND constraints.[type] = N'PK';
        IF @PkName IS NULL THROW 51404, N'AgOrchestrationDefinition primary key is missing.', 1;
        SET @Sql = N'ALTER TABLE dbo.AgOrchestrationDefinition DROP CONSTRAINT ' + QUOTENAME(@PkName) + N';';
        EXEC sys.sp_executesql @Sql;
        ALTER TABLE dbo.AgOrchestrationDefinition ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL;
        SET @Sql = N'ALTER TABLE dbo.AgOrchestrationDefinition ADD CONSTRAINT ' + QUOTENAME(@PkName)
            + N' PRIMARY KEY ' + @PkType + N' (Id);';
        EXEC sys.sp_executesql @Sql;
    END;
    IF @IdName COLLATE Latin1_General_100_BIN2 <> N'ID'
        EXEC sys.sp_rename N'dbo.AgOrchestrationDefinition.Id', N'ID', N'COLUMN';

    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'IsDeleted') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD IsDeleted BIT NOT NULL CONSTRAINT DF_AgOrchestrationDefinition_IsDeleted DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'IsActive') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD IsActive BIT NULL CONSTRAINT DF_AgOrchestrationDefinition_IsActive DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'ImportDataId') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD ImportDataId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'ModificationNum') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD ModificationNum INT NULL CONSTRAINT DF_AgOrchestrationDefinition_ModificationNum DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'Tag') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD Tag INT NULL CONSTRAINT DF_AgOrchestrationDefinition_Tag DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'GroupId') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD GroupId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'CompanyId') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD CompanyId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'AuditStatus') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgOrchestrationDefinition_AuditStatus DEFAULT ('Add') WITH VALUES;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'CurrentNode') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD CurrentNode VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'CreatedBy') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD CreatedBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'CreatedTime') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD CreatedTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'UpdateBy') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD UpdateBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'UpdateTime') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD UpdateTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'Name') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD Name VARCHAR(256) NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'Description') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD Description VARCHAR(MAX) NULL;
    IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'Status') IS NULL
        ALTER TABLE dbo.AgOrchestrationDefinition ADD Status VARCHAR(32) NULL;

    DECLARE @CodeConstraintName SYSNAME;
    SELECT @CodeConstraintName = constraintObject.name
    FROM sys.key_constraints AS constraintObject
    INNER JOIN sys.index_columns AS indexColumn
        ON indexColumn.object_id = constraintObject.parent_object_id
       AND indexColumn.index_id = constraintObject.unique_index_id
    INNER JOIN sys.columns AS columnObject
        ON columnObject.object_id = indexColumn.object_id
       AND columnObject.column_id = indexColumn.column_id
    WHERE constraintObject.parent_object_id = OBJECT_ID(N'dbo.AgOrchestrationDefinition')
      AND constraintObject.[type] = N'UQ'
      AND columnObject.name = N'Code';
    IF @CodeConstraintName IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.AgOrchestrationDefinition DROP CONSTRAINT ' + QUOTENAME(@CodeConstraintName) + N';';
        EXEC sys.sp_executesql @Sql;
    END;

    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationDefinition
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), Code)))
              <> CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), Code)))
        THROW 51405, N'AgOrchestrationDefinition.Code cannot be represented by VARCHAR under the current database collation.', 1;

    IF EXISTS (
        SELECT 1 FROM sys.columns columns
        INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
        WHERE columns.object_id = OBJECT_ID(N'dbo.AgOrchestrationDefinition')
          AND columns.name = N'Code' AND types.name <> N'varchar')
        ALTER TABLE dbo.AgOrchestrationDefinition ALTER COLUMN Code VARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationDefinition')
          AND name = N'ux_ag_orchestration_definition_code')
        CREATE UNIQUE INDEX ux_ag_orchestration_definition_code
            ON dbo.AgOrchestrationDefinition(Code);
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationDefinition')
          AND name = N'ix_ag_orchestration_definition_is_deleted')
        CREATE INDEX ix_ag_orchestration_definition_is_deleted
            ON dbo.AgOrchestrationDefinition(IsDeleted);
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationDefinition')
          AND name = N'ix_ag_orchestration_definition_is_active')
        CREATE INDEX ix_ag_orchestration_definition_is_active
            ON dbo.AgOrchestrationDefinition(IsActive);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
