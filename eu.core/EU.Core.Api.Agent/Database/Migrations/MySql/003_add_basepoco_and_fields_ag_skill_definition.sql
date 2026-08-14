-- Add EU.Core BasePoco columns and normalized Skill definition fields.
-- Existing DocumentJson is retained until 005 completes the data cutover.
-- MySQL 8.0.13+

SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS `AgAddSkillDefinitionColumn`;
DROP PROCEDURE IF EXISTS `AgAddSkillDefinitionIndex`;

DELIMITER $$
CREATE PROCEDURE `AgAddSkillDefinitionColumn`(
    IN p_column_name VARCHAR(64),
    IN p_column_definition VARCHAR(512)
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'AgSkillDefinition'
          AND TABLE_TYPE = 'BASE TABLE'
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'AgSkillDefinition does not exist; run 001_initial_schema.sql first';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'AgSkillDefinition'
          AND COLUMN_NAME = p_column_name
    ) THEN
        SET @ddl = CONCAT(
            'ALTER TABLE `AgSkillDefinition` ADD COLUMN `',
            REPLACE(p_column_name, '`', '``'), '` ', p_column_definition
        );
        PREPARE statement FROM @ddl;
        EXECUTE statement;
        DEALLOCATE PREPARE statement;
    END IF;
END$$

CREATE PROCEDURE `AgAddSkillDefinitionIndex`(
    IN p_index_name VARCHAR(64),
    IN p_column_name VARCHAR(64)
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'AgSkillDefinition'
          AND INDEX_NAME = p_index_name
    ) THEN
        SET @ddl = CONCAT(
            'CREATE INDEX `', REPLACE(p_index_name, '`', '``'),
            '` ON `AgSkillDefinition` (`', REPLACE(p_column_name, '`', '``'), '`)'
        );
        PREPARE statement FROM @ddl;
        EXECUTE statement;
        DEALLOCATE PREPARE statement;
    END IF;
END$$
DELIMITER ;

CALL `AgAddSkillDefinitionColumn`('IsDeleted', 'TINYINT(1) NOT NULL DEFAULT 0');
CALL `AgAddSkillDefinitionColumn`('IsActive', 'TINYINT(1) NULL DEFAULT 1');
CALL `AgAddSkillDefinitionColumn`('ImportDataId', 'CHAR(36) NULL');
CALL `AgAddSkillDefinitionColumn`('ModificationNum', 'INT NULL DEFAULT 0');
CALL `AgAddSkillDefinitionColumn`('Tag', 'INT NULL DEFAULT 1');
CALL `AgAddSkillDefinitionColumn`('GroupId', 'CHAR(36) NULL');
CALL `AgAddSkillDefinitionColumn`('CompanyId', 'CHAR(36) NULL');
CALL `AgAddSkillDefinitionColumn`('AuditStatus', 'VARCHAR(32) NULL DEFAULT ''Add''');
CALL `AgAddSkillDefinitionColumn`('CurrentNode', 'VARCHAR(32) NULL');
CALL `AgAddSkillDefinitionColumn`('CreatedBy', 'CHAR(36) NULL');
CALL `AgAddSkillDefinitionColumn`('CreatedTime', 'DATETIME NULL');
CALL `AgAddSkillDefinitionColumn`('UpdateBy', 'CHAR(36) NULL');
CALL `AgAddSkillDefinitionColumn`('UpdateTime', 'DATETIME NULL');
CALL `AgAddSkillDefinitionColumn`('Name', 'VARCHAR(256) NULL');
CALL `AgAddSkillDefinitionColumn`('Description', 'LONGTEXT NULL');
CALL `AgAddSkillDefinitionColumn`('Category', 'VARCHAR(128) NULL');
CALL `AgAddSkillDefinitionColumn`('Status', 'VARCHAR(32) NULL');

CALL `AgAddSkillDefinitionIndex`('index_AgSkillDefinition_Enabled', 'IsActive');
CALL `AgAddSkillDefinitionIndex`('index_AgSkillDefinition_IsDeleted', 'IsDeleted');

DROP PROCEDURE `AgAddSkillDefinitionIndex`;
DROP PROCEDURE `AgAddSkillDefinitionColumn`;

SELECT COUNT(*) AS MissingSkillDefinitionColumns
FROM (
    SELECT 'ID' AS ColumnName UNION ALL SELECT 'IsDeleted'
    UNION ALL SELECT 'IsActive' UNION ALL SELECT 'ImportDataId'
    UNION ALL SELECT 'ModificationNum' UNION ALL SELECT 'Tag'
    UNION ALL SELECT 'GroupId' UNION ALL SELECT 'CompanyId'
    UNION ALL SELECT 'AuditStatus' UNION ALL SELECT 'CurrentNode'
    UNION ALL SELECT 'CreatedBy' UNION ALL SELECT 'CreatedTime'
    UNION ALL SELECT 'UpdateBy' UNION ALL SELECT 'UpdateTime'
    UNION ALL SELECT 'Code' UNION ALL SELECT 'DraftRevision'
    UNION ALL SELECT 'Name' UNION ALL SELECT 'Description'
    UNION ALL SELECT 'Category' UNION ALL SELECT 'Status'
) AS required
WHERE NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS AS columns
    WHERE columns.TABLE_SCHEMA = DATABASE()
      AND columns.TABLE_NAME = 'AgSkillDefinition'
      AND UPPER(columns.COLUMN_NAME) = UPPER(required.ColumnName)
);
