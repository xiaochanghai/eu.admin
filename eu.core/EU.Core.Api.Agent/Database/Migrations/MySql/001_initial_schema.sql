-- EU.Core.Api.Agent shared EU.Core schema for MySQL 8.0.13+
-- Run this script in the target database with utf8mb4 enabled.
-- The schema mirrors the final SQLite schema used by the Agent host.

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `AgSkillDefinition` (
    `Id` CHAR(36) NOT NULL,
    `Code` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    `DraftRevision` BIGINT NOT NULL,
    `DocumentJson` JSON NOT NULL,
    CONSTRAINT `pk_ag_skill_definition` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_skill_definition_code` UNIQUE (`Code`),
    CONSTRAINT `ck_ag_skill_definition_revision` CHECK (`DraftRevision` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgAgentDefinition` (
    `Id` CHAR(36) NOT NULL,
    `Code` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    `LogicalRevision` BIGINT NOT NULL,
    `DocumentJson` JSON NOT NULL,
    CONSTRAINT `pk_ag_agent_definition` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_agent_definition_code` UNIQUE (`Code`),
    CONSTRAINT `ck_ag_agent_definition_revision` CHECK (`LogicalRevision` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgMcpServerDefinition` (
    `Id` CHAR(36) NOT NULL,
    `Code` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    `LogicalRevision` BIGINT NOT NULL,
    `DocumentJson` JSON NOT NULL,
    CONSTRAINT `pk_ag_mcp_server_definition` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_mcp_server_definition_code` UNIQUE (`Code`),
    CONSTRAINT `ck_ag_mcp_server_definition_revision` CHECK (`LogicalRevision` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgKnowledgeBaseDefinition` (
    `Id` CHAR(36) NOT NULL,
    `Code` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    `LogicalRevision` BIGINT NOT NULL,
    `DocumentJson` JSON NOT NULL,
    CONSTRAINT `pk_ag_knowledge_base_definition` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_knowledge_base_definition_code` UNIQUE (`Code`),
    CONSTRAINT `ck_ag_knowledge_base_definition_revision` CHECK (`LogicalRevision` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgAgentRunAudit` (
    `RunId` CHAR(36) NOT NULL,
    `AgentId` CHAR(36) NOT NULL,
    `StartedAtUtc` VARCHAR(64) NOT NULL,
    `Status` VARCHAR(32) NOT NULL,
    `DocumentJson` JSON NOT NULL,
    CONSTRAINT `pk_ag_agent_run_audit` PRIMARY KEY (`RunId`),
    INDEX `ix_ag_agent_run_audit_agent_started` (`AgentId`, `StartedAtUtc` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgAgentOperationAudit` (
    `AuditId` CHAR(36) NOT NULL,
    `TenantId` VARCHAR(128) NOT NULL,
    `OccurredAtUtc` VARCHAR(64) NOT NULL,
    `Outcome` VARCHAR(32) NOT NULL,
    `DocumentJson` JSON NOT NULL,
    CONSTRAINT `pk_ag_agent_operation_audit` PRIMARY KEY (`AuditId`),
    INDEX `ix_ag_agent_operation_audit_tenant_time`
        (`TenantId`, `OccurredAtUtc` DESC, `AuditId` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgOrchestrationDefinition` (
    `Id` CHAR(36) NOT NULL,
    `Code` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    `LogicalRevision` BIGINT NOT NULL,
    `DocumentJson` JSON NOT NULL,
    CONSTRAINT `pk_ag_orchestration_definition` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_orchestration_definition_code` UNIQUE (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgOrchestrationRun` (
    `Id` CHAR(36) NOT NULL,
    `OrchestrationId` CHAR(36) NOT NULL,
    `StartedAtUtc` VARCHAR(64) NOT NULL,
    `DocumentJson` JSON NOT NULL,
    CONSTRAINT `pk_ag_orchestration_run` PRIMARY KEY (`Id`),
    INDEX `ix_ag_orchestration_run_owner` (`OrchestrationId`, `StartedAtUtc` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgOrchestrationRunDetail` (
    `RunId` CHAR(36) NOT NULL,
    `OrchestrationId` CHAR(36) NOT NULL,
    `InputText` LONGTEXT NOT NULL,
    `OutputText` LONGTEXT NOT NULL,
    CONSTRAINT `pk_ag_orchestration_run_detail` PRIMARY KEY (`RunId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgOrchestrationNodeAttempt` (
    `RunId` CHAR(36) NOT NULL,
    `NodeId` CHAR(36) NOT NULL,
    `Attempt` INT NOT NULL,
    `Sequence` INT NOT NULL,
    `AgentRunId` CHAR(36) NOT NULL,
    `InputText` LONGTEXT NOT NULL,
    `InputSha256` CHAR(64) NOT NULL,
    `OutputText` LONGTEXT NOT NULL,
    `OutputSha256` CHAR(64) NOT NULL,
    `Status` VARCHAR(32) NOT NULL,
    `StartedAtUtc` VARCHAR(64) NOT NULL,
    `FinishedAtUtc` VARCHAR(64) NULL,
    `ErrorCode` VARCHAR(128) NOT NULL,
    CONSTRAINT `pk_ag_orchestration_node_attempt` PRIMARY KEY (`RunId`, `NodeId`, `Attempt`),
    INDEX `ix_ag_orchestration_node_attempt_order` (`RunId`, `Sequence`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgOrchestrationToolCall` (
    `ToolCallId` CHAR(36) NOT NULL,
    `RunId` CHAR(36) NOT NULL,
    `NodeId` CHAR(36) NOT NULL,
    `Attempt` INT NOT NULL,
    `Sequence` INT NOT NULL,
    `AgentRunId` CHAR(36) NOT NULL,
    `ToolVersionId` CHAR(36) NOT NULL,
    `ToolName` VARCHAR(256) NOT NULL,
    `Status` VARCHAR(32) NOT NULL,
    `ArgumentsJson` LONGTEXT NOT NULL,
    `ResultContent` LONGTEXT NOT NULL,
    `ResultSha256` CHAR(64) NOT NULL,
    `ResultCharacters` BIGINT NOT NULL,
    `StartedAtUtc` VARCHAR(64) NOT NULL,
    `FinishedAtUtc` VARCHAR(64) NULL,
    `ErrorCode` VARCHAR(128) NOT NULL,
    CONSTRAINT `pk_ag_orchestration_tool_call` PRIMARY KEY (`ToolCallId`),
    INDEX `ix_ag_orchestration_tool_call_order` (`RunId`, `NodeId`, `Attempt`, `Sequence`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgChatConversation` (
    `Id` CHAR(36) NOT NULL,
    `Title` VARCHAR(512) NOT NULL,
    `CreatedAtUtc` VARCHAR(64) NOT NULL,
    `UpdatedAtUtc` VARCHAR(64) NOT NULL,
    `TenantId` VARCHAR(128) NOT NULL DEFAULT '__legacy_unowned__',
    `UserId` VARCHAR(128) NOT NULL DEFAULT '__legacy_unowned__',
    CONSTRAINT `pk_ag_chat_conversation` PRIMARY KEY (`Id`),
    INDEX `ix_ag_chat_conversation_updated` (`UpdatedAtUtc` DESC, `Id`),
    INDEX `ix_ag_chat_conversation_owner_updated`
        (`TenantId`, `UserId`, `UpdatedAtUtc` DESC, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgChatMessage` (
    `Id` CHAR(36) NOT NULL,
    `ConversationId` CHAR(36) NOT NULL,
    `Ordinal` BIGINT NOT NULL,
    `Role` VARCHAR(32) NOT NULL,
    `Content` LONGTEXT NOT NULL,
    `ContentSha256` CHAR(64) NOT NULL,
    `ContentUtf8Bytes` BIGINT NOT NULL,
    `CreatedAtUtc` VARCHAR(64) NOT NULL,
    `Kind` VARCHAR(64) NOT NULL DEFAULT 'Legacy',
    `BusinessQueryId` CHAR(36) NULL,
    `BusinessReceiptJson` LONGTEXT NOT NULL DEFAULT (''),
    `BusinessPresentationJson` LONGTEXT NOT NULL DEFAULT (''),
    `BusinessIntegritySha256` VARCHAR(64) NOT NULL DEFAULT '',
    CONSTRAINT `pk_ag_chat_message` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_chat_message_conversation_ordinal` UNIQUE (`ConversationId`, `Ordinal`),
    CONSTRAINT `ck_ag_chat_message_ordinal` CHECK (`Ordinal` >= 0),
    CONSTRAINT `ck_ag_chat_message_content_bytes` CHECK (`ContentUtf8Bytes` >= 0),
    CONSTRAINT `fk_ag_chat_message_conversation` FOREIGN KEY (`ConversationId`)
        REFERENCES `AgChatConversation` (`Id`),
    INDEX `ix_ag_chat_message_business_query` (`BusinessQueryId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgUnifiedEntryRun` (
    `Id` CHAR(36) NOT NULL,
    `ConversationId` CHAR(36) NOT NULL,
    `CorrelationId` CHAR(36) NOT NULL,
    `MainAgentVersionId` CHAR(36) NOT NULL,
    `Status` VARCHAR(32) NOT NULL,
    `StartedAtUtc` VARCHAR(64) NOT NULL,
    `FinishedAtUtc` VARCHAR(64) NULL,
    `DurationTicks` BIGINT NULL,
    `InputText` LONGTEXT NOT NULL,
    `InputSha256` CHAR(64) NOT NULL,
    `OutputText` LONGTEXT NOT NULL,
    `OutputSha256` CHAR(64) NOT NULL,
    `ErrorCode` VARCHAR(128) NOT NULL,
    `PersistenceRevision` BIGINT NOT NULL,
    `StateSha256` VARCHAR(64) NOT NULL DEFAULT '',
    `TenantId` VARCHAR(128) NOT NULL DEFAULT '__legacy_unowned__',
    `UserId` VARCHAR(128) NOT NULL DEFAULT '__legacy_unowned__',
    CONSTRAINT `pk_ag_unified_entry_run` PRIMARY KEY (`Id`),
    CONSTRAINT `ck_ag_unified_entry_run_revision` CHECK (`PersistenceRevision` >= 0),
    CONSTRAINT `fk_ag_unified_entry_run_conversation` FOREIGN KEY (`ConversationId`)
        REFERENCES `AgChatConversation` (`Id`),
    INDEX `ix_ag_unified_entry_run_conversation_started`
        (`ConversationId`, `StartedAtUtc` DESC, `Id`),
    INDEX `ix_ag_unified_entry_run_owner_started`
        (`TenantId`, `UserId`, `StartedAtUtc` DESC, `Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgUnifiedAgentRun` (
    `Id` CHAR(36) NOT NULL,
    `EntryRunId` CHAR(36) NOT NULL,
    `Ordinal` BIGINT NOT NULL,
    `ParentRunId` CHAR(36) NULL,
    `Kind` VARCHAR(64) NOT NULL,
    `AgentId` CHAR(36) NOT NULL,
    `AgentVersionId` CHAR(36) NOT NULL,
    `Depth` INT NOT NULL,
    `Status` VARCHAR(32) NOT NULL,
    `StartedAtUtc` VARCHAR(64) NOT NULL,
    `FinishedAtUtc` VARCHAR(64) NULL,
    `DurationTicks` BIGINT NULL,
    `InputText` LONGTEXT NOT NULL,
    `InputSha256` CHAR(64) NOT NULL,
    `OutputText` LONGTEXT NOT NULL,
    `OutputSha256` CHAR(64) NOT NULL,
    `ErrorCode` VARCHAR(128) NOT NULL,
    CONSTRAINT `pk_ag_unified_agent_run` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_unified_agent_run_entry_ordinal` UNIQUE (`EntryRunId`, `Ordinal`),
    CONSTRAINT `ck_ag_unified_agent_run_ordinal` CHECK (`Ordinal` >= 0),
    CONSTRAINT `ck_ag_unified_agent_run_depth` CHECK (`Depth` >= 0),
    CONSTRAINT `fk_ag_unified_agent_run_entry` FOREIGN KEY (`EntryRunId`)
        REFERENCES `AgUnifiedEntryRun` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgUnifiedOrchestrationLink` (
    `Id` CHAR(36) NOT NULL,
    `EntryRunId` CHAR(36) NOT NULL,
    `Ordinal` BIGINT NOT NULL,
    `ParentRunId` CHAR(36) NOT NULL,
    `OrchestrationRunId` CHAR(36) NOT NULL,
    `OrchestrationVersionId` CHAR(36) NOT NULL,
    `Depth` INT NOT NULL,
    `Status` VARCHAR(32) NOT NULL,
    `StartedAtUtc` VARCHAR(64) NOT NULL,
    `FinishedAtUtc` VARCHAR(64) NULL,
    `DurationTicks` BIGINT NULL,
    `InputText` LONGTEXT NOT NULL,
    `InputSha256` CHAR(64) NOT NULL,
    `OutputText` LONGTEXT NOT NULL,
    `OutputSha256` CHAR(64) NOT NULL,
    `ErrorCode` VARCHAR(128) NOT NULL,
    CONSTRAINT `pk_ag_unified_orchestration_link` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_unified_orchestration_link_entry_ordinal` UNIQUE (`EntryRunId`, `Ordinal`),
    CONSTRAINT `ck_ag_unified_orchestration_link_ordinal` CHECK (`Ordinal` >= 0),
    CONSTRAINT `ck_ag_unified_orchestration_link_depth` CHECK (`Depth` >= 0),
    CONSTRAINT `fk_ag_unified_orchestration_link_entry` FOREIGN KEY (`EntryRunId`)
        REFERENCES `AgUnifiedEntryRun` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgUnifiedToolCall` (
    `Id` CHAR(36) NOT NULL,
    `EntryRunId` CHAR(36) NOT NULL,
    `Ordinal` BIGINT NOT NULL,
    `ParentRunId` CHAR(36) NOT NULL,
    `ToolVersionId` CHAR(36) NOT NULL,
    `Depth` INT NOT NULL,
    `Status` VARCHAR(32) NOT NULL,
    `StartedAtUtc` VARCHAR(64) NOT NULL,
    `FinishedAtUtc` VARCHAR(64) NULL,
    `DurationTicks` BIGINT NULL,
    `ArgumentsJson` LONGTEXT NOT NULL,
    `ArgumentsSha256` CHAR(64) NOT NULL,
    `ResultContent` LONGTEXT NOT NULL,
    `ResultSha256` CHAR(64) NOT NULL,
    `ErrorCode` VARCHAR(128) NOT NULL,
    CONSTRAINT `pk_ag_unified_tool_call` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_unified_tool_call_entry_ordinal` UNIQUE (`EntryRunId`, `Ordinal`),
    CONSTRAINT `ck_ag_unified_tool_call_ordinal` CHECK (`Ordinal` >= 0),
    CONSTRAINT `ck_ag_unified_tool_call_depth` CHECK (`Depth` >= 0),
    CONSTRAINT `fk_ag_unified_tool_call_entry` FOREIGN KEY (`EntryRunId`)
        REFERENCES `AgUnifiedEntryRun` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgUnifiedRunEvent` (
    `Id` CHAR(36) NOT NULL,
    `EntryRunId` CHAR(36) NOT NULL,
    `Sequence` BIGINT NOT NULL,
    `CorrelationId` CHAR(36) NOT NULL,
    `Kind` VARCHAR(64) NOT NULL,
    `OccurredAtUtc` VARCHAR(64) NOT NULL,
    `ParentRunId` CHAR(36) NULL,
    `Depth` INT NOT NULL,
    `PayloadJson` LONGTEXT NOT NULL,
    `PayloadSha256` CHAR(64) NOT NULL,
    CONSTRAINT `pk_ag_unified_run_event` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_unified_run_event_entry_sequence` UNIQUE (`EntryRunId`, `Sequence`),
    CONSTRAINT `ck_ag_unified_run_event_sequence` CHECK (`Sequence` > 0),
    CONSTRAINT `ck_ag_unified_run_event_depth` CHECK (`Depth` >= 0),
    CONSTRAINT `fk_ag_unified_run_event_entry` FOREIGN KEY (`EntryRunId`)
        REFERENCES `AgUnifiedEntryRun` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgMainAgentAssignment` (
    `AssignmentKey` VARCHAR(64) NOT NULL,
    `AgentId` CHAR(36) NOT NULL,
    `AgentVersionId` CHAR(36) NOT NULL,
    `LogicalRevision` BIGINT NOT NULL,
    `UpdatedAtUtc` VARCHAR(64) NOT NULL,
    CONSTRAINT `pk_ag_main_agent_assignment` PRIMARY KEY (`AssignmentKey`),
    CONSTRAINT `ck_ag_main_agent_assignment_key`
        CHECK (`AssignmentKey` = 'platform-main-agent'),
    CONSTRAINT `ck_ag_main_agent_assignment_revision` CHECK (`LogicalRevision` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgToolApprovalRequest` (
    `Id` CHAR(36) NOT NULL,
    `TenantId` VARCHAR(128) NOT NULL,
    `RequesterUserId` VARCHAR(128) NOT NULL,
    `ConversationId` CHAR(36) NOT NULL,
    `EntryRunId` CHAR(36) NOT NULL,
    `AgentRunId` CHAR(36) NOT NULL,
    `AgentVersionId` CHAR(36) NOT NULL,
    `McpServerId` CHAR(36) NOT NULL,
    `ToolVersionId` CHAR(36) NOT NULL,
    `ToolName` VARCHAR(256) NOT NULL,
    `Risk` INT NOT NULL,
    `ToolSchemaSha256` CHAR(64) NOT NULL,
    `ArgumentsSha256` CHAR(64) NOT NULL,
    `SafeArgumentsSummaryJson` JSON NOT NULL,
    `Status` INT NOT NULL,
    `LogicalRevision` BIGINT NOT NULL,
    `RequestedAtUtc` VARCHAR(64) NOT NULL,
    `ExpiresAtUtc` VARCHAR(64) NOT NULL,
    `DecisionUserId` VARCHAR(128) NOT NULL,
    `DecisionReason` LONGTEXT NOT NULL,
    `DecidedAtUtc` VARCHAR(64) NULL,
    `ClaimedAtUtc` VARCHAR(64) NULL,
    `FinishedAtUtc` VARCHAR(64) NULL,
    `ErrorCode` VARCHAR(128) NOT NULL,
    CONSTRAINT `pk_ag_tool_approval_request` PRIMARY KEY (`Id`),
    CONSTRAINT `ck_ag_tool_approval_request_revision` CHECK (`LogicalRevision` >= 0),
    INDEX `ix_ag_tool_approval_request_tenant_status_requested`
        (`TenantId`, `Status`, `RequestedAtUtc` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgToolApprovalPayload` (
    `ApprovalId` CHAR(36) NOT NULL,
    `ProtectedPayload` LONGTEXT NOT NULL,
    `ProtectedPayloadSha256` CHAR(64) NOT NULL,
    CONSTRAINT `pk_ag_tool_approval_payload` PRIMARY KEY (`ApprovalId`),
    CONSTRAINT `fk_ag_tool_approval_payload_request` FOREIGN KEY (`ApprovalId`)
        REFERENCES `AgToolApprovalRequest` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgToolApprovalDecision` (
    `Id` CHAR(36) NOT NULL,
    `ApprovalId` CHAR(36) NOT NULL,
    `TenantId` VARCHAR(128) NOT NULL,
    `FromStatus` INT NOT NULL,
    `ToStatus` INT NOT NULL,
    `DecisionUserId` VARCHAR(128) NOT NULL,
    `DecisionReason` LONGTEXT NOT NULL,
    `DecidedAtUtc` VARCHAR(64) NOT NULL,
    `ResultingLogicalRevision` BIGINT NOT NULL,
    CONSTRAINT `pk_ag_tool_approval_decision` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_tool_approval_decision_revision`
        UNIQUE (`ApprovalId`, `ResultingLogicalRevision`),
    CONSTRAINT `fk_ag_tool_approval_decision_request` FOREIGN KEY (`ApprovalId`)
        REFERENCES `AgToolApprovalRequest` (`Id`) ON DELETE CASCADE,
    INDEX `ix_ag_tool_approval_decision_tenant_approval`
        (`TenantId`, `ApprovalId`, `ResultingLogicalRevision`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgToolApprovalExecutionResult` (
    `ApprovalId` CHAR(36) NOT NULL,
    `TenantId` VARCHAR(128) NOT NULL,
    `Succeeded` TINYINT(1) NOT NULL,
    `Blocked` TINYINT(1) NOT NULL,
    `ProtectedContent` LONGTEXT NOT NULL,
    `ProtectedContentSha256` CHAR(64) NOT NULL,
    `ContentSha256` CHAR(64) NOT NULL,
    `ErrorCode` VARCHAR(128) NOT NULL,
    `FinishedAtUtc` VARCHAR(64) NOT NULL,
    CONSTRAINT `pk_ag_tool_approval_execution_result` PRIMARY KEY (`ApprovalId`),
    CONSTRAINT `ck_ag_tool_approval_execution_result_succeeded` CHECK (`Succeeded` IN (0, 1)),
    CONSTRAINT `ck_ag_tool_approval_execution_result_blocked` CHECK (`Blocked` IN (0, 1)),
    CONSTRAINT `fk_ag_tool_approval_execution_result_request` FOREIGN KEY (`ApprovalId`)
        REFERENCES `AgToolApprovalRequest` (`Id`) ON DELETE CASCADE,
    INDEX `ix_ag_tool_approval_execution_result_tenant` (`TenantId`, `ApprovalId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgEvaluationSuite` (
    `Id` CHAR(36) NOT NULL,
    `TenantId` VARCHAR(128) NOT NULL,
    `Code` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    `LogicalRevision` BIGINT NOT NULL,
    `DocumentJson` JSON NOT NULL,
    CONSTRAINT `pk_ag_evaluation_suite` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_evaluation_suite_tenant_code` UNIQUE (`TenantId`, `Code`),
    CONSTRAINT `ck_ag_evaluation_suite_revision` CHECK (`LogicalRevision` >= 0),
    INDEX `ix_ag_evaluation_suite_tenant_code` (`TenantId`, `Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgEvaluationBatch` (
    `Id` CHAR(36) NOT NULL,
    `TenantId` VARCHAR(128) NOT NULL,
    `SuiteId` CHAR(36) NOT NULL,
    `SuiteVersionId` CHAR(36) NOT NULL,
    `Status` VARCHAR(32) NOT NULL,
    `LogicalRevision` BIGINT NOT NULL,
    `StartedAtUtc` VARCHAR(64) NOT NULL,
    `DocumentJson` JSON NOT NULL,
    CONSTRAINT `pk_ag_evaluation_batch` PRIMARY KEY (`Id`),
    CONSTRAINT `ck_ag_evaluation_batch_revision` CHECK (`LogicalRevision` >= 0),
    INDEX `ix_ag_evaluation_batch_suite_started`
        (`TenantId`, `SuiteId`, `StartedAtUtc` DESC),
    INDEX `ix_ag_evaluation_batch_status` (`Status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgEvaluationModelJudgement` (
    `Id` CHAR(36) NOT NULL,
    `TenantId` VARCHAR(128) NOT NULL,
    `BatchId` CHAR(36) NOT NULL,
    `ConfigurationSha256` CHAR(64) NOT NULL,
    `StartedAtUtc` VARCHAR(64) NOT NULL,
    `DocumentJson` JSON NOT NULL,
    CONSTRAINT `pk_ag_evaluation_model_judgement` PRIMARY KEY (`Id`),
    CONSTRAINT `ux_ag_evaluation_model_judgement_configuration`
        UNIQUE (`TenantId`, `BatchId`, `ConfigurationSha256`),
    INDEX `ix_ag_evaluation_model_judgement_batch_started`
        (`TenantId`, `BatchId`, `StartedAtUtc` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgApiIdempotency` (
    `ScopeSha256` CHAR(64) NOT NULL,
    `RequestSha256` CHAR(64) NOT NULL,
    `Status` VARCHAR(32) NOT NULL,
    `ResponseStatusCode` INT NOT NULL,
    `ResponseContentType` VARCHAR(256) NOT NULL,
    `ResponseLocation` VARCHAR(2048) NOT NULL,
    `ResponseBody` LONGBLOB NOT NULL,
    `CreatedAtUtc` VARCHAR(64) NOT NULL,
    `ExpiresAtUtc` VARCHAR(64) NOT NULL,
    CONSTRAINT `pk_ag_api_idempotency` PRIMARY KEY (`ScopeSha256`),
    INDEX `ix_ag_api_idempotency_expires` (`ExpiresAtUtc`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;
