-- 为主 API 与 Agent 共用的 SmApiLog 补充访问终态字段。
-- 先执行本脚本，再部署写入 StatusCode / Outcome / ErrorCode 的服务版本。
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.SmApiLog', N'U') IS NULL
    THROW 52060, N'SmApiLog is missing. Run the system-module schema migrations first.', 1;

BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.SmApiLog', N'StatusCode') IS NULL
    ALTER TABLE dbo.SmApiLog ADD StatusCode INT NULL;

IF COL_LENGTH(N'dbo.SmApiLog', N'Outcome') IS NULL
    ALTER TABLE dbo.SmApiLog ADD Outcome VARCHAR(32) NULL;

IF COL_LENGTH(N'dbo.SmApiLog', N'ErrorCode') IS NULL
    ALTER TABLE dbo.SmApiLog ADD ErrorCode VARCHAR(128) NULL;

COMMIT TRANSACTION;

-- 回滚（仅在确认新列未被业务查询或报表依赖时执行）：
-- ALTER TABLE dbo.SmApiLog DROP COLUMN ErrorCode, Outcome, StatusCode;
