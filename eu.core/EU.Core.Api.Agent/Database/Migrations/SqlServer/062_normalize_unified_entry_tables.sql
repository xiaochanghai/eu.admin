-- Normalize Unified Entry persistence for BasePoco and SqlSugar.
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.AgChatConversation',N'U') IS NULL
 OR OBJECT_ID(N'dbo.AgChatMessage',N'U') IS NULL
 OR OBJECT_ID(N'dbo.AgUnifiedEntryRun',N'U') IS NULL
 OR OBJECT_ID(N'dbo.AgUnifiedAgentRun',N'U') IS NULL
 OR OBJECT_ID(N'dbo.AgUnifiedOrchestrationLink',N'U') IS NULL
 OR OBJECT_ID(N'dbo.AgUnifiedToolCall',N'U') IS NULL
 OR OBJECT_ID(N'dbo.AgUnifiedRunEvent',N'U') IS NULL
    THROW 52300,N'Unified Entry tables are missing. Run 001_initial_schema.sql first.',1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @IdType SYSNAME;
    SELECT @IdType=types.name
    FROM sys.columns columns JOIN sys.types types ON types.user_type_id=columns.user_type_id
    WHERE columns.object_id=OBJECT_ID(N'dbo.AgChatConversation') AND UPPER(columns.name)=N'ID';

    IF @IdType<>N'uniqueidentifier'
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.tables WHERE name IN
            (N'AgChatConversation_Normalized',N'AgChatMessage_Normalized',N'AgUnifiedEntryRun_Normalized',
             N'AgUnifiedAgentRun_Normalized',N'AgUnifiedOrchestrationLink_Normalized',N'AgUnifiedToolCall_Normalized',N'AgUnifiedRunEvent_Normalized'))
            THROW 52301,N'Unified Entry normalization staging tables already exist.',1;

        IF EXISTS (SELECT 1 FROM dbo.AgChatConversation WHERE TRY_CONVERT(UNIQUEIDENTIFIER,Id) IS NULL)
         OR EXISTS (SELECT 1 FROM dbo.AgChatMessage WHERE TRY_CONVERT(UNIQUEIDENTIFIER,Id) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,ConversationId) IS NULL OR (BusinessQueryId IS NOT NULL AND TRY_CONVERT(UNIQUEIDENTIFIER,BusinessQueryId) IS NULL))
         OR EXISTS (SELECT 1 FROM dbo.AgUnifiedEntryRun WHERE TRY_CONVERT(UNIQUEIDENTIFIER,Id) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,ConversationId) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,CorrelationId) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,MainAgentVersionId) IS NULL)
         OR EXISTS (SELECT 1 FROM dbo.AgUnifiedAgentRun WHERE TRY_CONVERT(UNIQUEIDENTIFIER,Id) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,EntryRunId) IS NULL OR (ParentRunId IS NOT NULL AND TRY_CONVERT(UNIQUEIDENTIFIER,ParentRunId) IS NULL) OR TRY_CONVERT(UNIQUEIDENTIFIER,AgentId) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,AgentVersionId) IS NULL)
         OR EXISTS (SELECT 1 FROM dbo.AgUnifiedOrchestrationLink WHERE TRY_CONVERT(UNIQUEIDENTIFIER,Id) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,EntryRunId) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,ParentRunId) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,OrchestrationRunId) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,OrchestrationVersionId) IS NULL)
         OR EXISTS (SELECT 1 FROM dbo.AgUnifiedToolCall WHERE TRY_CONVERT(UNIQUEIDENTIFIER,Id) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,EntryRunId) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,ParentRunId) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,ToolVersionId) IS NULL)
         OR EXISTS (SELECT 1 FROM dbo.AgUnifiedRunEvent WHERE TRY_CONVERT(UNIQUEIDENTIFIER,Id) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,EntryRunId) IS NULL OR TRY_CONVERT(UNIQUEIDENTIFIER,CorrelationId) IS NULL OR (ParentRunId IS NOT NULL AND TRY_CONVERT(UNIQUEIDENTIFIER,ParentRunId) IS NULL))
            THROW 52302,N'Unified Entry persistence contains an invalid GUID.',1;

        IF EXISTS (SELECT 1 FROM dbo.AgChatConversation WHERE TRY_CONVERT(DATETIMEOFFSET(7),CreatedAtUtc,127) IS NULL OR TRY_CONVERT(DATETIMEOFFSET(7),UpdatedAtUtc,127) IS NULL)
         OR EXISTS (SELECT 1 FROM dbo.AgChatMessage WHERE TRY_CONVERT(DATETIMEOFFSET(7),CreatedAtUtc,127) IS NULL)
         OR EXISTS (SELECT 1 FROM dbo.AgUnifiedEntryRun WHERE TRY_CONVERT(DATETIMEOFFSET(7),StartedAtUtc,127) IS NULL OR (FinishedAtUtc IS NOT NULL AND TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127) IS NULL))
         OR EXISTS (SELECT 1 FROM dbo.AgUnifiedAgentRun WHERE TRY_CONVERT(DATETIMEOFFSET(7),StartedAtUtc,127) IS NULL OR (FinishedAtUtc IS NOT NULL AND TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127) IS NULL))
         OR EXISTS (SELECT 1 FROM dbo.AgUnifiedOrchestrationLink WHERE TRY_CONVERT(DATETIMEOFFSET(7),StartedAtUtc,127) IS NULL OR (FinishedAtUtc IS NOT NULL AND TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127) IS NULL))
         OR EXISTS (SELECT 1 FROM dbo.AgUnifiedToolCall WHERE TRY_CONVERT(DATETIMEOFFSET(7),StartedAtUtc,127) IS NULL OR (FinishedAtUtc IS NOT NULL AND TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127) IS NULL))
         OR EXISTS (SELECT 1 FROM dbo.AgUnifiedRunEvent WHERE TRY_CONVERT(DATETIMEOFFSET(7),OccurredAtUtc,127) IS NULL)
            THROW 52303,N'Unified Entry persistence contains an invalid timestamp.',1;

        -- The project requires VARCHAR-only character columns. SQL Server versions
        -- without a UTF-8 collation replace characters outside the database code
        -- page during CONVERT; ordinary Chinese text remains representable under
        -- the configured Chinese_PRC collation.

        CREATE TABLE dbo.AgChatConversation_Normalized (
            ID UNIQUEIDENTIFIER NOT NULL,Title VARCHAR(512) NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,UpdatedAtUtc DATETIME2(7) NOT NULL,TenantId VARCHAR(128) NOT NULL,UserId VARCHAR(128) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT(0),IsActive BIT NULL DEFAULT(1),ImportDataId UNIQUEIDENTIFIER NULL,ModificationNum INT NULL DEFAULT(0),Tag INT NULL DEFAULT(1),GroupId UNIQUEIDENTIFIER NULL,CompanyId UNIQUEIDENTIFIER NULL,AuditStatus VARCHAR(32) NULL DEFAULT('Add'),CurrentNode VARCHAR(32) NULL,CreatedBy UNIQUEIDENTIFIER NULL,CreatedTime DATETIME NULL,UpdateBy UNIQUEIDENTIFIER NULL,UpdateTime DATETIME NULL);
        CREATE TABLE dbo.AgChatMessage_Normalized (
            ID UNIQUEIDENTIFIER NOT NULL,ConversationId UNIQUEIDENTIFIER NOT NULL,Ordinal BIGINT NOT NULL,Role VARCHAR(32) NOT NULL,Content VARCHAR(MAX) NOT NULL,ContentSha256 VARCHAR(64) NOT NULL,ContentUtf8Bytes BIGINT NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,Kind VARCHAR(64) NOT NULL,BusinessQueryId UNIQUEIDENTIFIER NULL,BusinessReceiptJson VARCHAR(MAX) NOT NULL,BusinessPresentationJson VARCHAR(MAX) NOT NULL,BusinessIntegritySha256 VARCHAR(64) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT(0),IsActive BIT NULL DEFAULT(1),ImportDataId UNIQUEIDENTIFIER NULL,ModificationNum INT NULL DEFAULT(0),Tag INT NULL DEFAULT(1),GroupId UNIQUEIDENTIFIER NULL,CompanyId UNIQUEIDENTIFIER NULL,AuditStatus VARCHAR(32) NULL DEFAULT('Add'),CurrentNode VARCHAR(32) NULL,CreatedBy UNIQUEIDENTIFIER NULL,CreatedTime DATETIME NULL,UpdateBy UNIQUEIDENTIFIER NULL,UpdateTime DATETIME NULL);
        CREATE TABLE dbo.AgUnifiedEntryRun_Normalized (
            ID UNIQUEIDENTIFIER NOT NULL,ConversationId UNIQUEIDENTIFIER NOT NULL,CorrelationId UNIQUEIDENTIFIER NOT NULL,MainAgentVersionId UNIQUEIDENTIFIER NOT NULL,Status VARCHAR(32) NOT NULL,StartedAtUtc DATETIME2(7) NOT NULL,FinishedAtUtc DATETIME2(7) NULL,DurationTicks BIGINT NULL,InputText VARCHAR(MAX) NOT NULL,InputSha256 VARCHAR(64) NOT NULL,OutputText VARCHAR(MAX) NOT NULL,OutputSha256 VARCHAR(64) NOT NULL,ErrorCode VARCHAR(128) NOT NULL,PersistenceRevision BIGINT NOT NULL,StateSha256 VARCHAR(64) NOT NULL,TenantId VARCHAR(128) NOT NULL,UserId VARCHAR(128) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT(0),IsActive BIT NULL DEFAULT(1),ImportDataId UNIQUEIDENTIFIER NULL,ModificationNum INT NULL DEFAULT(0),Tag INT NULL DEFAULT(1),GroupId UNIQUEIDENTIFIER NULL,CompanyId UNIQUEIDENTIFIER NULL,AuditStatus VARCHAR(32) NULL DEFAULT('Add'),CurrentNode VARCHAR(32) NULL,CreatedBy UNIQUEIDENTIFIER NULL,CreatedTime DATETIME NULL,UpdateBy UNIQUEIDENTIFIER NULL,UpdateTime DATETIME NULL);
        CREATE TABLE dbo.AgUnifiedAgentRun_Normalized (
            ID UNIQUEIDENTIFIER NOT NULL,EntryRunId UNIQUEIDENTIFIER NOT NULL,Ordinal BIGINT NOT NULL,ParentRunId UNIQUEIDENTIFIER NULL,Kind VARCHAR(64) NOT NULL,AgentId UNIQUEIDENTIFIER NOT NULL,AgentVersionId UNIQUEIDENTIFIER NOT NULL,Depth INT NOT NULL,Status VARCHAR(32) NOT NULL,StartedAtUtc DATETIME2(7) NOT NULL,FinishedAtUtc DATETIME2(7) NULL,DurationTicks BIGINT NULL,InputText VARCHAR(MAX) NOT NULL,InputSha256 VARCHAR(64) NOT NULL,OutputText VARCHAR(MAX) NOT NULL,OutputSha256 VARCHAR(64) NOT NULL,ErrorCode VARCHAR(128) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT(0),IsActive BIT NULL DEFAULT(1),ImportDataId UNIQUEIDENTIFIER NULL,ModificationNum INT NULL DEFAULT(0),Tag INT NULL DEFAULT(1),GroupId UNIQUEIDENTIFIER NULL,CompanyId UNIQUEIDENTIFIER NULL,AuditStatus VARCHAR(32) NULL DEFAULT('Add'),CurrentNode VARCHAR(32) NULL,CreatedBy UNIQUEIDENTIFIER NULL,CreatedTime DATETIME NULL,UpdateBy UNIQUEIDENTIFIER NULL,UpdateTime DATETIME NULL);
        CREATE TABLE dbo.AgUnifiedOrchestrationLink_Normalized (
            ID UNIQUEIDENTIFIER NOT NULL,EntryRunId UNIQUEIDENTIFIER NOT NULL,Ordinal BIGINT NOT NULL,ParentRunId UNIQUEIDENTIFIER NOT NULL,OrchestrationRunId UNIQUEIDENTIFIER NOT NULL,OrchestrationVersionId UNIQUEIDENTIFIER NOT NULL,Depth INT NOT NULL,Status VARCHAR(32) NOT NULL,StartedAtUtc DATETIME2(7) NOT NULL,FinishedAtUtc DATETIME2(7) NULL,DurationTicks BIGINT NULL,InputText VARCHAR(MAX) NOT NULL,InputSha256 VARCHAR(64) NOT NULL,OutputText VARCHAR(MAX) NOT NULL,OutputSha256 VARCHAR(64) NOT NULL,ErrorCode VARCHAR(128) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT(0),IsActive BIT NULL DEFAULT(1),ImportDataId UNIQUEIDENTIFIER NULL,ModificationNum INT NULL DEFAULT(0),Tag INT NULL DEFAULT(1),GroupId UNIQUEIDENTIFIER NULL,CompanyId UNIQUEIDENTIFIER NULL,AuditStatus VARCHAR(32) NULL DEFAULT('Add'),CurrentNode VARCHAR(32) NULL,CreatedBy UNIQUEIDENTIFIER NULL,CreatedTime DATETIME NULL,UpdateBy UNIQUEIDENTIFIER NULL,UpdateTime DATETIME NULL);
        CREATE TABLE dbo.AgUnifiedToolCall_Normalized (
            ID UNIQUEIDENTIFIER NOT NULL,EntryRunId UNIQUEIDENTIFIER NOT NULL,Ordinal BIGINT NOT NULL,ParentRunId UNIQUEIDENTIFIER NOT NULL,ToolVersionId UNIQUEIDENTIFIER NOT NULL,Depth INT NOT NULL,Status VARCHAR(32) NOT NULL,StartedAtUtc DATETIME2(7) NOT NULL,FinishedAtUtc DATETIME2(7) NULL,DurationTicks BIGINT NULL,ArgumentsJson VARCHAR(MAX) NOT NULL,ArgumentsSha256 VARCHAR(64) NOT NULL,ResultContent VARCHAR(MAX) NOT NULL,ResultSha256 VARCHAR(64) NOT NULL,ErrorCode VARCHAR(128) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT(0),IsActive BIT NULL DEFAULT(1),ImportDataId UNIQUEIDENTIFIER NULL,ModificationNum INT NULL DEFAULT(0),Tag INT NULL DEFAULT(1),GroupId UNIQUEIDENTIFIER NULL,CompanyId UNIQUEIDENTIFIER NULL,AuditStatus VARCHAR(32) NULL DEFAULT('Add'),CurrentNode VARCHAR(32) NULL,CreatedBy UNIQUEIDENTIFIER NULL,CreatedTime DATETIME NULL,UpdateBy UNIQUEIDENTIFIER NULL,UpdateTime DATETIME NULL);
        CREATE TABLE dbo.AgUnifiedRunEvent_Normalized (
            ID UNIQUEIDENTIFIER NOT NULL,EntryRunId UNIQUEIDENTIFIER NOT NULL,Sequence BIGINT NOT NULL,CorrelationId UNIQUEIDENTIFIER NOT NULL,Kind VARCHAR(64) NOT NULL,OccurredAtUtc DATETIME2(7) NOT NULL,ParentRunId UNIQUEIDENTIFIER NULL,Depth INT NOT NULL,PayloadJson VARCHAR(MAX) NOT NULL,PayloadSha256 VARCHAR(64) NOT NULL,
            IsDeleted BIT NOT NULL DEFAULT(0),IsActive BIT NULL DEFAULT(1),ImportDataId UNIQUEIDENTIFIER NULL,ModificationNum INT NULL DEFAULT(0),Tag INT NULL DEFAULT(1),GroupId UNIQUEIDENTIFIER NULL,CompanyId UNIQUEIDENTIFIER NULL,AuditStatus VARCHAR(32) NULL DEFAULT('Add'),CurrentNode VARCHAR(32) NULL,CreatedBy UNIQUEIDENTIFIER NULL,CreatedTime DATETIME NULL,UpdateBy UNIQUEIDENTIFIER NULL,UpdateTime DATETIME NULL);

        INSERT dbo.AgChatConversation_Normalized(ID,Title,CreatedAtUtc,UpdatedAtUtc,TenantId,UserId,IsDeleted,IsActive)
        SELECT CONVERT(UNIQUEIDENTIFIER,Id),CONVERT(VARCHAR(512),Title),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),CreatedAtUtc,127),'+00:00')),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),UpdatedAtUtc,127),'+00:00')),CONVERT(VARCHAR(128),TenantId),CONVERT(VARCHAR(128),UserId),0,1 FROM dbo.AgChatConversation;
        INSERT dbo.AgChatMessage_Normalized(ID,ConversationId,Ordinal,Role,Content,ContentSha256,ContentUtf8Bytes,CreatedAtUtc,Kind,BusinessQueryId,BusinessReceiptJson,BusinessPresentationJson,BusinessIntegritySha256,IsDeleted,IsActive)
        SELECT CONVERT(UNIQUEIDENTIFIER,Id),CONVERT(UNIQUEIDENTIFIER,ConversationId),Ordinal,CONVERT(VARCHAR(32),Role),CONVERT(VARCHAR(MAX),Content),CONVERT(VARCHAR(64),ContentSha256),ContentUtf8Bytes,CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),CreatedAtUtc,127),'+00:00')),CONVERT(VARCHAR(64),Kind),TRY_CONVERT(UNIQUEIDENTIFIER,BusinessQueryId),CONVERT(VARCHAR(MAX),BusinessReceiptJson),CONVERT(VARCHAR(MAX),BusinessPresentationJson),CONVERT(VARCHAR(64),BusinessIntegritySha256),0,1 FROM dbo.AgChatMessage;
        INSERT dbo.AgUnifiedEntryRun_Normalized(ID,ConversationId,CorrelationId,MainAgentVersionId,Status,StartedAtUtc,FinishedAtUtc,DurationTicks,InputText,InputSha256,OutputText,OutputSha256,ErrorCode,PersistenceRevision,StateSha256,TenantId,UserId,IsDeleted,IsActive)
        SELECT CONVERT(UNIQUEIDENTIFIER,Id),CONVERT(UNIQUEIDENTIFIER,ConversationId),CONVERT(UNIQUEIDENTIFIER,CorrelationId),CONVERT(UNIQUEIDENTIFIER,MainAgentVersionId),CONVERT(VARCHAR(32),Status),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),StartedAtUtc,127),'+00:00')),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127),'+00:00')),DurationTicks,CONVERT(VARCHAR(MAX),InputText),CONVERT(VARCHAR(64),InputSha256),CONVERT(VARCHAR(MAX),OutputText),CONVERT(VARCHAR(64),OutputSha256),CONVERT(VARCHAR(128),ErrorCode),PersistenceRevision,CONVERT(VARCHAR(64),StateSha256),CONVERT(VARCHAR(128),TenantId),CONVERT(VARCHAR(128),UserId),0,1 FROM dbo.AgUnifiedEntryRun;
        INSERT dbo.AgUnifiedAgentRun_Normalized(ID,EntryRunId,Ordinal,ParentRunId,Kind,AgentId,AgentVersionId,Depth,Status,StartedAtUtc,FinishedAtUtc,DurationTicks,InputText,InputSha256,OutputText,OutputSha256,ErrorCode,IsDeleted,IsActive)
        SELECT CONVERT(UNIQUEIDENTIFIER,Id),CONVERT(UNIQUEIDENTIFIER,EntryRunId),Ordinal,TRY_CONVERT(UNIQUEIDENTIFIER,ParentRunId),CONVERT(VARCHAR(64),Kind),CONVERT(UNIQUEIDENTIFIER,AgentId),CONVERT(UNIQUEIDENTIFIER,AgentVersionId),Depth,CONVERT(VARCHAR(32),Status),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),StartedAtUtc,127),'+00:00')),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127),'+00:00')),DurationTicks,CONVERT(VARCHAR(MAX),InputText),CONVERT(VARCHAR(64),InputSha256),CONVERT(VARCHAR(MAX),OutputText),CONVERT(VARCHAR(64),OutputSha256),CONVERT(VARCHAR(128),ErrorCode),0,1 FROM dbo.AgUnifiedAgentRun;
        INSERT dbo.AgUnifiedOrchestrationLink_Normalized(ID,EntryRunId,Ordinal,ParentRunId,OrchestrationRunId,OrchestrationVersionId,Depth,Status,StartedAtUtc,FinishedAtUtc,DurationTicks,InputText,InputSha256,OutputText,OutputSha256,ErrorCode,IsDeleted,IsActive)
        SELECT CONVERT(UNIQUEIDENTIFIER,Id),CONVERT(UNIQUEIDENTIFIER,EntryRunId),Ordinal,CONVERT(UNIQUEIDENTIFIER,ParentRunId),CONVERT(UNIQUEIDENTIFIER,OrchestrationRunId),CONVERT(UNIQUEIDENTIFIER,OrchestrationVersionId),Depth,CONVERT(VARCHAR(32),Status),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),StartedAtUtc,127),'+00:00')),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127),'+00:00')),DurationTicks,CONVERT(VARCHAR(MAX),InputText),CONVERT(VARCHAR(64),InputSha256),CONVERT(VARCHAR(MAX),OutputText),CONVERT(VARCHAR(64),OutputSha256),CONVERT(VARCHAR(128),ErrorCode),0,1 FROM dbo.AgUnifiedOrchestrationLink;
        INSERT dbo.AgUnifiedToolCall_Normalized(ID,EntryRunId,Ordinal,ParentRunId,ToolVersionId,Depth,Status,StartedAtUtc,FinishedAtUtc,DurationTicks,ArgumentsJson,ArgumentsSha256,ResultContent,ResultSha256,ErrorCode,IsDeleted,IsActive)
        SELECT CONVERT(UNIQUEIDENTIFIER,Id),CONVERT(UNIQUEIDENTIFIER,EntryRunId),Ordinal,CONVERT(UNIQUEIDENTIFIER,ParentRunId),CONVERT(UNIQUEIDENTIFIER,ToolVersionId),Depth,CONVERT(VARCHAR(32),Status),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),StartedAtUtc,127),'+00:00')),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),FinishedAtUtc,127),'+00:00')),DurationTicks,CONVERT(VARCHAR(MAX),ArgumentsJson),CONVERT(VARCHAR(64),ArgumentsSha256),CONVERT(VARCHAR(MAX),ResultContent),CONVERT(VARCHAR(64),ResultSha256),CONVERT(VARCHAR(128),ErrorCode),0,1 FROM dbo.AgUnifiedToolCall;
        INSERT dbo.AgUnifiedRunEvent_Normalized(ID,EntryRunId,Sequence,CorrelationId,Kind,OccurredAtUtc,ParentRunId,Depth,PayloadJson,PayloadSha256,IsDeleted,IsActive)
        SELECT CONVERT(UNIQUEIDENTIFIER,Id),CONVERT(UNIQUEIDENTIFIER,EntryRunId),Sequence,CONVERT(UNIQUEIDENTIFIER,CorrelationId),CONVERT(VARCHAR(64),Kind),CONVERT(DATETIME2(7),SWITCHOFFSET(TRY_CONVERT(DATETIMEOFFSET(7),OccurredAtUtc,127),'+00:00')),TRY_CONVERT(UNIQUEIDENTIFIER,ParentRunId),Depth,CONVERT(VARCHAR(MAX),PayloadJson),CONVERT(VARCHAR(64),PayloadSha256),0,1 FROM dbo.AgUnifiedRunEvent;

        IF (SELECT COUNT_BIG(*) FROM dbo.AgChatConversation_Normalized)<>(SELECT COUNT_BIG(*) FROM dbo.AgChatConversation)
         OR (SELECT COUNT_BIG(*) FROM dbo.AgChatMessage_Normalized)<>(SELECT COUNT_BIG(*) FROM dbo.AgChatMessage)
         OR (SELECT COUNT_BIG(*) FROM dbo.AgUnifiedEntryRun_Normalized)<>(SELECT COUNT_BIG(*) FROM dbo.AgUnifiedEntryRun)
         OR (SELECT COUNT_BIG(*) FROM dbo.AgUnifiedAgentRun_Normalized)<>(SELECT COUNT_BIG(*) FROM dbo.AgUnifiedAgentRun)
         OR (SELECT COUNT_BIG(*) FROM dbo.AgUnifiedOrchestrationLink_Normalized)<>(SELECT COUNT_BIG(*) FROM dbo.AgUnifiedOrchestrationLink)
         OR (SELECT COUNT_BIG(*) FROM dbo.AgUnifiedToolCall_Normalized)<>(SELECT COUNT_BIG(*) FROM dbo.AgUnifiedToolCall)
         OR (SELECT COUNT_BIG(*) FROM dbo.AgUnifiedRunEvent_Normalized)<>(SELECT COUNT_BIG(*) FROM dbo.AgUnifiedRunEvent)
            THROW 52305,N'Unified Entry normalization row-count validation failed.',1;

        DROP TABLE dbo.AgUnifiedRunEvent; DROP TABLE dbo.AgUnifiedToolCall; DROP TABLE dbo.AgUnifiedOrchestrationLink; DROP TABLE dbo.AgUnifiedAgentRun; DROP TABLE dbo.AgChatMessage; DROP TABLE dbo.AgUnifiedEntryRun; DROP TABLE dbo.AgChatConversation;
        EXEC sys.sp_rename N'dbo.AgChatConversation_Normalized',N'AgChatConversation';
        EXEC sys.sp_rename N'dbo.AgChatMessage_Normalized',N'AgChatMessage';
        EXEC sys.sp_rename N'dbo.AgUnifiedEntryRun_Normalized',N'AgUnifiedEntryRun';
        EXEC sys.sp_rename N'dbo.AgUnifiedAgentRun_Normalized',N'AgUnifiedAgentRun';
        EXEC sys.sp_rename N'dbo.AgUnifiedOrchestrationLink_Normalized',N'AgUnifiedOrchestrationLink';
        EXEC sys.sp_rename N'dbo.AgUnifiedToolCall_Normalized',N'AgUnifiedToolCall';
        EXEC sys.sp_rename N'dbo.AgUnifiedRunEvent_Normalized',N'AgUnifiedRunEvent';
    END;

    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgChatConversation') AND type=N'PK') ALTER TABLE dbo.AgChatConversation ADD CONSTRAINT pk_ag_chat_conversation PRIMARY KEY(ID);
    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgChatMessage') AND type=N'PK') ALTER TABLE dbo.AgChatMessage ADD CONSTRAINT pk_ag_chat_message PRIMARY KEY(ID);
    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedEntryRun') AND type=N'PK') ALTER TABLE dbo.AgUnifiedEntryRun ADD CONSTRAINT pk_ag_unified_entry_run PRIMARY KEY(ID);
    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedAgentRun') AND type=N'PK') ALTER TABLE dbo.AgUnifiedAgentRun ADD CONSTRAINT pk_ag_unified_agent_run PRIMARY KEY(ID);
    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedOrchestrationLink') AND type=N'PK') ALTER TABLE dbo.AgUnifiedOrchestrationLink ADD CONSTRAINT pk_ag_unified_orchestration_link PRIMARY KEY(ID);
    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedToolCall') AND type=N'PK') ALTER TABLE dbo.AgUnifiedToolCall ADD CONSTRAINT pk_ag_unified_tool_call PRIMARY KEY(ID);
    IF NOT EXISTS(SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedRunEvent') AND type=N'PK') ALTER TABLE dbo.AgUnifiedRunEvent ADD CONSTRAINT pk_ag_unified_run_event PRIMARY KEY(ID);

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgChatMessage') AND name=N'ux_ag_chat_message_conversation_ordinal') CREATE UNIQUE INDEX ux_ag_chat_message_conversation_ordinal ON dbo.AgChatMessage(ConversationId,Ordinal);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUnifiedAgentRun') AND name=N'ux_ag_unified_agent_run_entry_ordinal') CREATE UNIQUE INDEX ux_ag_unified_agent_run_entry_ordinal ON dbo.AgUnifiedAgentRun(EntryRunId,Ordinal);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUnifiedOrchestrationLink') AND name=N'ux_ag_unified_orchestration_link_entry_ordinal') CREATE UNIQUE INDEX ux_ag_unified_orchestration_link_entry_ordinal ON dbo.AgUnifiedOrchestrationLink(EntryRunId,Ordinal);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUnifiedToolCall') AND name=N'ux_ag_unified_tool_call_entry_ordinal') CREATE UNIQUE INDEX ux_ag_unified_tool_call_entry_ordinal ON dbo.AgUnifiedToolCall(EntryRunId,Ordinal);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUnifiedRunEvent') AND name=N'ux_ag_unified_run_event_entry_sequence') CREATE UNIQUE INDEX ux_ag_unified_run_event_entry_sequence ON dbo.AgUnifiedRunEvent(EntryRunId,Sequence);

    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgChatMessage') AND name=N'ck_ag_chat_message_ordinal') ALTER TABLE dbo.AgChatMessage ADD CONSTRAINT ck_ag_chat_message_ordinal CHECK(Ordinal>=0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgChatMessage') AND name=N'ck_ag_chat_message_content_bytes') ALTER TABLE dbo.AgChatMessage ADD CONSTRAINT ck_ag_chat_message_content_bytes CHECK(ContentUtf8Bytes>=0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedEntryRun') AND name=N'ck_ag_unified_entry_run_revision') ALTER TABLE dbo.AgUnifiedEntryRun ADD CONSTRAINT ck_ag_unified_entry_run_revision CHECK(PersistenceRevision>=0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedAgentRun') AND name=N'ck_ag_unified_agent_run_ordinal') ALTER TABLE dbo.AgUnifiedAgentRun ADD CONSTRAINT ck_ag_unified_agent_run_ordinal CHECK(Ordinal>=0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedAgentRun') AND name=N'ck_ag_unified_agent_run_depth') ALTER TABLE dbo.AgUnifiedAgentRun ADD CONSTRAINT ck_ag_unified_agent_run_depth CHECK(Depth>=0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedOrchestrationLink') AND name=N'ck_ag_unified_orchestration_link_ordinal') ALTER TABLE dbo.AgUnifiedOrchestrationLink ADD CONSTRAINT ck_ag_unified_orchestration_link_ordinal CHECK(Ordinal>=0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedOrchestrationLink') AND name=N'ck_ag_unified_orchestration_link_depth') ALTER TABLE dbo.AgUnifiedOrchestrationLink ADD CONSTRAINT ck_ag_unified_orchestration_link_depth CHECK(Depth>=0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedToolCall') AND name=N'ck_ag_unified_tool_call_ordinal') ALTER TABLE dbo.AgUnifiedToolCall ADD CONSTRAINT ck_ag_unified_tool_call_ordinal CHECK(Ordinal>=0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedToolCall') AND name=N'ck_ag_unified_tool_call_depth') ALTER TABLE dbo.AgUnifiedToolCall ADD CONSTRAINT ck_ag_unified_tool_call_depth CHECK(Depth>=0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedRunEvent') AND name=N'ck_ag_unified_run_event_sequence') ALTER TABLE dbo.AgUnifiedRunEvent ADD CONSTRAINT ck_ag_unified_run_event_sequence CHECK(Sequence>0);
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedRunEvent') AND name=N'ck_ag_unified_run_event_depth') ALTER TABLE dbo.AgUnifiedRunEvent ADD CONSTRAINT ck_ag_unified_run_event_depth CHECK(Depth>=0);

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.AgChatMessage') AND name=N'fk_ag_chat_message_conversation') ALTER TABLE dbo.AgChatMessage ADD CONSTRAINT fk_ag_chat_message_conversation FOREIGN KEY(ConversationId) REFERENCES dbo.AgChatConversation(ID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedEntryRun') AND name=N'fk_ag_unified_entry_run_conversation') ALTER TABLE dbo.AgUnifiedEntryRun ADD CONSTRAINT fk_ag_unified_entry_run_conversation FOREIGN KEY(ConversationId) REFERENCES dbo.AgChatConversation(ID);
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedAgentRun') AND name=N'fk_ag_unified_agent_run_entry') ALTER TABLE dbo.AgUnifiedAgentRun ADD CONSTRAINT fk_ag_unified_agent_run_entry FOREIGN KEY(EntryRunId) REFERENCES dbo.AgUnifiedEntryRun(ID) ON DELETE CASCADE;
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedOrchestrationLink') AND name=N'fk_ag_unified_orchestration_link_entry') ALTER TABLE dbo.AgUnifiedOrchestrationLink ADD CONSTRAINT fk_ag_unified_orchestration_link_entry FOREIGN KEY(EntryRunId) REFERENCES dbo.AgUnifiedEntryRun(ID) ON DELETE CASCADE;
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedToolCall') AND name=N'fk_ag_unified_tool_call_entry') ALTER TABLE dbo.AgUnifiedToolCall ADD CONSTRAINT fk_ag_unified_tool_call_entry FOREIGN KEY(EntryRunId) REFERENCES dbo.AgUnifiedEntryRun(ID) ON DELETE CASCADE;
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID(N'dbo.AgUnifiedRunEvent') AND name=N'fk_ag_unified_run_event_entry') ALTER TABLE dbo.AgUnifiedRunEvent ADD CONSTRAINT fk_ag_unified_run_event_entry FOREIGN KEY(EntryRunId) REFERENCES dbo.AgUnifiedEntryRun(ID) ON DELETE CASCADE;

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgChatConversation') AND name=N'ix_ag_chat_conversation_updated') CREATE INDEX ix_ag_chat_conversation_updated ON dbo.AgChatConversation(UpdatedAtUtc DESC,ID);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgChatConversation') AND name=N'ix_ag_chat_conversation_owner_updated') CREATE INDEX ix_ag_chat_conversation_owner_updated ON dbo.AgChatConversation(TenantId,UserId,UpdatedAtUtc DESC,ID);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgChatMessage') AND name=N'ix_ag_chat_message_business_query') CREATE INDEX ix_ag_chat_message_business_query ON dbo.AgChatMessage(BusinessQueryId) WHERE BusinessQueryId IS NOT NULL;
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUnifiedEntryRun') AND name=N'ix_ag_unified_entry_run_conversation_started') CREATE INDEX ix_ag_unified_entry_run_conversation_started ON dbo.AgUnifiedEntryRun(ConversationId,StartedAtUtc DESC,ID);
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.AgUnifiedEntryRun') AND name=N'ix_ag_unified_entry_run_owner_started') CREATE INDEX ix_ag_unified_entry_run_owner_started ON dbo.AgUnifiedEntryRun(TenantId,UserId,StartedAtUtc DESC,ID);

    DECLARE @IndexTable SYSNAME,@IndexName SYSNAME,@IndexSql NVARCHAR(MAX);
    DECLARE base_indexes CURSOR LOCAL FAST_FORWARD FOR SELECT * FROM (VALUES
      (N'AgChatConversation',N'chat_conversation'),(N'AgChatMessage',N'chat_message'),(N'AgUnifiedEntryRun',N'unified_entry_run'),
      (N'AgUnifiedAgentRun',N'unified_agent_run'),(N'AgUnifiedOrchestrationLink',N'unified_orchestration_link'),(N'AgUnifiedToolCall',N'unified_tool_call'),(N'AgUnifiedRunEvent',N'unified_run_event')) valueset(TableName,IndexName);
    OPEN base_indexes; FETCH NEXT FROM base_indexes INTO @IndexTable,@IndexName;
    WHILE @@FETCH_STATUS=0
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@IndexTable) AND name=N'ix_ag_'+@IndexName+N'_is_deleted')
        BEGIN SET @IndexSql=N'CREATE INDEX '+QUOTENAME(N'ix_ag_'+@IndexName+N'_is_deleted')+N' ON dbo.'+QUOTENAME(@IndexTable)+N'(IsDeleted);'; EXEC sys.sp_executesql @IndexSql; END;
        IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.'+@IndexTable) AND name=N'ix_ag_'+@IndexName+N'_is_active')
        BEGIN SET @IndexSql=N'CREATE INDEX '+QUOTENAME(N'ix_ag_'+@IndexName+N'_is_active')+N' ON dbo.'+QUOTENAME(@IndexTable)+N'(IsActive);'; EXEC sys.sp_executesql @IndexSql; END;
        FETCH NEXT FROM base_indexes INTO @IndexTable,@IndexName;
    END;
    CLOSE base_indexes; DEALLOCATE base_indexes;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
