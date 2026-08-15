-- Normalize Agent run audits exported from current SQL Server data.
-- Source row-set SHA-256: da3d1693d4dae4478de5649ac486d16e0d1053ab0058fd4bb6dcdb0ad831d812
-- Run 048 first, then this script, then Data/049, 050, and 051.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'DocumentJson') IS NULL
    THROW 51920, N'DocumentJson is absent; Agent run audit cutover was already finalized.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AgAgentRunAuditNormalizationCheckpoint', N'U') IS NULL
        CREATE TABLE dbo.AgAgentRunAuditNormalizationCheckpoint (RunId CHAR(36) NOT NULL PRIMARY KEY);

    -- Agent run audit 5c0edcfd-5269-4afb-8df9-94bc09d3855d
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-31T14:03:57.5675006+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-31T14:03:57.5675006+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-31T14:04:05.9983938+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-31T14:04:05.9983938+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c33c8bd72bf65b21676413986123fa87d2172d7fe73174770c6ea5b188577d6'))) <> CONVERT(VARBINARY(MAX), N'2c33c8bd72bf65b21676413986123fa87d2172d7fe73174770c6ea5b188577d6')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5c0edcfd-5269-4afb-8df9-94bc09d3855d'))) <> CONVERT(VARBINARY(MAX), N'5c0edcfd-5269-4afb-8df9-94bc09d3855d')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f2f3206f-f4b2-4af4-9a35-f3eb87449a1e'))) <> CONVERT(VARBINARY(MAX), N'f2f3206f-f4b2-4af4-9a35-f3eb87449a1e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'f2f3206f-f4b2-4af4-9a35-f3eb87449a1e',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-31T14:03:57.5675006+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-31T14:04:05.9983938+00:00', 127)),
        InputSha256 = N'2c33c8bd72bf65b21676413986123fa87d2172d7fe73174770c6ea5b188577d6',
        OutputCharacters = 69,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'5c0edcfd-5269-4afb-8df9-94bc09d3855d' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'5c0edcfd-5269-4afb-8df9-94bc09d3855d';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'5c0edcfd-5269-4afb-8df9-94bc09d3855d')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'5c0edcfd-5269-4afb-8df9-94bc09d3855d');

    -- Agent run audit b43df331-9414-4be4-b8e8-e27ca8082135
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-31T14:18:00.0546322+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-31T14:18:00.0546322+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-31T14:18:06.2404204+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-31T14:18:06.2404204+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b43df331-9414-4be4-b8e8-e27ca8082135'))) <> CONVERT(VARBINARY(MAX), N'b43df331-9414-4be4-b8e8-e27ca8082135')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'daa423a1ea94e73a7a1f83f7b42409510109c6202116f13dcbd32d5a5c6f5423'))) <> CONVERT(VARBINARY(MAX), N'daa423a1ea94e73a7a1f83f7b42409510109c6202116f13dcbd32d5a5c6f5423')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f2f3206f-f4b2-4af4-9a35-f3eb87449a1e'))) <> CONVERT(VARBINARY(MAX), N'f2f3206f-f4b2-4af4-9a35-f3eb87449a1e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'f2f3206f-f4b2-4af4-9a35-f3eb87449a1e',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-31T14:18:00.0546322+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-31T14:18:06.2404204+00:00', 127)),
        InputSha256 = N'daa423a1ea94e73a7a1f83f7b42409510109c6202116f13dcbd32d5a5c6f5423',
        OutputCharacters = 58,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'b43df331-9414-4be4-b8e8-e27ca8082135' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'b43df331-9414-4be4-b8e8-e27ca8082135';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'b43df331-9414-4be4-b8e8-e27ca8082135')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'b43df331-9414-4be4-b8e8-e27ca8082135');

    -- Agent run audit d82957bd-b8e5-4625-9bb9-ed4000e0a5af
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-31T14:18:27.3846096+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-31T14:18:27.3846096+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-31T14:18:35.138963+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-31T14:18:35.138963+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'd82957bd-b8e5-4625-9bb9-ed4000e0a5af'))) <> CONVERT(VARBINARY(MAX), N'd82957bd-b8e5-4625-9bb9-ed4000e0a5af')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f2f3206f-f4b2-4af4-9a35-f3eb87449a1e'))) <> CONVERT(VARBINARY(MAX), N'f2f3206f-f4b2-4af4-9a35-f3eb87449a1e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'f2f3206f-f4b2-4af4-9a35-f3eb87449a1e',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-31T14:18:27.3846096+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-31T14:18:35.138963+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 14,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'd82957bd-b8e5-4625-9bb9-ed4000e0a5af' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'd82957bd-b8e5-4625-9bb9-ed4000e0a5af';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'd82957bd-b8e5-4625-9bb9-ed4000e0a5af')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'd82957bd-b8e5-4625-9bb9-ed4000e0a5af');

    -- Agent run audit d8543158-c93b-4e14-a047-04a28ade3df8
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:40:10.7674818+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:40:10.7674818+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:40:17.8980871+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:40:17.8980871+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4ef566502f45a57067510b893d72ae5395889757df3c17cb5a8d916283d24dd2'))) <> CONVERT(VARBINARY(MAX), N'4ef566502f45a57067510b893d72ae5395889757df3c17cb5a8d916283d24dd2')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a40de0e0-3f30-4fbc-a53e-892ec68b030b'))) <> CONVERT(VARBINARY(MAX), N'a40de0e0-3f30-4fbc-a53e-892ec68b030b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'd8543158-c93b-4e14-a047-04a28ade3df8'))) <> CONVERT(VARBINARY(MAX), N'd8543158-c93b-4e14-a047-04a28ade3df8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'a40de0e0-3f30-4fbc-a53e-892ec68b030b',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:40:10.7674818+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:40:17.8980871+00:00', 127)),
        InputSha256 = N'4ef566502f45a57067510b893d72ae5395889757df3c17cb5a8d916283d24dd2',
        OutputCharacters = 188,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'd8543158-c93b-4e14-a047-04a28ade3df8' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'd8543158-c93b-4e14-a047-04a28ade3df8';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'd8543158-c93b-4e14-a047-04a28ade3df8')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'd8543158-c93b-4e14-a047-04a28ade3df8');

    -- Agent run audit d2e49919-eb1b-4ad6-9f56-269ce824ad9f
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:41:03.8899241+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:41:03.8899241+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:41:05.7752039+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:41:05.7752039+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:41:07.024314+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:41:07.024314+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:41:10.591254+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:41:10.591254+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a40de0e0-3f30-4fbc-a53e-892ec68b030b'))) <> CONVERT(VARBINARY(MAX), N'a40de0e0-3f30-4fbc-a53e-892ec68b030b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8'))) <> CONVERT(VARBINARY(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'd2e49919-eb1b-4ad6-9f56-269ce824ad9f'))) <> CONVERT(VARBINARY(MAX), N'd2e49919-eb1b-4ad6-9f56-269ce824ad9f')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'a40de0e0-3f30-4fbc-a53e-892ec68b030b',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:41:03.8899241+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:41:10.591254+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 94,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'd2e49919-eb1b-4ad6-9f56-269ce824ad9f' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'd2e49919-eb1b-4ad6-9f56-269ce824ad9f';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'34248165-1203-5ec4-b144-58e5c0939323', N'd2e49919-eb1b-4ad6-9f56-269ce824ad9f', 0, N'b65c0544-e334-4c98-a7bd-f153eb10fde8', N'get_supplier', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:41:05.7752039+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:41:07.024314+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'd2e49919-eb1b-4ad6-9f56-269ce824ad9f')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'd2e49919-eb1b-4ad6-9f56-269ce824ad9f');

    -- Agent run audit 3d2f077d-76d2-4cc4-95fa-0b140c2c19a7
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:42:51.4862463+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:42:51.4862463+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:42:54.2702984+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:42:54.2702984+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:42:54.3245381+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:42:54.3245381+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:42:57.2705937+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:42:57.2705937+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3d2f077d-76d2-4cc4-95fa-0b140c2c19a7'))) <> CONVERT(VARBINARY(MAX), N'3d2f077d-76d2-4cc4-95fa-0b140c2c19a7')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a40de0e0-3f30-4fbc-a53e-892ec68b030b'))) <> CONVERT(VARBINARY(MAX), N'a40de0e0-3f30-4fbc-a53e-892ec68b030b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8'))) <> CONVERT(VARBINARY(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'a40de0e0-3f30-4fbc-a53e-892ec68b030b',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:42:51.4862463+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:42:57.2705937+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 123,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'3d2f077d-76d2-4cc4-95fa-0b140c2c19a7' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'3d2f077d-76d2-4cc4-95fa-0b140c2c19a7';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'992e0ab7-e6e1-59d7-8d19-8303a250b913', N'3d2f077d-76d2-4cc4-95fa-0b140c2c19a7', 0, N'b65c0544-e334-4c98-a7bd-f153eb10fde8', N'get_supplier', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:42:54.2702984+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:42:54.3245381+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'3d2f077d-76d2-4cc4-95fa-0b140c2c19a7')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'3d2f077d-76d2-4cc4-95fa-0b140c2c19a7');

    -- Agent run audit 3f5692d2-29ae-49ed-940a-8a1d37324fc4
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:45:57.3261852+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:45:57.3261852+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:46:04.0186476+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:46:04.0186476+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3f5692d2-29ae-49ed-940a-8a1d37324fc4'))) <> CONVERT(VARBINARY(MAX), N'3f5692d2-29ae-49ed-940a-8a1d37324fc4')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'94213082a09af5df0e7de62c9b3500a450536dd2bd68557dc24916452f0a5aae'))) <> CONVERT(VARBINARY(MAX), N'94213082a09af5df0e7de62c9b3500a450536dd2bd68557dc24916452f0a5aae')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68'))) <> CONVERT(VARBINARY(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c9be0598-c504-46d8-9d94-17b596affd68',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:45:57.3261852+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:46:04.0186476+00:00', 127)),
        InputSha256 = N'94213082a09af5df0e7de62c9b3500a450536dd2bd68557dc24916452f0a5aae',
        OutputCharacters = 618,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'3f5692d2-29ae-49ed-940a-8a1d37324fc4' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'3f5692d2-29ae-49ed-940a-8a1d37324fc4';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'3f5692d2-29ae-49ed-940a-8a1d37324fc4')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'3f5692d2-29ae-49ed-940a-8a1d37324fc4');

    -- Agent run audit 36ba02d8-63f7-49db-a2a9-9c5b3cea245a
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1950027ed7b7cdac86998997b4b3055f373d7ee242e99ce3acf4058f0f66eea5'))) <> CONVERT(VARBINARY(MAX), N'1950027ed7b7cdac86998997b4b3055f373d7ee242e99ce3acf4058f0f66eea5')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:46:35.8789521+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:46:35.8789521+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T05:46:42.6548607+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T05:46:42.6548607+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'36ba02d8-63f7-49db-a2a9-9c5b3cea245a'))) <> CONVERT(VARBINARY(MAX), N'36ba02d8-63f7-49db-a2a9-9c5b3cea245a')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68'))) <> CONVERT(VARBINARY(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c9be0598-c504-46d8-9d94-17b596affd68',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:46:35.8789521+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T05:46:42.6548607+00:00', 127)),
        InputSha256 = N'1950027ed7b7cdac86998997b4b3055f373d7ee242e99ce3acf4058f0f66eea5',
        OutputCharacters = 284,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'36ba02d8-63f7-49db-a2a9-9c5b3cea245a' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'36ba02d8-63f7-49db-a2a9-9c5b3cea245a';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'36ba02d8-63f7-49db-a2a9-9c5b3cea245a')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'36ba02d8-63f7-49db-a2a9-9c5b3cea245a');

    -- Agent run audit 8b7468a1-8ee0-4569-873c-b87bdb672d92
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:43:17.7835919+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:43:17.7835919+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:43:21.3877743+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:43:21.3877743+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'8b7468a1-8ee0-4569-873c-b87bdb672d92'))) <> CONVERT(VARBINARY(MAX), N'8b7468a1-8ee0-4569-873c-b87bdb672d92')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a43462f613aea5895e67e4d7b61a61d8e664bcfb43ba45ee0b66f850206b993d'))) <> CONVERT(VARBINARY(MAX), N'a43462f613aea5895e67e4d7b61a61d8e664bcfb43ba45ee0b66f850206b993d')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68'))) <> CONVERT(VARBINARY(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c9be0598-c504-46d8-9d94-17b596affd68',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:43:17.7835919+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:43:21.3877743+00:00', 127)),
        InputSha256 = N'a43462f613aea5895e67e4d7b61a61d8e664bcfb43ba45ee0b66f850206b993d',
        OutputCharacters = 7,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'8b7468a1-8ee0-4569-873c-b87bdb672d92' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'8b7468a1-8ee0-4569-873c-b87bdb672d92';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'8b7468a1-8ee0-4569-873c-b87bdb672d92')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'8b7468a1-8ee0-4569-873c-b87bdb672d92');

    -- Agent run audit 2dca16fd-a189-468d-bdeb-071341aa9d4d
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:43:51.9216308+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:43:51.9216308+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:43:54.2103266+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:43:54.2103266+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:43:54.5897831+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:43:54.5897831+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:43:57.7879462+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:43:57.7879462+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2dca16fd-a189-468d-bdeb-071341aa9d4d'))) <> CONVERT(VARBINARY(MAX), N'2dca16fd-a189-468d-bdeb-071341aa9d4d')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8'))) <> CONVERT(VARBINARY(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68'))) <> CONVERT(VARBINARY(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c9be0598-c504-46d8-9d94-17b596affd68',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:43:51.9216308+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:43:57.7879462+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 62,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'2dca16fd-a189-468d-bdeb-071341aa9d4d' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'2dca16fd-a189-468d-bdeb-071341aa9d4d';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'1244cc3a-6261-5aed-9566-5d0f68326e4c', N'2dca16fd-a189-468d-bdeb-071341aa9d4d', 0, N'b65c0544-e334-4c98-a7bd-f153eb10fde8', N'get_supplier', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:43:54.2103266+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:43:54.5897831+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'2dca16fd-a189-468d-bdeb-071341aa9d4d')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'2dca16fd-a189-468d-bdeb-071341aa9d4d');

    -- Agent run audit cb512a35-a63c-4f7c-9ed9-18270486e071
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:44:17.8896224+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:44:17.8896224+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:44:29.458302+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:44:29.458302+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'840317c68ec68d23ffaea4567f98b4cd42c96e7234cb7c56af30e45f16aa2588'))) <> CONVERT(VARBINARY(MAX), N'840317c68ec68d23ffaea4567f98b4cd42c96e7234cb7c56af30e45f16aa2588')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68'))) <> CONVERT(VARBINARY(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'cb512a35-a63c-4f7c-9ed9-18270486e071'))) <> CONVERT(VARBINARY(MAX), N'cb512a35-a63c-4f7c-9ed9-18270486e071')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c9be0598-c504-46d8-9d94-17b596affd68',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:44:17.8896224+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:44:29.458302+00:00', 127)),
        InputSha256 = N'840317c68ec68d23ffaea4567f98b4cd42c96e7234cb7c56af30e45f16aa2588',
        OutputCharacters = 830,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'cb512a35-a63c-4f7c-9ed9-18270486e071' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'cb512a35-a63c-4f7c-9ed9-18270486e071';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'cb512a35-a63c-4f7c-9ed9-18270486e071')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'cb512a35-a63c-4f7c-9ed9-18270486e071');

    -- Agent run audit f8aad50f-be7a-418e-b5a8-7e9e310ce67f
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:46:05.1141629+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:46:05.1141629+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:46:07.8436296+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:46:07.8436296+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68'))) <> CONVERT(VARBINARY(MAX), N'c9be0598-c504-46d8-9d94-17b596affd68')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'de723f269914669968cbd941e33ef89e54c9ef32753fdcf687267a18020ece4a'))) <> CONVERT(VARBINARY(MAX), N'de723f269914669968cbd941e33ef89e54c9ef32753fdcf687267a18020ece4a')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f8aad50f-be7a-418e-b5a8-7e9e310ce67f'))) <> CONVERT(VARBINARY(MAX), N'f8aad50f-be7a-418e-b5a8-7e9e310ce67f')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c9be0598-c504-46d8-9d94-17b596affd68',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:46:05.1141629+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:46:07.8436296+00:00', 127)),
        InputSha256 = N'de723f269914669968cbd941e33ef89e54c9ef32753fdcf687267a18020ece4a',
        OutputCharacters = 6,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'f8aad50f-be7a-418e-b5a8-7e9e310ce67f' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'f8aad50f-be7a-418e-b5a8-7e9e310ce67f';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'f8aad50f-be7a-418e-b5a8-7e9e310ce67f')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'f8aad50f-be7a-418e-b5a8-7e9e310ce67f');

    -- Agent run audit c4c80d66-6341-4b6c-a921-74f01519ccb2
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:47:25.065391+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:47:25.065391+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:47:33.7783552+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:47:33.7783552+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c4c80d66-6341-4b6c-a921-74f01519ccb2'))) <> CONVERT(VARBINARY(MAX), N'c4c80d66-6341-4b6c-a921-74f01519ccb2')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'd55cc2c1620af4e0d602bce86869f947c477a1b8f7c4d1768c00fe9e4dcdf06a'))) <> CONVERT(VARBINARY(MAX), N'd55cc2c1620af4e0d602bce86869f947c477a1b8f7c4d1768c00fe9e4dcdf06a')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9'))) <> CONVERT(VARBINARY(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:47:25.065391+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:47:33.7783552+00:00', 127)),
        InputSha256 = N'd55cc2c1620af4e0d602bce86869f947c477a1b8f7c4d1768c00fe9e4dcdf06a',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'c4c80d66-6341-4b6c-a921-74f01519ccb2' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'c4c80d66-6341-4b6c-a921-74f01519ccb2';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'c4c80d66-6341-4b6c-a921-74f01519ccb2')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'c4c80d66-6341-4b6c-a921-74f01519ccb2');

    -- Agent run audit 45c928b7-26b0-4bbf-a55e-b3d3489de24d
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:48:09.4244982+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:48:09.4244982+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:48:35.7117085+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:48:35.7117085+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'45c928b7-26b0-4bbf-a55e-b3d3489de24d'))) <> CONVERT(VARBINARY(MAX), N'45c928b7-26b0-4bbf-a55e-b3d3489de24d')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'd535623f0023b5a932b65e14a05a48b31959409f5319034db09c672ced9cc706'))) <> CONVERT(VARBINARY(MAX), N'd535623f0023b5a932b65e14a05a48b31959409f5319034db09c672ced9cc706')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9'))) <> CONVERT(VARBINARY(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:48:09.4244982+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:48:35.7117085+00:00', 127)),
        InputSha256 = N'd535623f0023b5a932b65e14a05a48b31959409f5319034db09c672ced9cc706',
        OutputCharacters = 92,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'45c928b7-26b0-4bbf-a55e-b3d3489de24d' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'45c928b7-26b0-4bbf-a55e-b3d3489de24d';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'45c928b7-26b0-4bbf-a55e-b3d3489de24d')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'45c928b7-26b0-4bbf-a55e-b3d3489de24d');

    -- Agent run audit 12f1d122-80dc-4a24-967c-f9ddc49f33e7
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'12f1d122-80dc-4a24-967c-f9ddc49f33e7'))) <> CONVERT(VARBINARY(MAX), N'12f1d122-80dc-4a24-967c-f9ddc49f33e7')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1dcca714dbe614c5926039371a972583e531d97413d9284371dfbf3df2d8a587'))) <> CONVERT(VARBINARY(MAX), N'1dcca714dbe614c5926039371a972583e531d97413d9284371dfbf3df2d8a587')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:13.3181275+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:13.3181275+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:31.9119554+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:31.9119554+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9'))) <> CONVERT(VARBINARY(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:13.3181275+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:31.9119554+00:00', 127)),
        InputSha256 = N'1dcca714dbe614c5926039371a972583e531d97413d9284371dfbf3df2d8a587',
        OutputCharacters = 212,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'12f1d122-80dc-4a24-967c-f9ddc49f33e7' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'12f1d122-80dc-4a24-967c-f9ddc49f33e7';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'12f1d122-80dc-4a24-967c-f9ddc49f33e7')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'12f1d122-80dc-4a24-967c-f9ddc49f33e7');

    -- Agent run audit 47c2997b-6f0e-46aa-b6de-3f3a2b308e31
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'01ad6ef14ab4182b6b6a5e3c9277f256ec2e7ee19fb5819e7e2ff99de96fb256'))) <> CONVERT(VARBINARY(MAX), N'01ad6ef14ab4182b6b6a5e3c9277f256ec2e7ee19fb5819e7e2ff99de96fb256')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:29.4242075+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:29.4242075+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:50.2254375+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:50.2254375+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'47c2997b-6f0e-46aa-b6de-3f3a2b308e31'))) <> CONVERT(VARBINARY(MAX), N'47c2997b-6f0e-46aa-b6de-3f3a2b308e31')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9'))) <> CONVERT(VARBINARY(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:29.4242075+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:50.2254375+00:00', 127)),
        InputSha256 = N'01ad6ef14ab4182b6b6a5e3c9277f256ec2e7ee19fb5819e7e2ff99de96fb256',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'47c2997b-6f0e-46aa-b6de-3f3a2b308e31' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'47c2997b-6f0e-46aa-b6de-3f3a2b308e31';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'47c2997b-6f0e-46aa-b6de-3f3a2b308e31')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'47c2997b-6f0e-46aa-b6de-3f3a2b308e31');

    -- Agent run audit de21ce29-a2d4-4f1a-a7c1-9643da41f62f
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:52:06.9397013+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:52:06.9397013+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:52:18.8399375+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:52:18.8399375+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9'))) <> CONVERT(VARBINARY(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'de21ce29-a2d4-4f1a-a7c1-9643da41f62f'))) <> CONVERT(VARBINARY(MAX), N'de21ce29-a2d4-4f1a-a7c1-9643da41f62f')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f5e75ae3930b304c2db2b0c8048a02f8c3e14172612bedfeaa2877f1e0ee6dcb'))) <> CONVERT(VARBINARY(MAX), N'f5e75ae3930b304c2db2b0c8048a02f8c3e14172612bedfeaa2877f1e0ee6dcb')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:52:06.9397013+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:52:18.8399375+00:00', 127)),
        InputSha256 = N'f5e75ae3930b304c2db2b0c8048a02f8c3e14172612bedfeaa2877f1e0ee6dcb',
        OutputCharacters = 546,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'de21ce29-a2d4-4f1a-a7c1-9643da41f62f' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'de21ce29-a2d4-4f1a-a7c1-9643da41f62f';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'de21ce29-a2d4-4f1a-a7c1-9643da41f62f')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'de21ce29-a2d4-4f1a-a7c1-9643da41f62f');

    -- Agent run audit 9a9f618a-5352-404e-be68-1a32f3313385
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0b9d534c94c5c8ab6e99e17edc0c8d2cd46617522959a29948cac4933c83c0e9'))) <> CONVERT(VARBINARY(MAX), N'0b9d534c94c5c8ab6e99e17edc0c8d2cd46617522959a29948cac4933c83c0e9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:53:47.6736507+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:53:47.6736507+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:53:50.1552742+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:53:50.1552742+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9a9f618a-5352-404e-be68-1a32f3313385'))) <> CONVERT(VARBINARY(MAX), N'9a9f618a-5352-404e-be68-1a32f3313385')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9'))) <> CONVERT(VARBINARY(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:53:47.6736507+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:53:50.1552742+00:00', 127)),
        InputSha256 = N'0b9d534c94c5c8ab6e99e17edc0c8d2cd46617522959a29948cac4933c83c0e9',
        OutputCharacters = 9,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'9a9f618a-5352-404e-be68-1a32f3313385' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'9a9f618a-5352-404e-be68-1a32f3313385';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'9a9f618a-5352-404e-be68-1a32f3313385')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'9a9f618a-5352-404e-be68-1a32f3313385');

    -- Agent run audit 784b894d-9bd8-4779-b151-2a5925309bc3
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:54:03.6527863+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:54:03.6527863+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:54:06.1463877+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:54:06.1463877+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:54:06.3413775+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:54:06.3413775+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:54:09.0466444+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:54:09.0466444+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'784b894d-9bd8-4779-b151-2a5925309bc3'))) <> CONVERT(VARBINARY(MAX), N'784b894d-9bd8-4779-b151-2a5925309bc3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8'))) <> CONVERT(VARBINARY(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9'))) <> CONVERT(VARBINARY(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:54:03.6527863+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:54:09.0466444+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 60,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'784b894d-9bd8-4779-b151-2a5925309bc3' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'784b894d-9bd8-4779-b151-2a5925309bc3';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'7f9fa13b-4d41-50f4-a030-174720e31538', N'784b894d-9bd8-4779-b151-2a5925309bc3', 0, N'b65c0544-e334-4c98-a7bd-f153eb10fde8', N'get_supplier', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:54:06.1463877+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:54:06.3413775+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'784b894d-9bd8-4779-b151-2a5925309bc3')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'784b894d-9bd8-4779-b151-2a5925309bc3');

    -- Agent run audit b7c0efee-cd72-4d2e-908e-50ae854dedba
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:34.9700924+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:34.9700924+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:56.3239199+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:56.3239199+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'7eee8ed5a99b368b4ce9a0bb4aba85fb710d0f2af52bface41d7ac03512eb202'))) <> CONVERT(VARBINARY(MAX), N'7eee8ed5a99b368b4ce9a0bb4aba85fb710d0f2af52bface41d7ac03512eb202')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b7c0efee-cd72-4d2e-908e-50ae854dedba'))) <> CONVERT(VARBINARY(MAX), N'b7c0efee-cd72-4d2e-908e-50ae854dedba')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9'))) <> CONVERT(VARBINARY(MAX), N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'da2be35d-ef83-4b95-a4e7-35f9ec95e7f9',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:34.9700924+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:56.3239199+00:00', 127)),
        InputSha256 = N'7eee8ed5a99b368b4ce9a0bb4aba85fb710d0f2af52bface41d7ac03512eb202',
        OutputCharacters = 251,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'b7c0efee-cd72-4d2e-908e-50ae854dedba' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'b7c0efee-cd72-4d2e-908e-50ae854dedba';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'b7c0efee-cd72-4d2e-908e-50ae854dedba')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'b7c0efee-cd72-4d2e-908e-50ae854dedba');

    -- Agent run audit ad04eeac-180e-4b6a-8bf1-a01643671385
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-02T12:24:50.842094+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-02T12:24:50.842094+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-02T12:25:15.3759604+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-02T12:25:15.3759604+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ad04eeac-180e-4b6a-8bf1-a01643671385'))) <> CONVERT(VARBINARY(MAX), N'ad04eeac-180e-4b6a-8bf1-a01643671385')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'cb8620c7-28df-49fb-8036-0a949b78c7e5'))) <> CONVERT(VARBINARY(MAX), N'cb8620c7-28df-49fb-8036-0a949b78c7e5')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'cb8620c7-28df-49fb-8036-0a949b78c7e5',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-02T12:24:50.842094+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-02T12:25:15.3759604+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 2075,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'ad04eeac-180e-4b6a-8bf1-a01643671385' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'ad04eeac-180e-4b6a-8bf1-a01643671385';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'ad04eeac-180e-4b6a-8bf1-a01643671385')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'ad04eeac-180e-4b6a-8bf1-a01643671385');

    -- Agent run audit bf569234-b197-4073-adc8-d9fbc37a4fd8
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-02T12:27:03.0077202+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-02T12:27:03.0077202+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-02T12:27:17.1364277+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-02T12:27:17.1364277+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'bf569234-b197-4073-adc8-d9fbc37a4fd8'))) <> CONVERT(VARBINARY(MAX), N'bf569234-b197-4073-adc8-d9fbc37a4fd8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'cb8620c7-28df-49fb-8036-0a949b78c7e5'))) <> CONVERT(VARBINARY(MAX), N'cb8620c7-28df-49fb-8036-0a949b78c7e5')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'cb8620c7-28df-49fb-8036-0a949b78c7e5',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-02T12:27:03.0077202+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-02T12:27:17.1364277+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 555,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'bf569234-b197-4073-adc8-d9fbc37a4fd8' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'bf569234-b197-4073-adc8-d9fbc37a4fd8';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'bf569234-b197-4073-adc8-d9fbc37a4fd8')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'bf569234-b197-4073-adc8-d9fbc37a4fd8');

    -- Agent run audit eeb2eb2f-40ad-4c7c-8f43-e6a433e1d24c
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-02T12:28:01.0279907+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-02T12:28:01.0279907+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-02T12:28:25.4822576+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-02T12:28:25.4822576+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'cb8620c7-28df-49fb-8036-0a949b78c7e5'))) <> CONVERT(VARBINARY(MAX), N'cb8620c7-28df-49fb-8036-0a949b78c7e5')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'eeb2eb2f-40ad-4c7c-8f43-e6a433e1d24c'))) <> CONVERT(VARBINARY(MAX), N'eeb2eb2f-40ad-4c7c-8f43-e6a433e1d24c')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'cb8620c7-28df-49fb-8036-0a949b78c7e5',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-02T12:28:01.0279907+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-02T12:28:25.4822576+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 1957,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'eeb2eb2f-40ad-4c7c-8f43-e6a433e1d24c' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'eeb2eb2f-40ad-4c7c-8f43-e6a433e1d24c';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'eeb2eb2f-40ad-4c7c-8f43-e6a433e1d24c')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'eeb2eb2f-40ad-4c7c-8f43-e6a433e1d24c');

    -- Agent run audit 3d145f3b-af42-418e-ade6-097210ccda18
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-02T12:55:13.422234+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-02T12:55:13.422234+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-02T12:56:00.4132151+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-02T12:56:00.4132151+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3d145f3b-af42-418e-ade6-097210ccda18'))) <> CONVERT(VARBINARY(MAX), N'3d145f3b-af42-418e-ade6-097210ccda18')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'cb8620c7-28df-49fb-8036-0a949b78c7e5'))) <> CONVERT(VARBINARY(MAX), N'cb8620c7-28df-49fb-8036-0a949b78c7e5')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'cb8620c7-28df-49fb-8036-0a949b78c7e5',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-02T12:55:13.422234+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-02T12:56:00.4132151+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 2501,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'3d145f3b-af42-418e-ade6-097210ccda18' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'3d145f3b-af42-418e-ade6-097210ccda18';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'3d145f3b-af42-418e-ade6-097210ccda18')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'3d145f3b-af42-418e-ade6-097210ccda18');

    -- Agent run audit 47660303-7d39-4364-bfe3-44731302f51c
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-02T13:25:02.5551761+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-02T13:25:02.5551761+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-02T13:25:29.4103219+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-02T13:25:29.4103219+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'47660303-7d39-4364-bfe3-44731302f51c'))) <> CONVERT(VARBINARY(MAX), N'47660303-7d39-4364-bfe3-44731302f51c')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'cb8620c7-28df-49fb-8036-0a949b78c7e5'))) <> CONVERT(VARBINARY(MAX), N'cb8620c7-28df-49fb-8036-0a949b78c7e5')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'cb8620c7-28df-49fb-8036-0a949b78c7e5',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-02T13:25:02.5551761+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-02T13:25:29.4103219+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 2124,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'47660303-7d39-4364-bfe3-44731302f51c' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'47660303-7d39-4364-bfe3-44731302f51c';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'47660303-7d39-4364-bfe3-44731302f51c')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'47660303-7d39-4364-bfe3-44731302f51c');

    -- Agent run audit ef13caba-bf92-436a-ba3a-5d370009c064
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'03423b90d4244e60c5b677dc826f7303fccde6d0186221c6aca9b62c8b38eb2a'))) <> CONVERT(VARBINARY(MAX), N'03423b90d4244e60c5b677dc826f7303fccde6d0186221c6aca9b62c8b38eb2a')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T07:42:41.2453201+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T07:42:41.2453201+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T07:42:50.8453237+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T07:42:50.8453237+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'adb035fa-e140-47bd-9637-86be703027d1'))) <> CONVERT(VARBINARY(MAX), N'adb035fa-e140-47bd-9637-86be703027d1')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ef13caba-bf92-436a-ba3a-5d370009c064'))) <> CONVERT(VARBINARY(MAX), N'ef13caba-bf92-436a-ba3a-5d370009c064')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'adb035fa-e140-47bd-9637-86be703027d1',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T07:42:41.2453201+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T07:42:50.8453237+00:00', 127)),
        InputSha256 = N'03423b90d4244e60c5b677dc826f7303fccde6d0186221c6aca9b62c8b38eb2a',
        OutputCharacters = 357,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'ef13caba-bf92-436a-ba3a-5d370009c064' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'ef13caba-bf92-436a-ba3a-5d370009c064';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'ef13caba-bf92-436a-ba3a-5d370009c064')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'ef13caba-bf92-436a-ba3a-5d370009c064');

    -- Agent run audit db946dc5-0108-4f17-93e0-12a8532a653a
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'03423b90d4244e60c5b677dc826f7303fccde6d0186221c6aca9b62c8b38eb2a'))) <> CONVERT(VARBINARY(MAX), N'03423b90d4244e60c5b677dc826f7303fccde6d0186221c6aca9b62c8b38eb2a')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T07:49:43.3019091+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T07:49:43.3019091+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T07:50:08.7114721+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T07:50:08.7114721+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T07:50:09.201341+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T07:50:09.201341+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T07:50:14.4303729+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T07:50:14.4303729+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T07:50:14.7176339+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T07:50:14.7176339+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T07:50:19.3435363+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T07:50:19.3435363+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T07:50:19.6309749+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T07:50:19.6309749+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T07:50:26.3675806+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T07:50:26.3675806+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_CALL_FAILED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_CALL_FAILED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolFailed'))) <> CONVERT(VARBINARY(MAX), N'ToolFailed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548'))) <> CONVERT(VARBINARY(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'db946dc5-0108-4f17-93e0-12a8532a653a'))) <> CONVERT(VARBINARY(MAX), N'db946dc5-0108-4f17-93e0-12a8532a653a')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c00f9b33-11c5-4c00-a159-3cf947535548',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T07:49:43.3019091+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T07:50:26.3675806+00:00', 127)),
        InputSha256 = N'03423b90d4244e60c5b677dc826f7303fccde6d0186221c6aca9b62c8b38eb2a',
        OutputCharacters = 290,
        ToolCallCount = 3,
        ErrorCode = N''
    WHERE ID = N'db946dc5-0108-4f17-93e0-12a8532a653a' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'db946dc5-0108-4f17-93e0-12a8532a653a';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'045df965-23b4-545e-a7d2-051fe07c1800', N'db946dc5-0108-4f17-93e0-12a8532a653a', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T07:50:08.7114721+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T07:50:09.201341+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'a295326c-dfac-5a6a-aeea-470c8aa5407f', N'db946dc5-0108-4f17-93e0-12a8532a653a', 1, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T07:50:14.4303729+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T07:50:14.7176339+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'ccef9e15-7f6e-559f-a8ce-9b5acc543521', N'db946dc5-0108-4f17-93e0-12a8532a653a', 2, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T07:50:19.3435363+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T07:50:19.6309749+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'db946dc5-0108-4f17-93e0-12a8532a653a')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'db946dc5-0108-4f17-93e0-12a8532a653a');

    -- Agent run audit c9c36ff4-ff87-40be-982e-6b474c2d8bb2
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:05:55.9493053+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:05:55.9493053+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:04.8325089+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:04.8325089+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:05.5271361+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:05.5271361+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:13.23424+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:13.23424+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:13.5630559+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:13.5630559+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:19.09517+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:19.09517+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:19.4579376+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:19.4579376+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:24.0690482+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:24.0690482+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:24.6763625+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:24.6763625+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:29.516443+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:29.516443+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:29.8366902+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:29.8366902+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:34.7955778+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:34.7955778+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:35.0789314+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:35.0789314+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:40.5877707+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:40.5877707+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:40.9281801+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:40.9281801+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:45.6164216+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:45.6164216+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:45.9023653+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:45.9023653+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:06:46.5546029+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:06:46.5546029+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Failed'))) <> CONVERT(VARBINARY(MAX), N'Failed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_CALL_FAILED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_CALL_FAILED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolFailed'))) <> CONVERT(VARBINARY(MAX), N'ToolFailed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548'))) <> CONVERT(VARBINARY(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2'))) <> CONVERT(VARBINARY(MAX), N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c00f9b33-11c5-4c00-a159-3cf947535548',
        AgentCode = N'main-agent',
        Status = N'Failed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:05:55.9493053+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:46.5546029+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 71,
        ToolCallCount = 8,
        ErrorCode = N'MCP_TOOL_CALL_FAILED'
    WHERE ID = N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'957a3568-78f8-51dc-a376-3e2d70df6d07', N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:04.8325089+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:05.5271361+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'f0f4a4a4-720f-55b8-8e67-3ceac6ecb3d2', N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2', 1, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:13.23424+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:13.5630559+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'764a33f0-5679-5f2d-a733-4c013836db27', N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2', 2, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:19.09517+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:19.4579376+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'f4140d9f-260f-5945-b9a0-7797cdcaf4bb', N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2', 3, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:24.0690482+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:24.6763625+00:00', 127)), N'');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'01a4609d-c1b7-5799-9e5b-9e317e3dfaf0', N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2', 4, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:29.516443+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:29.8366902+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'723d4c19-e042-58bd-9094-22d298fe3665', N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2', 5, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:34.7955778+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:35.0789314+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'7e96191d-0e56-5afb-b7b9-ca5782ba215f', N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2', 6, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:40.5877707+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:40.9281801+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'08e93389-593d-5c44-9646-ac0fde135250', N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2', 7, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:45.6164216+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:06:45.9023653+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'c9c36ff4-ff87-40be-982e-6b474c2d8bb2');

    -- Agent run audit 8dafc0cd-248c-40bd-8d94-b3529506119b
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:15:27.7584506+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:15:27.7584506+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:15:36.382011+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:15:36.382011+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:15:37.2504017+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:15:37.2504017+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:15:41.4462428+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:15:41.4462428+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'8dafc0cd-248c-40bd-8d94-b3529506119b'))) <> CONVERT(VARBINARY(MAX), N'8dafc0cd-248c-40bd-8d94-b3529506119b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548'))) <> CONVERT(VARBINARY(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c00f9b33-11c5-4c00-a159-3cf947535548',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:15:27.7584506+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:15:41.4462428+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 76,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'8dafc0cd-248c-40bd-8d94-b3529506119b' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'8dafc0cd-248c-40bd-8d94-b3529506119b';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'8719aac9-6f6a-5a55-b5f8-2dfa3ca4e291', N'8dafc0cd-248c-40bd-8d94-b3529506119b', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:15:36.382011+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:15:37.2504017+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'8dafc0cd-248c-40bd-8d94-b3529506119b')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'8dafc0cd-248c-40bd-8d94-b3529506119b');

    -- Agent run audit 3b78aa87-8fab-4ee7-91dd-e86b00101c88
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:26.3498617+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:26.3498617+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:51.2349492+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:51.2349492+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:51.8294464+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:51.8294464+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:57.3214192+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:57.3214192+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:57.6974293+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:57.6974293+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:20:02.8762775+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:20:02.8762775+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:20:03.4050012+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:20:03.4050012+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:20:03.9938473+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:20:03.9938473+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3b78aa87-8fab-4ee7-91dd-e86b00101c88'))) <> CONVERT(VARBINARY(MAX), N'3b78aa87-8fab-4ee7-91dd-e86b00101c88')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_CALL_FAILED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_CALL_FAILED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolFailed'))) <> CONVERT(VARBINARY(MAX), N'ToolFailed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'acd7ff7a26793ce955139558149b8d63a5c12650d7ceb8bff2a2ed1ac2a2b751'))) <> CONVERT(VARBINARY(MAX), N'acd7ff7a26793ce955139558149b8d63a5c12650d7ceb8bff2a2ed1ac2a2b751')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548'))) <> CONVERT(VARBINARY(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c00f9b33-11c5-4c00-a159-3cf947535548',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:26.3498617+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:20:03.9938473+00:00', 127)),
        InputSha256 = N'acd7ff7a26793ce955139558149b8d63a5c12650d7ceb8bff2a2ed1ac2a2b751',
        OutputCharacters = 50,
        ToolCallCount = 3,
        ErrorCode = N''
    WHERE ID = N'3b78aa87-8fab-4ee7-91dd-e86b00101c88' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'3b78aa87-8fab-4ee7-91dd-e86b00101c88';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'1f1cb179-9b0e-5003-8ddf-38c8a7552e58', N'3b78aa87-8fab-4ee7-91dd-e86b00101c88', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:51.2349492+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:51.8294464+00:00', 127)), N'');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'a13e0e34-47e0-56cc-b10d-8826c24a7b13', N'3b78aa87-8fab-4ee7-91dd-e86b00101c88', 1, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:57.3214192+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:57.6974293+00:00', 127)), N'');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'284fca24-98e6-5975-bc1b-ba4633409fd9', N'3b78aa87-8fab-4ee7-91dd-e86b00101c88', 2, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:20:02.8762775+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:20:03.4050012+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'3b78aa87-8fab-4ee7-91dd-e86b00101c88')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'3b78aa87-8fab-4ee7-91dd-e86b00101c88');

    -- Agent run audit e4186421-0230-45b1-9db0-937d39a3bda4
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:21:22.7950813+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:21:22.7950813+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:21:32.0904931+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:21:32.0904931+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:21:32.5609941+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:21:32.5609941+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:21:36.7529107+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:21:36.7529107+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:21:37.3123085+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:21:37.3123085+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:21:42.8481413+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:21:42.8481413+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'acd7ff7a26793ce955139558149b8d63a5c12650d7ceb8bff2a2ed1ac2a2b751'))) <> CONVERT(VARBINARY(MAX), N'acd7ff7a26793ce955139558149b8d63a5c12650d7ceb8bff2a2ed1ac2a2b751')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548'))) <> CONVERT(VARBINARY(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'e4186421-0230-45b1-9db0-937d39a3bda4'))) <> CONVERT(VARBINARY(MAX), N'e4186421-0230-45b1-9db0-937d39a3bda4')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c00f9b33-11c5-4c00-a159-3cf947535548',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:21:22.7950813+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:21:42.8481413+00:00', 127)),
        InputSha256 = N'acd7ff7a26793ce955139558149b8d63a5c12650d7ceb8bff2a2ed1ac2a2b751',
        OutputCharacters = 369,
        ToolCallCount = 2,
        ErrorCode = N''
    WHERE ID = N'e4186421-0230-45b1-9db0-937d39a3bda4' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'e4186421-0230-45b1-9db0-937d39a3bda4';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'36af1acc-597a-5f7c-84ef-d0f2f6c7697c', N'e4186421-0230-45b1-9db0-937d39a3bda4', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:21:32.0904931+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:21:32.5609941+00:00', 127)), N'');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'277cd246-a34e-5efc-8982-82e02f36056a', N'e4186421-0230-45b1-9db0-937d39a3bda4', 1, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:21:36.7529107+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:21:37.3123085+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'e4186421-0230-45b1-9db0-937d39a3bda4')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'e4186421-0230-45b1-9db0-937d39a3bda4');

    -- Agent run audit c0905951-1169-4808-876d-399f4036972f
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'07929cd0725095f90dfd0cfffc6f82cb8e1432d27b2d3073a4ceb3b0c5073f85'))) <> CONVERT(VARBINARY(MAX), N'07929cd0725095f90dfd0cfffc6f82cb8e1432d27b2d3073a4ceb3b0c5073f85')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:42:48.0037126+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:42:48.0037126+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:42:54.4714422+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:42:54.4714422+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:42:55.0158694+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:42:55.0158694+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:42:59.5862264+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:42:59.5862264+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:43:00.1471104+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:43:00.1471104+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:43:08.621947+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:43:08.621947+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548'))) <> CONVERT(VARBINARY(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c0905951-1169-4808-876d-399f4036972f'))) <> CONVERT(VARBINARY(MAX), N'c0905951-1169-4808-876d-399f4036972f')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c00f9b33-11c5-4c00-a159-3cf947535548',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:42:48.0037126+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:43:08.621947+00:00', 127)),
        InputSha256 = N'07929cd0725095f90dfd0cfffc6f82cb8e1432d27b2d3073a4ceb3b0c5073f85',
        OutputCharacters = 269,
        ToolCallCount = 2,
        ErrorCode = N''
    WHERE ID = N'c0905951-1169-4808-876d-399f4036972f' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'c0905951-1169-4808-876d-399f4036972f';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'f14e12ec-efff-5848-9437-e26a73ab29b8', N'c0905951-1169-4808-876d-399f4036972f', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:42:54.4714422+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:42:55.0158694+00:00', 127)), N'');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'b7b88286-7b3d-58f2-8a6c-65ef6194093d', N'c0905951-1169-4808-876d-399f4036972f', 1, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:42:59.5862264+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:43:00.1471104+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'c0905951-1169-4808-876d-399f4036972f')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'c0905951-1169-4808-876d-399f4036972f');

    -- Agent run audit 768d4a4e-ec4b-4c11-8a93-d3ed29235aae
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:44:38.9958006+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:44:38.9958006+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:44:49.1838662+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:44:49.1838662+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'768d4a4e-ec4b-4c11-8a93-d3ed29235aae'))) <> CONVERT(VARBINARY(MAX), N'768d4a4e-ec4b-4c11-8a93-d3ed29235aae')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548'))) <> CONVERT(VARBINARY(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f57911f2181c9b13bd3acebb5f09ba148a29e3f76636bb388590b624efc07c30'))) <> CONVERT(VARBINARY(MAX), N'f57911f2181c9b13bd3acebb5f09ba148a29e3f76636bb388590b624efc07c30')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c00f9b33-11c5-4c00-a159-3cf947535548',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:44:38.9958006+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:44:49.1838662+00:00', 127)),
        InputSha256 = N'f57911f2181c9b13bd3acebb5f09ba148a29e3f76636bb388590b624efc07c30',
        OutputCharacters = 432,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'768d4a4e-ec4b-4c11-8a93-d3ed29235aae' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'768d4a4e-ec4b-4c11-8a93-d3ed29235aae';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'768d4a4e-ec4b-4c11-8a93-d3ed29235aae')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'768d4a4e-ec4b-4c11-8a93-d3ed29235aae');

    -- Agent run audit 820b9f21-e829-4b5c-b65e-740c9c5430c3
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:45:57.8844824+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:45:57.8844824+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:46:04.9654368+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:46:04.9654368+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'49dfdb94d4a0ba7946a35e32eebc88ab53ca0dde982c7e78947be076e1a88784'))) <> CONVERT(VARBINARY(MAX), N'49dfdb94d4a0ba7946a35e32eebc88ab53ca0dde982c7e78947be076e1a88784')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'820b9f21-e829-4b5c-b65e-740c9c5430c3'))) <> CONVERT(VARBINARY(MAX), N'820b9f21-e829-4b5c-b65e-740c9c5430c3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548'))) <> CONVERT(VARBINARY(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c00f9b33-11c5-4c00-a159-3cf947535548',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:45:57.8844824+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:46:04.9654368+00:00', 127)),
        InputSha256 = N'49dfdb94d4a0ba7946a35e32eebc88ab53ca0dde982c7e78947be076e1a88784',
        OutputCharacters = 372,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'820b9f21-e829-4b5c-b65e-740c9c5430c3' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'820b9f21-e829-4b5c-b65e-740c9c5430c3';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'820b9f21-e829-4b5c-b65e-740c9c5430c3')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'820b9f21-e829-4b5c-b65e-740c9c5430c3');

    -- Agent run audit 3ce0393b-4351-4a9d-8353-77b51c991e8b
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:46:33.8825749+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:46:33.8825749+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:46:42.1816507+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:46:42.1816507+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3ce0393b-4351-4a9d-8353-77b51c991e8b'))) <> CONVERT(VARBINARY(MAX), N'3ce0393b-4351-4a9d-8353-77b51c991e8b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6e5a5e2bedb0992f77326f7536ad21712795d2da35a81fc8ae3633d50fe5c409'))) <> CONVERT(VARBINARY(MAX), N'6e5a5e2bedb0992f77326f7536ad21712795d2da35a81fc8ae3633d50fe5c409')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548'))) <> CONVERT(VARBINARY(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c00f9b33-11c5-4c00-a159-3cf947535548',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:46:33.8825749+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:46:42.1816507+00:00', 127)),
        InputSha256 = N'6e5a5e2bedb0992f77326f7536ad21712795d2da35a81fc8ae3633d50fe5c409',
        OutputCharacters = 374,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'3ce0393b-4351-4a9d-8353-77b51c991e8b' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'3ce0393b-4351-4a9d-8353-77b51c991e8b';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'3ce0393b-4351-4a9d-8353-77b51c991e8b')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'3ce0393b-4351-4a9d-8353-77b51c991e8b');

    -- Agent run audit aa683164-26fb-4848-9aed-53b7043ca3ff
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:47:04.0428596+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:47:04.0428596+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:47:12.2013468+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:47:12.2013468+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:47:12.8225246+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:47:12.8225246+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:47:17.6718628+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:47:17.6718628+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4ee5590dcb7b5d787e827ddb792e8bf48f16eb2fb0f73c99803eceb2646881c8'))) <> CONVERT(VARBINARY(MAX), N'4ee5590dcb7b5d787e827ddb792e8bf48f16eb2fb0f73c99803eceb2646881c8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'aa683164-26fb-4848-9aed-53b7043ca3ff'))) <> CONVERT(VARBINARY(MAX), N'aa683164-26fb-4848-9aed-53b7043ca3ff')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548'))) <> CONVERT(VARBINARY(MAX), N'c00f9b33-11c5-4c00-a159-3cf947535548')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c00f9b33-11c5-4c00-a159-3cf947535548',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:47:04.0428596+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:47:17.6718628+00:00', 127)),
        InputSha256 = N'4ee5590dcb7b5d787e827ddb792e8bf48f16eb2fb0f73c99803eceb2646881c8',
        OutputCharacters = 289,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'aa683164-26fb-4848-9aed-53b7043ca3ff' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'aa683164-26fb-4848-9aed-53b7043ca3ff';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'ab0e2e13-2ef4-599f-ae99-211412fdb3f4', N'aa683164-26fb-4848-9aed-53b7043ca3ff', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:47:12.2013468+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:47:12.8225246+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'aa683164-26fb-4848-9aed-53b7043ca3ff')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'aa683164-26fb-4848-9aed-53b7043ca3ff');

    -- Agent run audit 06bb0538-045d-47ca-be1b-d0e072617c99
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'06bb0538-045d-47ca-be1b-d0e072617c99'))) <> CONVERT(VARBINARY(MAX), N'06bb0538-045d-47ca-be1b-d0e072617c99')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T02:47:24.2725147+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T02:47:24.2725147+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T02:47:28.7290834+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T02:47:28.7290834+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3'))) <> CONVERT(VARBINARY(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T02:47:24.2725147+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T02:47:28.7290834+00:00', 127)),
        InputSha256 = N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'06bb0538-045d-47ca-be1b-d0e072617c99' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'06bb0538-045d-47ca-be1b-d0e072617c99';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'06bb0538-045d-47ca-be1b-d0e072617c99')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'06bb0538-045d-47ca-be1b-d0e072617c99');

    -- Agent run audit 64884b17-2f43-4b5c-bea2-e8b5de42887b
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T02:56:12.0238453+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T02:56:12.0238453+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T02:56:17.1352093+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T02:56:17.1352093+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3'))) <> CONVERT(VARBINARY(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'64884b17-2f43-4b5c-bea2-e8b5de42887b'))) <> CONVERT(VARBINARY(MAX), N'64884b17-2f43-4b5c-bea2-e8b5de42887b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T02:56:12.0238453+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T02:56:17.1352093+00:00', 127)),
        InputSha256 = N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'64884b17-2f43-4b5c-bea2-e8b5de42887b' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'64884b17-2f43-4b5c-bea2-e8b5de42887b';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'64884b17-2f43-4b5c-bea2-e8b5de42887b')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'64884b17-2f43-4b5c-bea2-e8b5de42887b');

    -- Agent run audit 923b4a25-1a17-45f2-8632-2a387537b339
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:02:36.1432088+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:02:36.1432088+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:02:40.1993548+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:02:40.1993548+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3'))) <> CONVERT(VARBINARY(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'923b4a25-1a17-45f2-8632-2a387537b339'))) <> CONVERT(VARBINARY(MAX), N'923b4a25-1a17-45f2-8632-2a387537b339')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:02:36.1432088+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:02:40.1993548+00:00', 127)),
        InputSha256 = N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'923b4a25-1a17-45f2-8632-2a387537b339' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'923b4a25-1a17-45f2-8632-2a387537b339';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'923b4a25-1a17-45f2-8632-2a387537b339')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'923b4a25-1a17-45f2-8632-2a387537b339');

    -- Agent run audit 791cad3a-f4a2-4f1f-8006-f38418331567
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:05:44.569597+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:05:44.569597+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:05:48.1093557+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:05:48.1093557+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3'))) <> CONVERT(VARBINARY(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'791cad3a-f4a2-4f1f-8006-f38418331567'))) <> CONVERT(VARBINARY(MAX), N'791cad3a-f4a2-4f1f-8006-f38418331567')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:05:44.569597+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:05:48.1093557+00:00', 127)),
        InputSha256 = N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'791cad3a-f4a2-4f1f-8006-f38418331567' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'791cad3a-f4a2-4f1f-8006-f38418331567';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'791cad3a-f4a2-4f1f-8006-f38418331567')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'791cad3a-f4a2-4f1f-8006-f38418331567');

    -- Agent run audit fe592326-200b-4fe2-9b42-1389da4f031c
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:06:21.1176295+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:06:21.1176295+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:06:27.3412558+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:06:27.3412558+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3'))) <> CONVERT(VARBINARY(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'fe592326-200b-4fe2-9b42-1389da4f031c'))) <> CONVERT(VARBINARY(MAX), N'fe592326-200b-4fe2-9b42-1389da4f031c')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:06:21.1176295+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:06:27.3412558+00:00', 127)),
        InputSha256 = N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'fe592326-200b-4fe2-9b42-1389da4f031c' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'fe592326-200b-4fe2-9b42-1389da4f031c';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'fe592326-200b-4fe2-9b42-1389da4f031c')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'fe592326-200b-4fe2-9b42-1389da4f031c');

    -- Agent run audit 64e7f98c-ec09-47ee-8f1a-d99d76a6bd54
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:08:32.3972398+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:08:32.3972398+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:08:36.5795894+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:08:36.5795894+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'64e7f98c-ec09-47ee-8f1a-d99d76a6bd54'))) <> CONVERT(VARBINARY(MAX), N'64e7f98c-ec09-47ee-8f1a-d99d76a6bd54')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e'))) <> CONVERT(VARBINARY(MAX), N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:08:32.3972398+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:08:36.5795894+00:00', 127)),
        InputSha256 = N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'64e7f98c-ec09-47ee-8f1a-d99d76a6bd54' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'64e7f98c-ec09-47ee-8f1a-d99d76a6bd54';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'64e7f98c-ec09-47ee-8f1a-d99d76a6bd54')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'64e7f98c-ec09-47ee-8f1a-d99d76a6bd54');

    -- Agent run audit 7f1f5ee6-558e-471e-a6ac-02831c587fae
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:14:33.649074+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:14:33.649074+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:14:38.6553734+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:14:38.6553734+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'7f1f5ee6-558e-471e-a6ac-02831c587fae'))) <> CONVERT(VARBINARY(MAX), N'7f1f5ee6-558e-471e-a6ac-02831c587fae')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e'))) <> CONVERT(VARBINARY(MAX), N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:14:33.649074+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:14:38.6553734+00:00', 127)),
        InputSha256 = N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'7f1f5ee6-558e-471e-a6ac-02831c587fae' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'7f1f5ee6-558e-471e-a6ac-02831c587fae';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'7f1f5ee6-558e-471e-a6ac-02831c587fae')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'7f1f5ee6-558e-471e-a6ac-02831c587fae');

    -- Agent run audit 36a1f6a1-b3d3-4754-ab90-d88105583e41
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:21:00.5211885+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:21:00.5211885+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:21:04.0897298+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:21:04.0897298+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'36a1f6a1-b3d3-4754-ab90-d88105583e41'))) <> CONVERT(VARBINARY(MAX), N'36a1f6a1-b3d3-4754-ab90-d88105583e41')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c5bf18cd76aab103ca77019f3195e3d94b96926d85d50b8a09420824a79e9522'))) <> CONVERT(VARBINARY(MAX), N'c5bf18cd76aab103ca77019f3195e3d94b96926d85d50b8a09420824a79e9522')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:21:00.5211885+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:21:04.0897298+00:00', 127)),
        InputSha256 = N'c5bf18cd76aab103ca77019f3195e3d94b96926d85d50b8a09420824a79e9522',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'36a1f6a1-b3d3-4754-ab90-d88105583e41' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'36a1f6a1-b3d3-4754-ab90-d88105583e41';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'36a1f6a1-b3d3-4754-ab90-d88105583e41')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'36a1f6a1-b3d3-4754-ab90-d88105583e41');

    -- Agent run audit 095ff861-2500-4925-ab6c-efeabac04ea2
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'095ff861-2500-4925-ab6c-efeabac04ea2'))) <> CONVERT(VARBINARY(MAX), N'095ff861-2500-4925-ab6c-efeabac04ea2')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:27:01.0274833+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:27:01.0274833+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:27:04.0286219+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:27:04.0286219+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ceaca1ba32f7886d80afd2411ae2b7fc9d9656f793084d66c2d6b71d889c33bc'))) <> CONVERT(VARBINARY(MAX), N'ceaca1ba32f7886d80afd2411ae2b7fc9d9656f793084d66c2d6b71d889c33bc')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:27:01.0274833+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:27:04.0286219+00:00', 127)),
        InputSha256 = N'ceaca1ba32f7886d80afd2411ae2b7fc9d9656f793084d66c2d6b71d889c33bc',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'095ff861-2500-4925-ab6c-efeabac04ea2' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'095ff861-2500-4925-ab6c-efeabac04ea2';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'095ff861-2500-4925-ab6c-efeabac04ea2')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'095ff861-2500-4925-ab6c-efeabac04ea2');

    -- Agent run audit e7157763-2115-475d-8fdf-8be49bff74c8
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:30:12.9297897+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:30:12.9297897+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:30:16.8588951+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:30:16.8588951+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3'))) <> CONVERT(VARBINARY(MAX), N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'e7157763-2115-475d-8fdf-8be49bff74c8'))) <> CONVERT(VARBINARY(MAX), N'e7157763-2115-475d-8fdf-8be49bff74c8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:30:12.9297897+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:30:16.8588951+00:00', 127)),
        InputSha256 = N'4dbabc2c79edb578b5a30d2d06f55214342896f77dfe3978dbf0f0d0fb59aec3',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'e7157763-2115-475d-8fdf-8be49bff74c8' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'e7157763-2115-475d-8fdf-8be49bff74c8';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'e7157763-2115-475d-8fdf-8be49bff74c8')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'e7157763-2115-475d-8fdf-8be49bff74c8');

    -- Agent run audit 8ab3240d-c51e-414a-b555-818c319e92e5
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:48:01.7640492+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:48:01.7640492+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:48:05.9399841+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:48:05.9399841+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'8ab3240d-c51e-414a-b555-818c319e92e5'))) <> CONVERT(VARBINARY(MAX), N'8ab3240d-c51e-414a-b555-818c319e92e5')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c5bf18cd76aab103ca77019f3195e3d94b96926d85d50b8a09420824a79e9522'))) <> CONVERT(VARBINARY(MAX), N'c5bf18cd76aab103ca77019f3195e3d94b96926d85d50b8a09420824a79e9522')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:48:01.7640492+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:48:05.9399841+00:00', 127)),
        InputSha256 = N'c5bf18cd76aab103ca77019f3195e3d94b96926d85d50b8a09420824a79e9522',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'8ab3240d-c51e-414a-b555-818c319e92e5' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'8ab3240d-c51e-414a-b555-818c319e92e5';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'8ab3240d-c51e-414a-b555-818c319e92e5')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'8ab3240d-c51e-414a-b555-818c319e92e5');

    -- Agent run audit 421a1a8e-4694-44c7-b95e-05de020e7d72
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:48:38.3153645+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:48:38.3153645+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:48:53.0893516+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:48:53.0893516+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'421a1a8e-4694-44c7-b95e-05de020e7d72'))) <> CONVERT(VARBINARY(MAX), N'421a1a8e-4694-44c7-b95e-05de020e7d72')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e'))) <> CONVERT(VARBINARY(MAX), N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:48:38.3153645+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:48:53.0893516+00:00', 127)),
        InputSha256 = N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e',
        OutputCharacters = 21,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'421a1a8e-4694-44c7-b95e-05de020e7d72' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'421a1a8e-4694-44c7-b95e-05de020e7d72';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'421a1a8e-4694-44c7-b95e-05de020e7d72')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'421a1a8e-4694-44c7-b95e-05de020e7d72');

    -- Agent run audit aa7759e8-43d5-4223-82ae-11dd6c9bfaba
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:56:57.7557906+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:56:57.7557906+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-05T03:57:03.7070707+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-05T03:57:03.7070707+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_APPROVAL_REQUIRED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_APPROVAL_REQUIRED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'WaitingForApproval'))) <> CONVERT(VARBINARY(MAX), N'WaitingForApproval')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'aa7759e8-43d5-4223-82ae-11dd6c9bfaba'))) <> CONVERT(VARBINARY(MAX), N'aa7759e8-43d5-4223-82ae-11dd6c9bfaba')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e'))) <> CONVERT(VARBINARY(MAX), N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'WaitingForApproval',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:56:57.7557906+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-05T03:57:03.7070707+00:00', 127)),
        InputSha256 = N'ec1dacfd0f3804a85b9418b17f021850b8d2481507bdf30bb090bee810d8185e',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MCP_TOOL_APPROVAL_REQUIRED'
    WHERE ID = N'aa7759e8-43d5-4223-82ae-11dd6c9bfaba' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'aa7759e8-43d5-4223-82ae-11dd6c9bfaba';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'aa7759e8-43d5-4223-82ae-11dd6c9bfaba')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'aa7759e8-43d5-4223-82ae-11dd6c9bfaba');

    -- Agent run audit 0803eea3-04e4-403d-95b2-950215d0f660
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0803eea3-04e4-403d-95b2-950215d0f660'))) <> CONVERT(VARBINARY(MAX), N'0803eea3-04e4-403d-95b2-950215d0f660')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:46:06.6093084+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:46:06.6093084+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:46:15.9895627+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:46:15.9895627+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:46:16.1695214+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:46:16.1695214+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:46:24.2131921+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:46:24.2131921+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:46:24.3411152+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:46:24.3411152+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:46:33.4393535+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:46:33.4393535+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:46:33.5790991+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:46:33.5790991+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:46:38.453789+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:46:38.453789+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:46:38.5827097+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:46:38.5827097+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:46:39.0442987+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:46:39.0442987+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Failed'))) <> CONVERT(VARBINARY(MAX), N'Failed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_CALL_FAILED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_CALL_FAILED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolFailed'))) <> CONVERT(VARBINARY(MAX), N'ToolFailed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'Failed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:46:06.6093084+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:46:39.0442987+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 36,
        ToolCallCount = 4,
        ErrorCode = N'MCP_TOOL_CALL_FAILED'
    WHERE ID = N'0803eea3-04e4-403d-95b2-950215d0f660' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'0803eea3-04e4-403d-95b2-950215d0f660';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'99bd3c85-dc64-56a1-b8da-3e6a2959cebe', N'0803eea3-04e4-403d-95b2-950215d0f660', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:46:15.9895627+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:46:16.1695214+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'47628769-8f1e-531f-8172-49726ff76f6b', N'0803eea3-04e4-403d-95b2-950215d0f660', 1, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:46:24.2131921+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:46:24.3411152+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'c92a882c-777e-5144-a7ae-aa26b236b22a', N'0803eea3-04e4-403d-95b2-950215d0f660', 2, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:46:33.4393535+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:46:33.5790991+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'e437a846-1765-56f1-a8ba-9ce3042da2aa', N'0803eea3-04e4-403d-95b2-950215d0f660', 3, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:46:38.453789+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:46:38.5827097+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'0803eea3-04e4-403d-95b2-950215d0f660')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'0803eea3-04e4-403d-95b2-950215d0f660');

    -- Agent run audit b233eafd-fef4-48ca-a0ef-85170f68b83d
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:52:00.2201912+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:52:00.2201912+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:52:09.3396804+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:52:09.3396804+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:52:09.4517017+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:52:09.4517017+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:52:15.7337332+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:52:15.7337332+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:52:15.8843166+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:52:15.8843166+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:52:20.1958836+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:52:20.1958836+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:52:20.3334994+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:52:20.3334994+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:52:23.7595599+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:52:23.7595599+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:52:24.005896+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:52:24.005896+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T16:52:24.5052535+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T16:52:24.5052535+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Failed'))) <> CONVERT(VARBINARY(MAX), N'Failed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_CALL_FAILED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_CALL_FAILED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolFailed'))) <> CONVERT(VARBINARY(MAX), N'ToolFailed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b233eafd-fef4-48ca-a0ef-85170f68b83d'))) <> CONVERT(VARBINARY(MAX), N'b233eafd-fef4-48ca-a0ef-85170f68b83d')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'Failed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:52:00.2201912+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:52:24.5052535+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 47,
        ToolCallCount = 4,
        ErrorCode = N'MCP_TOOL_CALL_FAILED'
    WHERE ID = N'b233eafd-fef4-48ca-a0ef-85170f68b83d' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'b233eafd-fef4-48ca-a0ef-85170f68b83d';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'62936a67-3b93-5a03-be5e-987eadb00041', N'b233eafd-fef4-48ca-a0ef-85170f68b83d', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:52:09.3396804+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:52:09.4517017+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'74149d19-fa45-52f5-bb6e-75f9864e624b', N'b233eafd-fef4-48ca-a0ef-85170f68b83d', 1, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:52:15.7337332+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:52:15.8843166+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'13311608-f702-5892-b4b2-4b0b3b813644', N'b233eafd-fef4-48ca-a0ef-85170f68b83d', 2, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:52:20.1958836+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:52:20.3334994+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'1984c382-bd50-52e9-89f0-fa63bd6b3a6e', N'b233eafd-fef4-48ca-a0ef-85170f68b83d', 3, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:52:23.7595599+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T16:52:24.005896+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'b233eafd-fef4-48ca-a0ef-85170f68b83d')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'b233eafd-fef4-48ca-a0ef-85170f68b83d');

    -- Agent run audit 329dcb16-068a-4ceb-8f72-39eeea7bf68b
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T17:04:35.924642+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T17:04:35.924642+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T17:04:46.3427971+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T17:04:46.3427971+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T17:04:47.4997791+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T17:04:47.4997791+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-09T17:04:49.8214894+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-09T17:04:49.8214894+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'329dcb16-068a-4ceb-8f72-39eeea7bf68b'))) <> CONVERT(VARBINARY(MAX), N'329dcb16-068a-4ceb-8f72-39eeea7bf68b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T17:04:35.924642+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T17:04:49.8214894+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 65,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'329dcb16-068a-4ceb-8f72-39eeea7bf68b' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'329dcb16-068a-4ceb-8f72-39eeea7bf68b';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'49fac2eb-fbf4-5c65-b21c-f70a27f5f806', N'329dcb16-068a-4ceb-8f72-39eeea7bf68b', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T17:04:46.3427971+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-09T17:04:47.4997791+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'329dcb16-068a-4ceb-8f72-39eeea7bf68b')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'329dcb16-068a-4ceb-8f72-39eeea7bf68b');

    -- Agent run audit 0b1ce3d2-e6fb-4672-9b0f-74b417adbd42
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0b1ce3d2-e6fb-4672-9b0f-74b417adbd42'))) <> CONVERT(VARBINARY(MAX), N'0b1ce3d2-e6fb-4672-9b0f-74b417adbd42')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T00:32:16.3109712+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T00:32:16.3109712+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T00:32:21.2584003+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T00:32:21.2584003+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c10e9e3b4c38b18e92453b9d19c1137fef0c119a3ca23318a857c22a9bb6e17d'))) <> CONVERT(VARBINARY(MAX), N'c10e9e3b4c38b18e92453b9d19c1137fef0c119a3ca23318a857c22a9bb6e17d')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T00:32:16.3109712+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T00:32:21.2584003+00:00', 127)),
        InputSha256 = N'c10e9e3b4c38b18e92453b9d19c1137fef0c119a3ca23318a857c22a9bb6e17d',
        OutputCharacters = 127,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'0b1ce3d2-e6fb-4672-9b0f-74b417adbd42' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'0b1ce3d2-e6fb-4672-9b0f-74b417adbd42';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'0b1ce3d2-e6fb-4672-9b0f-74b417adbd42')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'0b1ce3d2-e6fb-4672-9b0f-74b417adbd42');

    -- Agent run audit c6540ca3-ad0e-4fda-8b1d-2235b9a7ada3
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T00:32:34.2076357+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T00:32:34.2076357+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T00:32:43.2032339+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T00:32:43.2032339+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T00:32:43.6810996+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T00:32:43.6810996+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T00:32:46.4258729+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T00:32:46.4258729+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c6540ca3-ad0e-4fda-8b1d-2235b9a7ada3'))) <> CONVERT(VARBINARY(MAX), N'c6540ca3-ad0e-4fda-8b1d-2235b9a7ada3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T00:32:34.2076357+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T00:32:46.4258729+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 71,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'c6540ca3-ad0e-4fda-8b1d-2235b9a7ada3' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'c6540ca3-ad0e-4fda-8b1d-2235b9a7ada3';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'966aa040-5085-5ba5-97ce-92ae8769d3c4', N'c6540ca3-ad0e-4fda-8b1d-2235b9a7ada3', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T00:32:43.2032339+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T00:32:43.6810996+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'c6540ca3-ad0e-4fda-8b1d-2235b9a7ada3')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'c6540ca3-ad0e-4fda-8b1d-2235b9a7ada3');

    -- Agent run audit 5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:46:38.6208059+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:46:38.6208059+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:46:49.2303698+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:46:49.2303698+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:46:49.7536583+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:46:49.7536583+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:46:54.1477631+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:46:54.1477631+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:46:54.1478965+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:46:54.1478965+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:46:58.571424+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:46:58.571424+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:46:58.5717951+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:46:58.5717951+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:47:02.616361+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:47:02.616361+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:47:02.6164529+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:47:02.6164529+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:47:10.9340433+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:47:10.9340433+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:47:10.9341265+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:47:10.9341265+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:47:10.9365168+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:47:10.9365168+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36'))) <> CONVERT(VARBINARY(MAX), N'4951b525-75a8-42f3-86f1-9ddc27e94f36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117'))) <> CONVERT(VARBINARY(MAX), N'5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98'))) <> CONVERT(VARBINARY(MAX), N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED'))) <> CONVERT(VARBINARY(MAX), N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Failed'))) <> CONVERT(VARBINARY(MAX), N'Failed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolBlocked'))) <> CONVERT(VARBINARY(MAX), N'ToolBlocked')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e'))) <> CONVERT(VARBINARY(MAX), N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',
        AgentCode = N'main-agent',
        Status = N'Failed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:46:38.6208059+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:47:10.9365168+00:00', 127)),
        InputSha256 = N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98',
        OutputCharacters = 0,
        ToolCallCount = 5,
        ErrorCode = N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED'
    WHERE ID = N'5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'caf49308-529f-58c4-a71a-b1a116c9976e', N'5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117', 0, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:46:49.2303698+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:46:49.7536583+00:00', 127)), N'');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'5b65e603-df54-50b9-a04d-da6acabbe8da', N'5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117', 1, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolBlocked', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:46:54.1477631+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:46:54.1478965+00:00', 127)), N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'd4467149-721e-58f7-a471-427cc563cee3', N'5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117', 2, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolBlocked', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:46:58.571424+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:46:58.5717951+00:00', 127)), N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'8c18a9d9-4b65-5dbf-aed8-7e077dabd922', N'5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117', 3, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolBlocked', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:47:02.616361+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:47:02.6164529+00:00', 127)), N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'16214de0-8850-5a1c-912a-aef106ccdb0c', N'5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117', 4, N'4951b525-75a8-42f3-86f1-9ddc27e94f36', N'query_business_data', N'ReadOnly', N'ToolBlocked', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:47:10.9340433+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:47:10.9341265+00:00', 127)), N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'5c51aef8-25a7-4f3f-b6e0-d5f9f1ca9117');

    -- Agent run audit c35222f2-f6f1-485c-98ea-9770ca729e40
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:53:02.7916417+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:53:02.7916417+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:53:09.7660177+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:53:09.7660177+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:53:10.3661103+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:53:10.3661103+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:53:15.1239008+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:53:15.1239008+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98'))) <> CONVERT(VARBINARY(MAX), N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c35222f2-f6f1-485c-98ea-9770ca729e40'))) <> CONVERT(VARBINARY(MAX), N'c35222f2-f6f1-485c-98ea-9770ca729e40')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:53:02.7916417+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:53:15.1239008+00:00', 127)),
        InputSha256 = N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98',
        OutputCharacters = 135,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'c35222f2-f6f1-485c-98ea-9770ca729e40' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'c35222f2-f6f1-485c-98ea-9770ca729e40';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'9980399f-5835-59d1-ac0f-0906007e9325', N'c35222f2-f6f1-485c-98ea-9770ca729e40', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:53:09.7660177+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:53:10.3661103+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'c35222f2-f6f1-485c-98ea-9770ca729e40')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'c35222f2-f6f1-485c-98ea-9770ca729e40');

    -- Agent run audit 9e2b2001-3424-4bc9-96c6-463f3d8dc69b
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T12:51:06.260364+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T12:51:06.260364+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T12:51:13.6441575+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T12:51:13.6441575+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T12:51:14.4189049+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T12:51:14.4189049+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T12:51:18.1942377+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T12:51:18.1942377+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98'))) <> CONVERT(VARBINARY(MAX), N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9e2b2001-3424-4bc9-96c6-463f3d8dc69b'))) <> CONVERT(VARBINARY(MAX), N'9e2b2001-3424-4bc9-96c6-463f3d8dc69b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T12:51:06.260364+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T12:51:18.1942377+00:00', 127)),
        InputSha256 = N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98',
        OutputCharacters = 139,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'9e2b2001-3424-4bc9-96c6-463f3d8dc69b' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'9e2b2001-3424-4bc9-96c6-463f3d8dc69b';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'4f5f449b-03ea-547c-acf3-31d7006ed5cb', N'9e2b2001-3424-4bc9-96c6-463f3d8dc69b', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T12:51:13.6441575+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T12:51:14.4189049+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'9e2b2001-3424-4bc9-96c6-463f3d8dc69b')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'9e2b2001-3424-4bc9-96c6-463f3d8dc69b');

    -- Agent run audit 82336501-1f75-4376-865c-ec1814548a05
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T15:30:45.3787286+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T15:30:45.3787286+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T15:30:54.411644+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T15:30:54.411644+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T15:30:55.8127792+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T15:30:55.8127792+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T15:30:58.9019117+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T15:30:58.9019117+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'82336501-1f75-4376-865c-ec1814548a05'))) <> CONVERT(VARBINARY(MAX), N'82336501-1f75-4376-865c-ec1814548a05')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98'))) <> CONVERT(VARBINARY(MAX), N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T15:30:45.3787286+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T15:30:58.9019117+00:00', 127)),
        InputSha256 = N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98',
        OutputCharacters = 106,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'82336501-1f75-4376-865c-ec1814548a05' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'82336501-1f75-4376-865c-ec1814548a05';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'3d3771d7-ef17-5642-aac5-40009a5d5c03', N'82336501-1f75-4376-865c-ec1814548a05', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T15:30:54.411644+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T15:30:55.8127792+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'82336501-1f75-4376-865c-ec1814548a05')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'82336501-1f75-4376-865c-ec1814548a05');

    -- Agent run audit 5b9ce26b-097e-40ff-b01d-d164fc3b9e87
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T15:53:17.0159219+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T15:53:17.0159219+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T15:53:26.1266515+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T15:53:26.1266515+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T15:53:26.9965961+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T15:53:26.9965961+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T15:53:29.4185064+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T15:53:29.4185064+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5b9ce26b-097e-40ff-b01d-d164fc3b9e87'))) <> CONVERT(VARBINARY(MAX), N'5b9ce26b-097e-40ff-b01d-d164fc3b9e87')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98'))) <> CONVERT(VARBINARY(MAX), N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T15:53:17.0159219+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T15:53:29.4185064+00:00', 127)),
        InputSha256 = N'832c96a3d4b5952c727375e6dcbcc96f24904b807df1c67386bc8d28c4442c98',
        OutputCharacters = 106,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'5b9ce26b-097e-40ff-b01d-d164fc3b9e87' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'5b9ce26b-097e-40ff-b01d-d164fc3b9e87';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'53c5343f-4acc-5d38-9c0d-66679a85fb72', N'5b9ce26b-097e-40ff-b01d-d164fc3b9e87', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T15:53:26.1266515+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T15:53:26.9965961+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'5b9ce26b-097e-40ff-b01d-d164fc3b9e87')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'5b9ce26b-097e-40ff-b01d-d164fc3b9e87');

    -- Agent run audit 091a30da-c4c2-4398-9d21-804c28acf699
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'091a30da-c4c2-4398-9d21-804c28acf699'))) <> CONVERT(VARBINARY(MAX), N'091a30da-c4c2-4398-9d21-804c28acf699')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0ab6f1e0bf216a0db52a4a5a247f95cba6f51496de7a24dfd01f3985dfcf6085'))) <> CONVERT(VARBINARY(MAX), N'0ab6f1e0bf216a0db52a4a5a247f95cba6f51496de7a24dfd01f3985dfcf6085')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T17:17:41.2401764+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T17:17:41.2401764+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T17:17:42.0326388+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T17:17:42.0326388+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Failed'))) <> CONVERT(VARBINARY(MAX), N'Failed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MODEL_CREDENTIAL_MISSING'))) <> CONVERT(VARBINARY(MAX), N'MODEL_CREDENTIAL_MISSING')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Failed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T17:17:41.2401764+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T17:17:42.0326388+00:00', 127)),
        InputSha256 = N'0ab6f1e0bf216a0db52a4a5a247f95cba6f51496de7a24dfd01f3985dfcf6085',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MODEL_CREDENTIAL_MISSING'
    WHERE ID = N'091a30da-c4c2-4398-9d21-804c28acf699' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'091a30da-c4c2-4398-9d21-804c28acf699';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'091a30da-c4c2-4398-9d21-804c28acf699')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'091a30da-c4c2-4398-9d21-804c28acf699');

    -- Agent run audit 4cdc8517-019a-482f-b838-cb12a1faabf4
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T17:18:28.6791861+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T17:18:28.6791861+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T17:18:29.3172158+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T17:18:29.3172158+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4cdc8517-019a-482f-b838-cb12a1faabf4'))) <> CONVERT(VARBINARY(MAX), N'4cdc8517-019a-482f-b838-cb12a1faabf4')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9abc8fcb478b8ee86c4a36cf0801a35ad79b25b6521e552f66c8ec383ac178da'))) <> CONVERT(VARBINARY(MAX), N'9abc8fcb478b8ee86c4a36cf0801a35ad79b25b6521e552f66c8ec383ac178da')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Failed'))) <> CONVERT(VARBINARY(MAX), N'Failed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MODEL_CREDENTIAL_MISSING'))) <> CONVERT(VARBINARY(MAX), N'MODEL_CREDENTIAL_MISSING')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Failed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T17:18:28.6791861+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T17:18:29.3172158+00:00', 127)),
        InputSha256 = N'9abc8fcb478b8ee86c4a36cf0801a35ad79b25b6521e552f66c8ec383ac178da',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N'MODEL_CREDENTIAL_MISSING'
    WHERE ID = N'4cdc8517-019a-482f-b838-cb12a1faabf4' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'4cdc8517-019a-482f-b838-cb12a1faabf4';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'4cdc8517-019a-482f-b838-cb12a1faabf4')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'4cdc8517-019a-482f-b838-cb12a1faabf4');

    -- Agent run audit 6ef10300-c0ab-4c85-bf40-9ae55f18d304
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T17:22:17.9153177+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T17:22:17.9153177+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T17:22:27.665582+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T17:22:27.665582+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T17:22:28.5182401+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T17:22:28.5182401+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T17:22:31.8188677+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T17:22:31.8188677+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6ef10300-c0ab-4c85-bf40-9ae55f18d304'))) <> CONVERT(VARBINARY(MAX), N'6ef10300-c0ab-4c85-bf40-9ae55f18d304')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T17:22:17.9153177+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T17:22:31.8188677+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 145,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'6ef10300-c0ab-4c85-bf40-9ae55f18d304' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'6ef10300-c0ab-4c85-bf40-9ae55f18d304';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'669bf294-1ed6-51bf-89c6-9fdab2fe81e7', N'6ef10300-c0ab-4c85-bf40-9ae55f18d304', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T17:22:27.665582+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T17:22:28.5182401+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'6ef10300-c0ab-4c85-bf40-9ae55f18d304')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'6ef10300-c0ab-4c85-bf40-9ae55f18d304');

    -- Agent run audit 1ceae544-cb8c-4516-a3c4-3ebff5ab27a8
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ceae544-cb8c-4516-a3c4-3ebff5ab27a8'))) <> CONVERT(VARBINARY(MAX), N'1ceae544-cb8c-4516-a3c4-3ebff5ab27a8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T01:36:04.5284593+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T01:36:04.5284593+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T01:36:11.5479939+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T01:36:11.5479939+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T01:36:12.4501114+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T01:36:12.4501114+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T01:36:16.0430927+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T01:36:16.0430927+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T01:36:04.5284593+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T01:36:16.0430927+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 134,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'1ceae544-cb8c-4516-a3c4-3ebff5ab27a8' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'1ceae544-cb8c-4516-a3c4-3ebff5ab27a8';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'4704d897-6fb9-5aef-b17b-636019445210', N'1ceae544-cb8c-4516-a3c4-3ebff5ab27a8', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T01:36:11.5479939+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T01:36:12.4501114+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'1ceae544-cb8c-4516-a3c4-3ebff5ab27a8')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'1ceae544-cb8c-4516-a3c4-3ebff5ab27a8');

    -- Agent run audit ce013863-539a-4feb-b515-84d8a4bfa3e6
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T02:07:13.3643628+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T02:07:13.3643628+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T02:07:23.2150212+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T02:07:23.2150212+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T02:07:23.7670142+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T02:07:23.7670142+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T02:07:26.4827653+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T02:07:26.4827653+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ce013863-539a-4feb-b515-84d8a4bfa3e6'))) <> CONVERT(VARBINARY(MAX), N'ce013863-539a-4feb-b515-84d8a4bfa3e6')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T02:07:13.3643628+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T02:07:26.4827653+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 172,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'ce013863-539a-4feb-b515-84d8a4bfa3e6' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'ce013863-539a-4feb-b515-84d8a4bfa3e6';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'718768c4-4b58-58ab-bb11-b45c372e264a', N'ce013863-539a-4feb-b515-84d8a4bfa3e6', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T02:07:23.2150212+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T02:07:23.7670142+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'ce013863-539a-4feb-b515-84d8a4bfa3e6')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'ce013863-539a-4feb-b515-84d8a4bfa3e6');

    -- Agent run audit b0daa481-6615-4bc0-8d0b-8deac4f35999
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T03:31:14.2140024+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T03:31:14.2140024+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T03:31:21.6195456+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T03:31:21.6195456+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T03:31:22.448407+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T03:31:22.448407+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T03:31:24.9135012+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T03:31:24.9135012+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b0daa481-6615-4bc0-8d0b-8deac4f35999'))) <> CONVERT(VARBINARY(MAX), N'b0daa481-6615-4bc0-8d0b-8deac4f35999')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T03:31:14.2140024+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T03:31:24.9135012+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 92,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'b0daa481-6615-4bc0-8d0b-8deac4f35999' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'b0daa481-6615-4bc0-8d0b-8deac4f35999';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'1e0f0dfd-5285-50e7-bef3-bfa505a7e60a', N'b0daa481-6615-4bc0-8d0b-8deac4f35999', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T03:31:21.6195456+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T03:31:22.448407+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'b0daa481-6615-4bc0-8d0b-8deac4f35999')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'b0daa481-6615-4bc0-8d0b-8deac4f35999');

    -- Agent run audit a6386f18-92af-4790-8ebe-b11748420cfb
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T08:51:18.0756572+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T08:51:18.0756572+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T08:51:26.2730975+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T08:51:26.2730975+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T08:51:27.1536252+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T08:51:27.1536252+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T08:51:29.2768344+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T08:51:29.2768344+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a6386f18-92af-4790-8ebe-b11748420cfb'))) <> CONVERT(VARBINARY(MAX), N'a6386f18-92af-4790-8ebe-b11748420cfb')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T08:51:18.0756572+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T08:51:29.2768344+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 59,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'a6386f18-92af-4790-8ebe-b11748420cfb' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'a6386f18-92af-4790-8ebe-b11748420cfb';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'bd1df5ed-2e81-5b7c-960d-f5d31a305272', N'a6386f18-92af-4790-8ebe-b11748420cfb', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T08:51:26.2730975+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T08:51:27.1536252+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'a6386f18-92af-4790-8ebe-b11748420cfb')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'a6386f18-92af-4790-8ebe-b11748420cfb');

    -- Agent run audit 419cb943-10f5-43ad-8d74-c1be0ecafd39
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T08:52:02.4601851+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T08:52:02.4601851+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T08:52:06.901819+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T08:52:06.901819+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'419cb943-10f5-43ad-8d74-c1be0ecafd39'))) <> CONVERT(VARBINARY(MAX), N'419cb943-10f5-43ad-8d74-c1be0ecafd39')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'daa423a1ea94e73a7a1f83f7b42409510109c6202116f13dcbd32d5a5c6f5423'))) <> CONVERT(VARBINARY(MAX), N'daa423a1ea94e73a7a1f83f7b42409510109c6202116f13dcbd32d5a5c6f5423')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T08:52:02.4601851+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T08:52:06.901819+00:00', 127)),
        InputSha256 = N'daa423a1ea94e73a7a1f83f7b42409510109c6202116f13dcbd32d5a5c6f5423',
        OutputCharacters = 174,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'419cb943-10f5-43ad-8d74-c1be0ecafd39' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'419cb943-10f5-43ad-8d74-c1be0ecafd39';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'419cb943-10f5-43ad-8d74-c1be0ecafd39')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'419cb943-10f5-43ad-8d74-c1be0ecafd39');

    -- Agent run audit 2e16de78-4873-4f81-9be6-ea2224bf90fd
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T08:52:51.6395746+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T08:52:51.6395746+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T08:53:09.4669469+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T08:53:09.4669469+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2e16de78-4873-4f81-9be6-ea2224bf90fd'))) <> CONVERT(VARBINARY(MAX), N'2e16de78-4873-4f81-9be6-ea2224bf90fd')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T08:52:51.6395746+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T08:53:09.4669469+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 1823,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'2e16de78-4873-4f81-9be6-ea2224bf90fd' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'2e16de78-4873-4f81-9be6-ea2224bf90fd';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'2e16de78-4873-4f81-9be6-ea2224bf90fd')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'2e16de78-4873-4f81-9be6-ea2224bf90fd');

    -- Agent run audit 9fc3a596-8ae8-41ed-b5f3-8920368748cd
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T09:23:44.7158176+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T09:23:44.7158176+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T09:24:02.645314+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T09:24:02.645314+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9fc3a596-8ae8-41ed-b5f3-8920368748cd'))) <> CONVERT(VARBINARY(MAX), N'9fc3a596-8ae8-41ed-b5f3-8920368748cd')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T09:23:44.7158176+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T09:24:02.645314+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 1973,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'9fc3a596-8ae8-41ed-b5f3-8920368748cd' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'9fc3a596-8ae8-41ed-b5f3-8920368748cd';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'9fc3a596-8ae8-41ed-b5f3-8920368748cd')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'9fc3a596-8ae8-41ed-b5f3-8920368748cd');

    -- Agent run audit 47357f3c-4bd2-46a2-8f1a-26b9ec14d979
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T09:45:22.4243338+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T09:45:22.4243338+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T09:45:44.2074166+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T09:45:44.2074166+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'47357f3c-4bd2-46a2-8f1a-26b9ec14d979'))) <> CONVERT(VARBINARY(MAX), N'47357f3c-4bd2-46a2-8f1a-26b9ec14d979')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T09:45:22.4243338+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T09:45:44.2074166+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 2071,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'47357f3c-4bd2-46a2-8f1a-26b9ec14d979' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'47357f3c-4bd2-46a2-8f1a-26b9ec14d979';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'47357f3c-4bd2-46a2-8f1a-26b9ec14d979')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'47357f3c-4bd2-46a2-8f1a-26b9ec14d979');

    -- Agent run audit 6d6cf9cf-13e0-4f62-8d88-f43827fea106
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T09:48:18.1316376+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T09:48:18.1316376+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T09:48:42.0282187+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T09:48:42.0282187+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6d6cf9cf-13e0-4f62-8d88-f43827fea106'))) <> CONVERT(VARBINARY(MAX), N'6d6cf9cf-13e0-4f62-8d88-f43827fea106')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T09:48:18.1316376+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T09:48:42.0282187+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 2543,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'6d6cf9cf-13e0-4f62-8d88-f43827fea106' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'6d6cf9cf-13e0-4f62-8d88-f43827fea106';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'6d6cf9cf-13e0-4f62-8d88-f43827fea106')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'6d6cf9cf-13e0-4f62-8d88-f43827fea106');

    -- Agent run audit 91c2d2ec-888f-4426-a101-2240a4f1fd00
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T15:53:14.4467829+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T15:53:14.4467829+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T15:53:37.2072135+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T15:53:37.2072135+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'91c2d2ec-888f-4426-a101-2240a4f1fd00'))) <> CONVERT(VARBINARY(MAX), N'91c2d2ec-888f-4426-a101-2240a4f1fd00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T15:53:14.4467829+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T15:53:37.2072135+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 2136,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'91c2d2ec-888f-4426-a101-2240a4f1fd00' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'91c2d2ec-888f-4426-a101-2240a4f1fd00';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'91c2d2ec-888f-4426-a101-2240a4f1fd00')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'91c2d2ec-888f-4426-a101-2240a4f1fd00');

    -- Agent run audit 2bf77813-60ac-4e25-9693-4d964d1fe076
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T15:53:56.7221897+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T15:53:56.7221897+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T15:54:05.1639355+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T15:54:05.1639355+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T15:54:06.0562859+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T15:54:06.0562859+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-11T15:54:08.2443944+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-11T15:54:08.2443944+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2bf77813-60ac-4e25-9693-4d964d1fe076'))) <> CONVERT(VARBINARY(MAX), N'2bf77813-60ac-4e25-9693-4d964d1fe076')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T15:53:56.7221897+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T15:54:08.2443944+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 59,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'2bf77813-60ac-4e25-9693-4d964d1fe076' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'2bf77813-60ac-4e25-9693-4d964d1fe076';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'3289985e-4d62-555a-9131-65ebc5946c3b', N'2bf77813-60ac-4e25-9693-4d964d1fe076', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T15:54:05.1639355+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-11T15:54:06.0562859+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'2bf77813-60ac-4e25-9693-4d964d1fe076')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'2bf77813-60ac-4e25-9693-4d964d1fe076');

    -- Agent run audit 22846704-a03d-4ab5-8058-a2aaa4cf17aa
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-12T05:26:35.7018807+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-12T05:26:35.7018807+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-12T05:26:49.845458+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-12T05:26:49.845458+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'22846704-a03d-4ab5-8058-a2aaa4cf17aa'))) <> CONVERT(VARBINARY(MAX), N'22846704-a03d-4ab5-8058-a2aaa4cf17aa')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-12T05:26:35.7018807+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-12T05:26:49.845458+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 467,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'22846704-a03d-4ab5-8058-a2aaa4cf17aa' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'22846704-a03d-4ab5-8058-a2aaa4cf17aa';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'22846704-a03d-4ab5-8058-a2aaa4cf17aa')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'22846704-a03d-4ab5-8058-a2aaa4cf17aa');

    -- Agent run audit c274bba5-d64c-43ef-9b47-5da4fefa07a3
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:16:11.976871+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:16:11.976871+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:16:23.2904289+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:16:23.2904289+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:16:26.0369937+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:16:26.0369937+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:16:34.5092519+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:16:34.5092519+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:16:34.5093895+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:16:34.5093895+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:16:39.9261682+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:16:39.9261682+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:16:39.9262567+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:16:39.9262567+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:16:44.0678619+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:16:44.0678619+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:16:44.0679343+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:16:44.0679343+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:16:44.5543978+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:16:44.5543978+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED'))) <> CONVERT(VARBINARY(MAX), N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Failed'))) <> CONVERT(VARBINARY(MAX), N'Failed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_CALL_FAILED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_CALL_FAILED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolBlocked'))) <> CONVERT(VARBINARY(MAX), N'ToolBlocked')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolFailed'))) <> CONVERT(VARBINARY(MAX), N'ToolFailed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c274bba5-d64c-43ef-9b47-5da4fefa07a3'))) <> CONVERT(VARBINARY(MAX), N'c274bba5-d64c-43ef-9b47-5da4fefa07a3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Failed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:16:11.976871+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:16:44.5543978+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 59,
        ToolCallCount = 4,
        ErrorCode = N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED'
    WHERE ID = N'c274bba5-d64c-43ef-9b47-5da4fefa07a3' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'c274bba5-d64c-43ef-9b47-5da4fefa07a3';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'c3bfd6c3-9b67-5bc2-834b-4fceac1f8b0b', N'c274bba5-d64c-43ef-9b47-5da4fefa07a3', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolFailed', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:16:23.2904289+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:16:26.0369937+00:00', 127)), N'MCP_TOOL_CALL_FAILED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'33ae1911-df4e-58d8-9fc9-a4781e869aad', N'c274bba5-d64c-43ef-9b47-5da4fefa07a3', 1, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolBlocked', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:16:34.5092519+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:16:34.5093895+00:00', 127)), N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'5bd54ae9-15f2-5db0-bcf9-e037ab9c721c', N'c274bba5-d64c-43ef-9b47-5da4fefa07a3', 2, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolBlocked', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:16:39.9261682+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:16:39.9262567+00:00', 127)), N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED');
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'b2e62e31-c6ea-5e32-8dcf-ec60f37949d8', N'c274bba5-d64c-43ef-9b47-5da4fefa07a3', 3, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolBlocked', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:16:44.0678619+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:16:44.0679343+00:00', 127)), N'BUSINESS_QUERY_CALL_LIMIT_EXCEEDED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'c274bba5-d64c-43ef-9b47-5da4fefa07a3')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'c274bba5-d64c-43ef-9b47-5da4fefa07a3');

    -- Agent run audit 04b8fd3b-3fcc-4f86-bf20-8a94327a58d6
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'04b8fd3b-3fcc-4f86-bf20-8a94327a58d6'))) <> CONVERT(VARBINARY(MAX), N'04b8fd3b-3fcc-4f86-bf20-8a94327a58d6')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:19:47.9626605+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:19:47.9626605+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:19:57.488602+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:19:57.488602+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:19:58.4949529+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:19:58.4949529+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T03:20:02.8425088+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T03:20:02.8425088+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5cc7b457f43e3c62e8342c31387cb65245e335021db71032b52c1ac47f4a479a'))) <> CONVERT(VARBINARY(MAX), N'5cc7b457f43e3c62e8342c31387cb65245e335021db71032b52c1ac47f4a479a')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:19:47.9626605+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:20:02.8425088+00:00', 127)),
        InputSha256 = N'5cc7b457f43e3c62e8342c31387cb65245e335021db71032b52c1ac47f4a479a',
        OutputCharacters = 164,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'04b8fd3b-3fcc-4f86-bf20-8a94327a58d6' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'04b8fd3b-3fcc-4f86-bf20-8a94327a58d6';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'7a07a856-f500-529f-92bc-0f19dd4e09d4', N'04b8fd3b-3fcc-4f86-bf20-8a94327a58d6', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:19:57.488602+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T03:19:58.4949529+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'04b8fd3b-3fcc-4f86-bf20-8a94327a58d6')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'04b8fd3b-3fcc-4f86-bf20-8a94327a58d6');

    -- Agent run audit 6324fd35-7605-4385-a92c-02ccb709aca5
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9'))) <> CONVERT(VARBINARY(MAX), N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T09:50:05.3707433+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T09:50:05.3707433+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T09:50:14.5879712+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T09:50:14.5879712+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T09:50:15.457824+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T09:50:15.457824+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T09:50:20.1277925+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T09:50:20.1277925+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6324fd35-7605-4385-a92c-02ccb709aca5'))) <> CONVERT(VARBINARY(MAX), N'6324fd35-7605-4385-a92c-02ccb709aca5')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0'))) <> CONVERT(VARBINARY(MAX), N'b9e74725-1170-4ec7-8cb7-125510dbd2b0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query_business_data'))) <> CONVERT(VARBINARY(MAX), N'query_business_data')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T09:50:05.3707433+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T09:50:20.1277925+00:00', 127)),
        InputSha256 = N'1ca5f2e7b5885beb6925344913e4c28261d845a0c80100aebd650fbf40d1acc9',
        OutputCharacters = 70,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'6324fd35-7605-4385-a92c-02ccb709aca5' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'6324fd35-7605-4385-a92c-02ccb709aca5';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'34c68054-9b45-567b-94c2-94686536eb58', N'6324fd35-7605-4385-a92c-02ccb709aca5', 0, N'b9e74725-1170-4ec7-8cb7-125510dbd2b0', N'query_business_data', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T09:50:14.5879712+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T09:50:15.457824+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'6324fd35-7605-4385-a92c-02ccb709aca5')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'6324fd35-7605-4385-a92c-02ccb709aca5');

    -- Agent run audit 19274f42-3c56-475a-bac5-1e4c48336198
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'19274f42-3c56-475a-bac5-1e4c48336198'))) <> CONVERT(VARBINARY(MAX), N'19274f42-3c56-475a-bac5-1e4c48336198')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-14T06:01:59.5595397+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-14T06:01:59.5595397+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-14T06:02:13.6136711+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-14T06:02:13.6136711+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-14T06:01:59.5595397+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-14T06:02:13.6136711+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 486,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'19274f42-3c56-475a-bac5-1e4c48336198' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'19274f42-3c56-475a-bac5-1e4c48336198';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'19274f42-3c56-475a-bac5-1e4c48336198')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'19274f42-3c56-475a-bac5-1e4c48336198');

    -- Agent run audit 6a8d7f29-16f6-44dd-9775-67895b3f591e
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-14T07:02:29.4392388+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-14T07:02:29.4392388+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-14T07:02:43.3691758+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-14T07:02:43.3691758+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6a8d7f29-16f6-44dd-9775-67895b3f591e'))) <> CONVERT(VARBINARY(MAX), N'6a8d7f29-16f6-44dd-9775-67895b3f591e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46'))) <> CONVERT(VARBINARY(MAX), N'c28ddaec-1d54-410e-8533-8fd45e955e46')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'c28ddaec-1d54-410e-8533-8fd45e955e46',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-14T07:02:29.4392388+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-14T07:02:43.3691758+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 712,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'6a8d7f29-16f6-44dd-9775-67895b3f591e' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'6a8d7f29-16f6-44dd-9775-67895b3f591e';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'6a8d7f29-16f6-44dd-9775-67895b3f591e')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'6a8d7f29-16f6-44dd-9775-67895b3f591e');

    -- Agent run audit 680e9d1f-3410-4fae-9c4c-b63a27edc745
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:28:08.7059729+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:28:08.7059729+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:28:24.6974425+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:28:24.6974425+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'528e9728-a28a-498b-8e38-392afd34bae2'))) <> CONVERT(VARBINARY(MAX), N'528e9728-a28a-498b-8e38-392afd34bae2')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'680e9d1f-3410-4fae-9c4c-b63a27edc745'))) <> CONVERT(VARBINARY(MAX), N'680e9d1f-3410-4fae-9c4c-b63a27edc745')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'528e9728-a28a-498b-8e38-392afd34bae2',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:28:08.7059729+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:28:24.6974425+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 1037,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'680e9d1f-3410-4fae-9c4c-b63a27edc745' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'680e9d1f-3410-4fae-9c4c-b63a27edc745';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'680e9d1f-3410-4fae-9c4c-b63a27edc745')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'680e9d1f-3410-4fae-9c4c-b63a27edc745');

    -- Agent run audit 5a5341d8-74a1-4510-b5d4-89b460b94eb4
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:30:15.4148415+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:30:15.4148415+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:30:34.899307+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:30:34.899307+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3c99239358482c331853a8a0fef0cdc8753a87ffdc4752241b428b83a8c022ab'))) <> CONVERT(VARBINARY(MAX), N'3c99239358482c331853a8a0fef0cdc8753a87ffdc4752241b428b83a8c022ab')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'528e9728-a28a-498b-8e38-392afd34bae2'))) <> CONVERT(VARBINARY(MAX), N'528e9728-a28a-498b-8e38-392afd34bae2')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5a5341d8-74a1-4510-b5d4-89b460b94eb4'))) <> CONVERT(VARBINARY(MAX), N'5a5341d8-74a1-4510-b5d4-89b460b94eb4')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'528e9728-a28a-498b-8e38-392afd34bae2',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:30:15.4148415+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:30:34.899307+00:00', 127)),
        InputSha256 = N'3c99239358482c331853a8a0fef0cdc8753a87ffdc4752241b428b83a8c022ab',
        OutputCharacters = 1203,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'5a5341d8-74a1-4510-b5d4-89b460b94eb4' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'5a5341d8-74a1-4510-b5d4-89b460b94eb4';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'5a5341d8-74a1-4510-b5d4-89b460b94eb4')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'5a5341d8-74a1-4510-b5d4-89b460b94eb4');

    -- Agent run audit 133d574c-f901-4343-97be-5e539b43193c
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'133d574c-f901-4343-97be-5e539b43193c'))) <> CONVERT(VARBINARY(MAX), N'133d574c-f901-4343-97be-5e539b43193c')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:31:07.3957643+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:31:07.3957643+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:31:21.9049979+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:31:21.9049979+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3c99239358482c331853a8a0fef0cdc8753a87ffdc4752241b428b83a8c022ab'))) <> CONVERT(VARBINARY(MAX), N'3c99239358482c331853a8a0fef0cdc8753a87ffdc4752241b428b83a8c022ab')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'95dfbfef-4fd0-4c93-8785-6c93035c3574',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:31:07.3957643+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:31:21.9049979+00:00', 127)),
        InputSha256 = N'3c99239358482c331853a8a0fef0cdc8753a87ffdc4752241b428b83a8c022ab',
        OutputCharacters = 942,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'133d574c-f901-4343-97be-5e539b43193c' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'133d574c-f901-4343-97be-5e539b43193c';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'133d574c-f901-4343-97be-5e539b43193c')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'133d574c-f901-4343-97be-5e539b43193c');

    -- Agent run audit 81fe0db6-f026-4fd6-990c-5ffdd4a844f4
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:31:36.7868871+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:31:36.7868871+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:31:46.4111398+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:31:46.4111398+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'81fe0db6-f026-4fd6-990c-5ffdd4a844f4'))) <> CONVERT(VARBINARY(MAX), N'81fe0db6-f026-4fd6-990c-5ffdd4a844f4')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'd7e7d2aec7de0d9d24ce428ed7ae91aedbc340d361464f2e497f285470779b36'))) <> CONVERT(VARBINARY(MAX), N'd7e7d2aec7de0d9d24ce428ed7ae91aedbc340d361464f2e497f285470779b36')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'95dfbfef-4fd0-4c93-8785-6c93035c3574',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:31:36.7868871+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:31:46.4111398+00:00', 127)),
        InputSha256 = N'd7e7d2aec7de0d9d24ce428ed7ae91aedbc340d361464f2e497f285470779b36',
        OutputCharacters = 523,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'81fe0db6-f026-4fd6-990c-5ffdd4a844f4' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'81fe0db6-f026-4fd6-990c-5ffdd4a844f4';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'81fe0db6-f026-4fd6-990c-5ffdd4a844f4')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'81fe0db6-f026-4fd6-990c-5ffdd4a844f4');

    -- Agent run audit 68be77b2-3dc9-4c67-b8a5-0dde8b3700c7
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'15e3adaf0aaf9e7aced722f51c086d145a9150892f0ab092f32a88528a544b06'))) <> CONVERT(VARBINARY(MAX), N'15e3adaf0aaf9e7aced722f51c086d145a9150892f0ab092f32a88528a544b06')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:32:59.4545807+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:32:59.4545807+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:33:18.8651647+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:33:18.8651647+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'68be77b2-3dc9-4c67-b8a5-0dde8b3700c7'))) <> CONVERT(VARBINARY(MAX), N'68be77b2-3dc9-4c67-b8a5-0dde8b3700c7')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'95dfbfef-4fd0-4c93-8785-6c93035c3574',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:32:59.4545807+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:33:18.8651647+00:00', 127)),
        InputSha256 = N'15e3adaf0aaf9e7aced722f51c086d145a9150892f0ab092f32a88528a544b06',
        OutputCharacters = 960,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'68be77b2-3dc9-4c67-b8a5-0dde8b3700c7' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'68be77b2-3dc9-4c67-b8a5-0dde8b3700c7';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'68be77b2-3dc9-4c67-b8a5-0dde8b3700c7')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'68be77b2-3dc9-4c67-b8a5-0dde8b3700c7');

    -- Agent run audit b203ec69-783b-4b99-ae7f-7171763d62be
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'15e3adaf0aaf9e7aced722f51c086d145a9150892f0ab092f32a88528a544b06'))) <> CONVERT(VARBINARY(MAX), N'15e3adaf0aaf9e7aced722f51c086d145a9150892f0ab092f32a88528a544b06')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:39:03.9092385+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:39:03.9092385+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:39:18.8385315+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:39:18.8385315+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b203ec69-783b-4b99-ae7f-7171763d62be'))) <> CONVERT(VARBINARY(MAX), N'b203ec69-783b-4b99-ae7f-7171763d62be')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'95dfbfef-4fd0-4c93-8785-6c93035c3574',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:39:03.9092385+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:39:18.8385315+00:00', 127)),
        InputSha256 = N'15e3adaf0aaf9e7aced722f51c086d145a9150892f0ab092f32a88528a544b06',
        OutputCharacters = 1009,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'b203ec69-783b-4b99-ae7f-7171763d62be' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'b203ec69-783b-4b99-ae7f-7171763d62be';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'b203ec69-783b-4b99-ae7f-7171763d62be')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'b203ec69-783b-4b99-ae7f-7171763d62be');

    -- Agent run audit 919e21a3-f6fa-49da-b35f-ada2110da622
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:39:57.1508448+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:39:57.1508448+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:40:25.8998725+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:40:25.8998725+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'919e21a3-f6fa-49da-b35f-ada2110da622'))) <> CONVERT(VARBINARY(MAX), N'919e21a3-f6fa-49da-b35f-ada2110da622')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'95dfbfef-4fd0-4c93-8785-6c93035c3574',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:39:57.1508448+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:40:25.8998725+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 2341,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'919e21a3-f6fa-49da-b35f-ada2110da622' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'919e21a3-f6fa-49da-b35f-ada2110da622';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'919e21a3-f6fa-49da-b35f-ada2110da622')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'919e21a3-f6fa-49da-b35f-ada2110da622');

    -- Agent run audit a695b454-82c3-4b46-9b89-2e934ccb553d
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:44:28.216708+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:44:28.216708+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:44:44.86242+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:44:44.86242+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80'))) <> CONVERT(VARBINARY(MAX), N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a695b454-82c3-4b46-9b89-2e934ccb553d'))) <> CONVERT(VARBINARY(MAX), N'a695b454-82c3-4b46-9b89-2e934ccb553d')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'95dfbfef-4fd0-4c93-8785-6c93035c3574',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:44:28.216708+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:44:44.86242+00:00', 127)),
        InputSha256 = N'80105d0d9877573d0dc3b70a2ccf95297c11cbd6644e3f15265ab881191b0b80',
        OutputCharacters = 1935,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'a695b454-82c3-4b46-9b89-2e934ccb553d' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'a695b454-82c3-4b46-9b89-2e934ccb553d';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'a695b454-82c3-4b46-9b89-2e934ccb553d')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'a695b454-82c3-4b46-9b89-2e934ccb553d');

    -- Agent run audit 47e8c29d-30e3-4474-a6fb-145d8ea9be26
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:44:50.1368864+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:44:50.1368864+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T06:45:02.1007404+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T06:45:02.1007404+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3c99239358482c331853a8a0fef0cdc8753a87ffdc4752241b428b83a8c022ab'))) <> CONVERT(VARBINARY(MAX), N'3c99239358482c331853a8a0fef0cdc8753a87ffdc4752241b428b83a8c022ab')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'47e8c29d-30e3-4474-a6fb-145d8ea9be26'))) <> CONVERT(VARBINARY(MAX), N'47e8c29d-30e3-4474-a6fb-145d8ea9be26')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Cancelled'))) <> CONVERT(VARBINARY(MAX), N'Cancelled')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'95dfbfef-4fd0-4c93-8785-6c93035c3574',
        AgentCode = N'main-agent',
        Status = N'Cancelled',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:44:50.1368864+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T06:45:02.1007404+00:00', 127)),
        InputSha256 = N'3c99239358482c331853a8a0fef0cdc8753a87ffdc4752241b428b83a8c022ab',
        OutputCharacters = 875,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'47e8c29d-30e3-4474-a6fb-145d8ea9be26' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'47e8c29d-30e3-4474-a6fb-145d8ea9be26';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'47e8c29d-30e3-4474-a6fb-145d8ea9be26')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'47e8c29d-30e3-4474-a6fb-145d8ea9be26');

    -- Agent run audit d9ab3636-0546-4fa7-9fb3-4824668e3565
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675'))) <> CONVERT(VARBINARY(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T09:39:30.7619913+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T09:39:30.7619913+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T09:39:34.81567+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T09:39:34.81567+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'd9ab3636-0546-4fa7-9fb3-4824668e3565'))) <> CONVERT(VARBINARY(MAX), N'd9ab3636-0546-4fa7-9fb3-4824668e3565')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'95dfbfef-4fd0-4c93-8785-6c93035c3574',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T09:39:30.7619913+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T09:39:34.81567+00:00', 127)),
        InputSha256 = N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675',
        OutputCharacters = 70,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'd9ab3636-0546-4fa7-9fb3-4824668e3565' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'd9ab3636-0546-4fa7-9fb3-4824668e3565';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'd9ab3636-0546-4fa7-9fb3-4824668e3565')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'd9ab3636-0546-4fa7-9fb3-4824668e3565');

    -- Agent run audit 8c59aaa5-476a-444c-bc85-e5c580a16398
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675'))) <> CONVERT(VARBINARY(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T09:43:16.7165292+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T09:43:16.7165292+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T09:43:19.4520717+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T09:43:19.4520717+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'8c59aaa5-476a-444c-bc85-e5c580a16398'))) <> CONVERT(VARBINARY(MAX), N'8c59aaa5-476a-444c-bc85-e5c580a16398')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'95dfbfef-4fd0-4c93-8785-6c93035c3574',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T09:43:16.7165292+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T09:43:19.4520717+00:00', 127)),
        InputSha256 = N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675',
        OutputCharacters = 11,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'8c59aaa5-476a-444c-bc85-e5c580a16398' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'8c59aaa5-476a-444c-bc85-e5c580a16398';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'8c59aaa5-476a-444c-bc85-e5c580a16398')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'8c59aaa5-476a-444c-bc85-e5c580a16398');

    -- Agent run audit 940788b1-2009-43ba-a33d-3468874c8c0c
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675'))) <> CONVERT(VARBINARY(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T12:16:08.3166566+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T12:16:08.3166566+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T12:16:11.5200451+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T12:16:11.5200451+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'940788b1-2009-43ba-a33d-3468874c8c0c'))) <> CONVERT(VARBINARY(MAX), N'940788b1-2009-43ba-a33d-3468874c8c0c')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'95dfbfef-4fd0-4c93-8785-6c93035c3574',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T12:16:08.3166566+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T12:16:11.5200451+00:00', 127)),
        InputSha256 = N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675',
        OutputCharacters = 11,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'940788b1-2009-43ba-a33d-3468874c8c0c' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'940788b1-2009-43ba-a33d-3468874c8c0c';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'940788b1-2009-43ba-a33d-3468874c8c0c')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'940788b1-2009-43ba-a33d-3468874c8c0c');

    -- Agent run audit 7a0de562-21b3-4e2b-b62a-10c166aefeb1
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675'))) <> CONVERT(VARBINARY(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e'))) <> CONVERT(VARBINARY(MAX), N'1e1c9aab-71e7-4e8b-9905-b34588f4515e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T12:16:32.8147846+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T12:16:32.8147846+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T12:16:36.0821706+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T12:16:36.0821706+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'7a0de562-21b3-4e2b-b62a-10c166aefeb1'))) <> CONVERT(VARBINARY(MAX), N'7a0de562-21b3-4e2b-b62a-10c166aefeb1')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574'))) <> CONVERT(VARBINARY(MAX), N'95dfbfef-4fd0-4c93-8785-6c93035c3574')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'main-agent'))) <> CONVERT(VARBINARY(MAX), N'main-agent')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'95dfbfef-4fd0-4c93-8785-6c93035c3574',
        AgentCode = N'main-agent',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T12:16:32.8147846+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T12:16:36.0821706+00:00', 127)),
        InputSha256 = N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675',
        OutputCharacters = 70,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'7a0de562-21b3-4e2b-b62a-10c166aefeb1' AND AgentId = N'1e1c9aab-71e7-4e8b-9905-b34588f4515e';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'7a0de562-21b3-4e2b-b62a-10c166aefeb1';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'7a0de562-21b3-4e2b-b62a-10c166aefeb1')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'7a0de562-21b3-4e2b-b62a-10c166aefeb1');

    -- Agent run audit 51095b6f-87fc-44f4-ae79-b23181e882ae
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:46:22.4463684+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:46:22.4463684+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:46:27.3403672+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:46:27.3403672+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:46:27.3456127+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:46:27.3456127+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:46:32.4506922+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:46:32.4506922+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4246d104-7d19-4b42-b6c8-d7a9dd46897a'))) <> CONVERT(VARBINARY(MAX), N'4246d104-7d19-4b42-b6c8-d7a9dd46897a')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'51095b6f-87fc-44f4-ae79-b23181e882ae'))) <> CONVERT(VARBINARY(MAX), N'51095b6f-87fc-44f4-ae79-b23181e882ae')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'859fc218-5f7c-4fdd-92ad-b64ce340e683'))) <> CONVERT(VARBINARY(MAX), N'859fc218-5f7c-4fdd-92ad-b64ce340e683')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_CALL_BLOCKED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_CALL_BLOCKED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Mutating'))) <> CONVERT(VARBINARY(MAX), N'Mutating')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolBlocked'))) <> CONVERT(VARBINARY(MAX), N'ToolBlocked')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'859fc218-5f7c-4fdd-92ad-b64ce340e683',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:46:22.4463684+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:46:32.4506922+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 64,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'51095b6f-87fc-44f4-ae79-b23181e882ae' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'51095b6f-87fc-44f4-ae79-b23181e882ae';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'0fdab41d-a313-5dcb-8c5a-7ae9f80b9d50', N'51095b6f-87fc-44f4-ae79-b23181e882ae', 0, N'4246d104-7d19-4b42-b6c8-d7a9dd46897a', N'get_supplier', N'Mutating', N'ToolBlocked', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:46:27.3403672+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:46:27.3456127+00:00', 127)), N'MCP_TOOL_CALL_BLOCKED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'51095b6f-87fc-44f4-ae79-b23181e882ae')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'51095b6f-87fc-44f4-ae79-b23181e882ae');

    -- Agent run audit 55f8e132-9515-4164-a5c8-ffded7889439
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:47:41.5234232+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:47:41.5234232+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:47:44.2611427+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:47:44.2611427+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:47:44.2612141+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:47:44.2612141+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:47:48.8585841+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:47:48.8585841+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4246d104-7d19-4b42-b6c8-d7a9dd46897a'))) <> CONVERT(VARBINARY(MAX), N'4246d104-7d19-4b42-b6c8-d7a9dd46897a')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'55f8e132-9515-4164-a5c8-ffded7889439'))) <> CONVERT(VARBINARY(MAX), N'55f8e132-9515-4164-a5c8-ffded7889439')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'859fc218-5f7c-4fdd-92ad-b64ce340e683'))) <> CONVERT(VARBINARY(MAX), N'859fc218-5f7c-4fdd-92ad-b64ce340e683')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_CALL_BLOCKED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_CALL_BLOCKED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Mutating'))) <> CONVERT(VARBINARY(MAX), N'Mutating')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolBlocked'))) <> CONVERT(VARBINARY(MAX), N'ToolBlocked')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'859fc218-5f7c-4fdd-92ad-b64ce340e683',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:47:41.5234232+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:47:48.8585841+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 122,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'55f8e132-9515-4164-a5c8-ffded7889439' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'55f8e132-9515-4164-a5c8-ffded7889439';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'bfa77200-0ef6-576a-b61a-1e9cdc1ae03b', N'55f8e132-9515-4164-a5c8-ffded7889439', 0, N'4246d104-7d19-4b42-b6c8-d7a9dd46897a', N'get_supplier', N'Mutating', N'ToolBlocked', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:47:44.2611427+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:47:44.2612141+00:00', 127)), N'MCP_TOOL_CALL_BLOCKED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'55f8e132-9515-4164-a5c8-ffded7889439')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'55f8e132-9515-4164-a5c8-ffded7889439');

    -- Agent run audit 2199cc44-1d80-4a1d-b653-effe05f8d1e3
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:49:51.5025551+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:49:51.5025551+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:49:53.7050444+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:49:53.7050444+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:49:53.7050565+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:49:53.7050565+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:49:59.7036009+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:49:59.7036009+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2199cc44-1d80-4a1d-b653-effe05f8d1e3'))) <> CONVERT(VARBINARY(MAX), N'2199cc44-1d80-4a1d-b653-effe05f8d1e3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4246d104-7d19-4b42-b6c8-d7a9dd46897a'))) <> CONVERT(VARBINARY(MAX), N'4246d104-7d19-4b42-b6c8-d7a9dd46897a')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'859fc218-5f7c-4fdd-92ad-b64ce340e683'))) <> CONVERT(VARBINARY(MAX), N'859fc218-5f7c-4fdd-92ad-b64ce340e683')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'MCP_TOOL_CALL_BLOCKED'))) <> CONVERT(VARBINARY(MAX), N'MCP_TOOL_CALL_BLOCKED')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Mutating'))) <> CONVERT(VARBINARY(MAX), N'Mutating')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolBlocked'))) <> CONVERT(VARBINARY(MAX), N'ToolBlocked')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'859fc218-5f7c-4fdd-92ad-b64ce340e683',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:49:51.5025551+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:49:59.7036009+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 53,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'2199cc44-1d80-4a1d-b653-effe05f8d1e3' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'2199cc44-1d80-4a1d-b653-effe05f8d1e3';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'56c5b573-5d4e-5b0c-a5e5-2f563c1a19b6', N'2199cc44-1d80-4a1d-b653-effe05f8d1e3', 0, N'4246d104-7d19-4b42-b6c8-d7a9dd46897a', N'get_supplier', N'Mutating', N'ToolBlocked', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:49:53.7050444+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:49:53.7050565+00:00', 127)), N'MCP_TOOL_CALL_BLOCKED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'2199cc44-1d80-4a1d-b653-effe05f8d1e3')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'2199cc44-1d80-4a1d-b653-effe05f8d1e3');

    -- Agent run audit 97aaf21f-5d66-4581-a0b6-60f315fff322
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:57:14.9023342+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:57:14.9023342+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:57:17.4152517+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:57:17.4152517+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:57:17.743083+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:57:17.743083+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T03:57:20.8236932+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T03:57:20.8236932+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6c8d6243-dbc2-48e0-a56d-fb69559edfce'))) <> CONVERT(VARBINARY(MAX), N'6c8d6243-dbc2-48e0-a56d-fb69559edfce')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'97aaf21f-5d66-4581-a0b6-60f315fff322'))) <> CONVERT(VARBINARY(MAX), N'97aaf21f-5d66-4581-a0b6-60f315fff322')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8'))) <> CONVERT(VARBINARY(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'6c8d6243-dbc2-48e0-a56d-fb69559edfce',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:57:14.9023342+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:57:20.8236932+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 118,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'97aaf21f-5d66-4581-a0b6-60f315fff322' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'97aaf21f-5d66-4581-a0b6-60f315fff322';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'b1b86375-7f98-5a09-9520-11cc4894c798', N'97aaf21f-5d66-4581-a0b6-60f315fff322', 0, N'b65c0544-e334-4c98-a7bd-f153eb10fde8', N'get_supplier', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:57:17.4152517+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T03:57:17.743083+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'97aaf21f-5d66-4581-a0b6-60f315fff322')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'97aaf21f-5d66-4581-a0b6-60f315fff322');

    -- Agent run audit 73a5dfe2-9b61-4c95-912d-0f9ec1c14cc6
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:20:54.3284512+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:20:54.3284512+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:21:00.3043061+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:21:00.3043061+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:21:00.6363595+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:21:00.6363595+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:21:04.6437014+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:21:04.6437014+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6c8d6243-dbc2-48e0-a56d-fb69559edfce'))) <> CONVERT(VARBINARY(MAX), N'6c8d6243-dbc2-48e0-a56d-fb69559edfce')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'73a5dfe2-9b61-4c95-912d-0f9ec1c14cc6'))) <> CONVERT(VARBINARY(MAX), N'73a5dfe2-9b61-4c95-912d-0f9ec1c14cc6')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8'))) <> CONVERT(VARBINARY(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'6c8d6243-dbc2-48e0-a56d-fb69559edfce',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:20:54.3284512+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:21:04.6437014+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 184,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'73a5dfe2-9b61-4c95-912d-0f9ec1c14cc6' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'73a5dfe2-9b61-4c95-912d-0f9ec1c14cc6';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'0841c076-6293-524d-bbb2-bd40b8ade8b2', N'73a5dfe2-9b61-4c95-912d-0f9ec1c14cc6', 0, N'b65c0544-e334-4c98-a7bd-f153eb10fde8', N'get_supplier', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:21:00.3043061+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:21:00.6363595+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'73a5dfe2-9b61-4c95-912d-0f9ec1c14cc6')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'73a5dfe2-9b61-4c95-912d-0f9ec1c14cc6');

    -- Agent run audit e69b5e55-55f5-4081-b1c3-57058484f8c1
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:23:43.5187331+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:23:43.5187331+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:23:51.8332328+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:23:51.8332328+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6c48294a24bf9811ff523cfaeb26995f503b1692d1d781cb1f15eaab751fb900'))) <> CONVERT(VARBINARY(MAX), N'6c48294a24bf9811ff523cfaeb26995f503b1692d1d781cb1f15eaab751fb900')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'))) <> CONVERT(VARBINARY(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'e69b5e55-55f5-4081-b1c3-57058484f8c1'))) <> CONVERT(VARBINARY(MAX), N'e69b5e55-55f5-4081-b1c3-57058484f8c1')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:23:43.5187331+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:23:51.8332328+00:00', 127)),
        InputSha256 = N'6c48294a24bf9811ff523cfaeb26995f503b1692d1d781cb1f15eaab751fb900',
        OutputCharacters = 523,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'e69b5e55-55f5-4081-b1c3-57058484f8c1' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'e69b5e55-55f5-4081-b1c3-57058484f8c1';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'e69b5e55-55f5-4081-b1c3-57058484f8c1')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'e69b5e55-55f5-4081-b1c3-57058484f8c1');

    -- Agent run audit 5e882b81-01a2-499b-b96f-137c02479cf3
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:40:04.4730739+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:40:04.4730739+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:40:10.2304006+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:40:10.2304006+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:40:10.4209318+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:40:10.4209318+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:40:12.8723559+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:40:12.8723559+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5e882b81-01a2-499b-b96f-137c02479cf3'))) <> CONVERT(VARBINARY(MAX), N'5e882b81-01a2-499b-b96f-137c02479cf3')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'))) <> CONVERT(VARBINARY(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8'))) <> CONVERT(VARBINARY(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:40:04.4730739+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:40:12.8723559+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 36,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'5e882b81-01a2-499b-b96f-137c02479cf3' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'5e882b81-01a2-499b-b96f-137c02479cf3';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'dff46d7f-431d-50ae-bec0-3c273fb15d76', N'5e882b81-01a2-499b-b96f-137c02479cf3', 0, N'b65c0544-e334-4c98-a7bd-f153eb10fde8', N'get_supplier', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:40:10.2304006+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:40:10.4209318+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'5e882b81-01a2-499b-b96f-137c02479cf3')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'5e882b81-01a2-499b-b96f-137c02479cf3');

    -- Agent run audit dbb364a0-be10-4008-9c91-29ab67868e66
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:45:16.0672865+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:45:16.0672865+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:45:22.303771+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:45:22.303771+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:45:22.6349398+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:45:22.6349398+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:45:30.6355621+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:45:30.6355621+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'))) <> CONVERT(VARBINARY(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8'))) <> CONVERT(VARBINARY(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'dbb364a0-be10-4008-9c91-29ab67868e66'))) <> CONVERT(VARBINARY(MAX), N'dbb364a0-be10-4008-9c91-29ab67868e66')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:45:16.0672865+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:45:30.6355621+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 65,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'dbb364a0-be10-4008-9c91-29ab67868e66' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'dbb364a0-be10-4008-9c91-29ab67868e66';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'cb41fdd4-53d5-588d-a621-3aa57c65da32', N'dbb364a0-be10-4008-9c91-29ab67868e66', 0, N'b65c0544-e334-4c98-a7bd-f153eb10fde8', N'get_supplier', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:45:22.303771+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:45:22.6349398+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'dbb364a0-be10-4008-9c91-29ab67868e66')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'dbb364a0-be10-4008-9c91-29ab67868e66');

    -- Agent run audit 833cc4cb-b8db-4cbe-93c0-0f49852c06e2
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:35:17.3715905+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:35:17.3715905+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:35:23.6853983+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:35:23.6853983+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:35:24.0217361+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:35:24.0217361+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:35:27.3016013+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:35:27.3016013+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'833cc4cb-b8db-4cbe-93c0-0f49852c06e2'))) <> CONVERT(VARBINARY(MAX), N'833cc4cb-b8db-4cbe-93c0-0f49852c06e2')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'))) <> CONVERT(VARBINARY(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8'))) <> CONVERT(VARBINARY(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:35:17.3715905+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:35:27.3016013+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 161,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'833cc4cb-b8db-4cbe-93c0-0f49852c06e2' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'833cc4cb-b8db-4cbe-93c0-0f49852c06e2';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'12938867-7f07-5d45-bb0b-124ba920b28c', N'833cc4cb-b8db-4cbe-93c0-0f49852c06e2', 0, N'b65c0544-e334-4c98-a7bd-f153eb10fde8', N'get_supplier', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:35:23.6853983+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:35:24.0217361+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'833cc4cb-b8db-4cbe-93c0-0f49852c06e2')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'833cc4cb-b8db-4cbe-93c0-0f49852c06e2');

    -- Agent run audit b7f15f49-7d74-4f75-a103-f4325e21e45b
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:36:34.0751717+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:36:34.0751717+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:36:38.9230532+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:36:38.9230532+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:36:39.1134229+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:36:39.1134229+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:36:44.009323+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:36:44.009323+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'))) <> CONVERT(VARBINARY(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8'))) <> CONVERT(VARBINARY(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b7f15f49-7d74-4f75-a103-f4325e21e45b'))) <> CONVERT(VARBINARY(MAX), N'b7f15f49-7d74-4f75-a103-f4325e21e45b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:36:34.0751717+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:36:44.009323+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 122,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'b7f15f49-7d74-4f75-a103-f4325e21e45b' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'b7f15f49-7d74-4f75-a103-f4325e21e45b';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'f5ef6025-1161-5c22-84cc-2f1c08487f6a', N'b7f15f49-7d74-4f75-a103-f4325e21e45b', 0, N'b65c0544-e334-4c98-a7bd-f153eb10fde8', N'get_supplier', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:36:38.9230532+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:36:39.1134229+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'b7f15f49-7d74-4f75-a103-f4325e21e45b')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'b7f15f49-7d74-4f75-a103-f4325e21e45b');

    -- Agent run audit 1d678c5c-e6e5-41dc-a74f-ab3eeec6342d
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1d678c5c-e6e5-41dc-a74f-ab3eeec6342d'))) <> CONVERT(VARBINARY(MAX), N'1d678c5c-e6e5-41dc-a74f-ab3eeec6342d')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-31T08:19:53.1929572+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-31T08:19:53.1929572+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-31T08:19:57.4330284+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-31T08:19:57.4330284+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-31T08:19:58.6234159+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-31T08:19:58.6234159+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-31T08:20:01.7060273+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-31T08:20:01.7060273+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'))) <> CONVERT(VARBINARY(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ReadOnly'))) <> CONVERT(VARBINARY(MAX), N'ReadOnly')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ToolSucceeded'))) <> CONVERT(VARBINARY(MAX), N'ToolSucceeded')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8'))) <> CONVERT(VARBINARY(MAX), N'b65c0544-e334-4c98-a7bd-f153eb10fde8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'get_supplier'))) <> CONVERT(VARBINARY(MAX), N'get_supplier')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-31T08:19:53.1929572+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-31T08:20:01.7060273+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        OutputCharacters = 33,
        ToolCallCount = 1,
        ErrorCode = N''
    WHERE ID = N'1d678c5c-e6e5-41dc-a74f-ab3eeec6342d' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'1d678c5c-e6e5-41dc-a74f-ab3eeec6342d';
    INSERT INTO dbo.AgAgentToolCallAudit (ID, RunId, Ordinal, ToolVersionId, ToolName, Risk, Status, StartedAtUtc, FinishedAtUtc, ErrorCode)
    VALUES (N'77151ba6-9ec2-5b35-8a40-77a1c07fd3a9', N'1d678c5c-e6e5-41dc-a74f-ab3eeec6342d', 0, N'b65c0544-e334-4c98-a7bd-f153eb10fde8', N'get_supplier', N'ReadOnly', N'ToolSucceeded', CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-31T08:19:57.4330284+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-31T08:19:58.6234159+00:00', 127)), N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'1d678c5c-e6e5-41dc-a74f-ab3eeec6342d')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'1d678c5c-e6e5-41dc-a74f-ab3eeec6342d');

    -- Agent run audit e17aa019-ccff-41d4-8478-d42a4ea638ba
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:48:18.8759634+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:48:18.8759634+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:48:33.2973785+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:48:33.2973785+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'))) <> CONVERT(VARBINARY(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'dcd37a5443a3a291d374f6a3ab430d6630c449bf340e0748a6f7d57400a45149'))) <> CONVERT(VARBINARY(MAX), N'dcd37a5443a3a291d374f6a3ab430d6630c449bf340e0748a6f7d57400a45149')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'e17aa019-ccff-41d4-8478-d42a4ea638ba'))) <> CONVERT(VARBINARY(MAX), N'e17aa019-ccff-41d4-8478-d42a4ea638ba')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'test'))) <> CONVERT(VARBINARY(MAX), N'test')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf',
        AgentCode = N'test',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:48:18.8759634+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:48:33.2973785+00:00', 127)),
        InputSha256 = N'dcd37a5443a3a291d374f6a3ab430d6630c449bf340e0748a6f7d57400a45149',
        OutputCharacters = 65,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'e17aa019-ccff-41d4-8478-d42a4ea638ba' AND AgentId = N'2999f08b-fcef-4d4c-ab30-f1443048b6f0';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'e17aa019-ccff-41d4-8478-d42a4ea638ba';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'e17aa019-ccff-41d4-8478-d42a4ea638ba')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'e17aa019-ccff-41d4-8478-d42a4ea638ba');

    -- Agent run audit 5c8b4611-f126-43ab-b475-f0605cbb9f3e
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:52:57.5825068+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:52:57.5825068+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:53:08.9697316+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:53:08.9697316+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5885a518167b37b112ee4f3e6a6115d27745e1c4fa27cdc2881b8a36a0d0abe1'))) <> CONVERT(VARBINARY(MAX), N'5885a518167b37b112ee4f3e6a6115d27745e1c4fa27cdc2881b8a36a0d0abe1')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5c8b4611-f126-43ab-b475-f0605cbb9f3e'))) <> CONVERT(VARBINARY(MAX), N'5c8b4611-f126-43ab-b475-f0605cbb9f3e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:52:57.5825068+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:53:08.9697316+00:00', 127)),
        InputSha256 = N'5885a518167b37b112ee4f3e6a6115d27745e1c4fa27cdc2881b8a36a0d0abe1',
        OutputCharacters = 8,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'5c8b4611-f126-43ab-b475-f0605cbb9f3e' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'5c8b4611-f126-43ab-b475-f0605cbb9f3e';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'5c8b4611-f126-43ab-b475-f0605cbb9f3e')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'5c8b4611-f126-43ab-b475-f0605cbb9f3e');

    -- Agent run audit 9b0d8740-5fdd-445e-8b07-25963466ebd8
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:34:35.2091938+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:34:35.2091938+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:34:37.9438356+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:34:37.9438356+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3ca591fadf790701a82e12bcd3434d8862c521a288f38100703eb64d377507a9'))) <> CONVERT(VARBINARY(MAX), N'3ca591fadf790701a82e12bcd3434d8862c521a288f38100703eb64d377507a9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b0d8740-5fdd-445e-8b07-25963466ebd8'))) <> CONVERT(VARBINARY(MAX), N'9b0d8740-5fdd-445e-8b07-25963466ebd8')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:34:35.2091938+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:34:37.9438356+00:00', 127)),
        InputSha256 = N'3ca591fadf790701a82e12bcd3434d8862c521a288f38100703eb64d377507a9',
        OutputCharacters = 8,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'9b0d8740-5fdd-445e-8b07-25963466ebd8' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'9b0d8740-5fdd-445e-8b07-25963466ebd8';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'9b0d8740-5fdd-445e-8b07-25963466ebd8')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'9b0d8740-5fdd-445e-8b07-25963466ebd8');

    -- Agent run audit ff2ed6dd-b260-4ac0-b031-d8c96c5635ff
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:19.4520223+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:19.4520223+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:23.192091+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:23.192091+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5d04224b427d0a1da3a18113d5b9d86be3c2f173188f28dc6b2d1a5b3fdc48cb'))) <> CONVERT(VARBINARY(MAX), N'5d04224b427d0a1da3a18113d5b9d86be3c2f173188f28dc6b2d1a5b3fdc48cb')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ff2ed6dd-b260-4ac0-b031-d8c96c5635ff'))) <> CONVERT(VARBINARY(MAX), N'ff2ed6dd-b260-4ac0-b031-d8c96c5635ff')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:19.4520223+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:23.192091+00:00', 127)),
        InputSha256 = N'5d04224b427d0a1da3a18113d5b9d86be3c2f173188f28dc6b2d1a5b3fdc48cb',
        OutputCharacters = 8,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'ff2ed6dd-b260-4ac0-b031-d8c96c5635ff' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'ff2ed6dd-b260-4ac0-b031-d8c96c5635ff';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'ff2ed6dd-b260-4ac0-b031-d8c96c5635ff')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'ff2ed6dd-b260-4ac0-b031-d8c96c5635ff');

    -- Agent run audit ef14d7e1-3c85-4f47-a4d3-b6e61a4c2597
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:36.6196813+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:36.6196813+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:40.0968292+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:40.0968292+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b27137c42c73835f99464b920d0f3a9b9cc19a9a301332682acc128981e8144f'))) <> CONVERT(VARBINARY(MAX), N'b27137c42c73835f99464b920d0f3a9b9cc19a9a301332682acc128981e8144f')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ef14d7e1-3c85-4f47-a4d3-b6e61a4c2597'))) <> CONVERT(VARBINARY(MAX), N'ef14d7e1-3c85-4f47-a4d3-b6e61a4c2597')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:36.6196813+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:40.0968292+00:00', 127)),
        InputSha256 = N'b27137c42c73835f99464b920d0f3a9b9cc19a9a301332682acc128981e8144f',
        OutputCharacters = 8,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'ef14d7e1-3c85-4f47-a4d3-b6e61a4c2597' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'ef14d7e1-3c85-4f47-a4d3-b6e61a4c2597';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'ef14d7e1-3c85-4f47-a4d3-b6e61a4c2597')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'ef14d7e1-3c85-4f47-a4d3-b6e61a4c2597');

    -- Agent run audit 49c0c435-a5a9-4d95-834b-408401846c27
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:41.9786515+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:41.9786515+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:45.7296486+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:45.7296486+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'49c0c435-a5a9-4d95-834b-408401846c27'))) <> CONVERT(VARBINARY(MAX), N'49c0c435-a5a9-4d95-834b-408401846c27')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a1c30e8b8ab22872ce6324cea2f021e7bcdff6c099ab527bb999fe40e541bb14'))) <> CONVERT(VARBINARY(MAX), N'a1c30e8b8ab22872ce6324cea2f021e7bcdff6c099ab527bb999fe40e541bb14')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:41.9786515+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:45.7296486+00:00', 127)),
        InputSha256 = N'a1c30e8b8ab22872ce6324cea2f021e7bcdff6c099ab527bb999fe40e541bb14',
        OutputCharacters = 8,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'49c0c435-a5a9-4d95-834b-408401846c27' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'49c0c435-a5a9-4d95-834b-408401846c27';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'49c0c435-a5a9-4d95-834b-408401846c27')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'49c0c435-a5a9-4d95-834b-408401846c27');

    -- Agent run audit 1b421505-1ac5-4688-bde7-30cad50464db
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1b421505-1ac5-4688-bde7-30cad50464db'))) <> CONVERT(VARBINARY(MAX), N'1b421505-1ac5-4688-bde7-30cad50464db')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:36.4920098+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:36.4920098+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:40.0015835+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:40.0015835+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'8eb1d15e49c28a83fe49e35c33aa550dd2bea530cf23e3dce4d5978b1929f820'))) <> CONVERT(VARBINARY(MAX), N'8eb1d15e49c28a83fe49e35c33aa550dd2bea530cf23e3dce4d5978b1929f820')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:36.4920098+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:40.0015835+00:00', 127)),
        InputSha256 = N'8eb1d15e49c28a83fe49e35c33aa550dd2bea530cf23e3dce4d5978b1929f820',
        OutputCharacters = 8,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'1b421505-1ac5-4688-bde7-30cad50464db' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'1b421505-1ac5-4688-bde7-30cad50464db';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'1b421505-1ac5-4688-bde7-30cad50464db')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'1b421505-1ac5-4688-bde7-30cad50464db');

    -- Agent run audit 5baaa061-2c9b-4a84-ae22-d9a43dc0ea01
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T06:25:45.1418674+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T06:25:45.1418674+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-13T06:25:46.8956282+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-13T06:25:46.8956282+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5baaa061-2c9b-4a84-ae22-d9a43dc0ea01'))) <> CONVERT(VARBINARY(MAX), N'5baaa061-2c9b-4a84-ae22-d9a43dc0ea01')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'670d9743542cae3ea7ebe36af56bd53648b0a1126162e78d81a32934a711302e'))) <> CONVERT(VARBINARY(MAX), N'670d9743542cae3ea7ebe36af56bd53648b0a1126162e78d81a32934a711302e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T06:25:45.1418674+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-13T06:25:46.8956282+00:00', 127)),
        InputSha256 = N'670d9743542cae3ea7ebe36af56bd53648b0a1126162e78d81a32934a711302e',
        OutputCharacters = 8,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'5baaa061-2c9b-4a84-ae22-d9a43dc0ea01' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'5baaa061-2c9b-4a84-ae22-d9a43dc0ea01';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'5baaa061-2c9b-4a84-ae22-d9a43dc0ea01')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'5baaa061-2c9b-4a84-ae22-d9a43dc0ea01');

    -- Agent run audit 4169f038-34e9-4296-af60-ca8f8e10004d
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0ffe1abd1a08215353c233d6e009613e95eec4253832a761af28ff37ac5a150c'))) <> CONVERT(VARBINARY(MAX), N'0ffe1abd1a08215353c233d6e009613e95eec4253832a761af28ff37ac5a150c')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:58:22.9931702+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:58:22.9931702+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4169f038-34e9-4296-af60-ca8f8e10004d'))) <> CONVERT(VARBINARY(MAX), N'4169f038-34e9-4296-af60-ca8f8e10004d')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Running'))) <> CONVERT(VARBINARY(MAX), N'Running')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Running',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:58:22.9931702+00:00', 127)),
        FinishedAtUtc = NULL,
        InputSha256 = N'0ffe1abd1a08215353c233d6e009613e95eec4253832a761af28ff37ac5a150c',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'4169f038-34e9-4296-af60-ca8f8e10004d' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'4169f038-34e9-4296-af60-ca8f8e10004d';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'4169f038-34e9-4296-af60-ca8f8e10004d')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'4169f038-34e9-4296-af60-ca8f8e10004d');

    -- Agent run audit 5c5a5798-a882-4f52-8599-c1273568d592
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:59:23.5805914+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:59:23.5805914+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5c5a5798-a882-4f52-8599-c1273568d592'))) <> CONVERT(VARBINARY(MAX), N'5c5a5798-a882-4f52-8599-c1273568d592')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Running'))) <> CONVERT(VARBINARY(MAX), N'Running')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0'))) <> CONVERT(VARBINARY(MAX), N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Running',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:23.5805914+00:00', 127)),
        FinishedAtUtc = NULL,
        InputSha256 = N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'5c5a5798-a882-4f52-8599-c1273568d592' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'5c5a5798-a882-4f52-8599-c1273568d592';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'5c5a5798-a882-4f52-8599-c1273568d592')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'5c5a5798-a882-4f52-8599-c1273568d592');

    -- Agent run audit bc926fac-7e16-4570-9db9-d1285732b8ee
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:59:37.0552723+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:59:37.0552723+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Running'))) <> CONVERT(VARBINARY(MAX), N'Running')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'bc926fac-7e16-4570-9db9-d1285732b8ee'))) <> CONVERT(VARBINARY(MAX), N'bc926fac-7e16-4570-9db9-d1285732b8ee')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0'))) <> CONVERT(VARBINARY(MAX), N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Running',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:37.0552723+00:00', 127)),
        FinishedAtUtc = NULL,
        InputSha256 = N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0',
        OutputCharacters = 0,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'bc926fac-7e16-4570-9db9-d1285732b8ee' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'bc926fac-7e16-4570-9db9-d1285732b8ee';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'bc926fac-7e16-4570-9db9-d1285732b8ee')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'bc926fac-7e16-4570-9db9-d1285732b8ee');

    -- Agent run audit d873e730-2cba-4e8d-97a5-ca30905c1ef2
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'119ee882385be8065158ad3be6c143f8c57f57c8893a67dd659ef7ac46c47dbf'))) <> CONVERT(VARBINARY(MAX), N'119ee882385be8065158ad3be6c143f8c57f57c8893a67dd659ef7ac46c47dbf')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T08:08:32.2095804+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T08:08:32.2095804+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T08:08:43.9932838+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T08:08:43.9932838+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'd873e730-2cba-4e8d-97a5-ca30905c1ef2'))) <> CONVERT(VARBINARY(MAX), N'd873e730-2cba-4e8d-97a5-ca30905c1ef2')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T08:08:32.2095804+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T08:08:43.9932838+00:00', 127)),
        InputSha256 = N'119ee882385be8065158ad3be6c143f8c57f57c8893a67dd659ef7ac46c47dbf',
        OutputCharacters = 8,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'd873e730-2cba-4e8d-97a5-ca30905c1ef2' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'd873e730-2cba-4e8d-97a5-ca30905c1ef2';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'd873e730-2cba-4e8d-97a5-ca30905c1ef2')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'd873e730-2cba-4e8d-97a5-ca30905c1ef2');

    -- Agent run audit 28fca0e7-19f8-431c-9388-aa285bbc6fec
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'119ee882385be8065158ad3be6c143f8c57f57c8893a67dd659ef7ac46c47dbf'))) <> CONVERT(VARBINARY(MAX), N'119ee882385be8065158ad3be6c143f8c57f57c8893a67dd659ef7ac46c47dbf')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T15:04:54.3872635+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T15:04:54.3872635+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T15:04:58.9035022+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T15:04:58.9035022+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'28fca0e7-19f8-431c-9388-aa285bbc6fec'))) <> CONVERT(VARBINARY(MAX), N'28fca0e7-19f8-431c-9388-aa285bbc6fec')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-one'))) <> CONVERT(VARBINARY(MAX), N'flow-step-one')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4415f81c-29a1-4412-affd-a5161c72267b',
        AgentCode = N'flow-step-one',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T15:04:54.3872635+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T15:04:58.9035022+00:00', 127)),
        InputSha256 = N'119ee882385be8065158ad3be6c143f8c57f57c8893a67dd659ef7ac46c47dbf',
        OutputCharacters = 8,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'28fca0e7-19f8-431c-9388-aa285bbc6fec' AND AgentId = N'2c1003cd-abad-423f-a604-19279b7a2401';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'28fca0e7-19f8-431c-9388-aa285bbc6fec';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'28fca0e7-19f8-431c-9388-aa285bbc6fec')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'28fca0e7-19f8-431c-9388-aa285bbc6fec');

    -- Agent run audit f8434167-0e94-48f0-8d2a-f0bf4164f9e1
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:53:09.2588983+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:53:09.2588983+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:53:11.6465042+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:53:11.6465042+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f8434167-0e94-48f0-8d2a-f0bf4164f9e1'))) <> CONVERT(VARBINARY(MAX), N'f8434167-0e94-48f0-8d2a-f0bf4164f9e1')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-two'))) <> CONVERT(VARBINARY(MAX), N'flow-step-two')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4820b4bb-93e2-40bb-a849-b80768da34dc',
        AgentCode = N'flow-step-two',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:53:09.2588983+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:53:11.6465042+00:00', 127)),
        InputSha256 = N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee',
        OutputCharacters = 24,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'f8434167-0e94-48f0-8d2a-f0bf4164f9e1' AND AgentId = N'b175ca33-4aba-4d78-b8ae-6bbac3562815';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'f8434167-0e94-48f0-8d2a-f0bf4164f9e1';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'f8434167-0e94-48f0-8d2a-f0bf4164f9e1')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'f8434167-0e94-48f0-8d2a-f0bf4164f9e1');

    -- Agent run audit dd6834f8-f89f-48e1-9a6b-c2fa662c8839
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:34:38.805139+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:34:38.805139+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:34:41.9397032+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:34:41.9397032+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'dd6834f8-f89f-48e1-9a6b-c2fa662c8839'))) <> CONVERT(VARBINARY(MAX), N'dd6834f8-f89f-48e1-9a6b-c2fa662c8839')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-two'))) <> CONVERT(VARBINARY(MAX), N'flow-step-two')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4820b4bb-93e2-40bb-a849-b80768da34dc',
        AgentCode = N'flow-step-two',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:34:38.805139+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:34:41.9397032+00:00', 127)),
        InputSha256 = N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee',
        OutputCharacters = 24,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'dd6834f8-f89f-48e1-9a6b-c2fa662c8839' AND AgentId = N'b175ca33-4aba-4d78-b8ae-6bbac3562815';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'dd6834f8-f89f-48e1-9a6b-c2fa662c8839';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'dd6834f8-f89f-48e1-9a6b-c2fa662c8839')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'dd6834f8-f89f-48e1-9a6b-c2fa662c8839');

    -- Agent run audit 91210481-1152-4ccb-a527-aceb3972235b
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:24.0350008+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:24.0350008+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:26.2174191+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:26.2174191+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'91210481-1152-4ccb-a527-aceb3972235b'))) <> CONVERT(VARBINARY(MAX), N'91210481-1152-4ccb-a527-aceb3972235b')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-two'))) <> CONVERT(VARBINARY(MAX), N'flow-step-two')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4820b4bb-93e2-40bb-a849-b80768da34dc',
        AgentCode = N'flow-step-two',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:24.0350008+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:26.2174191+00:00', 127)),
        InputSha256 = N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee',
        OutputCharacters = 24,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'91210481-1152-4ccb-a527-aceb3972235b' AND AgentId = N'b175ca33-4aba-4d78-b8ae-6bbac3562815';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'91210481-1152-4ccb-a527-aceb3972235b';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'91210481-1152-4ccb-a527-aceb3972235b')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'91210481-1152-4ccb-a527-aceb3972235b');

    -- Agent run audit cf91dd8f-bd95-4ac7-8fd7-5adaf330dcd2
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:41.160982+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:41.160982+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:47.521841+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:47.521841+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'cf91dd8f-bd95-4ac7-8fd7-5adaf330dcd2'))) <> CONVERT(VARBINARY(MAX), N'cf91dd8f-bd95-4ac7-8fd7-5adaf330dcd2')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-two'))) <> CONVERT(VARBINARY(MAX), N'flow-step-two')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4820b4bb-93e2-40bb-a849-b80768da34dc',
        AgentCode = N'flow-step-two',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:41.160982+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:47.521841+00:00', 127)),
        InputSha256 = N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee',
        OutputCharacters = 24,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'cf91dd8f-bd95-4ac7-8fd7-5adaf330dcd2' AND AgentId = N'b175ca33-4aba-4d78-b8ae-6bbac3562815';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'cf91dd8f-bd95-4ac7-8fd7-5adaf330dcd2';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'cf91dd8f-bd95-4ac7-8fd7-5adaf330dcd2')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'cf91dd8f-bd95-4ac7-8fd7-5adaf330dcd2');

    -- Agent run audit 28b67069-5e8d-4bbd-94a4-b1615b1d3b88
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:46.7072957+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:46.7072957+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:49.8005914+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:49.8005914+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'28b67069-5e8d-4bbd-94a4-b1615b1d3b88'))) <> CONVERT(VARBINARY(MAX), N'28b67069-5e8d-4bbd-94a4-b1615b1d3b88')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-two'))) <> CONVERT(VARBINARY(MAX), N'flow-step-two')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4820b4bb-93e2-40bb-a849-b80768da34dc',
        AgentCode = N'flow-step-two',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:46.7072957+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:49.8005914+00:00', 127)),
        InputSha256 = N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee',
        OutputCharacters = 24,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'28b67069-5e8d-4bbd-94a4-b1615b1d3b88' AND AgentId = N'b175ca33-4aba-4d78-b8ae-6bbac3562815';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'28b67069-5e8d-4bbd-94a4-b1615b1d3b88';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'28b67069-5e8d-4bbd-94a4-b1615b1d3b88')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'28b67069-5e8d-4bbd-94a4-b1615b1d3b88');

    -- Agent run audit 2167c7e6-a370-48d4-bfb6-ed8da3d41f87
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:40.9115365+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:40.9115365+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:43.3667662+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:43.3667662+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2167c7e6-a370-48d4-bfb6-ed8da3d41f87'))) <> CONVERT(VARBINARY(MAX), N'2167c7e6-a370-48d4-bfb6-ed8da3d41f87')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-two'))) <> CONVERT(VARBINARY(MAX), N'flow-step-two')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4820b4bb-93e2-40bb-a849-b80768da34dc',
        AgentCode = N'flow-step-two',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:40.9115365+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:43.3667662+00:00', 127)),
        InputSha256 = N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee',
        OutputCharacters = 24,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'2167c7e6-a370-48d4-bfb6-ed8da3d41f87' AND AgentId = N'b175ca33-4aba-4d78-b8ae-6bbac3562815';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'2167c7e6-a370-48d4-bfb6-ed8da3d41f87';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'2167c7e6-a370-48d4-bfb6-ed8da3d41f87')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'2167c7e6-a370-48d4-bfb6-ed8da3d41f87');

    -- Agent run audit bc883ffd-f4dd-4c2c-a380-818d24b7148e
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T08:08:45.2584109+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T08:08:45.2584109+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T08:08:47.4960958+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T08:08:47.4960958+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'bc883ffd-f4dd-4c2c-a380-818d24b7148e'))) <> CONVERT(VARBINARY(MAX), N'bc883ffd-f4dd-4c2c-a380-818d24b7148e')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-two'))) <> CONVERT(VARBINARY(MAX), N'flow-step-two')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4820b4bb-93e2-40bb-a849-b80768da34dc',
        AgentCode = N'flow-step-two',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T08:08:45.2584109+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T08:08:47.4960958+00:00', 127)),
        InputSha256 = N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee',
        OutputCharacters = 24,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'bc883ffd-f4dd-4c2c-a380-818d24b7148e' AND AgentId = N'b175ca33-4aba-4d78-b8ae-6bbac3562815';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'bc883ffd-f4dd-4c2c-a380-818d24b7148e';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'bc883ffd-f4dd-4c2c-a380-818d24b7148e')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'bc883ffd-f4dd-4c2c-a380-818d24b7148e');

    -- Agent run audit 6a1c8bdf-8a4c-430e-b606-16d5c3acede4
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T15:05:01.2569393+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T15:05:01.2569393+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T15:05:03.6781167+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T15:05:03.6781167+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6a1c8bdf-8a4c-430e-b606-16d5c3acede4'))) <> CONVERT(VARBINARY(MAX), N'6a1c8bdf-8a4c-430e-b606-16d5c3acede4')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'flow-step-two'))) <> CONVERT(VARBINARY(MAX), N'flow-step-two')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'4820b4bb-93e2-40bb-a849-b80768da34dc',
        AgentCode = N'flow-step-two',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T15:05:01.2569393+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T15:05:03.6781167+00:00', 127)),
        InputSha256 = N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee',
        OutputCharacters = 24,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'6a1c8bdf-8a4c-430e-b606-16d5c3acede4' AND AgentId = N'b175ca33-4aba-4d78-b8ae-6bbac3562815';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'6a1c8bdf-8a4c-430e-b606-16d5c3acede4';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'6a1c8bdf-8a4c-430e-b606-16d5c3acede4')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'6a1c8bdf-8a4c-430e-b606-16d5c3acede4');

    -- Agent run audit 67c86cb8-ca9b-4884-b895-998e6e82258f
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675'))) <> CONVERT(VARBINARY(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:26:06.0608059+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:26:06.0608059+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:26:28.0623777+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:26:28.0623777+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'67c86cb8-ca9b-4884-b895-998e6e82258f'))) <> CONVERT(VARBINARY(MAX), N'67c86cb8-ca9b-4884-b895-998e6e82258f')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9'))) <> CONVERT(VARBINARY(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6'))) <> CONVERT(VARBINARY(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'pdf-acceptance-agent-172555'))) <> CONVERT(VARBINARY(MAX), N'pdf-acceptance-agent-172555')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9',
        AgentCode = N'pdf-acceptance-agent-172555',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:26:06.0608059+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:26:28.0623777+00:00', 127)),
        InputSha256 = N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675',
        OutputCharacters = 69,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'67c86cb8-ca9b-4884-b895-998e6e82258f' AND AgentId = N'ff84c83b-3adb-4f9f-950d-030056f4eeb6';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'67c86cb8-ca9b-4884-b895-998e6e82258f';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'67c86cb8-ca9b-4884-b895-998e6e82258f')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'67c86cb8-ca9b-4884-b895-998e6e82258f');

    -- Agent run audit c682978f-827a-4439-ae98-23c1d16829e6
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675'))) <> CONVERT(VARBINARY(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:38:38.9626559+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:38:38.9626559+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:38:46.4890448+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:38:46.4890448+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9'))) <> CONVERT(VARBINARY(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c682978f-827a-4439-ae98-23c1d16829e6'))) <> CONVERT(VARBINARY(MAX), N'c682978f-827a-4439-ae98-23c1d16829e6')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6'))) <> CONVERT(VARBINARY(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'pdf-acceptance-agent-172555'))) <> CONVERT(VARBINARY(MAX), N'pdf-acceptance-agent-172555')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9',
        AgentCode = N'pdf-acceptance-agent-172555',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:38:38.9626559+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:38:46.4890448+00:00', 127)),
        InputSha256 = N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675',
        OutputCharacters = 11,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'c682978f-827a-4439-ae98-23c1d16829e6' AND AgentId = N'ff84c83b-3adb-4f9f-950d-030056f4eeb6';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'c682978f-827a-4439-ae98-23c1d16829e6';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'c682978f-827a-4439-ae98-23c1d16829e6')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'c682978f-827a-4439-ae98-23c1d16829e6');

    -- Agent run audit c22f8c13-39d4-452b-82a4-671c0e18350f
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675'))) <> CONVERT(VARBINARY(MAX), N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:39:12.0652744+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:39:12.0652744+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-10T09:39:16.9436054+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-10T09:39:16.9436054+00:00')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9'))) <> CONVERT(VARBINARY(MAX), N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'Completed'))) <> CONVERT(VARBINARY(MAX), N'Completed')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'c22f8c13-39d4-452b-82a4-671c0e18350f'))) <> CONVERT(VARBINARY(MAX), N'c22f8c13-39d4-452b-82a4-671c0e18350f')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6'))) <> CONVERT(VARBINARY(MAX), N'ff84c83b-3adb-4f9f-950d-030056f4eeb6')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'pdf-acceptance-agent-172555'))) <> CONVERT(VARBINARY(MAX), N'pdf-acceptance-agent-172555')
        THROW 51923, N'Agent run audit text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgAgentRunAudit SET
        AgentVersionId = N'86263d9b-e0ca-4bac-a0de-c80318dfd8f9',
        AgentCode = N'pdf-acceptance-agent-172555',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:39:12.0652744+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-10T09:39:16.9436054+00:00', 127)),
        InputSha256 = N'13cf587b98db0bcfad1480c1dc8b8a790d301b48964820c2b59566e444046675',
        OutputCharacters = 11,
        ToolCallCount = 0,
        ErrorCode = N''
    WHERE ID = N'c22f8c13-39d4-452b-82a4-671c0e18350f' AND AgentId = N'ff84c83b-3adb-4f9f-950d-030056f4eeb6';
    IF @@ROWCOUNT <> 1 THROW 51921, N'Agent run audit source row was not found.', 1;
    DELETE FROM dbo.AgAgentToolCallAudit WHERE RunId = N'c22f8c13-39d4-452b-82a4-671c0e18350f';
    IF NOT EXISTS (SELECT 1 FROM dbo.AgAgentRunAuditNormalizationCheckpoint WHERE RunId = N'c22f8c13-39d4-452b-82a4-671c0e18350f')
        INSERT INTO dbo.AgAgentRunAuditNormalizationCheckpoint (RunId) VALUES (N'c22f8c13-39d4-452b-82a4-671c0e18350f');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
