-- EU.Core.Api.Agent shared EU.Core schema for SQL Server 2014+
-- Run this script in the target database. It mirrors the final SQLite schema.
-- Tables and secondary indexes are created idempotently; existing columns are not altered.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.AgSkillDefinition', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgSkillDefinition (
        Id CHAR(36) NOT NULL,
        Code NVARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL,
        DraftRevision BIGINT NOT NULL,
        DocumentJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_skill_definition PRIMARY KEY (Id),
        CONSTRAINT ux_ag_skill_definition_code UNIQUE (Code),
        CONSTRAINT ck_ag_skill_definition_revision CHECK (DraftRevision >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgAgentDefinition', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgAgentDefinition (
        ID UNIQUEIDENTIFIER NOT NULL,
        Code NVARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL,
        LogicalRevision BIGINT NOT NULL,
        DocumentJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_agent_definition PRIMARY KEY (ID),
        CONSTRAINT ux_ag_agent_definition_code UNIQUE (Code),
        CONSTRAINT ck_ag_agent_definition_revision CHECK (LogicalRevision >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgMcpServerDefinition', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgMcpServerDefinition (
        Id CHAR(36) NOT NULL,
        Code NVARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL,
        LogicalRevision BIGINT NOT NULL,
        DocumentJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_mcp_server_definition PRIMARY KEY (Id),
        CONSTRAINT ux_ag_mcp_server_definition_code UNIQUE (Code),
        CONSTRAINT ck_ag_mcp_server_definition_revision CHECK (LogicalRevision >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgKnowledgeBaseDefinition', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgKnowledgeBaseDefinition (
        Id CHAR(36) NOT NULL,
        Code NVARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL,
        LogicalRevision BIGINT NOT NULL,
        DocumentJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_knowledge_base_definition PRIMARY KEY (Id),
        CONSTRAINT ux_ag_knowledge_base_definition_code UNIQUE (Code),
        CONSTRAINT ck_ag_knowledge_base_definition_revision CHECK (LogicalRevision >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgAgentRunAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgAgentRunAudit (
        RunId CHAR(36) NOT NULL,
        AgentId CHAR(36) NOT NULL,
        StartedAtUtc VARCHAR(64) NOT NULL,
        Status VARCHAR(32) NOT NULL,
        DocumentJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_agent_run_audit PRIMARY KEY (RunId)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgAgentOperationAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgAgentOperationAudit (
        AuditId CHAR(36) NOT NULL,
        TenantId NVARCHAR(128) NOT NULL,
        OccurredAtUtc VARCHAR(64) NOT NULL,
        Outcome VARCHAR(32) NOT NULL,
        DocumentJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_agent_operation_audit PRIMARY KEY (AuditId)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationDefinition', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgOrchestrationDefinition (
        Id CHAR(36) NOT NULL,
        Code NVARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL,
        LogicalRevision BIGINT NOT NULL,
        DocumentJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_orchestration_definition PRIMARY KEY (Id),
        CONSTRAINT ux_ag_orchestration_definition_code UNIQUE (Code)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgOrchestrationRun (
        Id CHAR(36) NOT NULL,
        OrchestrationId CHAR(36) NOT NULL,
        StartedAtUtc VARCHAR(64) NOT NULL,
        DocumentJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_orchestration_run PRIMARY KEY (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationRunDetail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgOrchestrationRunDetail (
        RunId CHAR(36) NOT NULL,
        OrchestrationId CHAR(36) NOT NULL,
        InputText NVARCHAR(MAX) NOT NULL,
        OutputText NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_orchestration_run_detail PRIMARY KEY (RunId)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationNodeAttempt', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgOrchestrationNodeAttempt (
        RunId CHAR(36) NOT NULL,
        NodeId CHAR(36) NOT NULL,
        Attempt INT NOT NULL,
        Sequence INT NOT NULL,
        AgentRunId CHAR(36) NOT NULL,
        InputText NVARCHAR(MAX) NOT NULL,
        InputSha256 CHAR(64) NOT NULL,
        OutputText NVARCHAR(MAX) NOT NULL,
        OutputSha256 CHAR(64) NOT NULL,
        Status VARCHAR(32) NOT NULL,
        StartedAtUtc VARCHAR(64) NOT NULL,
        FinishedAtUtc VARCHAR(64) NULL,
        ErrorCode VARCHAR(128) NOT NULL,
        CONSTRAINT pk_ag_orchestration_node_attempt PRIMARY KEY (RunId, NodeId, Attempt)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgOrchestrationToolCall', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgOrchestrationToolCall (
        ToolCallId CHAR(36) NOT NULL,
        RunId CHAR(36) NOT NULL,
        NodeId CHAR(36) NOT NULL,
        Attempt INT NOT NULL,
        Sequence INT NOT NULL,
        AgentRunId CHAR(36) NOT NULL,
        ToolVersionId CHAR(36) NOT NULL,
        ToolName NVARCHAR(256) NOT NULL,
        Status VARCHAR(32) NOT NULL,
        ArgumentsJson NVARCHAR(MAX) NOT NULL,
        ResultContent NVARCHAR(MAX) NOT NULL,
        ResultSha256 CHAR(64) NOT NULL,
        ResultCharacters BIGINT NOT NULL,
        StartedAtUtc VARCHAR(64) NOT NULL,
        FinishedAtUtc VARCHAR(64) NULL,
        ErrorCode VARCHAR(128) NOT NULL,
        CONSTRAINT pk_ag_orchestration_tool_call PRIMARY KEY (ToolCallId)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgChatConversation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgChatConversation (
        Id CHAR(36) NOT NULL,
        Title NVARCHAR(512) NOT NULL,
        CreatedAtUtc VARCHAR(64) NOT NULL,
        UpdatedAtUtc VARCHAR(64) NOT NULL,
        TenantId NVARCHAR(128) NOT NULL CONSTRAINT df_ag_chat_conversation_tenant DEFAULT N'__legacy_unowned__',
        UserId NVARCHAR(128) NOT NULL CONSTRAINT df_ag_chat_conversation_user DEFAULT N'__legacy_unowned__',
        CONSTRAINT pk_ag_chat_conversation PRIMARY KEY (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgChatMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgChatMessage (
        Id CHAR(36) NOT NULL,
        ConversationId CHAR(36) NOT NULL,
        Ordinal BIGINT NOT NULL,
        Role VARCHAR(32) NOT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        ContentSha256 CHAR(64) NOT NULL,
        ContentUtf8Bytes BIGINT NOT NULL,
        CreatedAtUtc VARCHAR(64) NOT NULL,
        Kind VARCHAR(64) NOT NULL CONSTRAINT df_ag_chat_message_kind DEFAULT 'Legacy',
        BusinessQueryId CHAR(36) NULL,
        BusinessReceiptJson NVARCHAR(MAX) NOT NULL CONSTRAINT df_ag_chat_message_business_receipt DEFAULT N'',
        BusinessPresentationJson NVARCHAR(MAX) NOT NULL CONSTRAINT df_ag_chat_message_business_presentation DEFAULT N'',
        BusinessIntegritySha256 VARCHAR(64) NOT NULL CONSTRAINT df_ag_chat_message_business_integrity DEFAULT '',
        CONSTRAINT pk_ag_chat_message PRIMARY KEY (Id),
        CONSTRAINT ux_ag_chat_message_conversation_ordinal UNIQUE (ConversationId, Ordinal),
        CONSTRAINT ck_ag_chat_message_ordinal CHECK (Ordinal >= 0),
        CONSTRAINT ck_ag_chat_message_content_bytes CHECK (ContentUtf8Bytes >= 0),
        CONSTRAINT fk_ag_chat_message_conversation FOREIGN KEY (ConversationId)
            REFERENCES dbo.AgChatConversation (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgUnifiedEntryRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgUnifiedEntryRun (
        Id CHAR(36) NOT NULL,
        ConversationId CHAR(36) NOT NULL,
        CorrelationId CHAR(36) NOT NULL,
        MainAgentVersionId CHAR(36) NOT NULL,
        Status VARCHAR(32) NOT NULL,
        StartedAtUtc VARCHAR(64) NOT NULL,
        FinishedAtUtc VARCHAR(64) NULL,
        DurationTicks BIGINT NULL,
        InputText NVARCHAR(MAX) NOT NULL,
        InputSha256 CHAR(64) NOT NULL,
        OutputText NVARCHAR(MAX) NOT NULL,
        OutputSha256 CHAR(64) NOT NULL,
        ErrorCode VARCHAR(128) NOT NULL,
        PersistenceRevision BIGINT NOT NULL,
        StateSha256 VARCHAR(64) NOT NULL CONSTRAINT df_ag_unified_entry_run_state DEFAULT '',
        TenantId NVARCHAR(128) NOT NULL CONSTRAINT df_ag_unified_entry_run_tenant DEFAULT N'__legacy_unowned__',
        UserId NVARCHAR(128) NOT NULL CONSTRAINT df_ag_unified_entry_run_user DEFAULT N'__legacy_unowned__',
        CONSTRAINT pk_ag_unified_entry_run PRIMARY KEY (Id),
        CONSTRAINT ck_ag_unified_entry_run_revision CHECK (PersistenceRevision >= 0),
        CONSTRAINT fk_ag_unified_entry_run_conversation FOREIGN KEY (ConversationId)
            REFERENCES dbo.AgChatConversation (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgUnifiedAgentRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgUnifiedAgentRun (
        Id CHAR(36) NOT NULL,
        EntryRunId CHAR(36) NOT NULL,
        Ordinal BIGINT NOT NULL,
        ParentRunId CHAR(36) NULL,
        Kind VARCHAR(64) NOT NULL,
        AgentId CHAR(36) NOT NULL,
        AgentVersionId CHAR(36) NOT NULL,
        Depth INT NOT NULL,
        Status VARCHAR(32) NOT NULL,
        StartedAtUtc VARCHAR(64) NOT NULL,
        FinishedAtUtc VARCHAR(64) NULL,
        DurationTicks BIGINT NULL,
        InputText NVARCHAR(MAX) NOT NULL,
        InputSha256 CHAR(64) NOT NULL,
        OutputText NVARCHAR(MAX) NOT NULL,
        OutputSha256 CHAR(64) NOT NULL,
        ErrorCode VARCHAR(128) NOT NULL,
        CONSTRAINT pk_ag_unified_agent_run PRIMARY KEY (Id),
        CONSTRAINT ux_ag_unified_agent_run_entry_ordinal UNIQUE (EntryRunId, Ordinal),
        CONSTRAINT ck_ag_unified_agent_run_ordinal CHECK (Ordinal >= 0),
        CONSTRAINT ck_ag_unified_agent_run_depth CHECK (Depth >= 0),
        CONSTRAINT fk_ag_unified_agent_run_entry FOREIGN KEY (EntryRunId)
            REFERENCES dbo.AgUnifiedEntryRun (Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.AgUnifiedOrchestrationLink', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgUnifiedOrchestrationLink (
        Id CHAR(36) NOT NULL,
        EntryRunId CHAR(36) NOT NULL,
        Ordinal BIGINT NOT NULL,
        ParentRunId CHAR(36) NOT NULL,
        OrchestrationRunId CHAR(36) NOT NULL,
        OrchestrationVersionId CHAR(36) NOT NULL,
        Depth INT NOT NULL,
        Status VARCHAR(32) NOT NULL,
        StartedAtUtc VARCHAR(64) NOT NULL,
        FinishedAtUtc VARCHAR(64) NULL,
        DurationTicks BIGINT NULL,
        InputText NVARCHAR(MAX) NOT NULL,
        InputSha256 CHAR(64) NOT NULL,
        OutputText NVARCHAR(MAX) NOT NULL,
        OutputSha256 CHAR(64) NOT NULL,
        ErrorCode VARCHAR(128) NOT NULL,
        CONSTRAINT pk_ag_unified_orchestration_link PRIMARY KEY (Id),
        CONSTRAINT ux_ag_unified_orchestration_link_entry_ordinal UNIQUE (EntryRunId, Ordinal),
        CONSTRAINT ck_ag_unified_orchestration_link_ordinal CHECK (Ordinal >= 0),
        CONSTRAINT ck_ag_unified_orchestration_link_depth CHECK (Depth >= 0),
        CONSTRAINT fk_ag_unified_orchestration_link_entry FOREIGN KEY (EntryRunId)
            REFERENCES dbo.AgUnifiedEntryRun (Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.AgUnifiedToolCall', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgUnifiedToolCall (
        Id CHAR(36) NOT NULL,
        EntryRunId CHAR(36) NOT NULL,
        Ordinal BIGINT NOT NULL,
        ParentRunId CHAR(36) NOT NULL,
        ToolVersionId CHAR(36) NOT NULL,
        Depth INT NOT NULL,
        Status VARCHAR(32) NOT NULL,
        StartedAtUtc VARCHAR(64) NOT NULL,
        FinishedAtUtc VARCHAR(64) NULL,
        DurationTicks BIGINT NULL,
        ArgumentsJson NVARCHAR(MAX) NOT NULL,
        ArgumentsSha256 CHAR(64) NOT NULL,
        ResultContent NVARCHAR(MAX) NOT NULL,
        ResultSha256 CHAR(64) NOT NULL,
        ErrorCode VARCHAR(128) NOT NULL,
        CONSTRAINT pk_ag_unified_tool_call PRIMARY KEY (Id),
        CONSTRAINT ux_ag_unified_tool_call_entry_ordinal UNIQUE (EntryRunId, Ordinal),
        CONSTRAINT ck_ag_unified_tool_call_ordinal CHECK (Ordinal >= 0),
        CONSTRAINT ck_ag_unified_tool_call_depth CHECK (Depth >= 0),
        CONSTRAINT fk_ag_unified_tool_call_entry FOREIGN KEY (EntryRunId)
            REFERENCES dbo.AgUnifiedEntryRun (Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.AgUnifiedRunEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgUnifiedRunEvent (
        Id CHAR(36) NOT NULL,
        EntryRunId CHAR(36) NOT NULL,
        Sequence BIGINT NOT NULL,
        CorrelationId CHAR(36) NOT NULL,
        Kind VARCHAR(64) NOT NULL,
        OccurredAtUtc VARCHAR(64) NOT NULL,
        ParentRunId CHAR(36) NULL,
        Depth INT NOT NULL,
        PayloadJson NVARCHAR(MAX) NOT NULL,
        PayloadSha256 CHAR(64) NOT NULL,
        CONSTRAINT pk_ag_unified_run_event PRIMARY KEY (Id),
        CONSTRAINT ux_ag_unified_run_event_entry_sequence UNIQUE (EntryRunId, Sequence),
        CONSTRAINT ck_ag_unified_run_event_sequence CHECK (Sequence > 0),
        CONSTRAINT ck_ag_unified_run_event_depth CHECK (Depth >= 0),
        CONSTRAINT fk_ag_unified_run_event_entry FOREIGN KEY (EntryRunId)
            REFERENCES dbo.AgUnifiedEntryRun (Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.AgMainAgentAssignment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgMainAgentAssignment (
        AssignmentKey VARCHAR(64) NOT NULL,
        AgentId CHAR(36) NOT NULL,
        AgentVersionId CHAR(36) NOT NULL,
        LogicalRevision BIGINT NOT NULL,
        UpdatedAtUtc VARCHAR(64) NOT NULL,
        CONSTRAINT pk_ag_main_agent_assignment PRIMARY KEY (AssignmentKey),
        CONSTRAINT ck_ag_main_agent_assignment_key CHECK (AssignmentKey = 'platform-main-agent'),
        CONSTRAINT ck_ag_main_agent_assignment_revision CHECK (LogicalRevision >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgToolApprovalRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgToolApprovalRequest (
        Id CHAR(36) NOT NULL,
        TenantId NVARCHAR(128) NOT NULL,
        RequesterUserId NVARCHAR(128) NOT NULL,
        ConversationId CHAR(36) NOT NULL,
        EntryRunId CHAR(36) NOT NULL,
        AgentRunId CHAR(36) NOT NULL,
        AgentVersionId CHAR(36) NOT NULL,
        McpServerId CHAR(36) NOT NULL,
        ToolVersionId CHAR(36) NOT NULL,
        ToolName NVARCHAR(256) NOT NULL,
        Risk INT NOT NULL,
        ToolSchemaSha256 CHAR(64) NOT NULL,
        ArgumentsSha256 CHAR(64) NOT NULL,
        SafeArgumentsSummaryJson NVARCHAR(MAX) NOT NULL,
        Status INT NOT NULL,
        LogicalRevision BIGINT NOT NULL,
        RequestedAtUtc VARCHAR(64) NOT NULL,
        ExpiresAtUtc VARCHAR(64) NOT NULL,
        DecisionUserId NVARCHAR(128) NOT NULL,
        DecisionReason NVARCHAR(MAX) NOT NULL,
        DecidedAtUtc VARCHAR(64) NULL,
        ClaimedAtUtc VARCHAR(64) NULL,
        FinishedAtUtc VARCHAR(64) NULL,
        ErrorCode VARCHAR(128) NOT NULL,
        CONSTRAINT pk_ag_tool_approval_request PRIMARY KEY (Id),
        CONSTRAINT ck_ag_tool_approval_request_revision CHECK (LogicalRevision >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgToolApprovalPayload', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgToolApprovalPayload (
        ApprovalId CHAR(36) NOT NULL,
        ProtectedPayload NVARCHAR(MAX) NOT NULL,
        ProtectedPayloadSha256 CHAR(64) NOT NULL,
        CONSTRAINT pk_ag_tool_approval_payload PRIMARY KEY (ApprovalId),
        CONSTRAINT fk_ag_tool_approval_payload_request FOREIGN KEY (ApprovalId)
            REFERENCES dbo.AgToolApprovalRequest (Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.AgToolApprovalDecision', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgToolApprovalDecision (
        Id CHAR(36) NOT NULL,
        ApprovalId CHAR(36) NOT NULL,
        TenantId NVARCHAR(128) NOT NULL,
        FromStatus INT NOT NULL,
        ToStatus INT NOT NULL,
        DecisionUserId NVARCHAR(128) NOT NULL,
        DecisionReason NVARCHAR(MAX) NOT NULL,
        DecidedAtUtc VARCHAR(64) NOT NULL,
        ResultingLogicalRevision BIGINT NOT NULL,
        CONSTRAINT pk_ag_tool_approval_decision PRIMARY KEY (Id),
        CONSTRAINT ux_ag_tool_approval_decision_revision UNIQUE (ApprovalId, ResultingLogicalRevision),
        CONSTRAINT fk_ag_tool_approval_decision_request FOREIGN KEY (ApprovalId)
            REFERENCES dbo.AgToolApprovalRequest (Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.AgToolApprovalExecutionResult', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgToolApprovalExecutionResult (
        ApprovalId CHAR(36) NOT NULL,
        TenantId NVARCHAR(128) NOT NULL,
        Succeeded BIT NOT NULL,
        Blocked BIT NOT NULL,
        ProtectedContent NVARCHAR(MAX) NOT NULL,
        ProtectedContentSha256 CHAR(64) NOT NULL,
        ContentSha256 CHAR(64) NOT NULL,
        ErrorCode VARCHAR(128) NOT NULL,
        FinishedAtUtc VARCHAR(64) NOT NULL,
        CONSTRAINT pk_ag_tool_approval_execution_result PRIMARY KEY (ApprovalId),
        CONSTRAINT fk_ag_tool_approval_execution_result_request FOREIGN KEY (ApprovalId)
            REFERENCES dbo.AgToolApprovalRequest (Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'dbo.AgEvaluationSuite', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgEvaluationSuite (
        Id CHAR(36) NOT NULL,
        TenantId NVARCHAR(128) NOT NULL,
        Code NVARCHAR(128) COLLATE Latin1_General_100_BIN2 NOT NULL,
        LogicalRevision BIGINT NOT NULL,
        DocumentJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_evaluation_suite PRIMARY KEY (Id),
        CONSTRAINT ux_ag_evaluation_suite_tenant_code UNIQUE (TenantId, Code),
        CONSTRAINT ck_ag_evaluation_suite_revision CHECK (LogicalRevision >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgEvaluationBatch', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgEvaluationBatch (
        Id CHAR(36) NOT NULL,
        TenantId NVARCHAR(128) NOT NULL,
        SuiteId CHAR(36) NOT NULL,
        SuiteVersionId CHAR(36) NOT NULL,
        Status VARCHAR(32) NOT NULL,
        LogicalRevision BIGINT NOT NULL,
        StartedAtUtc VARCHAR(64) NOT NULL,
        DocumentJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_evaluation_batch PRIMARY KEY (Id),
        CONSTRAINT ck_ag_evaluation_batch_revision CHECK (LogicalRevision >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgEvaluationModelJudgement', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgEvaluationModelJudgement (
        Id CHAR(36) NOT NULL,
        TenantId NVARCHAR(128) NOT NULL,
        BatchId CHAR(36) NOT NULL,
        ConfigurationSha256 CHAR(64) NOT NULL,
        StartedAtUtc VARCHAR(64) NOT NULL,
        DocumentJson NVARCHAR(MAX) NOT NULL,
        CONSTRAINT pk_ag_evaluation_model_judgement PRIMARY KEY (Id),
        CONSTRAINT ux_ag_evaluation_model_judgement_configuration
            UNIQUE (TenantId, BatchId, ConfigurationSha256)
    );
END;
GO

IF OBJECT_ID(N'dbo.AgApiIdempotency', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AgApiIdempotency (
        ScopeSha256 CHAR(64) NOT NULL,
        RequestSha256 CHAR(64) NOT NULL,
        Status VARCHAR(32) NOT NULL,
        ResponseStatusCode INT NOT NULL,
        ResponseContentType NVARCHAR(256) NOT NULL,
        ResponseLocation NVARCHAR(2048) NOT NULL,
        ResponseBody VARBINARY(MAX) NOT NULL,
        CreatedAtUtc VARCHAR(64) NOT NULL,
        ExpiresAtUtc VARCHAR(64) NOT NULL,
        CONSTRAINT pk_ag_api_idempotency PRIMARY KEY (ScopeSha256)
    );
END;
GO

-- Create secondary indexes independently so rerunning the baseline also repairs missing indexes.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentRunAudit') AND name = N'ix_ag_agent_run_audit_agent_started')
    CREATE INDEX ix_ag_agent_run_audit_agent_started ON dbo.AgAgentRunAudit (AgentId, StartedAtUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgAgentOperationAudit') AND name = N'ix_ag_agent_operation_audit_tenant_time')
    CREATE INDEX ix_ag_agent_operation_audit_tenant_time ON dbo.AgAgentOperationAudit (TenantId, OccurredAtUtc DESC, AuditId DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationRun') AND name = N'ix_ag_orchestration_run_owner')
    CREATE INDEX ix_ag_orchestration_run_owner ON dbo.AgOrchestrationRun (OrchestrationId, StartedAtUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationNodeAttempt') AND name = N'ix_ag_orchestration_node_attempt_order')
    CREATE INDEX ix_ag_orchestration_node_attempt_order ON dbo.AgOrchestrationNodeAttempt (RunId, Sequence);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgOrchestrationToolCall') AND name = N'ix_ag_orchestration_tool_call_order')
    CREATE INDEX ix_ag_orchestration_tool_call_order ON dbo.AgOrchestrationToolCall (RunId, NodeId, Attempt, Sequence);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgChatConversation') AND name = N'ix_ag_chat_conversation_updated')
    CREATE INDEX ix_ag_chat_conversation_updated ON dbo.AgChatConversation (UpdatedAtUtc DESC, Id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgChatConversation') AND name = N'ix_ag_chat_conversation_owner_updated')
    CREATE INDEX ix_ag_chat_conversation_owner_updated ON dbo.AgChatConversation (TenantId, UserId, UpdatedAtUtc DESC, Id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgChatMessage') AND name = N'ix_ag_chat_message_business_query')
    CREATE INDEX ix_ag_chat_message_business_query ON dbo.AgChatMessage (BusinessQueryId) WHERE BusinessQueryId IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgUnifiedEntryRun') AND name = N'ix_ag_unified_entry_run_conversation_started')
    CREATE INDEX ix_ag_unified_entry_run_conversation_started ON dbo.AgUnifiedEntryRun (ConversationId, StartedAtUtc DESC, Id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgUnifiedEntryRun') AND name = N'ix_ag_unified_entry_run_owner_started')
    CREATE INDEX ix_ag_unified_entry_run_owner_started ON dbo.AgUnifiedEntryRun (TenantId, UserId, StartedAtUtc DESC, Id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgToolApprovalRequest') AND name = N'ix_ag_tool_approval_request_tenant_status_requested')
    CREATE INDEX ix_ag_tool_approval_request_tenant_status_requested ON dbo.AgToolApprovalRequest (TenantId, Status, RequestedAtUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgToolApprovalDecision') AND name = N'ix_ag_tool_approval_decision_tenant_approval')
    CREATE INDEX ix_ag_tool_approval_decision_tenant_approval ON dbo.AgToolApprovalDecision (TenantId, ApprovalId, ResultingLogicalRevision);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgToolApprovalExecutionResult') AND name = N'ix_ag_tool_approval_execution_result_tenant')
    CREATE INDEX ix_ag_tool_approval_execution_result_tenant ON dbo.AgToolApprovalExecutionResult (TenantId, ApprovalId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationSuite') AND name = N'ix_ag_evaluation_suite_tenant_code')
    CREATE INDEX ix_ag_evaluation_suite_tenant_code ON dbo.AgEvaluationSuite (TenantId, Code);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationBatch') AND name = N'ix_ag_evaluation_batch_suite_started')
    CREATE INDEX ix_ag_evaluation_batch_suite_started ON dbo.AgEvaluationBatch (TenantId, SuiteId, StartedAtUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationBatch') AND name = N'ix_ag_evaluation_batch_status')
    CREATE INDEX ix_ag_evaluation_batch_status ON dbo.AgEvaluationBatch (Status);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgEvaluationModelJudgement') AND name = N'ix_ag_evaluation_model_judgement_batch_started')
    CREATE INDEX ix_ag_evaluation_model_judgement_batch_started ON dbo.AgEvaluationModelJudgement (TenantId, BatchId, StartedAtUtc DESC);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AgApiIdempotency') AND name = N'ix_ag_api_idempotency_expires')
    CREATE INDEX ix_ag_api_idempotency_expires ON dbo.AgApiIdempotency (ExpiresAtUtc);
GO
