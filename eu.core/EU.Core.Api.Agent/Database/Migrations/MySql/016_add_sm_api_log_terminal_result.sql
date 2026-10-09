-- 为主 API 与 Agent 共用的 SmApiLog 补充访问终态字段。
-- 先执行本脚本，再部署写入 StatusCode / Outcome / ErrorCode 的服务版本。
DELIMITER $$
DROP PROCEDURE IF EXISTS eu_sm_api_log_terminal_result_016$$
CREATE PROCEDURE eu_sm_api_log_terminal_result_016()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = DATABASE()
          AND table_name = 'SmApiLog') THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'SmApiLog is missing. Run the system-module schema migrations first.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'SmApiLog'
          AND column_name = 'StatusCode') THEN
        ALTER TABLE `SmApiLog` ADD COLUMN `StatusCode` INT NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'SmApiLog'
          AND column_name = 'Outcome') THEN
        ALTER TABLE `SmApiLog` ADD COLUMN `Outcome` VARCHAR(32) NULL;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = 'SmApiLog'
          AND column_name = 'ErrorCode') THEN
        ALTER TABLE `SmApiLog` ADD COLUMN `ErrorCode` VARCHAR(128) NULL;
    END IF;
END$$
CALL eu_sm_api_log_terminal_result_016()$$
DROP PROCEDURE eu_sm_api_log_terminal_result_016$$
DELIMITER ;

-- 回滚（仅在确认新列未被业务查询或报表依赖时执行）：
-- ALTER TABLE `SmApiLog`
--     DROP COLUMN `ErrorCode`,
--     DROP COLUMN `Outcome`,
--     DROP COLUMN `StatusCode`;
