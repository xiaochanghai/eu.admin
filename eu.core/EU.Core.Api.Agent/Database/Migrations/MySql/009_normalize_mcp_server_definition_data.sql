-- Expand existing MCP aggregate documents and finalize relational storage.
-- MySQL 8.0.13+. Run 007 and 008 first, stop writes, and back up the database.

SET NAMES utf8mb4;
DROP PROCEDURE IF EXISTS `AgNormalizeMcpServerDefinitions`;

DELIMITER $$
CREATE PROCEDURE `AgNormalizeMcpServerDefinitions`()
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'AgMcpServerDefinition'
          AND COLUMN_NAME = 'DocumentJson'
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'DocumentJson is absent; the MCP cutover was already finalized';
    END IF;

    START TRANSACTION;

    UPDATE `AgMcpServerDefinition`
    SET `Name` = COALESCE(JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.name')), `Code`),
        `Description` = COALESCE(JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.description')), ''),
        `Transport` = JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.transport')),
        `Endpoint` = COALESCE(JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.endpoint')), ''),
        `Command` = COALESCE(JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.command')), ''),
        `CredentialAlias` = COALESCE(JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.credentialAlias')), ''),
        `Enabled` = CASE JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.enabled'))
            WHEN 'true' THEN 1 ELSE 0 END,
        `Status` = JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.status')),
        `LastError` = COALESCE(JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.lastError')), ''),
        `LastSyncedAtUtc` = CASE
            WHEN JSON_TYPE(JSON_EXTRACT(`DocumentJson`, '$.lastSyncedAtUtc')) = 'NULL' THEN NULL
            ELSE COALESCE(
                STR_TO_DATE(
                    REPLACE(REPLACE(REPLACE(
                        JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.lastSyncedAtUtc')),
                        'T', ' '), '+00:00', ''), 'Z', ''),
                    '%Y-%m-%d %H:%i:%s.%f'),
                STR_TO_DATE(
                    REPLACE(REPLACE(REPLACE(
                        JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.lastSyncedAtUtc')),
                        'T', ' '), '+00:00', ''), 'Z', ''),
                    '%Y-%m-%d %H:%i:%s'))
        END;

    INSERT INTO `AgMcpServerArgument` (`ID`, `ServerId`, `Ordinal`, `Value`)
    SELECT UUID(), definition.`Id`, arguments.`ArgumentOrdinal` - 1, arguments.`ArgumentValue`
    FROM `AgMcpServerDefinition` AS definition
    CROSS JOIN JSON_TABLE(
        definition.`DocumentJson`, '$.arguments[*]'
        COLUMNS (
            `ArgumentOrdinal` FOR ORDINALITY,
            `ArgumentValue` VARCHAR(1024) PATH '$'
        )
    ) AS arguments
    ON DUPLICATE KEY UPDATE `Value` = VALUES(`Value`);

    DELETE argument
    FROM `AgMcpServerArgument` AS argument
    INNER JOIN `AgMcpServerDefinition` AS definition ON definition.`Id` = argument.`ServerId`
    WHERE argument.`Ordinal` >= COALESCE(JSON_LENGTH(JSON_EXTRACT(definition.`DocumentJson`, '$.arguments')), 0);

    UPDATE `AgMcpToolVersion` AS tool
    INNER JOIN `AgMcpServerDefinition` AS definition ON definition.`Id` = tool.`ServerId`
    SET tool.`CurrentOrdinal` = NULL;

    INSERT INTO `AgMcpToolVersion`
        (`ID`, `ServerId`, `HistoryOrdinal`, `CurrentOrdinal`, `Name`, `Description`,
         `InputSchemaJson`, `Risk`, `Sha256`, `DiscoveredAtUtc`)
    SELECT
        versions.`ToolId`, definition.`Id`, versions.`HistoryOrdinal` - 1,
        currentTools.`CurrentOrdinal` - 1, versions.`Name`, versions.`Description`,
        versions.`InputSchemaJson`, versions.`Risk`, versions.`Sha256`,
        COALESCE(
            STR_TO_DATE(
                REPLACE(REPLACE(REPLACE(versions.`DiscoveredAtUtc`, 'T', ' '), '+00:00', ''), 'Z', ''),
                '%Y-%m-%d %H:%i:%s.%f'),
            STR_TO_DATE(
                REPLACE(REPLACE(REPLACE(versions.`DiscoveredAtUtc`, 'T', ' '), '+00:00', ''), 'Z', ''),
                '%Y-%m-%d %H:%i:%s'))
    FROM `AgMcpServerDefinition` AS definition
    CROSS JOIN JSON_TABLE(
        definition.`DocumentJson`, '$.toolVersions[*]'
        COLUMNS (
            `HistoryOrdinal` FOR ORDINALITY,
            `ToolId` CHAR(36) PATH '$.id',
            `Name` VARCHAR(256) PATH '$.name',
            `Description` VARCHAR(4096) PATH '$.description',
            `InputSchemaJson` LONGTEXT PATH '$.inputSchemaJson',
            `Risk` VARCHAR(32) PATH '$.risk',
            `Sha256` CHAR(64) PATH '$.sha256',
            `DiscoveredAtUtc` VARCHAR(64) PATH '$.discoveredAtUtc'
        )
    ) AS versions
    LEFT JOIN JSON_TABLE(
        definition.`DocumentJson`, '$.currentToolVersionIds[*]'
        COLUMNS (
            `CurrentOrdinal` FOR ORDINALITY,
            `CurrentToolId` CHAR(36) PATH '$'
        )
    ) AS currentTools ON currentTools.`CurrentToolId` = versions.`ToolId`
    ON DUPLICATE KEY UPDATE
        `ServerId` = VALUES(`ServerId`),
        `HistoryOrdinal` = VALUES(`HistoryOrdinal`),
        `CurrentOrdinal` = VALUES(`CurrentOrdinal`),
        `Name` = VALUES(`Name`),
        `Description` = VALUES(`Description`),
        `InputSchemaJson` = VALUES(`InputSchemaJson`),
        `Risk` = VALUES(`Risk`),
        `Sha256` = VALUES(`Sha256`),
        `DiscoveredAtUtc` = VALUES(`DiscoveredAtUtc`);

    COMMIT;
END$$
DELIMITER ;

CALL `AgNormalizeMcpServerDefinitions`();
DROP PROCEDURE `AgNormalizeMcpServerDefinitions`;

ALTER TABLE `AgMcpServerDefinition`
    MODIFY `Name` VARCHAR(256) NOT NULL,
    MODIFY `Description` LONGTEXT NOT NULL,
    MODIFY `Transport` VARCHAR(32) NOT NULL,
    MODIFY `Endpoint` VARCHAR(2048) NOT NULL,
    MODIFY `Command` VARCHAR(512) NOT NULL,
    MODIFY `CredentialAlias` VARCHAR(200) NOT NULL,
    MODIFY `Enabled` TINYINT(1) NOT NULL,
    MODIFY `Status` VARCHAR(32) NOT NULL,
    MODIFY `LastError` VARCHAR(4096) NOT NULL,
    ADD CONSTRAINT `ck_ag_mcp_server_transport`
        CHECK (`Transport` IN ('StreamableHttp', 'Sse', 'Stdio')),
    ADD CONSTRAINT `ck_ag_mcp_server_status`
        CHECK (`Status` IN ('NotSynced', 'Healthy', 'Unhealthy', 'Disabled', 'Archived')),
    DROP COLUMN `DocumentJson`;
