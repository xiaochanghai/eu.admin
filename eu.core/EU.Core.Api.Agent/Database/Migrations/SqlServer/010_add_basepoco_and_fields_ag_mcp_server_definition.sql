-- Prepare AgMcpServerDefinition for EU.Core BasePoco and normalized MCP fields.
-- Existing DocumentJson is retained until Data/012 finalizes the cutover.
-- SQL Server 2014+.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgMcpServerDefinition', N'U') IS NULL
    THROW 51200, N'dbo.AgMcpServerDefinition does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdName SYSNAME;
    DECLARE @IdType SYSNAME;
    SELECT @IdName = columns.name, @IdType = types.name
    FROM sys.columns AS columns
    INNER JOIN sys.types AS types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgMcpServerDefinition')
      AND UPPER(columns.name) = N'ID';

    IF @IdName IS NULL
        THROW 51201, N'AgMcpServerDefinition.Id is missing.', 1;

    IF @IdType <> N'uniqueidentifier'
    BEGIN
        IF @IdType NOT IN (N'char', N'varchar', N'nchar', N'nvarchar')
            THROW 51202, N'AgMcpServerDefinition.Id must be a character GUID or UNIQUEIDENTIFIER.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AgMcpServerDefinition WHERE TRY_CONVERT(UNIQUEIDENTIFIER, Id) IS NULL)
            THROW 51203, N'AgMcpServerDefinition contains an invalid GUID Id.', 1;

        DECLARE @PkName SYSNAME;
        DECLARE @PkType NVARCHAR(20);
        SELECT @PkName = constraints.name,
               @PkType = CASE WHEN indexes.type = 1 THEN N'CLUSTERED' ELSE N'NONCLUSTERED' END
        FROM sys.key_constraints AS constraints
        INNER JOIN sys.indexes AS indexes
            ON indexes.object_id = constraints.parent_object_id
           AND indexes.index_id = constraints.unique_index_id
        WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgMcpServerDefinition')
          AND constraints.type = N'PK';
        IF @PkName IS NULL
            THROW 51204, N'AgMcpServerDefinition primary key is missing.', 1;

        DECLARE @Sql NVARCHAR(MAX) = N'ALTER TABLE dbo.AgMcpServerDefinition DROP CONSTRAINT ' + QUOTENAME(@PkName) + N';';
        EXEC sys.sp_executesql @Sql;
        ALTER TABLE dbo.AgMcpServerDefinition ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL;
        SET @Sql = N'ALTER TABLE dbo.AgMcpServerDefinition ADD CONSTRAINT ' + QUOTENAME(@PkName)
            + N' PRIMARY KEY ' + @PkType + N' (Id);';
        EXEC sys.sp_executesql @Sql;
    END;

    IF @IdName COLLATE Latin1_General_100_BIN2 <> N'ID'
        EXEC sys.sp_rename N'dbo.AgMcpServerDefinition.Id', N'ID', N'COLUMN';

    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'IsDeleted') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD IsDeleted BIT NOT NULL CONSTRAINT DF_AgMcpServerDefinition_IsDeleted DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'IsActive') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD IsActive BIT NULL CONSTRAINT DF_AgMcpServerDefinition_IsActive DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'ImportDataId') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD ImportDataId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'ModificationNum') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD ModificationNum INT NULL CONSTRAINT DF_AgMcpServerDefinition_ModificationNum DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'Tag') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD Tag INT NULL CONSTRAINT DF_AgMcpServerDefinition_Tag DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'GroupId') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD GroupId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'CompanyId') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD CompanyId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'AuditStatus') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD AuditStatus VARCHAR(32) NULL CONSTRAINT DF_AgMcpServerDefinition_AuditStatus DEFAULT ('Add') WITH VALUES;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'CurrentNode') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD CurrentNode NVARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'CreatedBy') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD CreatedBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'CreatedTime') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD CreatedTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'UpdateBy') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD UpdateBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'UpdateTime') IS NULL
        ALTER TABLE dbo.AgMcpServerDefinition ADD UpdateTime DATETIME NULL;

    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'Name') IS NULL ALTER TABLE dbo.AgMcpServerDefinition ADD Name NVARCHAR(256) NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'Description') IS NULL ALTER TABLE dbo.AgMcpServerDefinition ADD Description NVARCHAR(MAX) NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'Transport') IS NULL ALTER TABLE dbo.AgMcpServerDefinition ADD Transport VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'Endpoint') IS NULL ALTER TABLE dbo.AgMcpServerDefinition ADD Endpoint NVARCHAR(2048) NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'Command') IS NULL ALTER TABLE dbo.AgMcpServerDefinition ADD Command NVARCHAR(512) NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'CredentialAlias') IS NULL ALTER TABLE dbo.AgMcpServerDefinition ADD CredentialAlias NVARCHAR(200) NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'Enabled') IS NULL ALTER TABLE dbo.AgMcpServerDefinition ADD Enabled BIT NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'Status') IS NULL ALTER TABLE dbo.AgMcpServerDefinition ADD Status VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'LastError') IS NULL ALTER TABLE dbo.AgMcpServerDefinition ADD LastError NVARCHAR(MAX) NULL;
    IF COL_LENGTH(N'dbo.AgMcpServerDefinition', N'LastSyncedAtUtc') IS NULL ALTER TABLE dbo.AgMcpServerDefinition ADD LastSyncedAtUtc DATETIME2(7) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgMcpServerDefinition') AND name = N'index_AgMcpServerDefinition_IsDeleted')
        CREATE INDEX index_AgMcpServerDefinition_IsDeleted ON dbo.AgMcpServerDefinition(IsDeleted);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgMcpServerDefinition') AND name = N'ux_ag_mcp_server_definition_code')
        CREATE UNIQUE INDEX ux_ag_mcp_server_definition_code ON dbo.AgMcpServerDefinition(Code);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
