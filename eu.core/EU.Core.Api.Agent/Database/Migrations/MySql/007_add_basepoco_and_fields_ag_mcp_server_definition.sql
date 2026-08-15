-- Prepare AgMcpServerDefinition for EU.Core BasePoco and normalized MCP fields.
-- Existing DocumentJson is retained until 009. MySQL 8.0.13+.

SET NAMES utf8mb4;
DROP PROCEDURE IF EXISTS `AgAddMcpServerColumn`;
DROP PROCEDURE IF EXISTS `AgAddMcpServerIndex`;
DELIMITER $$
CREATE PROCEDURE `AgAddMcpServerColumn`(IN p_name VARCHAR(64), IN p_definition VARCHAR(1024))
BEGIN
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'AgMcpServerDefinition') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'AgMcpServerDefinition does not exist; run 001 first';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'AgMcpServerDefinition' AND COLUMN_NAME = p_name) THEN
        SET @ddl = CONCAT('ALTER TABLE `AgMcpServerDefinition` ADD COLUMN `', REPLACE(p_name, '`', '``'), '` ', p_definition);
        PREPARE statement FROM @ddl; EXECUTE statement; DEALLOCATE PREPARE statement;
    END IF;
END$$
CREATE PROCEDURE `AgAddMcpServerIndex`(IN p_name VARCHAR(64), IN p_columns VARCHAR(256), IN p_unique TINYINT)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'AgMcpServerDefinition' AND INDEX_NAME = p_name) THEN
        SET @ddl = CONCAT('CREATE ', IF(p_unique = 1, 'UNIQUE ', ''), 'INDEX `', REPLACE(p_name, '`', '``'), '` ON `AgMcpServerDefinition` (', p_columns, ')');
        PREPARE statement FROM @ddl; EXECUTE statement; DEALLOCATE PREPARE statement;
    END IF;
END$$
DELIMITER ;

CALL `AgAddMcpServerColumn`('IsDeleted', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL `AgAddMcpServerColumn`('IsActive', 'TINYINT(1) NULL DEFAULT 1');
CALL `AgAddMcpServerColumn`('ImportDataId', 'CHAR(36) NULL');
CALL `AgAddMcpServerColumn`('ModificationNum', 'INT NULL DEFAULT 0');
CALL `AgAddMcpServerColumn`('Tag', 'INT NULL DEFAULT 1');
CALL `AgAddMcpServerColumn`('GroupId', 'CHAR(36) NULL');
CALL `AgAddMcpServerColumn`('CompanyId', 'CHAR(36) NULL');
CALL `AgAddMcpServerColumn`('AuditStatus', 'VARCHAR(32) NULL DEFAULT ''Add''');
CALL `AgAddMcpServerColumn`('CurrentNode', 'VARCHAR(32) NULL');
CALL `AgAddMcpServerColumn`('CreatedBy', 'CHAR(36) NULL');
CALL `AgAddMcpServerColumn`('CreatedTime', 'DATETIME NULL');
CALL `AgAddMcpServerColumn`('UpdateBy', 'CHAR(36) NULL');
CALL `AgAddMcpServerColumn`('UpdateTime', 'DATETIME NULL');
CALL `AgAddMcpServerColumn`('Name', 'VARCHAR(256) NULL');
CALL `AgAddMcpServerColumn`('Description', 'LONGTEXT NULL');
CALL `AgAddMcpServerColumn`('Transport', 'VARCHAR(32) NULL');
CALL `AgAddMcpServerColumn`('Endpoint', 'VARCHAR(2048) NULL');
CALL `AgAddMcpServerColumn`('Command', 'VARCHAR(512) NULL');
CALL `AgAddMcpServerColumn`('CredentialAlias', 'VARCHAR(200) NULL');
CALL `AgAddMcpServerColumn`('Enabled', 'TINYINT(1) NULL');
CALL `AgAddMcpServerColumn`('Status', 'VARCHAR(32) NULL');
CALL `AgAddMcpServerColumn`('LastError', 'VARCHAR(4096) NULL');
CALL `AgAddMcpServerColumn`('LastSyncedAtUtc', 'DATETIME(6) NULL');
DROP PROCEDURE `AgAddMcpServerColumn`;
CALL `AgAddMcpServerIndex`('index_AgMcpServerDefinition_IsDeleted', '`IsDeleted`', 0);
CALL `AgAddMcpServerIndex`('ux_ag_mcp_server_definition_code', '`Code`', 1);
DROP PROCEDURE `AgAddMcpServerIndex`;
