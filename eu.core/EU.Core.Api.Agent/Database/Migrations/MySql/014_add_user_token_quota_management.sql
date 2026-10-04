-- 新增集团公司级 Token 额度策略与人工对账审计。
-- 所有者仅为 GroupId + CompanyId；旧 TenantId / UserId 预览结构须先执行 015 的空表保护转换。
DELIMITER $$
DROP PROCEDURE IF EXISTS eu_agent_quota_management_014$$
CREATE PROCEDURE eu_agent_quota_management_014()
BEGIN
IF NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod')
 OR NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation') THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Deploy quota ledger migration 013 first.';
END IF;
IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE()
 AND TABLE_NAME IN ('AgUserTokenQuotaPeriod','AgUserTokenQuotaReservation','AgUserTokenQuotaPolicy','AgUserTokenQuotaAdjustment')
 AND COLUMN_NAME='TenantId')
 OR EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE()
 AND TABLE_NAME IN ('AgUserTokenQuotaPeriod','AgUserTokenQuotaPolicy','AgUserTokenQuotaAdjustment') AND COLUMN_NAME='UserId')
 OR EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE()
 AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='UserId') THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Legacy per-user quota schema detected; execute guarded migration 015 first.';
END IF;

CREATE TABLE IF NOT EXISTS AgUserTokenQuotaPolicy (
    ID char(36) NOT NULL COMMENT '主键',
    IsDeleted tinyint(1) NULL DEFAULT 0 COMMENT '逻辑删除',
    IsActive tinyint(1) NULL DEFAULT 1 COMMENT '有效标志',
    ImportDataId char(36) NULL COMMENT '导入模板',
    ModificationNum int NULL DEFAULT 0 COMMENT '修改次数',
    Tag int NULL DEFAULT 1 COMMENT '修改标志',
    GroupId char(36) NULL COMMENT '集团，仅插入',
    CompanyId char(36) NULL COMMENT '公司，仅插入',
    AuditStatus varchar(32) NULL DEFAULT 'Add' COMMENT '审核状态',
    CurrentNode varchar(32) NULL COMMENT '流程节点',
    CreatedBy char(36) NULL COMMENT '创建人，仅插入',
    CreatedTime datetime(3) NULL DEFAULT CURRENT_TIMESTAMP(3) COMMENT '创建时间，服务端时间',
    UpdateBy char(36) NULL COMMENT '更新人，仅更新',
    UpdateTime datetime(3) NULL COMMENT '更新时间，仅更新',
    UseDefaults tinyint(1) NOT NULL COMMENT '是否使用宿主默认设置',
    DailyTotalTokens bigint NULL COMMENT '每日 Token 上限',
    MonthlyTotalTokens bigint NULL COMMENT '每月 Token 上限',
    RequestReservationTokens bigint NULL COMMENT '单次固定预占',
    Revision bigint NOT NULL COMMENT '乐观并发版本',
    PRIMARY KEY (ID),
    KEY index_AgUserTokenQuotaPolicy_IsDeleted (IsDeleted),
    KEY index_AgUserTokenQuotaPolicy_Enabled (IsActive),
    UNIQUE KEY UX_AgUserTokenQuotaPolicy_Owner (GroupId, CompanyId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='集团公司共享 Token 额度设置';

CREATE TABLE IF NOT EXISTS AgUserTokenQuotaAdjustment (
    ID char(36) NOT NULL COMMENT '主键',
    IsDeleted tinyint(1) NULL DEFAULT 0 COMMENT '逻辑删除',
    IsActive tinyint(1) NULL DEFAULT 1 COMMENT '有效标志',
    ImportDataId char(36) NULL COMMENT '导入模板',
    ModificationNum int NULL DEFAULT 0 COMMENT '修改次数',
    Tag int NULL DEFAULT 1 COMMENT '修改标志',
    GroupId char(36) NULL COMMENT '集团，仅插入',
    CompanyId char(36) NULL COMMENT '公司，仅插入',
    AuditStatus varchar(32) NULL DEFAULT 'Add' COMMENT '审核状态',
    CurrentNode varchar(32) NULL COMMENT '流程节点',
    CreatedBy char(36) NULL COMMENT '创建人，仅插入',
    CreatedTime datetime(3) NULL DEFAULT CURRENT_TIMESTAMP(3) COMMENT '创建时间，服务端时间',
    UpdateBy char(36) NULL COMMENT '更新人，仅更新',
    UpdateTime datetime(3) NULL COMMENT '更新时间，仅更新',
    OperatorId char(36) NOT NULL COMMENT '操作者',
    Kind varchar(16) NOT NULL COMMENT '固定操作类型',
    TargetId char(36) NOT NULL COMMENT '设置或预占目标',
    CommandHash varchar(64) NOT NULL COMMENT '完整命令哈希',
    BeforeJson varchar(2000) NOT NULL COMMENT '修改前快照',
    ResultJson varchar(2000) NOT NULL COMMENT '幂等操作回执',
    Reason varchar(500) NOT NULL COMMENT '修改原因',
    EvidenceReference varchar(256) NOT NULL COMMENT '核查依据引用',
    AppliedAtUtc datetime(6) NOT NULL COMMENT '应用时间 UTC',
    PRIMARY KEY (ID),
    KEY index_AgUserTokenQuotaAdjustment_IsDeleted (IsDeleted),
    KEY index_AgUserTokenQuotaAdjustment_Enabled (IsActive),
    KEY IX_AgUserTokenQuotaAdjustment_Owner (GroupId, CompanyId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='集团公司共享 Token 额度调整审计';

IF NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND ENGINE='InnoDB')
 OR NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment' AND ENGINE='InnoDB') THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Quota management requires InnoDB.';
END IF;
IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND COLUMN_NAME='ID' AND DATA_TYPE='char' AND IS_NULLABLE='NO' AND CHARACTER_MAXIMUM_LENGTH=36)
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND COLUMN_NAME='GroupId' AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36)
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND COLUMN_NAME='CompanyId' AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36)
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND COLUMN_NAME='UseDefaults' AND DATA_TYPE='tinyint' AND IS_NULLABLE='NO')
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND COLUMN_NAME='Revision' AND DATA_TYPE='bigint' AND IS_NULLABLE='NO')
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment' AND COLUMN_NAME='OperatorId' AND DATA_TYPE='char' AND IS_NULLABLE='NO' AND CHARACTER_MAXIMUM_LENGTH=36)
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment' AND COLUMN_NAME='AppliedAtUtc' AND DATA_TYPE='datetime' AND IS_NULLABLE='NO') THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Incompatible quota management column; no conversion performed.';
END IF;
IF (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND INDEX_NAME='UX_AgUserTokenQuotaPolicy_Owner')<>2
 OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND INDEX_NAME='UX_AgUserTokenQuotaPolicy_Owner' AND COLUMN_NAME='GroupId' AND SEQ_IN_INDEX=1 AND NON_UNIQUE=0)
 OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND INDEX_NAME='UX_AgUserTokenQuotaPolicy_Owner' AND COLUMN_NAME='CompanyId' AND SEQ_IN_INDEX=2 AND NON_UNIQUE=0)
 OR (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment' AND INDEX_NAME='IX_AgUserTokenQuotaAdjustment_Owner')<>2 THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Quota management owner index is incompatible.';
END IF;
END$$
CALL eu_agent_quota_management_014()$$
DROP PROCEDURE eu_agent_quota_management_014$$
DELIMITER ;
