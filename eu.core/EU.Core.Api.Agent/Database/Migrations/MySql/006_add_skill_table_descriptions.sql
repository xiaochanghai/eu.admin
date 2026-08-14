-- Add or update Chinese descriptions for normalized Skill tables.
-- MySQL 8.0.13+. Run after 003, 004 and 005.
-- The file is UTF-8. Ensure the client connection uses utf8mb4.

SET NAMES utf8mb4;

ALTER TABLE `AgSkillDefinition`
    COMMENT = 'Skill 定义主表，保存 Skill 身份、名称、分类、状态和草稿修订号',
    MODIFY COLUMN `Id` CHAR(36) NOT NULL COMMENT 'Skill 主键',
    MODIFY COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '软删除标识',
    MODIFY COLUMN `IsActive` TINYINT(1) NULL DEFAULT 1 COMMENT '是否启用',
    MODIFY COLUMN `ImportDataId` CHAR(36) NULL COMMENT '外部导入数据标识',
    MODIFY COLUMN `ModificationNum` INT NULL DEFAULT 0 COMMENT '修改次数',
    MODIFY COLUMN `Tag` INT NULL DEFAULT 1 COMMENT '通用数据标签',
    MODIFY COLUMN `GroupId` CHAR(36) NULL COMMENT '所属集团标识',
    MODIFY COLUMN `CompanyId` CHAR(36) NULL COMMENT '所属公司标识',
    MODIFY COLUMN `AuditStatus` VARCHAR(32) NULL DEFAULT 'Add' COMMENT '审核状态',
    MODIFY COLUMN `CurrentNode` VARCHAR(32) NULL COMMENT '当前审核节点',
    MODIFY COLUMN `CreatedBy` CHAR(36) NULL COMMENT '创建人标识',
    MODIFY COLUMN `CreatedTime` DATETIME NULL COMMENT '创建时间',
    MODIFY COLUMN `UpdateBy` CHAR(36) NULL COMMENT '最后修改人标识',
    MODIFY COLUMN `UpdateTime` DATETIME NULL COMMENT '最后修改时间',
    MODIFY COLUMN `Code` VARCHAR(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL COMMENT 'Skill 唯一编码',
    MODIFY COLUMN `DraftRevision` BIGINT NOT NULL COMMENT '草稿修订号，用于乐观并发控制',
    MODIFY COLUMN `Name` VARCHAR(256) NOT NULL COMMENT 'Skill 显示名称',
    MODIFY COLUMN `Description` LONGTEXT NOT NULL COMMENT 'Skill 功能说明',
    MODIFY COLUMN `Category` VARCHAR(128) NOT NULL COMMENT 'Skill 分类',
    MODIFY COLUMN `Status` VARCHAR(32) NOT NULL COMMENT 'Skill 状态：Active 或 Archived';

ALTER TABLE `AgSkillVersion`
    COMMENT = 'Skill 发布版本表，保存版本标识、文件清单摘要和发布时间',
    MODIFY COLUMN `ID` CHAR(36) NOT NULL COMMENT 'Skill 发布版本主键',
    MODIFY COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '软删除标识',
    MODIFY COLUMN `IsActive` TINYINT(1) NULL DEFAULT 1 COMMENT '是否启用',
    MODIFY COLUMN `ImportDataId` CHAR(36) NULL COMMENT '外部导入数据标识',
    MODIFY COLUMN `ModificationNum` INT NULL DEFAULT 0 COMMENT '修改次数',
    MODIFY COLUMN `Tag` INT NULL DEFAULT 1 COMMENT '通用数据标签',
    MODIFY COLUMN `GroupId` CHAR(36) NULL COMMENT '所属集团标识',
    MODIFY COLUMN `CompanyId` CHAR(36) NULL COMMENT '所属公司标识',
    MODIFY COLUMN `AuditStatus` VARCHAR(32) NULL DEFAULT 'Add' COMMENT '审核状态',
    MODIFY COLUMN `CurrentNode` VARCHAR(32) NULL COMMENT '当前审核节点',
    MODIFY COLUMN `CreatedBy` CHAR(36) NULL COMMENT '创建人标识',
    MODIFY COLUMN `CreatedTime` DATETIME NULL COMMENT '创建时间',
    MODIFY COLUMN `UpdateBy` CHAR(36) NULL COMMENT '最后修改人标识',
    MODIFY COLUMN `UpdateTime` DATETIME NULL COMMENT '最后修改时间',
    MODIFY COLUMN `SkillId` CHAR(36) NOT NULL COMMENT '所属 Skill 主键，对应 AgSkillDefinition.ID',
    MODIFY COLUMN `Ordinal` INT NOT NULL COMMENT '发布版本排列顺序，从 0 开始',
    MODIFY COLUMN `Label` VARCHAR(128) NOT NULL COMMENT '严格 SemVer 版本标签，例如 1.0.0',
    MODIFY COLUMN `ManifestSha256` CHAR(64) NOT NULL COMMENT '发布文件清单的 SHA-256 摘要',
    MODIFY COLUMN `PublishedAtUtc` DATETIME(6) NOT NULL COMMENT 'UTC 发布时间';

ALTER TABLE `AgSkillVersionFile`
    COMMENT = 'Skill 发布版本文件表，保存不可变文件清单',
    MODIFY COLUMN `ID` CHAR(36) NOT NULL COMMENT 'Skill 发布版本文件主键',
    MODIFY COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 COMMENT '软删除标识',
    MODIFY COLUMN `IsActive` TINYINT(1) NULL DEFAULT 1 COMMENT '是否启用',
    MODIFY COLUMN `ImportDataId` CHAR(36) NULL COMMENT '外部导入数据标识',
    MODIFY COLUMN `ModificationNum` INT NULL DEFAULT 0 COMMENT '修改次数',
    MODIFY COLUMN `Tag` INT NULL DEFAULT 1 COMMENT '通用数据标签',
    MODIFY COLUMN `GroupId` CHAR(36) NULL COMMENT '所属集团标识',
    MODIFY COLUMN `CompanyId` CHAR(36) NULL COMMENT '所属公司标识',
    MODIFY COLUMN `AuditStatus` VARCHAR(32) NULL DEFAULT 'Add' COMMENT '审核状态',
    MODIFY COLUMN `CurrentNode` VARCHAR(32) NULL COMMENT '当前审核节点',
    MODIFY COLUMN `CreatedBy` CHAR(36) NULL COMMENT '创建人标识',
    MODIFY COLUMN `CreatedTime` DATETIME NULL COMMENT '创建时间',
    MODIFY COLUMN `UpdateBy` CHAR(36) NULL COMMENT '最后修改人标识',
    MODIFY COLUMN `UpdateTime` DATETIME NULL COMMENT '最后修改时间',
    MODIFY COLUMN `VersionId` CHAR(36) NOT NULL COMMENT '所属 Skill 发布版本主键，对应 AgSkillVersion.ID',
    MODIFY COLUMN `Ordinal` INT NOT NULL COMMENT '文件排列顺序，从 0 开始',
    MODIFY COLUMN `Path` VARCHAR(1024) NOT NULL COMMENT 'Skill 内相对文件路径',
    MODIFY COLUMN `Size` BIGINT NOT NULL COMMENT '文件字节数',
    MODIFY COLUMN `Sha256` CHAR(64) NOT NULL COMMENT '文件内容的 SHA-256 摘要';

SELECT
    TABLE_NAME AS TableName,
    TABLE_COMMENT AS TableDescription
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('AgSkillDefinition', 'AgSkillVersion', 'AgSkillVersionFile')
ORDER BY TABLE_NAME;

SELECT
    TABLE_NAME AS TableName,
    COLUMN_NAME AS ColumnName,
    COLUMN_COMMENT AS ColumnDescription
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('AgSkillDefinition', 'AgSkillVersion', 'AgSkillVersionFile')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
