-- Normalize existing orchestration run detail tables and create the run-node summary table.
-- Existing CHAR(36) identifier columns are intentionally preserved.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationRun', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationRunDetail', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationNodeAttempt', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgOrchestrationToolCall', N'U') IS NULL
    THROW 51810, N'Orchestration run tables are missing.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationRunDetail
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), InputText))) <> CONVERT(VARBINARY(MAX), InputText)
           OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), OutputText))) <> CONVERT(VARBINARY(MAX), OutputText))
        THROW 51811, N'AgOrchestrationRunDetail text cannot be represented by VARCHAR under the current database collation.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationNodeAttempt
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), InputText))) <> CONVERT(VARBINARY(MAX), InputText)
           OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), OutputText))) <> CONVERT(VARBINARY(MAX), OutputText))
        THROW 51812, N'AgOrchestrationNodeAttempt text cannot be represented by VARCHAR under the current database collation.', 1;
    IF EXISTS (
        SELECT 1 FROM dbo.AgOrchestrationToolCall
        WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), ToolName))) <> CONVERT(VARBINARY(MAX), ToolName)
           OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), ArgumentsJson))) <> CONVERT(VARBINARY(MAX), ArgumentsJson)
           OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), ResultContent))) <> CONVERT(VARBINARY(MAX), ResultContent))
        THROW 51813, N'AgOrchestrationToolCall text cannot be represented by VARCHAR under the current database collation.', 1;

    ALTER TABLE dbo.AgOrchestrationRunDetail ALTER COLUMN InputText VARCHAR(MAX) NOT NULL;
    ALTER TABLE dbo.AgOrchestrationRunDetail ALTER COLUMN OutputText VARCHAR(MAX) NOT NULL;
    ALTER TABLE dbo.AgOrchestrationNodeAttempt ALTER COLUMN InputText VARCHAR(MAX) NOT NULL;
    ALTER TABLE dbo.AgOrchestrationNodeAttempt ALTER COLUMN OutputText VARCHAR(MAX) NOT NULL;
    ALTER TABLE dbo.AgOrchestrationNodeAttempt ALTER COLUMN InputSha256 VARCHAR(64) NOT NULL;
    ALTER TABLE dbo.AgOrchestrationNodeAttempt ALTER COLUMN OutputSha256 VARCHAR(64) NOT NULL;
    ALTER TABLE dbo.AgOrchestrationToolCall ALTER COLUMN ToolName VARCHAR(256) NOT NULL;
    ALTER TABLE dbo.AgOrchestrationToolCall ALTER COLUMN ArgumentsJson VARCHAR(MAX) NOT NULL;
    ALTER TABLE dbo.AgOrchestrationToolCall ALTER COLUMN ResultContent VARCHAR(MAX) NOT NULL;
    ALTER TABLE dbo.AgOrchestrationToolCall ALTER COLUMN ResultSha256 VARCHAR(64) NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationNodeAttempt') AND name = N'ix_ag_orchestration_node_attempt_order')
        DROP INDEX ix_ag_orchestration_node_attempt_order ON dbo.AgOrchestrationNodeAttempt;
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationToolCall') AND name = N'ix_ag_orchestration_tool_call_order')
        DROP INDEX ix_ag_orchestration_tool_call_order ON dbo.AgOrchestrationToolCall;

    DECLARE @AttemptPk SYSNAME, @Sql NVARCHAR(MAX);
    SELECT @AttemptPk = name FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.AgOrchestrationNodeAttempt') AND [type] = N'PK';
    IF @AttemptPk IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.AgOrchestrationNodeAttempt DROP CONSTRAINT ' + QUOTENAME(@AttemptPk) + N';';
        EXEC sys.sp_executesql @Sql;
    END;
    ALTER TABLE dbo.AgOrchestrationNodeAttempt ALTER COLUMN NodeId VARCHAR(64) NOT NULL;
    ALTER TABLE dbo.AgOrchestrationToolCall ALTER COLUMN NodeId VARCHAR(64) NOT NULL;
    IF @AttemptPk IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE dbo.AgOrchestrationNodeAttempt ADD CONSTRAINT ' + QUOTENAME(@AttemptPk)
            + N' PRIMARY KEY (RunId, NodeId, Attempt);';
        EXEC sys.sp_executesql @Sql;
    END;

    IF EXISTS (SELECT 1 FROM dbo.AgOrchestrationNodeAttempt WHERE TRY_CONVERT(DATETIMEOFFSET(7), StartedAtUtc, 127) IS NULL OR (FinishedAtUtc IS NOT NULL AND TRY_CONVERT(DATETIMEOFFSET(7), FinishedAtUtc, 127) IS NULL))
        THROW 51814, N'AgOrchestrationNodeAttempt contains an invalid timestamp.', 1;
    IF EXISTS (SELECT 1 FROM dbo.AgOrchestrationToolCall WHERE TRY_CONVERT(DATETIMEOFFSET(7), StartedAtUtc, 127) IS NULL OR (FinishedAtUtc IS NOT NULL AND TRY_CONVERT(DATETIMEOFFSET(7), FinishedAtUtc, 127) IS NULL))
        THROW 51815, N'AgOrchestrationToolCall contains an invalid timestamp.', 1;

    ALTER TABLE dbo.AgOrchestrationNodeAttempt ADD StartedAtUtcValue DATETIME2(7) NULL, FinishedAtUtcValue DATETIME2(7) NULL;
    EXEC sys.sp_executesql N'UPDATE dbo.AgOrchestrationNodeAttempt SET StartedAtUtcValue=CONVERT(DATETIME2(7),TRY_CONVERT(DATETIMEOFFSET(7),StartedAtUtc,127)), FinishedAtUtcValue=CASE WHEN FinishedAtUtc IS NULL THEN NULL ELSE CONVERT(DATETIME2(7),TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127)) END;';
    ALTER TABLE dbo.AgOrchestrationNodeAttempt DROP COLUMN StartedAtUtc, FinishedAtUtc;
    EXEC sys.sp_rename N'dbo.AgOrchestrationNodeAttempt.StartedAtUtcValue', N'StartedAtUtc', N'COLUMN';
    EXEC sys.sp_rename N'dbo.AgOrchestrationNodeAttempt.FinishedAtUtcValue', N'FinishedAtUtc', N'COLUMN';
    ALTER TABLE dbo.AgOrchestrationNodeAttempt ALTER COLUMN StartedAtUtc DATETIME2(7) NOT NULL;

    ALTER TABLE dbo.AgOrchestrationToolCall ADD StartedAtUtcValue DATETIME2(7) NULL, FinishedAtUtcValue DATETIME2(7) NULL;
    EXEC sys.sp_executesql N'UPDATE dbo.AgOrchestrationToolCall SET StartedAtUtcValue=CONVERT(DATETIME2(7),TRY_CONVERT(DATETIMEOFFSET(7),StartedAtUtc,127)), FinishedAtUtcValue=CASE WHEN FinishedAtUtc IS NULL THEN NULL ELSE CONVERT(DATETIME2(7),TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127)) END;';
    ALTER TABLE dbo.AgOrchestrationToolCall DROP COLUMN StartedAtUtc, FinishedAtUtc;
    EXEC sys.sp_rename N'dbo.AgOrchestrationToolCall.StartedAtUtcValue', N'StartedAtUtc', N'COLUMN';
    EXEC sys.sp_rename N'dbo.AgOrchestrationToolCall.FinishedAtUtcValue', N'FinishedAtUtc', N'COLUMN';
    ALTER TABLE dbo.AgOrchestrationToolCall ALTER COLUMN StartedAtUtc DATETIME2(7) NOT NULL;

    DECLARE @TableName SYSNAME;
    DECLARE base_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT name FROM sys.tables WHERE name IN (N'AgOrchestrationRunDetail', N'AgOrchestrationNodeAttempt', N'AgOrchestrationToolCall');
    OPEN base_cursor;
    FETCH NEXT FROM base_cursor INTO @TableName;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF COL_LENGTH(N'dbo.' + @TableName, N'ID') IS NULL
        BEGIN
            SET @Sql = N'ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD ID UNIQUEIDENTIFIER NULL;';
            EXEC sys.sp_executesql @Sql;
            SET @Sql = N'UPDATE dbo.' + QUOTENAME(@TableName) + N' SET ID=NEWID() WHERE ID IS NULL; ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ALTER COLUMN ID UNIQUEIDENTIFIER NOT NULL;';
            EXEC sys.sp_executesql @Sql;
        END;
        SET @Sql = N'
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''IsDeleted'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD IsDeleted BIT NOT NULL DEFAULT(0) WITH VALUES;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''IsActive'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD IsActive BIT NULL DEFAULT(1) WITH VALUES;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''ImportDataId'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD ImportDataId UNIQUEIDENTIFIER NULL;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''ModificationNum'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD ModificationNum INT NULL DEFAULT(0) WITH VALUES;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''Tag'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD Tag INT NULL DEFAULT(1) WITH VALUES;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''GroupId'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD GroupId UNIQUEIDENTIFIER NULL;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''CompanyId'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD CompanyId UNIQUEIDENTIFIER NULL;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''AuditStatus'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD AuditStatus VARCHAR(32) NULL DEFAULT(''Add'') WITH VALUES;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''CurrentNode'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD CurrentNode VARCHAR(32) NULL;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''CreatedBy'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD CreatedBy UNIQUEIDENTIFIER NULL;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''CreatedTime'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD CreatedTime DATETIME NULL;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''UpdateBy'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD UpdateBy UNIQUEIDENTIFIER NULL;
            IF COL_LENGTH(N''dbo.' + @TableName + N''',N''UpdateTime'') IS NULL ALTER TABLE dbo.' + QUOTENAME(@TableName) + N' ADD UpdateTime DATETIME NULL;';
        EXEC sys.sp_executesql @Sql;
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.' + @TableName) AND name=N'ix_' + LOWER(@TableName) + N'_is_deleted')
        BEGIN
            SET @Sql=N'CREATE INDEX ' + QUOTENAME(N'ix_' + LOWER(@TableName) + N'_is_deleted') + N' ON dbo.' + QUOTENAME(@TableName) + N'(IsDeleted);';
            EXEC sys.sp_executesql @Sql;
        END;
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.' + @TableName) AND name=N'ix_' + LOWER(@TableName) + N'_is_active')
        BEGIN
            SET @Sql=N'CREATE INDEX ' + QUOTENAME(N'ix_' + LOWER(@TableName) + N'_is_active') + N' ON dbo.' + QUOTENAME(@TableName) + N'(IsActive);';
            EXEC sys.sp_executesql @Sql;
        END;
        FETCH NEXT FROM base_cursor INTO @TableName;
    END;
    CLOSE base_cursor;
    DEALLOCATE base_cursor;

    DECLARE @DetailPk SYSNAME, @ToolPk SYSNAME;
    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints constraints
        INNER JOIN sys.index_columns indexColumns
          ON indexColumns.object_id = constraints.parent_object_id
         AND indexColumns.index_id = constraints.unique_index_id
        INNER JOIN sys.columns columns
          ON columns.object_id = indexColumns.object_id
         AND columns.column_id = indexColumns.column_id
        WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgOrchestrationRunDetail')
          AND constraints.[type] = N'PK'
          AND columns.name = N'ID')
    BEGIN
        SELECT @DetailPk = name FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.AgOrchestrationRunDetail') AND [type] = N'PK';
        IF @DetailPk IS NOT NULL
        BEGIN
            SET @Sql = N'ALTER TABLE dbo.AgOrchestrationRunDetail DROP CONSTRAINT ' + QUOTENAME(@DetailPk) + N';';
            EXEC sys.sp_executesql @Sql;
        END;
        ALTER TABLE dbo.AgOrchestrationRunDetail
            ADD CONSTRAINT pk_ag_orchestration_run_detail PRIMARY KEY (ID);
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgOrchestrationRunDetail') AND name=N'ux_ag_orchestration_run_detail_run')
        CREATE UNIQUE INDEX ux_ag_orchestration_run_detail_run ON dbo.AgOrchestrationRunDetail(RunId);

    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints constraints
        INNER JOIN sys.index_columns indexColumns
          ON indexColumns.object_id = constraints.parent_object_id
         AND indexColumns.index_id = constraints.unique_index_id
        INNER JOIN sys.columns columns
          ON columns.object_id = indexColumns.object_id
         AND columns.column_id = indexColumns.column_id
        WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgOrchestrationNodeAttempt')
          AND constraints.[type] = N'PK'
          AND columns.name = N'ID')
    BEGIN
        SELECT @AttemptPk = name FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.AgOrchestrationNodeAttempt') AND [type] = N'PK';
        IF @AttemptPk IS NOT NULL
        BEGIN
            SET @Sql = N'ALTER TABLE dbo.AgOrchestrationNodeAttempt DROP CONSTRAINT ' + QUOTENAME(@AttemptPk) + N';';
            EXEC sys.sp_executesql @Sql;
        END;
        ALTER TABLE dbo.AgOrchestrationNodeAttempt
            ADD CONSTRAINT pk_ag_orchestration_node_attempt PRIMARY KEY (ID);
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgOrchestrationNodeAttempt') AND name=N'ux_ag_orchestration_node_attempt_identity')
        CREATE UNIQUE INDEX ux_ag_orchestration_node_attempt_identity ON dbo.AgOrchestrationNodeAttempt(RunId, NodeId, Attempt);

    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints constraints
        INNER JOIN sys.index_columns indexColumns
          ON indexColumns.object_id = constraints.parent_object_id
         AND indexColumns.index_id = constraints.unique_index_id
        INNER JOIN sys.columns columns
          ON columns.object_id = indexColumns.object_id
         AND columns.column_id = indexColumns.column_id
        WHERE constraints.parent_object_id = OBJECT_ID(N'dbo.AgOrchestrationToolCall')
          AND constraints.[type] = N'PK'
          AND columns.name = N'ID')
    BEGIN
        SELECT @ToolPk = name FROM sys.key_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.AgOrchestrationToolCall') AND [type] = N'PK';
        IF @ToolPk IS NOT NULL
        BEGIN
            SET @Sql = N'ALTER TABLE dbo.AgOrchestrationToolCall DROP CONSTRAINT ' + QUOTENAME(@ToolPk) + N';';
            EXEC sys.sp_executesql @Sql;
        END;
        ALTER TABLE dbo.AgOrchestrationToolCall
            ADD CONSTRAINT pk_ag_orchestration_tool_call PRIMARY KEY (ID);
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgOrchestrationToolCall') AND name=N'ux_ag_orchestration_tool_call_identity')
        CREATE UNIQUE INDEX ux_ag_orchestration_tool_call_identity ON dbo.AgOrchestrationToolCall(ToolCallId);

    IF OBJECT_ID(N'dbo.AgOrchestrationRunNode', N'U') IS NULL
        CREATE TABLE dbo.AgOrchestrationRunNode (
            ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY, RunId CHAR(36) NOT NULL, Ordinal INT NOT NULL,
            NodeId VARCHAR(64) NOT NULL, NodeName VARCHAR(256) NOT NULL,
            AgentId CHAR(36) NOT NULL, AgentVersionId CHAR(36) NOT NULL,
            Status VARCHAR(32) NOT NULL, Attempts INT NOT NULL,
            StartedAtUtc DATETIME2(7) NULL, FinishedAtUtc DATETIME2(7) NULL,
            OutputCharacters INT NOT NULL, InputSha256 VARCHAR(64) NOT NULL, ErrorCode VARCHAR(128) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT(0), IsActive BIT NULL DEFAULT(1), ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL DEFAULT(0), Tag INT NULL DEFAULT(1), GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL, AuditStatus VARCHAR(32) NULL DEFAULT('Add'), CurrentNode VARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL, UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL,
            CONSTRAINT FK_AgOrchestrationRunNode_Run FOREIGN KEY (RunId) REFERENCES dbo.AgOrchestrationRun(ID) ON DELETE CASCADE,
            CONSTRAINT UX_AgOrchestrationRunNode_Order UNIQUE (RunId, Ordinal));

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgOrchestrationNodeAttempt') AND name=N'ix_ag_orchestration_node_attempt_order')
        CREATE INDEX ix_ag_orchestration_node_attempt_order ON dbo.AgOrchestrationNodeAttempt(RunId, Sequence);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgOrchestrationToolCall') AND name=N'ix_ag_orchestration_tool_call_order')
        CREATE INDEX ix_ag_orchestration_tool_call_order ON dbo.AgOrchestrationToolCall(RunId, NodeId, Attempt, Sequence);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgOrchestrationRunNode') AND name=N'ix_ag_orchestration_run_node_run')
        CREATE INDEX ix_ag_orchestration_run_node_run ON dbo.AgOrchestrationRunNode(RunId, Ordinal);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgOrchestrationRunNode') AND name=N'ix_ag_orchestration_run_node_is_deleted')
        CREATE INDEX ix_ag_orchestration_run_node_is_deleted ON dbo.AgOrchestrationRunNode(IsDeleted);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgOrchestrationRunNode') AND name=N'ix_ag_orchestration_run_node_is_active')
        CREATE INDEX ix_ag_orchestration_run_node_is_active ON dbo.AgOrchestrationRunNode(IsActive);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
