-- BasePoco compatibility pilot for dbo.AgAgentDefinition only.
-- Safely converts a legacy character Id to UNIQUEIDENTIFIER, preserves the
-- primary key name/type, and adds non-key common columns. Existing definition
-- data and historical audit timestamps are preserved.
-- SQL Server 2014+

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.AgAgentDefinition', N'U') IS NULL
    THROW 51010, N'dbo.AgAgentDefinition does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdType SYSNAME;
    SELECT @IdType = types.name
    FROM sys.columns AS columns
    INNER JOIN sys.types AS types
        ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgAgentDefinition')
      AND columns.name = N'Id';

    IF @IdType IS NULL
        THROW 51012, N'dbo.AgAgentDefinition.Id does not exist.', 1;

    IF @IdType <> N'uniqueidentifier'
    BEGIN
        IF @IdType NOT IN (N'char', N'varchar', N'nchar', N'nvarchar')
            THROW 51013, N'AgAgentDefinition.Id must be a character GUID or UNIQUEIDENTIFIER.', 1;

        IF EXISTS (
            SELECT 1
            FROM dbo.AgAgentDefinition
            WHERE TRY_CONVERT(UNIQUEIDENTIFIER, Id) IS NULL
        )
            THROW 51014, N'AgAgentDefinition contains an Id that is not a valid GUID.', 1;

        IF EXISTS (
            SELECT TRY_CONVERT(UNIQUEIDENTIFIER, Id)
            FROM dbo.AgAgentDefinition
            GROUP BY TRY_CONVERT(UNIQUEIDENTIFIER, Id)
            HAVING COUNT_BIG(*) > 1
        )
            THROW 51015, N'AgAgentDefinition contains Id values that collide after GUID conversion.', 1;

        IF EXISTS (
            SELECT 1
            FROM sys.foreign_key_columns AS foreignKeyColumns
            WHERE (
                foreignKeyColumns.parent_object_id = OBJECT_ID(N'dbo.AgAgentDefinition')
                AND foreignKeyColumns.parent_column_id = COLUMNPROPERTY(
                    OBJECT_ID(N'dbo.AgAgentDefinition'), N'Id', N'ColumnId')
            ) OR (
                foreignKeyColumns.referenced_object_id = OBJECT_ID(N'dbo.AgAgentDefinition')
                AND foreignKeyColumns.referenced_column_id = COLUMNPROPERTY(
                    OBJECT_ID(N'dbo.AgAgentDefinition'), N'Id', N'ColumnId')
            )
        )
            THROW 51016, N'AgAgentDefinition.Id has foreign-key dependencies. Migrate those columns together first.', 1;

        IF EXISTS (
            SELECT 1
            FROM sys.index_columns AS indexColumns
            INNER JOIN sys.indexes AS indexes
                ON indexes.object_id = indexColumns.object_id
               AND indexes.index_id = indexColumns.index_id
            WHERE indexColumns.object_id = OBJECT_ID(N'dbo.AgAgentDefinition')
              AND indexColumns.column_id = COLUMNPROPERTY(
                  OBJECT_ID(N'dbo.AgAgentDefinition'), N'Id', N'ColumnId')
              AND indexes.is_primary_key = 0
        )
            THROW 51017, N'AgAgentDefinition.Id has a non-primary-key index. Drop or migrate that index explicitly first.', 1;

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
        WHERE keyConstraints.parent_object_id = OBJECT_ID(N'dbo.AgAgentDefinition')
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
        WHERE indexes.object_id = OBJECT_ID(N'dbo.AgAgentDefinition')
          AND indexes.is_primary_key = 1
          AND indexColumns.key_ordinal > 0;

        IF @PrimaryKeyName IS NULL
           OR @PrimaryKeyColumnCount <> 1
           OR @PrimaryKeyIdColumnCount <> 1
            THROW 51018, N'AgAgentDefinition must have a single-column primary key on Id.', 1;

        DECLARE @PrimaryKeySql NVARCHAR(MAX);
        SET @PrimaryKeySql = N'ALTER TABLE dbo.AgAgentDefinition DROP CONSTRAINT '
            + QUOTENAME(@PrimaryKeyName) + N';';
        EXEC sys.sp_executesql @PrimaryKeySql;

        ALTER TABLE dbo.AgAgentDefinition
            ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL;

        SET @PrimaryKeySql = N'ALTER TABLE dbo.AgAgentDefinition ADD CONSTRAINT '
            + QUOTENAME(@PrimaryKeyName) + N' PRIMARY KEY '
            + @PrimaryKeyType + N' (Id);';
        EXEC sys.sp_executesql @PrimaryKeySql;
    END;

    DECLARE @CurrentIdColumnName SYSNAME;
    SELECT @CurrentIdColumnName = columns.name
    FROM sys.columns AS columns
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgAgentDefinition')
      AND UPPER(columns.name) = N'ID';

    IF @CurrentIdColumnName IS NULL
        THROW 51019, N'AgAgentDefinition.ID does not exist after type migration.', 1;

    IF @CurrentIdColumnName COLLATE Latin1_General_100_BIN2 <> N'ID'
        EXEC sys.sp_rename N'dbo.AgAgentDefinition.Id', N'ID', N'COLUMN';

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'IsDeleted') IS NULL
        ALTER TABLE dbo.AgAgentDefinition
            ADD IsDeleted BIT NOT NULL
                CONSTRAINT DF_AgAgentDefinition_IsDeleted DEFAULT (0) WITH VALUES;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'IsActive') IS NULL
        ALTER TABLE dbo.AgAgentDefinition
            ADD IsActive BIT NULL
                CONSTRAINT DF_AgAgentDefinition_IsActive DEFAULT (1) WITH VALUES;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'ImportDataId') IS NULL
        ALTER TABLE dbo.AgAgentDefinition ADD ImportDataId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'ModificationNum') IS NULL
        ALTER TABLE dbo.AgAgentDefinition
            ADD ModificationNum INT NULL
                CONSTRAINT DF_AgAgentDefinition_ModificationNum DEFAULT (0) WITH VALUES;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'Tag') IS NULL
        ALTER TABLE dbo.AgAgentDefinition
            ADD Tag INT NULL
                CONSTRAINT DF_AgAgentDefinition_Tag DEFAULT (1) WITH VALUES;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'GroupId') IS NULL
        ALTER TABLE dbo.AgAgentDefinition ADD GroupId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'CompanyId') IS NULL
        ALTER TABLE dbo.AgAgentDefinition ADD CompanyId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'AuditStatus') IS NULL
        ALTER TABLE dbo.AgAgentDefinition
            ADD AuditStatus VARCHAR(32) NULL
                CONSTRAINT DF_AgAgentDefinition_AuditStatus DEFAULT ('Add') WITH VALUES;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'CurrentNode') IS NULL
        ALTER TABLE dbo.AgAgentDefinition ADD CurrentNode NVARCHAR(32) NULL;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'CreatedBy') IS NULL
        ALTER TABLE dbo.AgAgentDefinition ADD CreatedBy UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'CreatedTime') IS NULL
        ALTER TABLE dbo.AgAgentDefinition ADD CreatedTime DATETIME NULL;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'UpdateBy') IS NULL
        ALTER TABLE dbo.AgAgentDefinition ADD UpdateBy UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'dbo.AgAgentDefinition', N'UpdateTime') IS NULL
        ALTER TABLE dbo.AgAgentDefinition ADD UpdateTime DATETIME NULL;

    IF EXISTS (
        SELECT required.ColumnName
        FROM (VALUES
            (N'IsDeleted'), (N'IsActive'), (N'ImportDataId'),
            (N'ModificationNum'), (N'Tag'), (N'GroupId'), (N'CompanyId'),
            (N'AuditStatus'), (N'CurrentNode'), (N'CreatedBy'),
            (N'CreatedTime'), (N'UpdateBy'), (N'UpdateTime')
        ) AS required(ColumnName)
        WHERE COL_LENGTH(N'dbo.AgAgentDefinition', required.ColumnName) IS NULL
    )
        THROW 51011, N'AgAgentDefinition BasePoco column verification failed.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
