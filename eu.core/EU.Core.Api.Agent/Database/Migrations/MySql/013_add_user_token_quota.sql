-- 未发布配额：仅按 GroupId + CompanyId 归属，同一范围内所有用户和 Agent 共用。
-- MySQL 8/InnoDB：手动停写、备份后执行，与 SQL Server 075 字段语义一致。
-- DDL 自动提交，可重复执行；不清空账本、不退还在途/未知预占，不进行破坏性转换。
DELIMITER $$
DROP PROCEDURE IF EXISTS eu_agent_user_quota_013$$
CREATE PROCEDURE eu_agent_user_quota_013()
BEGIN
IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('AgUserTokenQuotaPeriod', 'AgUserTokenQuotaReservation', 'AgUserTokenQuotaPolicy', 'AgUserTokenQuotaAdjustment') AND COLUMN_NAME='TenantId') THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Legacy TenantId quota schema requires guarded migration 015; no conversion performed.';
END IF;
IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('AgUserTokenQuotaPeriod','AgUserTokenQuotaPolicy','AgUserTokenQuotaAdjustment') AND COLUMN_NAME='UserId')
 OR EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='UserId') THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Legacy per-user quota schema requires guarded migration 015; no conversion performed.';
END IF;
    CREATE TABLE IF NOT EXISTS AgUserTokenQuotaPeriod (
        ID char(36) NOT NULL COMMENT '主键',
        IsDeleted tinyint(1) NULL DEFAULT 0 COMMENT '逻辑删除标记',
        IsActive tinyint(1) NULL DEFAULT 1 COMMENT '有效标志',
        ImportDataId char(36) NULL COMMENT '导入模板标识',
        ModificationNum int NULL DEFAULT 0 COMMENT '修改次数',
        Tag int NULL DEFAULT 1 COMMENT '修改标志',
        GroupId char(36) NULL COMMENT '集团标识，仅插入',
        CompanyId char(36) NULL COMMENT '公司标识，仅插入',
        AuditStatus varchar(32) NULL DEFAULT 'Add' COMMENT '审核状态',
        CurrentNode varchar(32) NULL COMMENT '当前流程节点',
        CreatedBy char(36) NULL COMMENT '创建人，仅插入',
        CreatedTime datetime(3) NULL DEFAULT CURRENT_TIMESTAMP(3) COMMENT '创建时间，仅插入，服务端时间',
        UpdateBy char(36) NULL COMMENT '更新人，仅更新',
        UpdateTime datetime(3) NULL COMMENT '更新时间，仅更新',
        PeriodKind int NOT NULL COMMENT '周期种类：0 日，1 月',
        PeriodKey varchar(160) NOT NULL COMMENT '本地日期及配置时区的周期键',
        StartUtc datetime(6) NOT NULL COMMENT '周期含起点 UTC',
        EndUtc datetime(6) NOT NULL COMMENT '周期不含终点 UTC',
        UsedTokens bigint NOT NULL DEFAULT 0 COMMENT '已结算实际 Token',
        ReservedTokens bigint NOT NULL DEFAULT 0 COMMENT '未结算预占，禁止自动过期退还',
        HasUnknownUsage tinyint(1) NOT NULL DEFAULT 0 COMMENT '未知用量冻结标记',
        Revision bigint NOT NULL DEFAULT 0 COMMENT '乐观并发版本',
        PRIMARY KEY (ID),
        KEY index_AgUserTokenQuotaPeriod_Enabled (IsActive),
        KEY index_AgUserTokenQuotaPeriod_IsDeleted (IsDeleted),
        UNIQUE KEY UX_AgUserTokenQuotaPeriod_OwnerPeriod (GroupId, CompanyId, PeriodKind, PeriodKey)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户共享自然日/月 Token 配额账本';
    IF NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND ENGINE='InnoDB') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='User Token quota requires InnoDB transaction semantics.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='ID'
        AND DATA_TYPE='char' AND IS_NULLABLE='NO' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='IsDeleted'
        AND DATA_TYPE='tinyint' AND IS_NULLABLE='YES') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='IsActive'
        AND DATA_TYPE='tinyint' AND IS_NULLABLE='YES') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='ImportDataId'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='ModificationNum'
        AND DATA_TYPE='int' AND IS_NULLABLE='YES') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='Tag'
        AND DATA_TYPE='int' AND IS_NULLABLE='YES') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='GroupId'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='CompanyId'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='AuditStatus'
        AND DATA_TYPE='varchar' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=32) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='CurrentNode'
        AND DATA_TYPE='varchar' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=32) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='CreatedBy'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='CreatedTime'
        AND DATA_TYPE='datetime' AND IS_NULLABLE='YES' AND DATETIME_PRECISION=3) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='UpdateBy'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='UpdateTime'
        AND DATA_TYPE='datetime' AND IS_NULLABLE='YES' AND DATETIME_PRECISION=3) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='PeriodKind'
        AND DATA_TYPE='int' AND IS_NULLABLE='NO') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='PeriodKey'
        AND DATA_TYPE='varchar' AND IS_NULLABLE='NO' AND CHARACTER_MAXIMUM_LENGTH=160) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='StartUtc'
        AND DATA_TYPE='datetime' AND IS_NULLABLE='NO' AND DATETIME_PRECISION=6) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='EndUtc'
        AND DATA_TYPE='datetime' AND IS_NULLABLE='NO' AND DATETIME_PRECISION=6) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='UsedTokens'
        AND DATA_TYPE='bigint' AND IS_NULLABLE='NO') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='ReservedTokens'
        AND DATA_TYPE='bigint' AND IS_NULLABLE='NO') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='HasUnknownUsage'
        AND DATA_TYPE='tinyint' AND IS_NULLABLE='NO') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='Revision'
        AND DATA_TYPE='bigint' AND IS_NULLABLE='NO') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='PRIMARY')<>1
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='PRIMARY' AND COLUMN_NAME='ID' AND SEQ_IN_INDEX=1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='User Token quota primary key must be ID.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='index_AgUserTokenQuotaPeriod_Enabled') THEN
        CREATE INDEX index_AgUserTokenQuotaPeriod_Enabled ON AgUserTokenQuotaPeriod (IsActive);
    END IF;
    IF (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='index_AgUserTokenQuotaPeriod_Enabled')<>1
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='index_AgUserTokenQuotaPeriod_Enabled' AND COLUMN_NAME='IsActive' AND SEQ_IN_INDEX=1 AND NON_UNIQUE=1 AND SUB_PART IS NULL) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota index is incompatible.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='index_AgUserTokenQuotaPeriod_IsDeleted') THEN
        CREATE INDEX index_AgUserTokenQuotaPeriod_IsDeleted ON AgUserTokenQuotaPeriod (IsDeleted);
    END IF;
    IF (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='index_AgUserTokenQuotaPeriod_IsDeleted')<>1
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='index_AgUserTokenQuotaPeriod_IsDeleted' AND COLUMN_NAME='IsDeleted' AND SEQ_IN_INDEX=1 AND NON_UNIQUE=1 AND SUB_PART IS NULL) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota index is incompatible.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='UX_AgUserTokenQuotaPeriod_OwnerPeriod') THEN
        CREATE UNIQUE INDEX UX_AgUserTokenQuotaPeriod_OwnerPeriod ON AgUserTokenQuotaPeriod (GroupId, CompanyId, PeriodKind, PeriodKey);
    END IF;
    IF (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='UX_AgUserTokenQuotaPeriod_OwnerPeriod')<>4
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='UX_AgUserTokenQuotaPeriod_OwnerPeriod' AND COLUMN_NAME='GroupId' AND SEQ_IN_INDEX=1 AND NON_UNIQUE=0 AND SUB_PART IS NULL)
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='UX_AgUserTokenQuotaPeriod_OwnerPeriod' AND COLUMN_NAME='CompanyId' AND SEQ_IN_INDEX=2 AND NON_UNIQUE=0 AND SUB_PART IS NULL)
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='UX_AgUserTokenQuotaPeriod_OwnerPeriod' AND COLUMN_NAME='PeriodKind' AND SEQ_IN_INDEX=3 AND NON_UNIQUE=0 AND SUB_PART IS NULL)
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='UX_AgUserTokenQuotaPeriod_OwnerPeriod' AND COLUMN_NAME='PeriodKey' AND SEQ_IN_INDEX=4 AND NON_UNIQUE=0 AND SUB_PART IS NULL) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota index is incompatible.';
    END IF;
    CREATE TABLE IF NOT EXISTS AgUserTokenQuotaReservation (
        ID char(36) NOT NULL COMMENT '主键',
        IsDeleted tinyint(1) NULL DEFAULT 0 COMMENT '逻辑删除标记',
        IsActive tinyint(1) NULL DEFAULT 1 COMMENT '有效标志',
        ImportDataId char(36) NULL COMMENT '导入模板标识',
        ModificationNum int NULL DEFAULT 0 COMMENT '修改次数',
        Tag int NULL DEFAULT 1 COMMENT '修改标志',
        GroupId char(36) NULL COMMENT '集团标识，仅插入',
        CompanyId char(36) NULL COMMENT '公司标识，仅插入',
        AuditStatus varchar(32) NULL DEFAULT 'Add' COMMENT '审核状态',
        CurrentNode varchar(32) NULL COMMENT '当前流程节点',
        CreatedBy char(36) NULL COMMENT '创建人，仅插入',
        CreatedTime datetime(3) NULL DEFAULT CURRENT_TIMESTAMP(3) COMMENT '创建时间，仅插入，服务端时间',
        UpdateBy char(36) NULL COMMENT '更新人，仅更新',
        UpdateTime datetime(3) NULL COMMENT '更新时间，仅更新',
        ConsumerUserId char(36) NOT NULL COMMENT '实际调用用户，仅追踪，不参与额度归属',
        RunId char(36) NOT NULL COMMENT '实际供应商请求所在运行，仅追踪不分账',
        DailyPeriodId char(36) NULL COMMENT '原始日周期，未启用为空',
        MonthlyPeriodId char(36) NULL COMMENT '原始月周期，未启用为空',
        ReservedTokens bigint NOT NULL COMMENT '本次固定预占 Token',
        ActualTokens bigint NULL COMMENT '完整供应商报告，未知不记零',
        State int NOT NULL DEFAULT 0 COMMENT '0 预占，1 开始，2 结算，3 未知，4 未开始释放',
        SettledAtUtc datetime(6) NULL COMMENT '终态提交时间 UTC',
        PRIMARY KEY (ID),
        KEY index_AgUserTokenQuotaReservation_Enabled (IsActive),
        KEY index_AgUserTokenQuotaReservation_IsDeleted (IsDeleted),
        KEY IX_AgUserTokenQuotaReservation_OwnerState (GroupId, CompanyId, State)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='用户 Token 配额请求预占与结算记录';
    IF NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND ENGINE='InnoDB') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='User Token quota requires InnoDB transaction semantics.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='ID'
        AND DATA_TYPE='char' AND IS_NULLABLE='NO' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='IsDeleted'
        AND DATA_TYPE='tinyint' AND IS_NULLABLE='YES') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='IsActive'
        AND DATA_TYPE='tinyint' AND IS_NULLABLE='YES') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='ImportDataId'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='ModificationNum'
        AND DATA_TYPE='int' AND IS_NULLABLE='YES') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='Tag'
        AND DATA_TYPE='int' AND IS_NULLABLE='YES') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='GroupId'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='CompanyId'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='AuditStatus'
        AND DATA_TYPE='varchar' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=32) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='CurrentNode'
        AND DATA_TYPE='varchar' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=32) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='CreatedBy'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='CreatedTime'
        AND DATA_TYPE='datetime' AND IS_NULLABLE='YES' AND DATETIME_PRECISION=3) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='UpdateBy'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='UpdateTime'
        AND DATA_TYPE='datetime' AND IS_NULLABLE='YES' AND DATETIME_PRECISION=3) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='ConsumerUserId'
        AND DATA_TYPE='char' AND IS_NULLABLE='NO' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='RunId'
        AND DATA_TYPE='char' AND IS_NULLABLE='NO' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='DailyPeriodId'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='MonthlyPeriodId'
        AND DATA_TYPE='char' AND IS_NULLABLE='YES' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='ReservedTokens'
        AND DATA_TYPE='bigint' AND IS_NULLABLE='NO') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='ActualTokens'
        AND DATA_TYPE='bigint' AND IS_NULLABLE='YES') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='State'
        AND DATA_TYPE='int' AND IS_NULLABLE='NO') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='SettledAtUtc'
        AND DATA_TYPE='datetime' AND IS_NULLABLE='YES' AND DATETIME_PRECISION=6) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota column is incompatible; no conversion performed.';
    END IF;
    IF (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='PRIMARY')<>1
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='PRIMARY' AND COLUMN_NAME='ID' AND SEQ_IN_INDEX=1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='User Token quota primary key must be ID.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='index_AgUserTokenQuotaReservation_Enabled') THEN
        CREATE INDEX index_AgUserTokenQuotaReservation_Enabled ON AgUserTokenQuotaReservation (IsActive);
    END IF;
    IF (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='index_AgUserTokenQuotaReservation_Enabled')<>1
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='index_AgUserTokenQuotaReservation_Enabled' AND COLUMN_NAME='IsActive' AND SEQ_IN_INDEX=1 AND NON_UNIQUE=1 AND SUB_PART IS NULL) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota index is incompatible.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='index_AgUserTokenQuotaReservation_IsDeleted') THEN
        CREATE INDEX index_AgUserTokenQuotaReservation_IsDeleted ON AgUserTokenQuotaReservation (IsDeleted);
    END IF;
    IF (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='index_AgUserTokenQuotaReservation_IsDeleted')<>1
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='index_AgUserTokenQuotaReservation_IsDeleted' AND COLUMN_NAME='IsDeleted' AND SEQ_IN_INDEX=1 AND NON_UNIQUE=1 AND SUB_PART IS NULL) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota index is incompatible.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='IX_AgUserTokenQuotaReservation_OwnerState') THEN
        CREATE INDEX IX_AgUserTokenQuotaReservation_OwnerState ON AgUserTokenQuotaReservation (GroupId, CompanyId, State);
    END IF;
    IF (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='IX_AgUserTokenQuotaReservation_OwnerState')<>3
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='IX_AgUserTokenQuotaReservation_OwnerState' AND COLUMN_NAME='GroupId' AND SEQ_IN_INDEX=1 AND NON_UNIQUE=1 AND SUB_PART IS NULL)
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='IX_AgUserTokenQuotaReservation_OwnerState' AND COLUMN_NAME='CompanyId' AND SEQ_IN_INDEX=2 AND NON_UNIQUE=1 AND SUB_PART IS NULL)
        OR NOT EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='IX_AgUserTokenQuotaReservation_OwnerState' AND COLUMN_NAME='State' AND SEQ_IN_INDEX=3 AND NON_UNIQUE=1 AND SUB_PART IS NULL) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existing user Token quota index is incompatible.';
    END IF;
END$$
CALL eu_agent_user_quota_013()$$
DROP PROCEDURE eu_agent_user_quota_013$$
DELIMITER ;
