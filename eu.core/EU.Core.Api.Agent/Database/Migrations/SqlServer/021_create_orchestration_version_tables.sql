-- Create normalized Orchestration version, node, edge, and Agent binding tables.
-- Run after 020. All persisted character fields use VARCHAR. SQL Server 2014+.

SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationDefinition', N'U') IS NULL
    THROW 51410, N'dbo.AgOrchestrationDefinition is missing.', 1;
GO

DECLARE @BaseColumns NVARCHAR(MAX) = N'
    IsDeleted BIT NOT NULL DEFAULT (0),
    IsActive BIT NULL DEFAULT (1),
    ImportDataId UNIQUEIDENTIFIER NULL,
    ModificationNum INT NULL DEFAULT (0),
    Tag INT NULL DEFAULT (1),
    GroupId UNIQUEIDENTIFIER NULL,
    CompanyId UNIQUEIDENTIFIER NULL,
    AuditStatus VARCHAR(32) NULL DEFAULT (''Add''),
    CurrentNode VARCHAR(32) NULL,
    CreatedBy UNIQUEIDENTIFIER NULL,
    CreatedTime DATETIME NULL,
    UpdateBy UNIQUEIDENTIFIER NULL,
    UpdateTime DATETIME NULL';
DECLARE @Sql NVARCHAR(MAX);

IF OBJECT_ID(N'dbo.AgOrchestrationVersion', N'U') IS NULL
BEGIN
    SET @Sql = N'CREATE TABLE dbo.AgOrchestrationVersion (
        ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OrchestrationId UNIQUEIDENTIFIER NULL,
        Ordinal INT NULL,
        Label VARCHAR(128) NULL,
        IsDraft BIT NULL,
        StartNodeId VARCHAR(64) NULL,' + @BaseColumns + N',
        CONSTRAINT FK_AgOrchestrationVersion_Definition FOREIGN KEY (OrchestrationId)
            REFERENCES dbo.AgOrchestrationDefinition(ID)
    );';
    EXEC sys.sp_executesql @Sql;
END;

IF OBJECT_ID(N'dbo.AgOrchestrationNode', N'U') IS NULL
BEGIN
    SET @Sql = N'CREATE TABLE dbo.AgOrchestrationNode (
        ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OrchestrationId UNIQUEIDENTIFIER NULL,
        VersionId UNIQUEIDENTIFIER NULL,
        Ordinal INT NULL,
        NodeId VARCHAR(64) NULL,
        Name VARCHAR(256) NULL,
        AgentId UNIQUEIDENTIFIER NULL,
        InputMode VARCHAR(32) NULL,
        InputTemplate VARCHAR(MAX) NULL,
        MaximumRetries INT NULL,
        TimeoutSeconds INT NULL,' + @BaseColumns + N',
        CONSTRAINT FK_AgOrchestrationNode_Definition FOREIGN KEY (OrchestrationId)
            REFERENCES dbo.AgOrchestrationDefinition(ID),
        CONSTRAINT FK_AgOrchestrationNode_Version FOREIGN KEY (VersionId)
            REFERENCES dbo.AgOrchestrationVersion(ID)
    );';
    EXEC sys.sp_executesql @Sql;
END;

IF OBJECT_ID(N'dbo.AgOrchestrationEdge', N'U') IS NULL
BEGIN
    SET @Sql = N'CREATE TABLE dbo.AgOrchestrationEdge (
        ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OrchestrationId UNIQUEIDENTIFIER NULL,
        VersionId UNIQUEIDENTIFIER NULL,
        Ordinal INT NULL,
        FromNodeId VARCHAR(64) NULL,
        ToNodeId VARCHAR(64) NULL,
        Condition VARCHAR(32) NULL,
        ConditionValue VARCHAR(MAX) NULL,
        SortOrder INT NULL,' + @BaseColumns + N',
        CONSTRAINT FK_AgOrchestrationEdge_Definition FOREIGN KEY (OrchestrationId)
            REFERENCES dbo.AgOrchestrationDefinition(ID),
        CONSTRAINT FK_AgOrchestrationEdge_Version FOREIGN KEY (VersionId)
            REFERENCES dbo.AgOrchestrationVersion(ID)
    );';
    EXEC sys.sp_executesql @Sql;
END;

IF OBJECT_ID(N'dbo.AgOrchestrationAgentBinding', N'U') IS NULL
BEGIN
    SET @Sql = N'CREATE TABLE dbo.AgOrchestrationAgentBinding (
        ID UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        OrchestrationId UNIQUEIDENTIFIER NULL,
        VersionId UNIQUEIDENTIFIER NULL,
        Ordinal INT NULL,
        AgentId UNIQUEIDENTIFIER NULL,
        AgentVersionId UNIQUEIDENTIFIER NULL,' + @BaseColumns + N',
        CONSTRAINT FK_AgOrchestrationAgentBinding_Definition FOREIGN KEY (OrchestrationId)
            REFERENCES dbo.AgOrchestrationDefinition(ID),
        CONSTRAINT FK_AgOrchestrationAgentBinding_Version FOREIGN KEY (VersionId)
            REFERENCES dbo.AgOrchestrationVersion(ID)
    );';
    EXEC sys.sp_executesql @Sql;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationVersion') AND name = N'ux_ag_orchestration_version_ordinal')
    CREATE UNIQUE INDEX ux_ag_orchestration_version_ordinal ON dbo.AgOrchestrationVersion(OrchestrationId, Ordinal);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationVersion') AND name = N'ux_ag_orchestration_version_draft')
    CREATE UNIQUE INDEX ux_ag_orchestration_version_draft ON dbo.AgOrchestrationVersion(OrchestrationId) WHERE IsDraft = 1 AND IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationNode') AND name = N'ux_ag_orchestration_node_id')
    CREATE UNIQUE INDEX ux_ag_orchestration_node_id ON dbo.AgOrchestrationNode(VersionId, NodeId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationEdge') AND name = N'ix_ag_orchestration_edge_version')
    CREATE INDEX ix_ag_orchestration_edge_version ON dbo.AgOrchestrationEdge(VersionId, Ordinal);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationAgentBinding') AND name = N'ux_ag_orchestration_binding_agent')
    CREATE UNIQUE INDEX ux_ag_orchestration_binding_agent ON dbo.AgOrchestrationAgentBinding(VersionId, AgentId);

DECLARE @Sql NVARCHAR(MAX);
DECLARE @Tables TABLE (RowId INT IDENTITY(1, 1), TableName SYSNAME);
INSERT INTO @Tables (TableName) VALUES
    (N'AgOrchestrationVersion'), (N'AgOrchestrationNode'),
    (N'AgOrchestrationEdge'), (N'AgOrchestrationAgentBinding');
DECLARE @RowId INT = 1, @RowCount INT = (SELECT COUNT(*) FROM @Tables), @TableName SYSNAME;
WHILE @RowId <= @RowCount
BEGIN
    SELECT @TableName = TableName FROM @Tables WHERE RowId = @RowId;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ix_' + LOWER(@TableName) + N'_is_deleted')
    BEGIN
        SET @Sql = N'CREATE INDEX ' + QUOTENAME(N'ix_' + LOWER(@TableName) + N'_is_deleted')
            + N' ON dbo.' + QUOTENAME(@TableName) + N'(IsDeleted);';
        EXEC sys.sp_executesql @Sql;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.' + @TableName) AND name = N'ix_' + LOWER(@TableName) + N'_is_active')
    BEGIN
        SET @Sql = N'CREATE INDEX ' + QUOTENAME(N'ix_' + LOWER(@TableName) + N'_is_active')
            + N' ON dbo.' + QUOTENAME(@TableName) + N'(IsActive);';
        EXEC sys.sp_executesql @Sql;
    END;
    SET @RowId += 1;
END;
GO

PRINT N'Normalized Orchestration tables are ready; generate and execute orchestration_normalized_data.generated.sql next.';
GO
