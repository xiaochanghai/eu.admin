-- Convert losslessly representable NVARCHAR data columns in the normalized MCP tables to VARCHAR.
-- AgMcpToolVersion.Description remains NVARCHAR(MAX) because SQL Server 2014 code pages
-- cannot represent every Unicode character found in the imported tool descriptions.
-- LastError uses the declared VARCHAR(4096) contract.
-- Run after Data/012 and 013. The conversion stops instead of losing characters.
-- Stop EU.Core.Api.Agent and back up the database first. SQL Server 2014+.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgMcpServerDefinition', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgMcpServerArgument', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgMcpToolVersion', N'U') IS NULL
    THROW 51230, N'MCP normalized tables are missing. Complete migrations 010 through Data/012 first.', 1;

IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'DocumentJson') IS NOT NULL
    THROW 51231, N'MCP normalization is not finalized. Run the generated normalization script and Data/012 first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @CodeConstraintName SYSNAME;
    DECLARE @DropCodeConstraintSql NVARCHAR(MAX);
    SELECT @CodeConstraintName = constraintObject.name
    FROM sys.key_constraints AS constraintObject
    INNER JOIN sys.index_columns AS indexColumn
        ON indexColumn.object_id = constraintObject.parent_object_id
       AND indexColumn.index_id = constraintObject.unique_index_id
    INNER JOIN sys.columns AS columnObject
        ON columnObject.object_id = indexColumn.object_id
       AND columnObject.column_id = indexColumn.column_id
    WHERE constraintObject.parent_object_id = OBJECT_ID(N'dbo.AgMcpServerDefinition')
      AND constraintObject.[type] = N'UQ'
      AND columnObject.name = N'Code';

    IF @CodeConstraintName IS NOT NULL
    BEGIN
        SET @DropCodeConstraintSql =
            N'ALTER TABLE dbo.AgMcpServerDefinition DROP CONSTRAINT ' +
            QUOTENAME(@CodeConstraintName) + N';';
        EXEC sys.sp_executesql @DropCodeConstraintSql;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AgMcpServerDefinition')
          AND name = N'ux_ag_mcp_server_definition_code'
    )
        DROP INDEX ux_ag_mcp_server_definition_code ON dbo.AgMcpServerDefinition;

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
            WHEN tableObject.name = N'AgMcpServerDefinition' AND columnObject.name = N'LastError' THEN 4096
            WHEN columnObject.max_length = -1 THEN -1
            ELSE columnObject.max_length / 2
        END,
        columnObject.is_nullable
    FROM sys.tables AS tableObject
    INNER JOIN sys.schemas AS schemaObject
        ON schemaObject.schema_id = tableObject.schema_id
    INNER JOIN sys.columns AS columnObject
        ON columnObject.object_id = tableObject.object_id
    INNER JOIN sys.types AS typeObject
        ON typeObject.user_type_id = columnObject.user_type_id
    WHERE schemaObject.name = N'dbo'
      AND tableObject.name IN (
          N'AgMcpServerDefinition',
          N'AgMcpServerArgument',
          N'AgMcpToolVersion')
      AND typeObject.name = N'nvarchar'
      AND NOT (
          tableObject.name = N'AgMcpToolVersion'
          AND columnObject.name = N'Description');

    DECLARE @RowId INT = 1;
    DECLARE @RowCount INT = (SELECT COUNT(*) FROM @Columns);
    DECLARE @SchemaName SYSNAME;
    DECLARE @TableName SYSNAME;
    DECLARE @ColumnName SYSNAME;
    DECLARE @CharacterLength INT;
    DECLARE @IsNullable BIT;
    DECLARE @QualifiedTable NVARCHAR(517);
    DECLARE @QualifiedColumn NVARCHAR(258);
    DECLARE @Sql NVARCHAR(MAX);
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
            N'        CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), ' + @QualifiedColumn + N')) COLLATE DATABASE_DEFAULT' +
            N'            <> CONVERT(NVARCHAR(MAX), ' + @QualifiedColumn + N') COLLATE DATABASE_DEFAULT' +
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
            THROW 51232, @Message, 1;
        END;

        SET @Sql =
            N'ALTER TABLE ' + @QualifiedTable + N' ALTER COLUMN ' + @QualifiedColumn + N' VARCHAR(' +
            CASE WHEN @CharacterLength = -1
                THEN N'MAX'
                ELSE CONVERT(NVARCHAR(20), @CharacterLength)
            END + N') ' +
            CASE WHEN @IsNullable = 1 THEN N'NULL' ELSE N'NOT NULL' END + N';';
        EXEC sys.sp_executesql @Sql;

        SET @RowId += 1;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.AgMcpServerDefinition')
          AND name = N'ux_ag_mcp_server_definition_code'
    )
        ALTER TABLE dbo.AgMcpServerDefinition
            ADD CONSTRAINT ux_ag_mcp_server_definition_code UNIQUE (Code);

    IF EXISTS (
        SELECT 1
        FROM sys.tables AS tableObject
        INNER JOIN sys.schemas AS schemaObject
            ON schemaObject.schema_id = tableObject.schema_id
        INNER JOIN sys.columns AS columnObject
            ON columnObject.object_id = tableObject.object_id
        INNER JOIN sys.types AS typeObject
            ON typeObject.user_type_id = columnObject.user_type_id
        WHERE schemaObject.name = N'dbo'
          AND tableObject.name IN (
              N'AgMcpServerDefinition',
              N'AgMcpServerArgument',
              N'AgMcpToolVersion')
          AND typeObject.name = N'nvarchar'
          AND NOT (
              tableObject.name = N'AgMcpToolVersion'
              AND columnObject.name = N'Description')
    )
        THROW 51233, N'One or more MCP NVARCHAR columns were not converted.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
