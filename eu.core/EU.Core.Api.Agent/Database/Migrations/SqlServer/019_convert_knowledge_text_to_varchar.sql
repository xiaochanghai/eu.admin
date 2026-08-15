-- Convert all persisted character columns in the normalized Knowledge tables to VARCHAR.
-- Run after Data/017 and 018. The conversion stops instead of losing characters.
-- Stop EU.Core.Api.Agent and back up the database first. SQL Server 2014+.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgKnowledgeDocument', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgKnowledgeChunk', N'U') IS NULL
    THROW 51330, N'Knowledge normalized tables are missing. Complete migrations 015 through Data/017 first.', 1;
IF COL_LENGTH(N'dbo.AgKnowledgeBaseDefinition', N'DocumentJson') IS NOT NULL
    THROW 51331, N'Knowledge normalization is not finalized. Run the generated data script and Data/017 first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @CodeConstraintName SYSNAME;
    DECLARE @Sql NVARCHAR(MAX);
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

    DECLARE @Columns TABLE
    (
        RowId INT IDENTITY(1, 1) NOT NULL PRIMARY KEY,
        SchemaName SYSNAME NOT NULL,
        TableName SYSNAME NOT NULL,
        ColumnName SYSNAME NOT NULL,
        CharacterLength INT NOT NULL,
        IsNullable BIT NOT NULL
    );
    INSERT INTO @Columns (SchemaName, TableName, ColumnName, CharacterLength, IsNullable)
    SELECT
        schemaObject.name,
        tableObject.name,
        columnObject.name,
        CASE
            WHEN columnObject.max_length = -1 THEN -1
            WHEN typeObject.name IN (N'nchar', N'nvarchar') THEN columnObject.max_length / 2
            ELSE columnObject.max_length
        END,
        columnObject.is_nullable
    FROM sys.tables AS tableObject
    INNER JOIN sys.schemas AS schemaObject ON schemaObject.schema_id = tableObject.schema_id
    INNER JOIN sys.columns AS columnObject ON columnObject.object_id = tableObject.object_id
    INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
    WHERE schemaObject.name = N'dbo'
      AND tableObject.name IN (N'AgKnowledgeBaseDefinition', N'AgKnowledgeDocument', N'AgKnowledgeChunk')
      AND typeObject.name IN (N'char', N'nchar', N'nvarchar');

    DECLARE @RowId INT = 1;
    DECLARE @RowCount INT = (SELECT COUNT(*) FROM @Columns);
    DECLARE @SchemaName SYSNAME;
    DECLARE @TableName SYSNAME;
    DECLARE @ColumnName SYSNAME;
    DECLARE @CharacterLength INT;
    DECLARE @IsNullable BIT;
    DECLARE @QualifiedTable NVARCHAR(517);
    DECLARE @QualifiedColumn NVARCHAR(258);
    DECLARE @HasLoss BIT;
    DECLARE @Message NVARCHAR(2048);

    WHILE @RowId <= @RowCount
    BEGIN
        SELECT
            @SchemaName = SchemaName,
            @TableName = TableName,
            @ColumnName = ColumnName,
            @CharacterLength = CharacterLength,
            @IsNullable = IsNullable
        FROM @Columns
        WHERE RowId = @RowId;

        SET @QualifiedTable = QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);
        SET @QualifiedColumn = QUOTENAME(@ColumnName);
        SET @HasLoss = 0;
        SET @Sql =
            N'SELECT @HasLoss = CASE WHEN EXISTS (' +
            N'    SELECT 1 FROM ' + @QualifiedTable +
            N'    WHERE ' + @QualifiedColumn + N' IS NOT NULL AND (' +
            N'        CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), ' + @QualifiedColumn + N')))' +
            N'            <> CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), ' + @QualifiedColumn + N'))' +
            CASE WHEN @CharacterLength = -1
                THEN N''
                ELSE N' OR DATALENGTH(CONVERT(VARCHAR(MAX), ' + @QualifiedColumn + N')) > ' + CONVERT(NVARCHAR(20), @CharacterLength)
            END +
            N'    )' +
            N') THEN 1 ELSE 0 END;';
        EXEC sys.sp_executesql @Sql, N'@HasLoss BIT OUTPUT', @HasLoss OUTPUT;

        IF @HasLoss = 1
        BEGIN
            SET @Message = N'Cannot losslessly convert ' + @QualifiedTable + N'.' + @QualifiedColumn +
                N' to VARCHAR under the current database collation.';
            THROW 51332, @Message, 1;
        END;

        SET @Sql =
            N'ALTER TABLE ' + @QualifiedTable + N' ALTER COLUMN ' + @QualifiedColumn + N' VARCHAR(' +
            CASE WHEN @CharacterLength = -1 THEN N'MAX' ELSE CONVERT(NVARCHAR(20), @CharacterLength) END + N') ' +
            CASE WHEN @IsNullable = 1 THEN N'NULL' ELSE N'NOT NULL' END + N';';
        EXEC sys.sp_executesql @Sql;
        SET @RowId += 1;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition')
          AND name = N'ux_ag_knowledge_base_definition_code')
        CREATE UNIQUE INDEX ux_ag_knowledge_base_definition_code
            ON dbo.AgKnowledgeBaseDefinition(Code);

    IF EXISTS (
        SELECT 1
        FROM sys.tables AS tableObject
        INNER JOIN sys.schemas AS schemaObject ON schemaObject.schema_id = tableObject.schema_id
        INNER JOIN sys.columns AS columnObject ON columnObject.object_id = tableObject.object_id
        INNER JOIN sys.types AS typeObject ON typeObject.user_type_id = columnObject.user_type_id
        WHERE schemaObject.name = N'dbo'
          AND tableObject.name IN (N'AgKnowledgeBaseDefinition', N'AgKnowledgeDocument', N'AgKnowledgeChunk')
          AND typeObject.name IN (N'char', N'nchar', N'nvarchar'))
        THROW 51333, N'One or more Knowledge character columns were not converted to VARCHAR.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
