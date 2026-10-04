-- MySQL 8: additive usage columns on an already normalized AgAgentRunAudit.
-- The legacy DocumentJson table from 001 is NOT normalized by this script.
-- DDL auto-commits; after an interruption rerun this idempotent script.
SET NAMES utf8mb4;
DELIMITER $$
DROP PROCEDURE IF EXISTS eu_agent_add_run_usage_012$$
CREATE PROCEDURE eu_agent_add_run_usage_012()
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'ID')
       OR NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'AgentVersionId') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Normalize AgAgentRunAudit first; legacy DocumentJson tables are not supported by this migration.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'ModelProfileId') THEN
        ALTER TABLE `AgAgentRunAudit` ADD COLUMN `ModelProfileId` varchar(256) NULL COMMENT '模型配置编码';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'ModelProfileId'
                   AND data_type = 'varchar' AND is_nullable = 'YES' AND character_maximum_length = 256) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Incompatible existing ModelProfileId column; no destructive conversion is performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'InputTokens') THEN
        ALTER TABLE `AgAgentRunAudit` ADD COLUMN `InputTokens` bigint NULL COMMENT '模型报告的输入 Token 数，未报告时为空';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'InputTokens'
                   AND data_type = 'bigint' AND is_nullable = 'YES' AND column_type NOT LIKE '%unsigned%') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Incompatible existing InputTokens column; no destructive conversion is performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'OutputTokens') THEN
        ALTER TABLE `AgAgentRunAudit` ADD COLUMN `OutputTokens` bigint NULL COMMENT '模型报告的输出 Token 数，未报告时为空';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'OutputTokens'
                   AND data_type = 'bigint' AND is_nullable = 'YES' AND column_type NOT LIKE '%unsigned%') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Incompatible existing OutputTokens column; no destructive conversion is performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'TotalTokens') THEN
        ALTER TABLE `AgAgentRunAudit` ADD COLUMN `TotalTokens` bigint NULL COMMENT '模型报告的总 Token 数，不自行推算';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'TotalTokens'
                   AND data_type = 'bigint' AND is_nullable = 'YES' AND column_type NOT LIKE '%unsigned%') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Incompatible existing TotalTokens column; no destructive conversion is performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'TokenUsageStatus') THEN
        ALTER TABLE `AgAgentRunAudit` ADD COLUMN `TokenUsageStatus` varchar(32) NULL COMMENT 'Token 统计完整性：Unknown、Partial、Reported';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'TokenUsageStatus'
                   AND data_type = 'varchar' AND is_nullable = 'YES' AND character_maximum_length = 32) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Incompatible existing TokenUsageStatus column; no destructive conversion is performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'ModelDurationMilliseconds') THEN
        ALTER TABLE `AgAgentRunAudit` ADD COLUMN `ModelDurationMilliseconds` bigint NULL COMMENT '模型循环耗时（毫秒），包含工具等待';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'ModelDurationMilliseconds'
                   AND data_type = 'bigint' AND is_nullable = 'YES' AND column_type NOT LIKE '%unsigned%') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Incompatible existing ModelDurationMilliseconds column; no destructive conversion is performed.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'TimeToFirstTextMilliseconds') THEN
        ALTER TABLE `AgAgentRunAudit` ADD COLUMN `TimeToFirstTextMilliseconds` bigint NULL COMMENT '首段非空文本等待时间（毫秒）';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'AgAgentRunAudit' AND column_name = 'TimeToFirstTextMilliseconds'
                   AND data_type = 'bigint' AND is_nullable = 'YES' AND column_type NOT LIKE '%unsigned%') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Incompatible existing TimeToFirstTextMilliseconds column; no destructive conversion is performed.';
    END IF;
END$$
CALL eu_agent_add_run_usage_012()$$
DROP PROCEDURE eu_agent_add_run_usage_012$$
DELIMITER ;
