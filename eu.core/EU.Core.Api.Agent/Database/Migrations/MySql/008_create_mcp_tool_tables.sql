-- Create normalized MCP argument and immutable tool-version tables. MySQL 8.0.13+.

SET NAMES utf8mb4;
CREATE TABLE IF NOT EXISTS `AgMcpServerArgument` (
    `ID` CHAR(36) NOT NULL, `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `IsActive` TINYINT(1) NULL DEFAULT 1, `ImportDataId` CHAR(36) NULL,
    `ModificationNum` INT NULL DEFAULT 0, `Tag` INT NULL DEFAULT 1,
    `GroupId` CHAR(36) NULL, `CompanyId` CHAR(36) NULL,
    `AuditStatus` VARCHAR(32) NULL DEFAULT 'Add', `CurrentNode` VARCHAR(32) NULL,
    `CreatedBy` CHAR(36) NULL, `CreatedTime` DATETIME NULL,
    `UpdateBy` CHAR(36) NULL, `UpdateTime` DATETIME NULL,
    `ServerId` CHAR(36) NOT NULL, `Ordinal` INT NOT NULL, `Value` VARCHAR(1024) NOT NULL,
    PRIMARY KEY (`ID`),
    CONSTRAINT `fk_ag_mcp_argument_server` FOREIGN KEY (`ServerId`) REFERENCES `AgMcpServerDefinition` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `ux_ag_mcp_argument_order` UNIQUE (`ServerId`, `Ordinal`),
    CONSTRAINT `ck_ag_mcp_argument_ordinal` CHECK (`Ordinal` >= 0),
    INDEX `index_AgMcpServerArgument_IsDeleted` (`IsDeleted`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgMcpToolVersion` (
    `ID` CHAR(36) NOT NULL, `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `IsActive` TINYINT(1) NULL DEFAULT 1, `ImportDataId` CHAR(36) NULL,
    `ModificationNum` INT NULL DEFAULT 0, `Tag` INT NULL DEFAULT 1,
    `GroupId` CHAR(36) NULL, `CompanyId` CHAR(36) NULL,
    `AuditStatus` VARCHAR(32) NULL DEFAULT 'Add', `CurrentNode` VARCHAR(32) NULL,
    `CreatedBy` CHAR(36) NULL, `CreatedTime` DATETIME NULL,
    `UpdateBy` CHAR(36) NULL, `UpdateTime` DATETIME NULL,
    `ServerId` CHAR(36) NOT NULL, `HistoryOrdinal` INT NOT NULL, `CurrentOrdinal` INT NULL,
    `Name` VARCHAR(256) NOT NULL, `Description` VARCHAR(4096) NOT NULL,
    `InputSchemaJson` LONGTEXT NOT NULL, `Risk` VARCHAR(32) NOT NULL,
    `Sha256` CHAR(64) NOT NULL, `DiscoveredAtUtc` DATETIME(6) NOT NULL,
    PRIMARY KEY (`ID`),
    CONSTRAINT `fk_ag_mcp_tool_server` FOREIGN KEY (`ServerId`) REFERENCES `AgMcpServerDefinition` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `ux_ag_mcp_tool_history_order` UNIQUE (`ServerId`, `HistoryOrdinal`),
    CONSTRAINT `ux_ag_mcp_tool_current_order` UNIQUE (`ServerId`, `CurrentOrdinal`),
    CONSTRAINT `ck_ag_mcp_tool_history_ordinal` CHECK (`HistoryOrdinal` >= 0),
    CONSTRAINT `ck_ag_mcp_tool_current_ordinal` CHECK (`CurrentOrdinal` IS NULL OR `CurrentOrdinal` >= 0),
    CONSTRAINT `ck_ag_mcp_tool_risk` CHECK (`Risk` IN ('Unknown', 'ReadOnly', 'Mutating', 'HighRisk')),
    CONSTRAINT `ck_ag_mcp_tool_sha256` CHECK (CHAR_LENGTH(`Sha256`) = 64),
    INDEX `ix_ag_mcp_tool_server` (`ServerId`, `HistoryOrdinal`),
    INDEX `index_AgMcpToolVersion_IsDeleted` (`IsDeleted`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;
