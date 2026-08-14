-- Create normalized Skill published-version and file-manifest tables.
-- Prerequisite: 003_add_basepoco_and_fields_ag_skill_definition.sql.
-- MySQL 8.0.13+

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `AgSkillVersion` (
    `ID` CHAR(36) NOT NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `IsActive` TINYINT(1) NULL DEFAULT 1,
    `ImportDataId` CHAR(36) NULL,
    `ModificationNum` INT NULL DEFAULT 0,
    `Tag` INT NULL DEFAULT 1,
    `GroupId` CHAR(36) NULL,
    `CompanyId` CHAR(36) NULL,
    `AuditStatus` VARCHAR(32) NULL DEFAULT 'Add',
    `CurrentNode` VARCHAR(32) NULL,
    `CreatedBy` CHAR(36) NULL,
    `CreatedTime` DATETIME NULL,
    `UpdateBy` CHAR(36) NULL,
    `UpdateTime` DATETIME NULL,
    `SkillId` CHAR(36) NOT NULL,
    `Ordinal` INT NOT NULL,
    `Label` VARCHAR(128) NOT NULL,
    `ManifestSha256` CHAR(64) NOT NULL,
    `PublishedAtUtc` DATETIME(6) NOT NULL,
    CONSTRAINT `pk_ag_skill_version` PRIMARY KEY (`ID`),
    CONSTRAINT `fk_ag_skill_version_definition` FOREIGN KEY (`SkillId`)
        REFERENCES `AgSkillDefinition` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `ux_ag_skill_version_order` UNIQUE (`SkillId`, `Ordinal`),
    CONSTRAINT `ux_ag_skill_version_label` UNIQUE (`SkillId`, `Label`),
    CONSTRAINT `ck_ag_skill_version_ordinal` CHECK (`Ordinal` >= 0),
    CONSTRAINT `ck_ag_skill_version_manifest_sha256` CHECK (CHAR_LENGTH(`ManifestSha256`) = 64),
    INDEX `ix_ag_skill_version_skill` (`SkillId`, `Ordinal`),
    INDEX `index_AgSkillVersion_Enabled` (`IsActive`),
    INDEX `index_AgSkillVersion_IsDeleted` (`IsDeleted`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `AgSkillVersionFile` (
    `ID` CHAR(36) NOT NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `IsActive` TINYINT(1) NULL DEFAULT 1,
    `ImportDataId` CHAR(36) NULL,
    `ModificationNum` INT NULL DEFAULT 0,
    `Tag` INT NULL DEFAULT 1,
    `GroupId` CHAR(36) NULL,
    `CompanyId` CHAR(36) NULL,
    `AuditStatus` VARCHAR(32) NULL DEFAULT 'Add',
    `CurrentNode` VARCHAR(32) NULL,
    `CreatedBy` CHAR(36) NULL,
    `CreatedTime` DATETIME NULL,
    `UpdateBy` CHAR(36) NULL,
    `UpdateTime` DATETIME NULL,
    `VersionId` CHAR(36) NOT NULL,
    `Ordinal` INT NOT NULL,
    `Path` VARCHAR(1024) NOT NULL,
    `Size` BIGINT NOT NULL,
    `Sha256` CHAR(64) NOT NULL,
    CONSTRAINT `pk_ag_skill_version_file` PRIMARY KEY (`ID`),
    CONSTRAINT `fk_ag_skill_version_file_version` FOREIGN KEY (`VersionId`)
        REFERENCES `AgSkillVersion` (`ID`) ON DELETE CASCADE,
    CONSTRAINT `ux_ag_skill_version_file_order` UNIQUE (`VersionId`, `Ordinal`),
    CONSTRAINT `ck_ag_skill_version_file_ordinal` CHECK (`Ordinal` >= 0),
    CONSTRAINT `ck_ag_skill_version_file_size` CHECK (`Size` >= 0),
    CONSTRAINT `ck_ag_skill_version_file_sha256` CHECK (CHAR_LENGTH(`Sha256`) = 64),
    INDEX `ix_ag_skill_version_file_version` (`VersionId`, `Ordinal`),
    INDEX `index_AgSkillVersionFile_Enabled` (`IsActive`),
    INDEX `index_AgSkillVersionFile_IsDeleted` (`IsDeleted`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;
