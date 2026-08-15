-- Create normalized MCP argument and immutable tool-version tables.
-- SQL Server 2014+.

SET XACT_ABORT ON;
GO
BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AgMcpServerArgument', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AgMcpServerArgument (
            ID UNIQUEIDENTIFIER NOT NULL,
            IsDeleted BIT NOT NULL CONSTRAINT DF_AgMcpServerArgument_IsDeleted DEFAULT (0),
            IsActive BIT NULL CONSTRAINT DF_AgMcpServerArgument_IsActive DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL, ModificationNum INT NULL, Tag INT NULL,
            GroupId UNIQUEIDENTIFIER NULL, CompanyId UNIQUEIDENTIFIER NULL,
            AuditStatus VARCHAR(32) NULL, CurrentNode NVARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL,
            UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL,
            ServerId UNIQUEIDENTIFIER NOT NULL,
            Ordinal INT NOT NULL,
            [Value] NVARCHAR(1024) NOT NULL,
            CONSTRAINT pk_ag_mcp_server_argument PRIMARY KEY (ID),
            CONSTRAINT fk_ag_mcp_server_argument_server FOREIGN KEY (ServerId) REFERENCES dbo.AgMcpServerDefinition(ID) ON DELETE CASCADE,
            CONSTRAINT ux_ag_mcp_server_argument_order UNIQUE (ServerId, Ordinal),
            CONSTRAINT ck_ag_mcp_server_argument_ordinal CHECK (Ordinal >= 0)
        );
        CREATE INDEX index_AgMcpServerArgument_IsDeleted ON dbo.AgMcpServerArgument(IsDeleted);
    END;

    IF OBJECT_ID(N'dbo.AgMcpToolVersion', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.AgMcpToolVersion (
            ID UNIQUEIDENTIFIER NOT NULL,
            IsDeleted BIT NOT NULL CONSTRAINT DF_AgMcpToolVersion_IsDeleted DEFAULT (0),
            IsActive BIT NULL CONSTRAINT DF_AgMcpToolVersion_IsActive DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL, ModificationNum INT NULL, Tag INT NULL,
            GroupId UNIQUEIDENTIFIER NULL, CompanyId UNIQUEIDENTIFIER NULL,
            AuditStatus VARCHAR(32) NULL, CurrentNode NVARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL,
            UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL,
            ServerId UNIQUEIDENTIFIER NOT NULL,
            HistoryOrdinal INT NOT NULL,
            CurrentOrdinal INT NULL,
            Name NVARCHAR(256) NOT NULL,
            Description NVARCHAR(MAX) NOT NULL,
            InputSchemaJson NVARCHAR(MAX) NOT NULL,
            Risk VARCHAR(32) NOT NULL,
            Sha256 CHAR(64) NOT NULL,
            DiscoveredAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT pk_ag_mcp_tool_version PRIMARY KEY (ID),
            CONSTRAINT fk_ag_mcp_tool_version_server FOREIGN KEY (ServerId) REFERENCES dbo.AgMcpServerDefinition(ID) ON DELETE CASCADE,
            CONSTRAINT ux_ag_mcp_tool_history_order UNIQUE (ServerId, HistoryOrdinal),
            CONSTRAINT ck_ag_mcp_tool_history_ordinal CHECK (HistoryOrdinal >= 0),
            CONSTRAINT ck_ag_mcp_tool_current_ordinal CHECK (CurrentOrdinal IS NULL OR CurrentOrdinal >= 0),
            CONSTRAINT ck_ag_mcp_tool_risk CHECK (Risk IN ('Unknown', 'ReadOnly', 'Mutating', 'HighRisk')),
            CONSTRAINT ck_ag_mcp_tool_sha256 CHECK (LEN(Sha256) = 64)
        );
        CREATE INDEX ix_ag_mcp_tool_server ON dbo.AgMcpToolVersion(ServerId, HistoryOrdinal);
        CREATE UNIQUE INDEX ux_ag_mcp_tool_current_order
            ON dbo.AgMcpToolVersion(ServerId, CurrentOrdinal)
            WHERE CurrentOrdinal IS NOT NULL;
        CREATE INDEX index_AgMcpToolVersion_IsDeleted ON dbo.AgMcpToolVersion(IsDeleted);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
