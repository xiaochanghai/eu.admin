-- BasePoco compatibility pilot for AgAgentDefinition only.
-- Adds non-key common columns without changing Id CHAR(36), its primary key,
-- existing definition data, or historical audit timestamps.
-- MySQL 8.0.13+

SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS `AgAddAgentDefinitionColumn`;

DELIMITER $$
CREATE PROCEDURE `AgAddAgentDefinitionColumn`(
    IN p_column_name VARCHAR(64),
    IN p_column_definition VARCHAR(512)
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'AgAgentDefinition'
          AND TABLE_TYPE = 'BASE TABLE'
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'AgAgentDefinition does not exist; run 001_initial_schema.sql first';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'AgAgentDefinition'
          AND COLUMN_NAME = p_column_name
    ) THEN
        SET @ddl = CONCAT(
            'ALTER TABLE `AgAgentDefinition` ADD COLUMN `',
            REPLACE(p_column_name, '`', '``'), '` ', p_column_definition
        );
        PREPARE statement FROM @ddl;
        EXECUTE statement;
        DEALLOCATE PREPARE statement;
    END IF;
END$$
DELIMITER ;

CALL `AgAddAgentDefinitionColumn`('IsDeleted', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL `AgAddAgentDefinitionColumn`('IsActive', 'TINYINT(1) NULL DEFAULT 1');
CALL `AgAddAgentDefinitionColumn`('ImportDataId', 'CHAR(36) NULL');
CALL `AgAddAgentDefinitionColumn`('ModificationNum', 'INT NULL DEFAULT 0');
CALL `AgAddAgentDefinitionColumn`('Tag', 'INT NULL DEFAULT 1');
CALL `AgAddAgentDefinitionColumn`('GroupId', 'CHAR(36) NULL');
CALL `AgAddAgentDefinitionColumn`('CompanyId', 'CHAR(36) NULL');
CALL `AgAddAgentDefinitionColumn`('AuditStatus', 'VARCHAR(32) NULL DEFAULT ''Add''');
CALL `AgAddAgentDefinitionColumn`('CurrentNode', 'VARCHAR(32) NULL');
CALL `AgAddAgentDefinitionColumn`('CreatedBy', 'CHAR(36) NULL');
CALL `AgAddAgentDefinitionColumn`('CreatedTime', 'DATETIME NULL');
CALL `AgAddAgentDefinitionColumn`('UpdateBy', 'CHAR(36) NULL');
CALL `AgAddAgentDefinitionColumn`('UpdateTime', 'DATETIME NULL');

DROP PROCEDURE `AgAddAgentDefinitionColumn`;

SELECT COUNT(*) AS MissingBasePocoColumns
FROM (
    SELECT 'IsDeleted' AS ColumnName UNION ALL SELECT 'IsActive'
    UNION ALL SELECT 'ImportDataId' UNION ALL SELECT 'ModificationNum'
    UNION ALL SELECT 'Tag' UNION ALL SELECT 'GroupId' UNION ALL SELECT 'CompanyId'
    UNION ALL SELECT 'AuditStatus' UNION ALL SELECT 'CurrentNode'
    UNION ALL SELECT 'CreatedBy' UNION ALL SELECT 'CreatedTime'
    UNION ALL SELECT 'UpdateBy' UNION ALL SELECT 'UpdateTime'
) AS required
WHERE NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS AS columns
    WHERE columns.TABLE_SCHEMA = DATABASE()
      AND columns.TABLE_NAME = 'AgAgentDefinition'
      AND columns.COLUMN_NAME = required.ColumnName
);
