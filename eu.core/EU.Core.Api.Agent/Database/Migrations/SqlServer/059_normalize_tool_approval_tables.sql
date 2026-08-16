-- Normalize Tool Approval persistence for BasePoco and SqlSugar.
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgToolApprovalRequest', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgToolApprovalPayload', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgToolApprovalDecision', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgToolApprovalExecutionResult', N'U') IS NULL
    THROW 52200, N'Tool Approval tables are missing. Run 001_initial_schema.sql first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @RequestIdType SYSNAME;
    SELECT @RequestIdType = types.name
    FROM sys.columns columns
    INNER JOIN sys.types types ON types.user_type_id = columns.user_type_id
    WHERE columns.object_id = OBJECT_ID(N'dbo.AgToolApprovalRequest')
      AND UPPER(columns.name) = N'ID';

    IF @RequestIdType <> N'uniqueidentifier'
    BEGIN
        IF OBJECT_ID(N'dbo.AgToolApprovalRequest_Normalized', N'U') IS NOT NULL
           OR OBJECT_ID(N'dbo.AgToolApprovalPayload_Normalized', N'U') IS NOT NULL
           OR OBJECT_ID(N'dbo.AgToolApprovalDecision_Normalized', N'U') IS NOT NULL
           OR OBJECT_ID(N'dbo.AgToolApprovalExecutionResult_Normalized', N'U') IS NOT NULL
            THROW 52201, N'Tool Approval normalization staging tables already exist.', 1;

        IF EXISTS (
            SELECT 1 FROM dbo.AgToolApprovalRequest
            WHERE TRY_CONVERT(UNIQUEIDENTIFIER, Id) IS NULL
               OR TRY_CONVERT(UNIQUEIDENTIFIER, ConversationId) IS NULL
               OR TRY_CONVERT(UNIQUEIDENTIFIER, EntryRunId) IS NULL
               OR TRY_CONVERT(UNIQUEIDENTIFIER, AgentRunId) IS NULL
               OR TRY_CONVERT(UNIQUEIDENTIFIER, AgentVersionId) IS NULL
               OR TRY_CONVERT(UNIQUEIDENTIFIER, McpServerId) IS NULL
               OR TRY_CONVERT(UNIQUEIDENTIFIER, ToolVersionId) IS NULL)
            THROW 52202, N'AgToolApprovalRequest contains an invalid GUID.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AgToolApprovalPayload WHERE TRY_CONVERT(UNIQUEIDENTIFIER, ApprovalId) IS NULL)
            THROW 52203, N'AgToolApprovalPayload contains an invalid ApprovalId.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AgToolApprovalDecision WHERE TRY_CONVERT(UNIQUEIDENTIFIER, Id) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER, ApprovalId) IS NULL)
            THROW 52204, N'AgToolApprovalDecision contains an invalid GUID.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AgToolApprovalExecutionResult WHERE TRY_CONVERT(UNIQUEIDENTIFIER, ApprovalId) IS NULL)
            THROW 52205, N'AgToolApprovalExecutionResult contains an invalid ApprovalId.', 1;
        IF EXISTS (
            SELECT 1 FROM dbo.AgToolApprovalRequest
            WHERE TRY_CONVERT(DATETIMEOFFSET(7), RequestedAtUtc, 127) IS NULL
               OR TRY_CONVERT(DATETIMEOFFSET(7), ExpiresAtUtc, 127) IS NULL
               OR (DecidedAtUtc IS NOT NULL AND TRY_CONVERT(DATETIMEOFFSET(7), DecidedAtUtc, 127) IS NULL)
               OR (ClaimedAtUtc IS NOT NULL AND TRY_CONVERT(DATETIMEOFFSET(7), ClaimedAtUtc, 127) IS NULL)
               OR (FinishedAtUtc IS NOT NULL AND TRY_CONVERT(DATETIMEOFFSET(7), FinishedAtUtc, 127) IS NULL))
            THROW 52206, N'AgToolApprovalRequest contains an invalid timestamp.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AgToolApprovalDecision WHERE TRY_CONVERT(DATETIMEOFFSET(7), DecidedAtUtc, 127) IS NULL)
            THROW 52207, N'AgToolApprovalDecision contains an invalid timestamp.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AgToolApprovalExecutionResult WHERE TRY_CONVERT(DATETIMEOFFSET(7), FinishedAtUtc, 127) IS NULL)
            THROW 52208, N'AgToolApprovalExecutionResult contains an invalid timestamp.', 1;
        IF EXISTS (
            SELECT 1 FROM dbo.AgToolApprovalRequest
            WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), TenantId))) <> CONVERT(VARBINARY(MAX), TenantId)
               OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), RequesterUserId))) <> CONVERT(VARBINARY(MAX), RequesterUserId)
               OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), ToolName))) <> CONVERT(VARBINARY(MAX), ToolName)
               OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), SafeArgumentsSummaryJson))) <> CONVERT(VARBINARY(MAX), SafeArgumentsSummaryJson)
               OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), DecisionUserId))) <> CONVERT(VARBINARY(MAX), DecisionUserId)
               OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), DecisionReason))) <> CONVERT(VARBINARY(MAX), DecisionReason))
            THROW 52209, N'AgToolApprovalRequest text cannot be represented by VARCHAR.', 1;
        IF EXISTS (
            SELECT 1 FROM dbo.AgToolApprovalDecision
            WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), TenantId))) <> CONVERT(VARBINARY(MAX), TenantId)
               OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), DecisionUserId))) <> CONVERT(VARBINARY(MAX), DecisionUserId)
               OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), DecisionReason))) <> CONVERT(VARBINARY(MAX), DecisionReason))
            THROW 52210, N'AgToolApprovalDecision text cannot be represented by VARCHAR.', 1;
        IF EXISTS (SELECT 1 FROM dbo.AgToolApprovalPayload WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), ProtectedPayload))) <> CONVERT(VARBINARY(MAX), ProtectedPayload))
            THROW 52211, N'AgToolApprovalPayload text cannot be represented by VARCHAR.', 1;
        IF EXISTS (
            SELECT 1 FROM dbo.AgToolApprovalExecutionResult
            WHERE CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), TenantId))) <> CONVERT(VARBINARY(MAX), TenantId)
               OR CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), ProtectedContent))) <> CONVERT(VARBINARY(MAX), ProtectedContent))
            THROW 52212, N'AgToolApprovalExecutionResult text cannot be represented by VARCHAR.', 1;

        CREATE TABLE dbo.AgToolApprovalRequest_Normalized (
            ID UNIQUEIDENTIFIER NOT NULL, TenantId VARCHAR(256) NOT NULL, RequesterUserId VARCHAR(256) NOT NULL,
            ConversationId UNIQUEIDENTIFIER NOT NULL, EntryRunId UNIQUEIDENTIFIER NOT NULL, AgentRunId UNIQUEIDENTIFIER NOT NULL,
            AgentVersionId UNIQUEIDENTIFIER NOT NULL, McpServerId UNIQUEIDENTIFIER NOT NULL, ToolVersionId UNIQUEIDENTIFIER NOT NULL,
            ToolName VARCHAR(256) NOT NULL, Risk INT NOT NULL, ToolSchemaSha256 VARCHAR(64) NOT NULL,
            ArgumentsSha256 VARCHAR(64) NOT NULL, SafeArgumentsSummaryJson VARCHAR(MAX) NOT NULL, Status INT NOT NULL,
            LogicalRevision BIGINT NOT NULL, RequestedAtUtc DATETIME2(7) NOT NULL, ExpiresAtUtc DATETIME2(7) NOT NULL,
            DecisionUserId VARCHAR(256) NOT NULL, DecisionReason VARCHAR(MAX) NOT NULL, DecidedAtUtc DATETIME2(7) NULL,
            ClaimedAtUtc DATETIME2(7) NULL, FinishedAtUtc DATETIME2(7) NULL, ErrorCode VARCHAR(128) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT (0), IsActive BIT NULL DEFAULT (1), ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL DEFAULT (0), Tag INT NULL DEFAULT (1), GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL, AuditStatus VARCHAR(32) NULL DEFAULT ('Add'), CurrentNode VARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL, UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL);
        CREATE TABLE dbo.AgToolApprovalPayload_Normalized (
            ID UNIQUEIDENTIFIER NOT NULL, ApprovalId UNIQUEIDENTIFIER NOT NULL, ProtectedPayload VARCHAR(MAX) NOT NULL,
            ProtectedPayloadSha256 VARCHAR(64) NOT NULL, IsDeleted BIT NOT NULL DEFAULT (0), IsActive BIT NULL DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL, ModificationNum INT NULL DEFAULT (0), Tag INT NULL DEFAULT (1),
            GroupId UNIQUEIDENTIFIER NULL, CompanyId UNIQUEIDENTIFIER NULL, AuditStatus VARCHAR(32) NULL DEFAULT ('Add'),
            CurrentNode VARCHAR(32) NULL, CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL,
            UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL);
        CREATE TABLE dbo.AgToolApprovalDecision_Normalized (
            ID UNIQUEIDENTIFIER NOT NULL, ApprovalId UNIQUEIDENTIFIER NOT NULL, TenantId VARCHAR(256) NOT NULL,
            FromStatus INT NOT NULL, ToStatus INT NOT NULL, DecisionUserId VARCHAR(256) NOT NULL,
            DecisionReason VARCHAR(MAX) NOT NULL, DecidedAtUtc DATETIME2(7) NOT NULL, ResultingLogicalRevision BIGINT NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT (0), IsActive BIT NULL DEFAULT (1), ImportDataId UNIQUEIDENTIFIER NULL,
            ModificationNum INT NULL DEFAULT (0), Tag INT NULL DEFAULT (1), GroupId UNIQUEIDENTIFIER NULL,
            CompanyId UNIQUEIDENTIFIER NULL, AuditStatus VARCHAR(32) NULL DEFAULT ('Add'), CurrentNode VARCHAR(32) NULL,
            CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL, UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL);
        CREATE TABLE dbo.AgToolApprovalExecutionResult_Normalized (
            ID UNIQUEIDENTIFIER NOT NULL, ApprovalId UNIQUEIDENTIFIER NOT NULL, TenantId VARCHAR(256) NOT NULL,
            Succeeded BIT NOT NULL, Blocked BIT NOT NULL, ProtectedContent VARCHAR(MAX) NOT NULL,
            ProtectedContentSha256 VARCHAR(64) NOT NULL, ContentSha256 VARCHAR(64) NOT NULL, ErrorCode VARCHAR(128) NOT NULL,
            FinishedAtUtc DATETIME2(7) NOT NULL, IsDeleted BIT NOT NULL DEFAULT (0), IsActive BIT NULL DEFAULT (1),
            ImportDataId UNIQUEIDENTIFIER NULL, ModificationNum INT NULL DEFAULT (0), Tag INT NULL DEFAULT (1),
            GroupId UNIQUEIDENTIFIER NULL, CompanyId UNIQUEIDENTIFIER NULL, AuditStatus VARCHAR(32) NULL DEFAULT ('Add'),
            CurrentNode VARCHAR(32) NULL, CreatedBy UNIQUEIDENTIFIER NULL, CreatedTime DATETIME NULL,
            UpdateBy UNIQUEIDENTIFIER NULL, UpdateTime DATETIME NULL);

        INSERT dbo.AgToolApprovalRequest_Normalized
            (ID,TenantId,RequesterUserId,ConversationId,EntryRunId,AgentRunId,AgentVersionId,McpServerId,ToolVersionId,ToolName,Risk,ToolSchemaSha256,ArgumentsSha256,SafeArgumentsSummaryJson,Status,LogicalRevision,RequestedAtUtc,ExpiresAtUtc,DecisionUserId,DecisionReason,DecidedAtUtc,ClaimedAtUtc,FinishedAtUtc,ErrorCode,IsDeleted,IsActive)
        SELECT CONVERT(UNIQUEIDENTIFIER,Id),CONVERT(VARCHAR(256),TenantId),CONVERT(VARCHAR(256),RequesterUserId),CONVERT(UNIQUEIDENTIFIER,ConversationId),CONVERT(UNIQUEIDENTIFIER,EntryRunId),CONVERT(UNIQUEIDENTIFIER,AgentRunId),CONVERT(UNIQUEIDENTIFIER,AgentVersionId),CONVERT(UNIQUEIDENTIFIER,McpServerId),CONVERT(UNIQUEIDENTIFIER,ToolVersionId),CONVERT(VARCHAR(256),ToolName),Risk,CONVERT(VARCHAR(64),ToolSchemaSha256),CONVERT(VARCHAR(64),ArgumentsSha256),CONVERT(VARCHAR(MAX),SafeArgumentsSummaryJson),Status,LogicalRevision,CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),RequestedAtUtc,127),'+00:00')),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),ExpiresAtUtc,127),'+00:00')),CONVERT(VARCHAR(256),DecisionUserId),CONVERT(VARCHAR(MAX),DecisionReason),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),DecidedAtUtc,127),'+00:00')),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),ClaimedAtUtc,127),'+00:00')),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127),'+00:00')),ErrorCode,0,1
        FROM dbo.AgToolApprovalRequest;
        INSERT dbo.AgToolApprovalPayload_Normalized (ID,ApprovalId,ProtectedPayload,ProtectedPayloadSha256,IsDeleted,IsActive)
        SELECT NEWID(),CONVERT(UNIQUEIDENTIFIER,ApprovalId),CONVERT(VARCHAR(MAX),ProtectedPayload),CONVERT(VARCHAR(64),ProtectedPayloadSha256),0,1 FROM dbo.AgToolApprovalPayload;
        INSERT dbo.AgToolApprovalDecision_Normalized (ID,ApprovalId,TenantId,FromStatus,ToStatus,DecisionUserId,DecisionReason,DecidedAtUtc,ResultingLogicalRevision,IsDeleted,IsActive)
        SELECT CONVERT(UNIQUEIDENTIFIER,Id),CONVERT(UNIQUEIDENTIFIER,ApprovalId),CONVERT(VARCHAR(256),TenantId),FromStatus,ToStatus,CONVERT(VARCHAR(256),DecisionUserId),CONVERT(VARCHAR(MAX),DecisionReason),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),DecidedAtUtc,127),'+00:00')),ResultingLogicalRevision,0,1 FROM dbo.AgToolApprovalDecision;
        INSERT dbo.AgToolApprovalExecutionResult_Normalized (ID,ApprovalId,TenantId,Succeeded,Blocked,ProtectedContent,ProtectedContentSha256,ContentSha256,ErrorCode,FinishedAtUtc,IsDeleted,IsActive)
        SELECT NEWID(),CONVERT(UNIQUEIDENTIFIER,ApprovalId),CONVERT(VARCHAR(256),TenantId),Succeeded,Blocked,CONVERT(VARCHAR(MAX),ProtectedContent),CONVERT(VARCHAR(64),ProtectedContentSha256),CONVERT(VARCHAR(64),ContentSha256),ErrorCode,CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127),'+00:00')),0,1 FROM dbo.AgToolApprovalExecutionResult;

        IF (SELECT COUNT_BIG(*) FROM dbo.AgToolApprovalRequest_Normalized) <> (SELECT COUNT_BIG(*) FROM dbo.AgToolApprovalRequest)
           OR (SELECT COUNT_BIG(*) FROM dbo.AgToolApprovalPayload_Normalized) <> (SELECT COUNT_BIG(*) FROM dbo.AgToolApprovalPayload)
           OR (SELECT COUNT_BIG(*) FROM dbo.AgToolApprovalDecision_Normalized) <> (SELECT COUNT_BIG(*) FROM dbo.AgToolApprovalDecision)
           OR (SELECT COUNT_BIG(*) FROM dbo.AgToolApprovalExecutionResult_Normalized) <> (SELECT COUNT_BIG(*) FROM dbo.AgToolApprovalExecutionResult)
            THROW 52213, N'Tool Approval normalization row-count validation failed.', 1;

        DROP TABLE dbo.AgToolApprovalExecutionResult;
        DROP TABLE dbo.AgToolApprovalDecision;
        DROP TABLE dbo.AgToolApprovalPayload;
        DROP TABLE dbo.AgToolApprovalRequest;
        EXEC sys.sp_rename N'dbo.AgToolApprovalRequest_Normalized', N'AgToolApprovalRequest';
        EXEC sys.sp_rename N'dbo.AgToolApprovalPayload_Normalized', N'AgToolApprovalPayload';
        EXEC sys.sp_rename N'dbo.AgToolApprovalDecision_Normalized', N'AgToolApprovalDecision';
        EXEC sys.sp_rename N'dbo.AgToolApprovalExecutionResult_Normalized', N'AgToolApprovalExecutionResult';
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgToolApprovalRequest') AND [type]=N'PK') ALTER TABLE dbo.AgToolApprovalRequest ADD CONSTRAINT pk_ag_tool_approval_request PRIMARY KEY (ID);
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgToolApprovalPayload') AND [type]=N'PK') ALTER TABLE dbo.AgToolApprovalPayload ADD CONSTRAINT pk_ag_tool_approval_payload PRIMARY KEY (ID);
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgToolApprovalDecision') AND [type]=N'PK') ALTER TABLE dbo.AgToolApprovalDecision ADD CONSTRAINT pk_ag_tool_approval_decision PRIMARY KEY (ID);
    IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgToolApprovalExecutionResult') AND [type]=N'PK') ALTER TABLE dbo.AgToolApprovalExecutionResult ADD CONSTRAINT pk_ag_tool_approval_execution_result PRIMARY KEY (ID);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgToolApprovalPayload') AND name=N'ux_ag_tool_approval_payload_approval') CREATE UNIQUE INDEX ux_ag_tool_approval_payload_approval ON dbo.AgToolApprovalPayload(ApprovalId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgToolApprovalDecision') AND name=N'ux_ag_tool_approval_decision_revision') CREATE UNIQUE INDEX ux_ag_tool_approval_decision_revision ON dbo.AgToolApprovalDecision(ApprovalId,ResultingLogicalRevision);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgToolApprovalExecutionResult') AND name=N'ux_ag_tool_approval_execution_result_approval') CREATE UNIQUE INDEX ux_ag_tool_approval_execution_result_approval ON dbo.AgToolApprovalExecutionResult(ApprovalId);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgToolApprovalRequest') AND name=N'ix_ag_tool_approval_request_tenant_status_requested') CREATE INDEX ix_ag_tool_approval_request_tenant_status_requested ON dbo.AgToolApprovalRequest(TenantId,Status,RequestedAtUtc DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgToolApprovalRequest') AND name=N'ck_ag_tool_approval_request_revision') ALTER TABLE dbo.AgToolApprovalRequest ADD CONSTRAINT ck_ag_tool_approval_request_revision CHECK (LogicalRevision >= 0);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.AgToolApprovalPayload') AND name=N'fk_ag_tool_approval_payload_request') ALTER TABLE dbo.AgToolApprovalPayload ADD CONSTRAINT fk_ag_tool_approval_payload_request FOREIGN KEY(ApprovalId) REFERENCES dbo.AgToolApprovalRequest(ID) ON DELETE CASCADE;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.AgToolApprovalDecision') AND name=N'fk_ag_tool_approval_decision_request') ALTER TABLE dbo.AgToolApprovalDecision ADD CONSTRAINT fk_ag_tool_approval_decision_request FOREIGN KEY(ApprovalId) REFERENCES dbo.AgToolApprovalRequest(ID) ON DELETE CASCADE;
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.AgToolApprovalExecutionResult') AND name=N'fk_ag_tool_approval_execution_result_request') ALTER TABLE dbo.AgToolApprovalExecutionResult ADD CONSTRAINT fk_ag_tool_approval_execution_result_request FOREIGN KEY(ApprovalId) REFERENCES dbo.AgToolApprovalRequest(ID) ON DELETE CASCADE;

    DECLARE @Table SYSNAME;
    DECLARE tables CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM sys.tables WHERE name IN (N'AgToolApprovalRequest',N'AgToolApprovalPayload',N'AgToolApprovalDecision',N'AgToolApprovalExecutionResult');
    OPEN tables; FETCH NEXT FROM tables INTO @Table;
    WHILE @@FETCH_STATUS=0
    BEGIN
        DECLARE @Sql NVARCHAR(MAX), @Suffix VARCHAR(128)=LOWER(REPLACE(@Table,N'AgToolApproval',N'tool_approval_'));
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@Table) AND name=N'ix_ag_'+@Suffix+N'_is_deleted')
        BEGIN SET @Sql=N'CREATE INDEX '+QUOTENAME(N'ix_ag_'+@Suffix+N'_is_deleted')+N' ON dbo.'+QUOTENAME(@Table)+N'(IsDeleted);'; EXEC sys.sp_executesql @Sql; END;
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@Table) AND name=N'ix_ag_'+@Suffix+N'_is_active')
        BEGIN SET @Sql=N'CREATE INDEX '+QUOTENAME(N'ix_ag_'+@Suffix+N'_is_active')+N' ON dbo.'+QUOTENAME(@Table)+N'(IsActive);'; EXEC sys.sp_executesql @Sql; END;
        FETCH NEXT FROM tables INTO @Table;
    END;
    CLOSE tables; DEALLOCATE tables;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
