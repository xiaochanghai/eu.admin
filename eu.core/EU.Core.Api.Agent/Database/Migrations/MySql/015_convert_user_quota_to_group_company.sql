-- 将未发布的旧 TenantId / GroupId+CompanyId+UserId 额度预览结构转换为 GroupId+CompanyId 所有权。
-- MySQL DDL 会自动提交：必须先停写并备份。脚本先完成全部空表和依赖检查，再修改结构。
DELIMITER $$
DROP PROCEDURE IF EXISTS eu_agent_quota_owner_015$$
CREATE PROCEDURE eu_agent_quota_owner_015()
main: BEGIN
IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE()
 AND TABLE_NAME IN ('AgUserTokenQuotaPeriod','AgUserTokenQuotaReservation','AgUserTokenQuotaPolicy','AgUserTokenQuotaAdjustment')
 AND COLUMN_NAME IN ('TenantId','UserId')) THEN
 LEAVE main;
END IF;
IF NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod')
 OR NOT EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation') THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Legacy quota ledger tables are incomplete; no conversion performed.';
END IF;
IF (SELECT COUNT(*) FROM AgUserTokenQuotaPeriod)<>0 OR (SELECT COUNT(*) FROM AgUserTokenQuotaReservation)<>0
 OR (EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy')
     AND (SELECT COUNT(*) FROM AgUserTokenQuotaPolicy)<>0)
 OR (EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment')
     AND (SELECT COUNT(*) FROM AgUserTokenQuotaAdjustment)<>0) THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Legacy per-user quota schema contains data; stop and reconcile ownership before upgrading.';
END IF;
IF EXISTS (SELECT 1 FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA=DATABASE()
 AND (TABLE_NAME IN ('AgUserTokenQuotaPeriod','AgUserTokenQuotaReservation','AgUserTokenQuotaPolicy','AgUserTokenQuotaAdjustment')
   OR REFERENCED_TABLE_NAME IN ('AgUserTokenQuotaPeriod','AgUserTokenQuotaReservation','AgUserTokenQuotaPolicy','AgUserTokenQuotaAdjustment')))
 OR EXISTS (SELECT 1 FROM information_schema.TRIGGERS WHERE TRIGGER_SCHEMA=DATABASE()
  AND EVENT_OBJECT_TABLE IN ('AgUserTokenQuotaPeriod','AgUserTokenQuotaReservation','AgUserTokenQuotaPolicy','AgUserTokenQuotaAdjustment')) THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Custom quota dependencies require manual review; no conversion performed.';
END IF;
IF EXISTS (SELECT 1 FROM information_schema.STATISTICS s WHERE s.TABLE_SCHEMA=DATABASE()
 AND s.TABLE_NAME IN ('AgUserTokenQuotaPeriod','AgUserTokenQuotaReservation','AgUserTokenQuotaPolicy','AgUserTokenQuotaAdjustment')
 AND s.COLUMN_NAME IN ('TenantId','UserId')
 AND s.INDEX_NAME NOT IN ('UX_AgUserTokenQuotaPeriod_OwnerPeriod','IX_AgUserTokenQuotaReservation_OwnerState','UX_AgUserTokenQuotaPolicy_Owner','IX_AgUserTokenQuotaAdjustment_Owner')) THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Unrecognized legacy quota index; no conversion performed.';
END IF;
IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE()
 AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='UserId')
 AND EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE()
 AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='ConsumerUserId') THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Reservation contains both UserId and ConsumerUserId; no conversion performed.';
END IF;
IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='GroupId')
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='CompanyId')
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='PeriodKind')
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='PeriodKey')
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='GroupId')
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='CompanyId')
 OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='State')
 OR (EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy')
  AND (NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND COLUMN_NAME='GroupId')
   OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND COLUMN_NAME='CompanyId')))
 OR (EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment')
  AND (NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment' AND COLUMN_NAME='GroupId')
   OR NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment' AND COLUMN_NAME='CompanyId'))) THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Legacy quota owner columns are incomplete; no conversion performed.';
END IF;
IF NOT EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE()
 AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME IN ('UserId','ConsumerUserId')
 AND DATA_TYPE='char' AND IS_NULLABLE='NO' AND CHARACTER_MAXIMUM_LENGTH=36) THEN
 SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Reservation consumer user column is incompatible; no conversion performed.';
END IF;

IF EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND INDEX_NAME='UX_AgUserTokenQuotaPeriod_OwnerPeriod') THEN
 DROP INDEX UX_AgUserTokenQuotaPeriod_OwnerPeriod ON AgUserTokenQuotaPeriod;
END IF;
IF EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND INDEX_NAME='IX_AgUserTokenQuotaReservation_OwnerState') THEN
 DROP INDEX IX_AgUserTokenQuotaReservation_OwnerState ON AgUserTokenQuotaReservation;
END IF;
IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy')
 AND EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND INDEX_NAME='UX_AgUserTokenQuotaPolicy_Owner') THEN
 DROP INDEX UX_AgUserTokenQuotaPolicy_Owner ON AgUserTokenQuotaPolicy;
END IF;
IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment')
 AND EXISTS (SELECT 1 FROM information_schema.STATISTICS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment' AND INDEX_NAME='IX_AgUserTokenQuotaAdjustment_Owner') THEN
 DROP INDEX IX_AgUserTokenQuotaAdjustment_Owner ON AgUserTokenQuotaAdjustment;
END IF;

IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='TenantId') THEN
 ALTER TABLE AgUserTokenQuotaPeriod DROP COLUMN TenantId;
END IF;
IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPeriod' AND COLUMN_NAME='UserId') THEN
 ALTER TABLE AgUserTokenQuotaPeriod DROP COLUMN UserId;
END IF;
IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='TenantId') THEN
 ALTER TABLE AgUserTokenQuotaReservation DROP COLUMN TenantId;
END IF;
IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaReservation' AND COLUMN_NAME='UserId') THEN
 ALTER TABLE AgUserTokenQuotaReservation RENAME COLUMN UserId TO ConsumerUserId;
END IF;
IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy') THEN
 IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND COLUMN_NAME='TenantId') THEN
  ALTER TABLE AgUserTokenQuotaPolicy DROP COLUMN TenantId;
 END IF;
 IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy' AND COLUMN_NAME='UserId') THEN
  ALTER TABLE AgUserTokenQuotaPolicy DROP COLUMN UserId;
 END IF;
END IF;
IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment') THEN
 IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment' AND COLUMN_NAME='TenantId') THEN
  ALTER TABLE AgUserTokenQuotaAdjustment DROP COLUMN TenantId;
 END IF;
 IF EXISTS (SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment' AND COLUMN_NAME='UserId') THEN
  ALTER TABLE AgUserTokenQuotaAdjustment DROP COLUMN UserId;
 END IF;
END IF;

CREATE UNIQUE INDEX UX_AgUserTokenQuotaPeriod_OwnerPeriod ON AgUserTokenQuotaPeriod (GroupId, CompanyId, PeriodKind, PeriodKey);
CREATE INDEX IX_AgUserTokenQuotaReservation_OwnerState ON AgUserTokenQuotaReservation (GroupId, CompanyId, State);
IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaPolicy') THEN
 CREATE UNIQUE INDEX UX_AgUserTokenQuotaPolicy_Owner ON AgUserTokenQuotaPolicy (GroupId, CompanyId);
END IF;
IF EXISTS (SELECT 1 FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME='AgUserTokenQuotaAdjustment') THEN
 CREATE INDEX IX_AgUserTokenQuotaAdjustment_Owner ON AgUserTokenQuotaAdjustment (GroupId, CompanyId);
END IF;
END$$
CALL eu_agent_quota_owner_015()$$
DROP PROCEDURE eu_agent_quota_owner_015$$
DELIMITER ;
