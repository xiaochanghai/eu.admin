-- Add EU.Core BasePoco columns and normalized Skill definition fields.
-- Existing DocumentJson is retained until Data/008 completes the data cutover.
-- SQL Server 2014+

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.AgSkillDefinition', N'U') IS NULL
    THROW 51060, N'dbo.AgSkillDefinition does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdType SYSNAME;
    SELECT @IdType = types.name
    FROM sys.columns AS columns
    INNER JOIN sys.types AS types
        ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgSkillDefinition')
      AND columns.name = N'Id';

    IF @IdType IS NULL
        THROW 51061, N'dbo.AgSkillDefinition.Id does not exist.', 1;

    IF @IdType <> N'uniqueidentifier'
    BEGIN
        IF @IdType NOT IN (N'char', N'varchar', N'nchar', N'nvarchar')
            THROW 51062, N'AgSkillDefinition.Id must be a character GUID or UNIQUEIDENTIFIER.', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.AgSkillDefinition
            WHERE TRY_CONVERT(UNIQUEIDENTIFIER, Id) IS NULL
        )
            THROW 51063, N'AgSkillDefinition contains an Id that is not a valid GUID.', 1;

        IF EXISTS (
            SELECT TRY_CONVERT(UNIQUEIDENTIFIER, Id)
            FROM dbo.AgSkillDefinition
            GROUP BY TRY_CONVERT(UNIQUEIDENTIFIER, Id)
            HAVING COUNT_BIG(*) > 1
        )
            THROW 51064, N'AgSkillDefinition contains Id values that collide after GUID conversion.', 1;

        IF EXISTS (
            SELECT 1
            FROM sys.foreign_key_columns AS foreignKeyColumns
            WHERE (
                foreignKeyColumns.parent_object_id = OBJECT_ID(N'dbo.AgSkillDefinition')
                AND foreignKeyColumns.parent_column_id = COLUMNPROPERTY(
                    OBJECT_ID(N'dbo.AgSkillDefinition'), N'Id', N'ColumnId')
            ) OR (
                foreignKeyColumns.referenced_object_id = OBJECT_ID(N'dbo.AgSkillDefinition')
                AND foreignKeyColumns.referenced_column_id = COLUMNPROPERTY(
                    OBJECT_ID(N'dbo.AgSkillDefinition'), N'Id', N'ColumnId')
            )
        )
            THROW 51065, N'AgSkillDefinition.Id has foreign-key dependencies. Migrate those columns together first.', 1;

        IF EXISTS (
            SELECT 1
            FROM sys.index_columns AS indexColumns
            INNER JOIN sys.indexes AS indexes
                ON indexes.object_id = indexColumns.object_id
               AND indexes.index_id = indexColumns.index_id
            WHERE indexColumns.object_id = OBJECT_ID(N'dbo.AgSkillDefinition')
              AND indexColumns.column_id = COLUMNPROPERTY(
                  OBJECT_ID(N'dbo.AgSkillDefinition'), N'Id', N'ColumnId')
              AND indexes.is_primary_key = 0
        )
            THROW 51066, N'AgSkillDefinition.Id has a non-primary-key index. Drop or migrate that index explicitly first.', 1;

        DECLARE @PrimaryKeyName SYSNAME;
        DECLARE @PrimaryKeyType NVARCHAR(20);
        DECLARE @PrimaryKeyColumnCount INT;
        DECLARE @PrimaryKeyIdColumnCount INT;

        SELECT
            @PrimaryKeyName = keyConstraints.name,
            @PrimaryKeyType = CASE WHEN indexes.type = 1 THEN N'CLUSTERED' ELSE N'NONCLUSTERED' END
        FROM sys.key_constraints AS keyConstraints
        INNER JOIN sys.indexes AS indexes
            ON indexes.object_id = keyConstraints.parent_object_id
           AND indexes.index_id = keyConstraints.unique_index_id
        WHERE keyConstraints.parent_object_id = OBJECT_ID(N'dbo.AgSkillDefinition')
          AND keyConstraints.type = N'PK';

        SELECT
            @PrimaryKeyColumnCount = COUNT(*),
            @PrimaryKeyIdColumnCount = SUM(CASE WHEN columns.name = N'Id' THEN 1 ELSE 0 END)
        FROM sys.indexes AS indexes
        INNER JOIN sys.index_columns AS indexColumns
            ON indexColumns.object_id = indexes.object_id
           AND indexColumns.index_id = indexes.index_id
        INNER JOIN sys.columns AS columns
            ON columns.object_id = indexColumns.object_id
           AND columns.column_id = indexColumns.column_id
        WHERE indexes.object_id = OBJECT_ID(N'dbo.AgSkillDefinition')
          AND indexes.is_primary_key = 1
          AND indexColumns.key_ordinal > 0;

        IF @PrimaryKeyName IS NULL
           OR @PrimaryKeyColumnCount <> 1
           OR @PrimaryKeyIdColumnCount <> 1
            THROW 51067, N'AgSkillDefinition must have a single-column primary key on Id.', 1;

        DECLARE @PrimaryKeySql NVARCHAR(MAX);
        SET @PrimaryKeySql = N'ALTER TABLE dbo.AgSkillDefinition DROP CONSTRAINT '
            + QUOTENAME(@PrimaryKeyName) + N';';
        EXEC sys.sp_executesql @PrimaryKeySql;

        ALTER TABLE dbo.AgSkillDefinition
            ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL;

        SET @PrimaryKeySql = N'ALTER TABLE dbo.AgSkillDefinition ADD CONSTRAINT '
            + QUOTENAME(@PrimaryKeyName) + N' PRIMARY KEY '
            + @PrimaryKeyType + N' (Id);';
        EXEC sys.sp_executesql @PrimaryKeySql;
    END;

    DECLARE @CurrentIdColumnName SYSNAME;
    SELECT @CurrentIdColumnName = columns.name
    FROM sys.columns AS columns
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgSkillDefinition')
      AND UPPER(columns.name) = N'ID';

    IF @CurrentIdColumnName IS NULL
        THROW 51068, N'AgSkillDefinition.ID does not exist after type migration.', 1;

    IF @CurrentIdColumnName COLLATE Latin1_General_100_BIN2 <> N'ID'
        EXEC sys.sp_rename N'dbo.AgSkillDefinition.Id', N'ID', N'COLUMN';

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'IsDeleted') IS NULL
        ALTER TABLE dbo.AgSkillDefinition
            ADD IsDeleted BIT NOT NULL
                CONSTRAINT DF_AgSkillDefinition_IsDeleted DEFAULT (0) WITH VALUES;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'IsActive') IS NULL
        ALTER TABLE dbo.AgSkillDefinition
            ADD IsActive BIT NULL
                CONSTRAINT DF_AgSkillDefinition_IsActive DEFAULT (1) WITH VALUES;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'ImportDataId') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD ImportDataId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'ModificationNum') IS NULL
        ALTER TABLE dbo.AgSkillDefinition
            ADD ModificationNum INT NULL
                CONSTRAINT DF_AgSkillDefinition_ModificationNum DEFAULT (0) WITH VALUES;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'Tag') IS NULL
        ALTER TABLE dbo.AgSkillDefinition
            ADD Tag INT NULL
                CONSTRAINT DF_AgSkillDefinition_Tag DEFAULT (1) WITH VALUES;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'GroupId') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD GroupId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'CompanyId') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD CompanyId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'AuditStatus') IS NULL
        ALTER TABLE dbo.AgSkillDefinition
            ADD AuditStatus VARCHAR(32) NULL
                CONSTRAINT DF_AgSkillDefinition_AuditStatus DEFAULT ('Add') WITH VALUES;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'CurrentNode') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD CurrentNode NVARCHAR(32) NULL;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'CreatedBy') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD CreatedBy UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'CreatedTime') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD CreatedTime DATETIME NULL;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'UpdateBy') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD UpdateBy UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'UpdateTime') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD UpdateTime DATETIME NULL;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'Name') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD Name NVARCHAR(256) NULL;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'Description') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD Description NVARCHAR(MAX) NULL;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'Category') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD Category NVARCHAR(128) NULL;

    IF COL_LENGTH(N'dbo.AgSkillDefinition', N'Status') IS NULL
        ALTER TABLE dbo.AgSkillDefinition ADD Status VARCHAR(32) NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AgSkillDefinition')
          AND name = N'index_AgSkillDefinition_Enabled'
    )
        CREATE INDEX index_AgSkillDefinition_Enabled ON dbo.AgSkillDefinition(IsActive);

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AgSkillDefinition')
          AND name = N'index_AgSkillDefinition_IsDeleted'
    )
        CREATE INDEX index_AgSkillDefinition_IsDeleted ON dbo.AgSkillDefinition(IsDeleted);

    IF EXISTS (
        SELECT required.ColumnName
        FROM (VALUES
            (N'ID'), (N'IsDeleted'), (N'IsActive'), (N'ImportDataId'),
            (N'ModificationNum'), (N'Tag'), (N'GroupId'), (N'CompanyId'),
            (N'AuditStatus'), (N'CurrentNode'), (N'CreatedBy'),
            (N'CreatedTime'), (N'UpdateBy'), (N'UpdateTime'),
            (N'Code'), (N'DraftRevision'), (N'Name'), (N'Description'),
            (N'Category'), (N'Status')
        ) AS required(ColumnName)
        WHERE COL_LENGTH(N'dbo.AgSkillDefinition', required.ColumnName) IS NULL
    )
        THROW 51069, N'AgSkillDefinition column verification failed.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

PRINT N'AgSkillDefinition basic columns are ready; run 007 and Data/008 before deploying the normalized API.';
