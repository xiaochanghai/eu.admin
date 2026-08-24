SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `AgAgentTask` (
    `ID` char(36) NOT NULL,
    `TenantId` varchar(128) NOT NULL,
    `UserId` varchar(256) NOT NULL,
    `Title` varchar(256) NOT NULL,
    `Description` longtext NULL,
    `Input` longtext NOT NULL,
    `InputSha256` varchar(64) NOT NULL,
    `SourceType` varchar(64) NULL,
    `SourceId` varchar(256) NULL,
    `IdempotencyKey` varchar(128) NULL,
    `ConversationId` char(36) NULL,
    `CurrentRunId` char(36) NULL,
    `Status` int NOT NULL DEFAULT 0,
    `Priority` int NOT NULL DEFAULT 0,
    `AttemptCount` int NOT NULL DEFAULT 0,
    `MaximumAttempts` int NOT NULL DEFAULT 3,
    `LogicalRevision` bigint NOT NULL DEFAULT 0,
    `AvailableAtUtc` datetime(6) NOT NULL,
    `StartedAtUtc` datetime(6) NULL,
    `FinishedAtUtc` datetime(6) NULL,
    `LeaseOwner` varchar(128) NULL,
    `LeaseExpiresAtUtc` datetime(6) NULL,
    `CheckpointKind` varchar(64) NULL,
    `CheckpointJson` longtext NULL,
    `LastErrorCode` varchar(128) NULL,
    `LastErrorMessage` longtext NULL,
    `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
    `ActiveIdempotencyKey` varchar(128) GENERATED ALWAYS AS (
        CASE
            WHEN `IsDeleted` = 0 AND `IdempotencyKey` IS NOT NULL AND `IdempotencyKey` <> ''
                THEN `IdempotencyKey`
            ELSE NULL
        END
    ) STORED,
    `IsActive` tinyint(1) NULL DEFAULT 1,
    `ImportDataId` char(36) NULL, `ModificationNum` int NULL DEFAULT 0, `Tag` int NULL DEFAULT 1,
    `GroupId` char(36) NULL, `CompanyId` char(36) NULL, `AuditStatus` varchar(32) NULL DEFAULT 'Add',
    `CurrentNode` varchar(32) NULL, `CreatedBy` char(36) NULL, `CreatedTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdateBy` char(36) NULL, `UpdateTime` datetime NULL,
    PRIMARY KEY (`ID`),
    KEY `ix_ag_agent_task_claim` (`TenantId`, `Status`, `AvailableAtUtc`, `Priority`),
    KEY `ix_ag_agent_task_user` (`TenantId`, `UserId`, `CreatedTime`),
    UNIQUE KEY `ux_ag_agent_task_idempotency` (`TenantId`, `UserId`, `ActiveIdempotencyKey`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `AgAgentTaskAttempt` (
    `ID` char(36) NOT NULL,
    `TaskId` char(36) NOT NULL,
    `AttemptNumber` int NOT NULL,
    `RunId` char(36) NULL,
    `Status` int NOT NULL,
    `WorkerId` varchar(128) NOT NULL,
    `StartedAtUtc` datetime(6) NOT NULL,
    `FinishedAtUtc` datetime(6) NULL,
    `ErrorCode` varchar(128) NULL,
    `ErrorMessage` longtext NULL,
    `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
    `ActiveAttemptNumber` int GENERATED ALWAYS AS (
        CASE WHEN `IsDeleted` = 0 THEN `AttemptNumber` ELSE NULL END
    ) STORED,
    `IsActive` tinyint(1) NULL DEFAULT 1,
    `ImportDataId` char(36) NULL, `ModificationNum` int NULL DEFAULT 0, `Tag` int NULL DEFAULT 1,
    `GroupId` char(36) NULL, `CompanyId` char(36) NULL, `AuditStatus` varchar(32) NULL DEFAULT 'Add',
    `CurrentNode` varchar(32) NULL, `CreatedBy` char(36) NULL, `CreatedTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdateBy` char(36) NULL, `UpdateTime` datetime NULL,
    PRIMARY KEY (`ID`),
    UNIQUE KEY `ux_ag_agent_task_attempt` (`TaskId`, `ActiveAttemptNumber`),
    CONSTRAINT `fk_ag_agent_task_attempt_task` FOREIGN KEY (`TaskId`) REFERENCES `AgAgentTask` (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
CREATE TABLE IF NOT EXISTS `AgAgentTaskEvent`
(
    `ID` char(36) NOT NULL,
    `TaskId` char(36) NOT NULL,
    `AttemptNumber` int NULL,
    `RunId` char(36) NULL,
    `Kind` varchar(64) NOT NULL,
    `Status` int NOT NULL,
    `WorkerId` varchar(128) NULL,
    `OccurredAtUtc` datetime(6) NOT NULL,
    `PayloadJson` longtext NULL,
    `IsDeleted` tinyint(1) NOT NULL DEFAULT 0,
    `IsActive` tinyint(1) NULL DEFAULT 1,
    `ImportDataId` char(36) NULL,
    `ModificationNum` int NULL DEFAULT 0,
    `Tag` int NULL DEFAULT 1,
    `GroupId` char(36) NULL,
    `CompanyId` char(36) NULL,
    `AuditStatus` varchar(32) NULL DEFAULT 'Add',
    `CurrentNode` varchar(32) NULL,
    `CreatedBy` char(36) NULL,
    `CreatedTime` datetime NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdateBy` char(36) NULL,
    `UpdateTime` datetime NULL,
    PRIMARY KEY (`ID`),
    KEY `ix_ag_agent_task_event_time` (`TaskId`, `OccurredAtUtc`, `CreatedTime`),
    CONSTRAINT `fk_ag_agent_task_event_task` FOREIGN KEY (`TaskId`) REFERENCES `AgAgentTask` (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
