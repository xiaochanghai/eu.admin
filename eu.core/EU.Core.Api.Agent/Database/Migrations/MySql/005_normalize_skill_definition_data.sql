-- Normalize existing Skill documents without enforcing snapshot identity.
-- MySQL 8.0.13+. Run 003 and 004 first.

SET NAMES utf8mb4;

DROP PROCEDURE IF EXISTS `AgNormalizeSkillDefinitions`;

DELIMITER $$
CREATE PROCEDURE `AgNormalizeSkillDefinitions`()
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'AgSkillDefinition'
          AND COLUMN_NAME = 'DocumentJson'
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'DocumentJson is absent; the Skill cutover was already finalized';
    END IF;

    START TRANSACTION;

    UPDATE `AgSkillDefinition`
    SET `Name` = COALESCE(JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.name')), `Code`),
        `Description` = COALESCE(JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.description')), ''),
        `Category` = COALESCE(JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.category')), ''),
        `Status` = CASE JSON_UNQUOTE(JSON_EXTRACT(`DocumentJson`, '$.status'))
            WHEN '1' THEN 'Archived'
            WHEN 'Archived' THEN 'Archived'
            ELSE 'Active'
        END;

    INSERT INTO `AgSkillVersion`
        (`ID`, `SkillId`, `Ordinal`, `Label`, `ManifestSha256`, `PublishedAtUtc`)
    SELECT
        versions.`VersionId`,
        definition.`Id`,
        versions.`VersionOrdinal` - 1,
        versions.`Label`,
        versions.`ManifestSha256`,
        STR_TO_DATE(
            REPLACE(REPLACE(REPLACE(versions.`PublishedAtUtc`, 'T', ' '), '+00:00', ''), 'Z', ''),
            '%Y-%m-%d %H:%i:%s.%f')
    FROM `AgSkillDefinition` AS definition
    CROSS JOIN JSON_TABLE(
        definition.`DocumentJson`,
        '$.publishedVersions[*]'
        COLUMNS (
            `VersionOrdinal` FOR ORDINALITY,
            `VersionId` CHAR(36) PATH '$.id',
            `Label` VARCHAR(128) PATH '$.label',
            `ManifestSha256` CHAR(64) PATH '$.manifestSha256',
            `PublishedAtUtc` VARCHAR(64) PATH '$.publishedAtUtc'
        )
    ) AS versions
    ON DUPLICATE KEY UPDATE
        `SkillId` = VALUES(`SkillId`),
        `Ordinal` = VALUES(`Ordinal`),
        `Label` = VALUES(`Label`),
        `ManifestSha256` = VALUES(`ManifestSha256`),
        `PublishedAtUtc` = VALUES(`PublishedAtUtc`);

    INSERT INTO `AgSkillVersionFile`
        (`ID`, `VersionId`, `Ordinal`, `Path`, `Size`, `Sha256`)
    SELECT
        UUID(),
        files.`VersionId`,
        files.`FileOrdinal` - 1,
        files.`Path`,
        files.`Size`,
        files.`Sha256`
    FROM `AgSkillDefinition` AS definition
    CROSS JOIN JSON_TABLE(
        definition.`DocumentJson`,
        '$.publishedVersions[*]'
        COLUMNS (
            `VersionId` CHAR(36) PATH '$.id',
            NESTED PATH '$.files[*]'
            COLUMNS (
                `FileOrdinal` FOR ORDINALITY,
                `Path` VARCHAR(1024) PATH '$.path',
                `Size` BIGINT PATH '$.size',
                `Sha256` CHAR(64) PATH '$.sha256'
            )
        )
    ) AS files
    ON DUPLICATE KEY UPDATE
        `Path` = VALUES(`Path`),
        `Size` = VALUES(`Size`),
        `Sha256` = VALUES(`Sha256`);

    COMMIT;
END$$
DELIMITER ;

CALL `AgNormalizeSkillDefinitions`();
DROP PROCEDURE `AgNormalizeSkillDefinitions`;

ALTER TABLE `AgSkillDefinition`
    MODIFY COLUMN `Name` VARCHAR(256) NOT NULL,
    MODIFY COLUMN `Description` LONGTEXT NOT NULL,
    MODIFY COLUMN `Category` VARCHAR(128) NOT NULL,
    MODIFY COLUMN `Status` VARCHAR(32) NOT NULL,
    ADD CONSTRAINT `ck_ag_skill_definition_status`
        CHECK (`Status` IN ('Active', 'Archived')),
    DROP COLUMN `DocumentJson`;
