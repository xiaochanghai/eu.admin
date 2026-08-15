-- Normalize Main Agent assignment persistence for BasePoco and SqlSugar.
-- Existing CHAR(36) Agent identifier columns are intentionally preserved.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgMainAgentAssignment', N'U') IS NULL
    THROW 51900, N'dbo.AgMainAgentAssignment does not exist. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.AgMainAgentAssignment WHERE AssignmentKey <> 'platform-main-agent')
        THROW 51901, N'AgMainAgentAssignment contains an unsupported assignment key.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgMainAgentAssignment WHERE TRY_CONVERT(UNIQUEIDENTIFIER, AgentId) IS NULL)
        THROW 51902, N'AgMainAgentAssignment contains an invalid AgentId.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgMainAgentAssignment WHERE TRY_CONVERT(UNIQUEIDENTIFIER, AgentVersionId) IS NULL)
        THROW 51903, N'AgMainAgentAssignment contains an invalid AgentVersionId.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgMainAgentAssignment WHERE LogicalRevision < 0)
        THROW 51904, N'AgMainAgentAssignment contains a negative logical revision.', 1;

    DECLARE @UpdatedAtType SYSNAME;
    SELECT @UpdatedAtType = types.name
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgMainAgentAssignment')
      AND columns.name = N'UpdatedAtUtc';
    IF @UpdatedAtType <> N'datetime2'
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.AgMainAgentAssignment WHERE TRY_CONVERT(DATETIMEOFFSET(7), UpdatedAtUtc, 127) IS NULL)
            THROW 51905, N'AgMainAgentAssignment.UpdatedAtUtc contains an invalid timestamp.', 1;
        IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'UpdatedAtUtcValue') IS NULL
            ALTER TABLE dbo.AgMainAgentAssignment ADD UpdatedAtUtcValue DATETIME2(7) NULL;
        EXEC sys.sp_executesql N'
            UPDATE dbo.AgMainAgentAssignment
            SET UpdatedAtUtcValue = CONVERT(DATETIME2(7), TRY_CONVERT(DATETIMEOFFSET(7), UpdatedAtUtc, 127))
            WHERE UpdatedAtUtcValue IS NULL;
            IF EXISTS (SELECT 1 FROM dbo.AgMainAgentAssignment WHERE UpdatedAtUtcValue IS NULL)
                THROW 51906, N''AgMainAgentAssignment.UpdatedAtUtc conversion failed.'', 1;';
        ALTER TABLE dbo.AgMainAgentAssignment DROP COLUMN UpdatedAtUtc;
        EXEC sys.sp_rename N'dbo.AgMainAgentAssignment.UpdatedAtUtcValue', N'UpdatedAtUtc', N'COLUMN';
        ALTER TABLE dbo.AgMainAgentAssignment ALTER COLUMN UpdatedAtUtc DATETIME2(7) NOT NULL;
    END;

    DECLARE @Sql NVARCHAR(MAX);
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'ID') IS NULL
    BEGIN
        ALTER TABLE dbo.AgMainAgentAssignment ADD ID UNIQUEIDENTIFIER NULL;
        EXEC sys.sp_executesql N'
            UPDATE dbo.AgMainAgentAssignment SET ID = NEWID() WHERE ID IS NULL;
            ALTER TABLE dbo.AgMainAgentAssignment ALTER COLUMN ID UNIQUEIDENTIFIER NOT NULL;';
    END;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'IsDeleted') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD IsDeleted BIT NOT NULL DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'IsActive') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD IsActive BIT NULL DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'ImportDataId') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD ImportDataId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'ModificationNum') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD ModificationNum INT NULL DEFAULT (0) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'Tag') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD Tag INT NULL DEFAULT (1) WITH VALUES;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'GroupId') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD GroupId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'CompanyId') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD CompanyId UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'AuditStatus') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD AuditStatus VARCHAR(32) NULL DEFAULT ('Add') WITH VALUES;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'CurrentNode') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD CurrentNode VARCHAR(32) NULL;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'CreatedBy') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD CreatedBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'CreatedTime') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD CreatedTime DATETIME NULL;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'UpdateBy') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD UpdateBy UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH(N'dbo.AgMainAgentAssignment', N'UpdateTime') IS NULL ALTER TABLE dbo.AgMainAgentAssignment ADD UpdateTime DATETIME NULL;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints constraints
        INNER JOIN sys.index_columns indexColumns
          ON indexColumns.object_id = constraints.parent_object_id
         AND indexColumns.index_id = constraints.unique_index_id
        INNER JOIN sys.columns columns
          ON columns.object_id = indexColumns.object_id
         AND columns.column_id = indexColumns.column_id
        WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgMainAgentAssignment')
          AND constraints.[type] = N'PK'
          AND columns.name = N'ID')
    BEGIN
        DECLARE @PkName SYSNAME;
        SELECT @PkName = name FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.AgMainAgentAssignment') AND [type] = N'PK';
        IF @PkName IS NOT NULL
        BEGIN
            SET @Sql = N'ALTER TABLE dbo.AgMainAgentAssignment DROP CONSTRAINT ' + QUOTENAME(@PkName) + N';';
            EXEC sys.sp_executesql @Sql;
        END;
        ALTER TABLE dbo.AgMainAgentAssignment
            ADD CONSTRAINT pk_ag_main_agent_assignment PRIMARY KEY (ID);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgMainAgentAssignment') AND name = N'ux_ag_main_agent_assignment_key')
        CREATE UNIQUE INDEX ux_ag_main_agent_assignment_key ON dbo.AgMainAgentAssignment(AssignmentKey);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgMainAgentAssignment') AND name = N'ix_ag_main_agent_assignment_is_deleted')
        CREATE INDEX ix_ag_main_agent_assignment_is_deleted ON dbo.AgMainAgentAssignment(IsDeleted);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgMainAgentAssignment') AND name = N'ix_ag_main_agent_assignment_is_active')
        CREATE INDEX ix_ag_main_agent_assignment_is_active ON dbo.AgMainAgentAssignment(IsActive);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
