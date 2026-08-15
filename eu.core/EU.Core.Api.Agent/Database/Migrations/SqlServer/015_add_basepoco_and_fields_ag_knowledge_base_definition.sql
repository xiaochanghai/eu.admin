-- Prepare AgKnowledgeBaseDefinition for BasePoco and normalized knowledge fields.
-- DocumentJson remains until Data/017 finalizes the cutover. SQL Server 2014+.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
IF OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition', N'U') IS NULL
    THROW 51300, N'dbo.AgKnowledgeBaseDefinition does not exist. Run 001_initial_schema.sql first.', 1;
GO
BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdName SYSNAME, @IdType SYSNAME, @PkName SYSNAME, @PkType NVARCHAR(20), @Sql NVARCHAR(MAX);
    SELECT @IdName = c.name, @IdType = t.name
    FROM sys.columns c INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition') AND UPPER(c.name) = N'ID';
    IF @IdName IS NULL THROW 51301, N'AgKnowledgeBaseDefinition.Id is missing.', 1;

    IF @IdType <> N'uniqueidentifier'
    BEGIN
        IF @IdType NOT IN (N'char', N'varchar', N'nchar', N'nvarchar')
            THROW 51302, N'AgKnowledgeBaseDefinition.Id must be a character GUID or UNIQUEIDENTIFIER.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AgKnowledgeBaseDefinition WHERE TRY_CONVERT(UNIQUEIDENTIFIER, Id) IS NULL)
            THROW 51303, N'AgKnowledgeBaseDefinition contains an invalid GUID Id.', 1;
        SELECT @PkName = kc.name, @PkType = CASE WHEN i.type = 1 THEN N'CLUSTERED' ELSE N'NONCLUSTERED' END
        FROM sys.key_constraints kc INNER JOIN sys.indexes i
          ON i.object_id = kc.parent_object_id AND i.index_id = kc.unique_index_id
        WHERE kc.parent_object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition') AND kc.type = N'PK';
        IF @PkName IS NULL THROW 51304, N'AgKnowledgeBaseDefinition primary key is missing.', 1;
        SET @Sql = N'ALTER TABLE dbo.AgKnowledgeBaseDefinition DROP CONSTRAINT ' + QUOTENAME(@PkName) + N';';
        EXEC sys.sp_executesql @Sql;
        ALTER TABLE dbo.AgKnowledgeBaseDefinition ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL;
        SET @Sql = N'ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD CONSTRAINT ' + QUOTENAME(@PkName)
            + N' PRIMARY KEY ' + @PkType + N' (Id);';
        EXEC sys.sp_executesql @Sql;
    END;
    IF @IdName COLLATE Latin1_General_100_BIN2 <> N'ID'
        EXEC sys.sp_rename N'dbo.AgKnowledgeBaseDefinition.Id', N'ID', N'COLUMN';

    IF EXISTS (
        SELECT 1
        FROM dbo.AgKnowledgeBaseDefinition
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), DocumentJson)))
              <> CONVERT(VARBINARY(MAX), DocumentJson))
        THROW 51305, N'Knowledge DocumentJson contains characters that cannot be represented by VARCHAR under the current database collation.', 1;
    IF EXISTS (
        SELECT 1
        FROM dbo.AgKnowledgeBaseDefinition
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), Code)))
              <> CONVERT(VARBINARY(MAX), Code))
        THROW 51306, N'Knowledge Code contains characters that cannot be represented by VARCHAR under the current database collation.', 1;

    IF EXISTS (
        SELECT 1
        FROM sys.columns AS columnObject
        INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
        WHERE columnObject.object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition')
          AND columnObject.name = N'Code'
          AND typeObject.name IN (N'nchar', N'nvarchar'))
    BEGIN
        DECLARE @CodeConstraintName SYSNAME;
        SELECT @CodeConstraintName = constraintObject.name
        FROM sys.key_constraints AS constraintObject
        INNER JOIN sys.index_columns AS indexColumn
            ON indexColumn.object_id = constraintObject.parent_object_id
           AND indexColumn.index_id = constraintObject.unique_index_id
        INNER JOIN sys.columns AS columnObject
            ON columnObject.object_id = indexColumn.object_id
           AND columnObject.column_id = indexColumn.column_id
        WHERE constraintObject.parent_object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition')
          AND constraintObject.[type] = N'UQ'
          AND columnObject.name = N'Code';
        IF @CodeConstraintName IS NOT NULL
        BEGIN
            SET @Sql = N'ALTER TABLE dbo.AgKnowledgeBaseDefinition DROP CONSTRAINT ' + QUOTENAME(@CodeConstraintName) + N';';
            EXEC sys.sp_executesql @Sql;
        END;
        IF EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition')
              AND name = N'ux_ag_knowledge_base_definition_code')
            DROP INDEX ux_ag_knowledge_base_definition_code ON dbo.AgKnowledgeBaseDefinition;
        ALTER TABLE dbo.AgKnowledgeBaseDefinition
            ALTER COLUMN Code VARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL;
    END;

    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'IsDeleted') IS NULL
        ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD IsDeleted BIT NOT NULL CONSTRAINT DF_AgKnowledgeBaseDefinition_IsDeleted DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'IsActive') IS NULL
        ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD IsActive BIT NULL CONSTRAINT DF_AgKnowledgeBaseDefinition_IsActive DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'ImportDataId') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD ImportDataId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'ModificationNum') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD ModificationNum INT NULL CONSTRAINT DF_AgKnowledgeBaseDefinition_ModificationNum DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'Tag') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD Tag INT NULL CONSTRAINT DF_AgKnowledgeBaseDefinition_Tag DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'GroupId') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD GroupId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'CompanyId') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD CompanyId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'AuditStatus') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgKnowledgeBaseDefinition_AuditStatus DEFAULT ('Add') WITH VALUES;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'CurrentNode') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD CurrentNode VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'CreatedBy') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD CreatedBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'CreatedTime') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD CreatedTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'UpdateBy') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD UpdateBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'UpdateTime') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD UpdateTime DATETIME NULL;

    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'Name') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD Name VARCHAR(256) NULL;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'Description') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD Description VARCHAR(MAX) NULL;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'Status') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD Status VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'IndexedAtUtc') IS NULL ALTER TABLE dbo.AgKnowledgeBaseDefinition ADD IndexedAtUtc DATETIME2(7) NULL;

    IF EXISTS (
        SELECT 1 FROM sys.columns AS columnObject
        INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
        WHERE columnObject.object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition')
          AND columnObject.name = N'CurrentNode' AND typeObject.name = N'nvarchar')
        ALTER TABLE dbo.AgKnowledgeBaseDefinition ALTER COLUMN CurrentNode VARCHAR(32) NULL;
    IF EXISTS (
        SELECT 1 FROM sys.columns AS columnObject
        INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
        WHERE columnObject.object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition')
          AND columnObject.name = N'Name' AND typeObject.name = N'nvarchar')
        ALTER TABLE dbo.AgKnowledgeBaseDefinition ALTER COLUMN Name VARCHAR(256) NULL;
    IF EXISTS (
        SELECT 1 FROM sys.columns AS columnObject
        INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
        WHERE columnObject.object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition')
          AND columnObject.name = N'Description' AND typeObject.name = N'nvarchar')
        ALTER TABLE dbo.AgKnowledgeBaseDefinition ALTER COLUMN Description VARCHAR(MAX) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition') AND name = N'index_AgKnowledgeBaseDefinition_IsDeleted')
        CREATE INDEX index_AgKnowledgeBaseDefinition_IsDeleted ON dbo.AgKnowledgeBaseDefinition(IsDeleted);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition') AND name = N'ux_ag_knowledge_base_definition_code')
        CREATE UNIQUE INDEX ux_ag_knowledge_base_definition_code ON dbo.AgKnowledgeBaseDefinition(Code);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
