-- Normalize Orchestration Runs exported from current SQL Server data.
-- Source row-set SHA-256: ed209e6b342ac42551fbb05f199b79c0a78e6a2a2aefdbd7f401a01577344613
-- Run 040 and 041 first, then this script, then Data/042.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.AgOrchestrationRun', N'DocumentJson') IS NULL
    THROW 51820, N'DocumentJson is absent; Orchestration Run cutover was already finalized.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AgOrchestrationRunNormalizationCheckpoint', N'U') IS NULL
        CREATE TABLE dbo.AgOrchestrationRunNormalizationCheckpoint (RunId CHAR(36) NOT NULL PRIMARY KEY);

    -- Orchestration Run 0a1a9364-97ad-4574-a577-fb602055728a
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0a1a9364-97ad-4574-a577-fb602055728a'))) <> CONVERT(VARBINARY(MAX), N'0a1a9364-97ad-4574-a577-fb602055728a')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:40:04.2390074+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:40:04.2390074+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:40:04.3783902+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:40:04.3783902+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:40:12.9827676+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:40:12.9827676+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:40:13.0725017+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:40:13.0725017+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'))) <> CONVERT(VARBINARY(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f38eed6e-3474-48f5-b8b0-26616b769d22'))) <> CONVERT(VARBINARY(MAX), N'f38eed6e-3474-48f5-b8b0-26616b769d22')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query-supplier'))) <> CONVERT(VARBINARY(MAX), N'query-supplier')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'查询供应商'))) <> CONVERT(VARBINARY(MAX), N'查询供应商')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'f38eed6e-3474-48f5-b8b0-26616b769d22',
        OrchestrationCode = N'supplier-flow',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:40:04.2390074+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:40:13.0725017+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        ErrorCode = N''
    WHERE ID = N'0a1a9364-97ad-4574-a577-fb602055728a' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'0a1a9364-97ad-4574-a577-fb602055728a';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'cb7b735b-f7a3-55e3-8fc3-e4cf7e6363c4', N'0a1a9364-97ad-4574-a577-fb602055728a', 0, N'query-supplier', N'查询供应商', N'2999f08b-fcef-4d4c-ab30-f1443048b6f0', N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:40:04.3783902+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:40:12.9827676+00:00', 127)), 36, N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788', N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'0a1a9364-97ad-4574-a577-fb602055728a')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'0a1a9364-97ad-4574-a577-fb602055728a');

    -- Orchestration Run 0f40565e-7782-4d28-b3a9-eae149f136bb
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0f40565e-7782-4d28-b3a9-eae149f136bb'))) <> CONVERT(VARBINARY(MAX), N'0f40565e-7782-4d28-b3a9-eae149f136bb')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:45:15.82013+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:45:15.82013+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:45:15.9608519+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:45:15.9608519+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:45:30.7333245+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:45:30.7333245+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:45:30.8048758+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:45:30.8048758+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'))) <> CONVERT(VARBINARY(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f38eed6e-3474-48f5-b8b0-26616b769d22'))) <> CONVERT(VARBINARY(MAX), N'f38eed6e-3474-48f5-b8b0-26616b769d22')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query-supplier'))) <> CONVERT(VARBINARY(MAX), N'query-supplier')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'查询供应商'))) <> CONVERT(VARBINARY(MAX), N'查询供应商')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'f38eed6e-3474-48f5-b8b0-26616b769d22',
        OrchestrationCode = N'supplier-flow',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:45:15.82013+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:45:30.8048758+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        ErrorCode = N''
    WHERE ID = N'0f40565e-7782-4d28-b3a9-eae149f136bb' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'0f40565e-7782-4d28-b3a9-eae149f136bb';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'd02ff7e8-67d6-5f77-870d-78917493bd03', N'0f40565e-7782-4d28-b3a9-eae149f136bb', 0, N'query-supplier', N'查询供应商', N'2999f08b-fcef-4d4c-ab30-f1443048b6f0', N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:45:15.9608519+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:45:30.7333245+00:00', 127)), 65, N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788', N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'0f40565e-7782-4d28-b3a9-eae149f136bb')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'0f40565e-7782-4d28-b3a9-eae149f136bb');

    -- Orchestration Run 0e475eb2-12e3-48e0-82d2-654e1a4489e1
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0e475eb2-12e3-48e0-82d2-654e1a4489e1'))) <> CONVERT(VARBINARY(MAX), N'0e475eb2-12e3-48e0-82d2-654e1a4489e1')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:52:57.3576245+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:52:57.3576245+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:52:57.4722785+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:52:57.4722785+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:53:09.0437999+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:53:09.0437999+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:53:09.1373528+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:53:09.1373528+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:53:11.7172236+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:53:11.7172236+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T05:53:11.8222467+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T05:53:11.8222467+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5885a518167b37b112ee4f3e6a6115d27745e1c4fa27cdc2881b8a36a0d0abe1'))) <> CONVERT(VARBINARY(MAX), N'5885a518167b37b112ee4f3e6a6115d27745e1c4fa27cdc2881b8a36a0d0abe1')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6ee02027-b7c6-4cec-a038-02382e7f68ad'))) <> CONVERT(VARBINARY(MAX), N'6ee02027-b7c6-4cec-a038-02382e7f68ad')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-two'))) <> CONVERT(VARBINARY(MAX), N'step-two')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'6ee02027-b7c6-4cec-a038-02382e7f68ad',
        OrchestrationCode = N'supplier-flow',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:52:57.3576245+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:53:11.8222467+00:00', 127)),
        InputSha256 = N'5885a518167b37b112ee4f3e6a6115d27745e1c4fa27cdc2881b8a36a0d0abe1',
        ErrorCode = N''
    WHERE ID = N'0e475eb2-12e3-48e0-82d2-654e1a4489e1' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'0e475eb2-12e3-48e0-82d2-654e1a4489e1';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'50888b25-fe48-52e6-ab91-4c464fed7328', N'0e475eb2-12e3-48e0-82d2-654e1a4489e1', 0, N'step-one', N'第一步', N'2c1003cd-abad-423f-a604-19279b7a2401', N'4415f81c-29a1-4412-affd-a5161c72267b', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:52:57.4722785+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:53:09.0437999+00:00', 127)), 8, N'5885a518167b37b112ee4f3e6a6115d27745e1c4fa27cdc2881b8a36a0d0abe1', N'');
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'81751bfa-6334-586b-acd8-2e68b69b9d23', N'0e475eb2-12e3-48e0-82d2-654e1a4489e1', 1, N'step-two', N'', N'b175ca33-4aba-4d78-b8ae-6bbac3562815', N'4820b4bb-93e2-40bb-a849-b80768da34dc', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:53:09.1373528+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T05:53:11.7172236+00:00', 127)), 24, N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee', N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'0e475eb2-12e3-48e0-82d2-654e1a4489e1')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'0e475eb2-12e3-48e0-82d2-654e1a4489e1');

    -- Orchestration Run 2a8dec3a-1e7b-4644-9039-f825afc1b8c5
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:34:34.6426866+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:34:34.6426866+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:34:34.9249415+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:34:34.9249415+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:34:38.4300717+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:34:38.4300717+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:34:38.5439505+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:34:38.5439505+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:34:42.4632887+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:34:42.4632887+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:34:42.7306134+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:34:42.7306134+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2a8dec3a-1e7b-4644-9039-f825afc1b8c5'))) <> CONVERT(VARBINARY(MAX), N'2a8dec3a-1e7b-4644-9039-f825afc1b8c5')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3ca591fadf790701a82e12bcd3434d8862c521a288f38100703eb64d377507a9'))) <> CONVERT(VARBINARY(MAX), N'3ca591fadf790701a82e12bcd3434d8862c521a288f38100703eb64d377507a9')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'632b45a2-5145-469e-a35c-c71b656885c2'))) <> CONVERT(VARBINARY(MAX), N'632b45a2-5145-469e-a35c-c71b656885c2')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-two'))) <> CONVERT(VARBINARY(MAX), N'step-two')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第二'))) <> CONVERT(VARBINARY(MAX), N'第二')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'632b45a2-5145-469e-a35c-c71b656885c2',
        OrchestrationCode = N'supplier-flow',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:34:34.6426866+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:34:42.7306134+00:00', 127)),
        InputSha256 = N'3ca591fadf790701a82e12bcd3434d8862c521a288f38100703eb64d377507a9',
        ErrorCode = N''
    WHERE ID = N'2a8dec3a-1e7b-4644-9039-f825afc1b8c5' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'2a8dec3a-1e7b-4644-9039-f825afc1b8c5';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'998c850f-a13e-5870-b17b-70c36a74b538', N'2a8dec3a-1e7b-4644-9039-f825afc1b8c5', 0, N'step-one', N'第一步', N'2c1003cd-abad-423f-a604-19279b7a2401', N'4415f81c-29a1-4412-affd-a5161c72267b', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:34:34.9249415+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:34:38.4300717+00:00', 127)), 8, N'3ca591fadf790701a82e12bcd3434d8862c521a288f38100703eb64d377507a9', N'');
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'032612c3-683e-5e1c-945c-3ee83495d58c', N'2a8dec3a-1e7b-4644-9039-f825afc1b8c5', 1, N'step-two', N'第二', N'b175ca33-4aba-4d78-b8ae-6bbac3562815', N'4820b4bb-93e2-40bb-a849-b80768da34dc', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:34:38.5439505+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:34:42.4632887+00:00', 127)), 24, N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee', N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'2a8dec3a-1e7b-4644-9039-f825afc1b8c5')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'2a8dec3a-1e7b-4644-9039-f825afc1b8c5');

    -- Orchestration Run 23f8c3e4-7c1b-41ec-a9af-5149b84aa0da
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788'))) <> CONVERT(VARBINARY(MAX), N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:35:16.9037318+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:35:16.9037318+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:35:17.1428863+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:35:17.1428863+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:35:30.1046779+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:35:30.1046779+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-07-29T06:35:30.3714361+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-07-29T06:35:30.3714361+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'23f8c3e4-7c1b-41ec-a9af-5149b84aa0da'))) <> CONVERT(VARBINARY(MAX), N'23f8c3e4-7c1b-41ec-a9af-5149b84aa0da')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'))) <> CONVERT(VARBINARY(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a91dd682-8778-4e35-95af-94cf9e56680f'))) <> CONVERT(VARBINARY(MAX), N'a91dd682-8778-4e35-95af-94cf9e56680f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'a91dd682-8778-4e35-95af-94cf9e56680f',
        OrchestrationCode = N'supplier-flow',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:35:16.9037318+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:35:30.3714361+00:00', 127)),
        InputSha256 = N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788',
        ErrorCode = N''
    WHERE ID = N'23f8c3e4-7c1b-41ec-a9af-5149b84aa0da' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'23f8c3e4-7c1b-41ec-a9af-5149b84aa0da';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'08292280-ac11-5e07-8fae-cbb64900b2e9', N'23f8c3e4-7c1b-41ec-a9af-5149b84aa0da', 0, N'step-one', N'第一步', N'2999f08b-fcef-4d4c-ab30-f1443048b6f0', N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:35:17.1428863+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-07-29T06:35:30.1046779+00:00', 127)), 161, N'13fc60aa7033f33984f28eb6eb91d39887bc85bbaebcbce21614d0ccfcfb7788', N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'23f8c3e4-7c1b-41ec-a9af-5149b84aa0da')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'23f8c3e4-7c1b-41ec-a9af-5149b84aa0da');

    -- Orchestration Run 11393c95-ccea-4f1e-8e99-41c4d1b2f9b8
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'11393c95-ccea-4f1e-8e99-41c4d1b2f9b8'))) <> CONVERT(VARBINARY(MAX), N'11393c95-ccea-4f1e-8e99-41c4d1b2f9b8')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:18.5362609+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:18.5362609+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:19.1335159+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:19.1335159+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:23.6566235+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:23.6566235+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:23.7821485+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:23.7821485+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:26.5736669+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:26.5736669+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:49:26.8525643+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:49:26.8525643+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc'))) <> CONVERT(VARBINARY(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5d04224b427d0a1da3a18113d5b9d86be3c2f173188f28dc6b2d1a5b3fdc48cb'))) <> CONVERT(VARBINARY(MAX), N'5d04224b427d0a1da3a18113d5b9d86be3c2f173188f28dc6b2d1a5b3fdc48cb')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-two'))) <> CONVERT(VARBINARY(MAX), N'step-two')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第二'))) <> CONVERT(VARBINARY(MAX), N'第二')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'2441a424-257a-45c1-8c4a-6f320f0809cc',
        OrchestrationCode = N'supplier-flow',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:18.5362609+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:26.8525643+00:00', 127)),
        InputSha256 = N'5d04224b427d0a1da3a18113d5b9d86be3c2f173188f28dc6b2d1a5b3fdc48cb',
        ErrorCode = N''
    WHERE ID = N'11393c95-ccea-4f1e-8e99-41c4d1b2f9b8' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'11393c95-ccea-4f1e-8e99-41c4d1b2f9b8';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'977f2067-ed32-5b48-84b8-9c82d3228262', N'11393c95-ccea-4f1e-8e99-41c4d1b2f9b8', 0, N'step-one', N'第一步', N'2c1003cd-abad-423f-a604-19279b7a2401', N'4415f81c-29a1-4412-affd-a5161c72267b', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:19.1335159+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:23.6566235+00:00', 127)), 8, N'5d04224b427d0a1da3a18113d5b9d86be3c2f173188f28dc6b2d1a5b3fdc48cb', N'');
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'd0cbc9e4-02e3-5860-9228-bfad10f7890d', N'11393c95-ccea-4f1e-8e99-41c4d1b2f9b8', 1, N'step-two', N'第二', N'b175ca33-4aba-4d78-b8ae-6bbac3562815', N'4820b4bb-93e2-40bb-a849-b80768da34dc', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:23.7821485+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:49:26.5736669+00:00', 127)), 24, N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee', N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'11393c95-ccea-4f1e-8e99-41c4d1b2f9b8')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'11393c95-ccea-4f1e-8e99-41c4d1b2f9b8');

    -- Orchestration Run edb8aee1-4b83-4b5d-9617-1a643e2f9d1d
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:35.7977356+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:35.7977356+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:36.2560557+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:36.2560557+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:40.7631745+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:40.7631745+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:40.8991697+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:40.8991697+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:48.0820872+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:48.0820872+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T07:50:48.3531627+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T07:50:48.3531627+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc'))) <> CONVERT(VARBINARY(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b27137c42c73835f99464b920d0f3a9b9cc19a9a301332682acc128981e8144f'))) <> CONVERT(VARBINARY(MAX), N'b27137c42c73835f99464b920d0f3a9b9cc19a9a301332682acc128981e8144f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'edb8aee1-4b83-4b5d-9617-1a643e2f9d1d'))) <> CONVERT(VARBINARY(MAX), N'edb8aee1-4b83-4b5d-9617-1a643e2f9d1d')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-two'))) <> CONVERT(VARBINARY(MAX), N'step-two')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第二'))) <> CONVERT(VARBINARY(MAX), N'第二')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'2441a424-257a-45c1-8c4a-6f320f0809cc',
        OrchestrationCode = N'supplier-flow',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:35.7977356+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:48.3531627+00:00', 127)),
        InputSha256 = N'b27137c42c73835f99464b920d0f3a9b9cc19a9a301332682acc128981e8144f',
        ErrorCode = N''
    WHERE ID = N'edb8aee1-4b83-4b5d-9617-1a643e2f9d1d' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'edb8aee1-4b83-4b5d-9617-1a643e2f9d1d';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'61e66bf7-ded0-5491-a45e-32b3aafd57fc', N'edb8aee1-4b83-4b5d-9617-1a643e2f9d1d', 0, N'step-one', N'第一步', N'2c1003cd-abad-423f-a604-19279b7a2401', N'4415f81c-29a1-4412-affd-a5161c72267b', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:36.2560557+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:40.7631745+00:00', 127)), 8, N'b27137c42c73835f99464b920d0f3a9b9cc19a9a301332682acc128981e8144f', N'');
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'a235a30d-3f43-5f26-943e-15be59e3a53c', N'edb8aee1-4b83-4b5d-9617-1a643e2f9d1d', 1, N'step-two', N'第二', N'b175ca33-4aba-4d78-b8ae-6bbac3562815', N'4820b4bb-93e2-40bb-a849-b80768da34dc', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:40.8991697+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T07:50:48.0820872+00:00', 127)), 24, N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee', N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'edb8aee1-4b83-4b5d-9617-1a643e2f9d1d')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'edb8aee1-4b83-4b5d-9617-1a643e2f9d1d');

    -- Orchestration Run 1155061e-85b5-4788-81bb-360eecef7626
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1155061e-85b5-4788-81bb-360eecef7626'))) <> CONVERT(VARBINARY(MAX), N'1155061e-85b5-4788-81bb-360eecef7626')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:41.0758161+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:41.0758161+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:41.6390316+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:41.6390316+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:46.2794793+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:46.2794793+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:46.437977+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:46.437977+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:50.3206567+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:50.3206567+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-01T09:03:50.5664083+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-01T09:03:50.5664083+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc'))) <> CONVERT(VARBINARY(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a1c30e8b8ab22872ce6324cea2f021e7bcdff6c099ab527bb999fe40e541bb14'))) <> CONVERT(VARBINARY(MAX), N'a1c30e8b8ab22872ce6324cea2f021e7bcdff6c099ab527bb999fe40e541bb14')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-two'))) <> CONVERT(VARBINARY(MAX), N'step-two')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第二'))) <> CONVERT(VARBINARY(MAX), N'第二')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'2441a424-257a-45c1-8c4a-6f320f0809cc',
        OrchestrationCode = N'supplier-flow',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:41.0758161+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:50.5664083+00:00', 127)),
        InputSha256 = N'a1c30e8b8ab22872ce6324cea2f021e7bcdff6c099ab527bb999fe40e541bb14',
        ErrorCode = N''
    WHERE ID = N'1155061e-85b5-4788-81bb-360eecef7626' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'1155061e-85b5-4788-81bb-360eecef7626';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'53bea3ef-9556-5708-bba0-74624489544d', N'1155061e-85b5-4788-81bb-360eecef7626', 0, N'step-one', N'第一步', N'2c1003cd-abad-423f-a604-19279b7a2401', N'4415f81c-29a1-4412-affd-a5161c72267b', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:41.6390316+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:46.2794793+00:00', 127)), 8, N'a1c30e8b8ab22872ce6324cea2f021e7bcdff6c099ab527bb999fe40e541bb14', N'');
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'aa48a882-f02e-5208-8a35-c42c459e7c4d', N'1155061e-85b5-4788-81bb-360eecef7626', 1, N'step-two', N'第二', N'b175ca33-4aba-4d78-b8ae-6bbac3562815', N'4820b4bb-93e2-40bb-a849-b80768da34dc', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:46.437977+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-01T09:03:50.3206567+00:00', 127)), 24, N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee', N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'1155061e-85b5-4788-81bb-360eecef7626')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'1155061e-85b5-4788-81bb-360eecef7626');

    -- Orchestration Run fda11f13-f62c-4b8b-a159-5af1ab123fc5
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:35.572676+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:35.572676+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:36.1764626+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:36.1764626+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:40.5255155+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:40.5255155+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:40.6626896+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:40.6626896+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:44.0138206+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:44.0138206+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-04T08:19:44.3130571+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-04T08:19:44.3130571+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc'))) <> CONVERT(VARBINARY(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'8eb1d15e49c28a83fe49e35c33aa550dd2bea530cf23e3dce4d5978b1929f820'))) <> CONVERT(VARBINARY(MAX), N'8eb1d15e49c28a83fe49e35c33aa550dd2bea530cf23e3dce4d5978b1929f820')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'fda11f13-f62c-4b8b-a159-5af1ab123fc5'))) <> CONVERT(VARBINARY(MAX), N'fda11f13-f62c-4b8b-a159-5af1ab123fc5')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-two'))) <> CONVERT(VARBINARY(MAX), N'step-two')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第二'))) <> CONVERT(VARBINARY(MAX), N'第二')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'2441a424-257a-45c1-8c4a-6f320f0809cc',
        OrchestrationCode = N'supplier-flow',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:35.572676+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:44.3130571+00:00', 127)),
        InputSha256 = N'8eb1d15e49c28a83fe49e35c33aa550dd2bea530cf23e3dce4d5978b1929f820',
        ErrorCode = N''
    WHERE ID = N'fda11f13-f62c-4b8b-a159-5af1ab123fc5' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'fda11f13-f62c-4b8b-a159-5af1ab123fc5';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'5477159a-f081-59b8-b82e-d6aff9ac8374', N'fda11f13-f62c-4b8b-a159-5af1ab123fc5', 0, N'step-one', N'第一步', N'2c1003cd-abad-423f-a604-19279b7a2401', N'4415f81c-29a1-4412-affd-a5161c72267b', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:36.1764626+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:40.5255155+00:00', 127)), 8, N'8eb1d15e49c28a83fe49e35c33aa550dd2bea530cf23e3dce4d5978b1929f820', N'');
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'0e91b22a-b24a-588b-9e22-c695a17dd6c1', N'fda11f13-f62c-4b8b-a159-5af1ab123fc5', 1, N'step-two', N'第二', N'b175ca33-4aba-4d78-b8ae-6bbac3562815', N'4820b4bb-93e2-40bb-a849-b80768da34dc', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:40.6626896+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-04T08:19:44.0138206+00:00', 127)), 24, N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee', N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'fda11f13-f62c-4b8b-a159-5af1ab123fc5')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'fda11f13-f62c-4b8b-a159-5af1ab123fc5');

    -- Orchestration Run 3ebc7782-b05a-4119-9e90-8928abb81c7a
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0ffe1abd1a08215353c233d6e009613e95eec4253832a761af28ff37ac5a150c'))) <> CONVERT(VARBINARY(MAX), N'0ffe1abd1a08215353c233d6e009613e95eec4253832a761af28ff37ac5a150c')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:58:22.2187396+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:58:22.2187396+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:58:22.3702374+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:58:22.3702374+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:58:23.229155+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:58:23.229155+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc'))) <> CONVERT(VARBINARY(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3ebc7782-b05a-4119-9e90-8928abb81c7a'))) <> CONVERT(VARBINARY(MAX), N'3ebc7782-b05a-4119-9e90-8928abb81c7a')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ORCHESTRATION_RUN_FAILED'))) <> CONVERT(VARBINARY(MAX), N'ORCHESTRATION_RUN_FAILED')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-two'))) <> CONVERT(VARBINARY(MAX), N'step-two')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第二'))) <> CONVERT(VARBINARY(MAX), N'第二')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'2441a424-257a-45c1-8c4a-6f320f0809cc',
        OrchestrationCode = N'supplier-flow',
        Status = N'Failed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:58:22.2187396+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:58:23.229155+00:00', 127)),
        InputSha256 = N'0ffe1abd1a08215353c233d6e009613e95eec4253832a761af28ff37ac5a150c',
        ErrorCode = N'ORCHESTRATION_RUN_FAILED'
    WHERE ID = N'3ebc7782-b05a-4119-9e90-8928abb81c7a' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'3ebc7782-b05a-4119-9e90-8928abb81c7a';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'8dc1ea48-467a-5622-a614-6f7f82341dd4', N'3ebc7782-b05a-4119-9e90-8928abb81c7a', 0, N'step-one', N'第一步', N'2c1003cd-abad-423f-a604-19279b7a2401', N'4415f81c-29a1-4412-affd-a5161c72267b', N'Failed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:58:22.3702374+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:58:23.229155+00:00', 127)), 0, N'0ffe1abd1a08215353c233d6e009613e95eec4253832a761af28ff37ac5a150c', N'ORCHESTRATION_RUN_FAILED');
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'fa37cc50-ce5c-5ae4-8702-c07f1bb480f9', N'3ebc7782-b05a-4119-9e90-8928abb81c7a', 1, N'step-two', N'第二', N'b175ca33-4aba-4d78-b8ae-6bbac3562815', N'4820b4bb-93e2-40bb-a849-b80768da34dc', N'Failed', 0, NULL, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:58:23.229155+00:00', 127)), 0, N'', N'ORCHESTRATION_RUN_FAILED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'3ebc7782-b05a-4119-9e90-8928abb81c7a')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'3ebc7782-b05a-4119-9e90-8928abb81c7a');

    -- Orchestration Run 1c53b4c2-90eb-4fba-bca0-1f7443a2894d
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c53b4c2-90eb-4fba-bca0-1f7443a2894d'))) <> CONVERT(VARBINARY(MAX), N'1c53b4c2-90eb-4fba-bca0-1f7443a2894d')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:59:22.9989261+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:59:22.9989261+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:59:23.154491+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:59:23.154491+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:59:23.8012654+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:59:23.8012654+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc'))) <> CONVERT(VARBINARY(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ORCHESTRATION_RUN_FAILED'))) <> CONVERT(VARBINARY(MAX), N'ORCHESTRATION_RUN_FAILED')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0'))) <> CONVERT(VARBINARY(MAX), N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-two'))) <> CONVERT(VARBINARY(MAX), N'step-two')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第二'))) <> CONVERT(VARBINARY(MAX), N'第二')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'2441a424-257a-45c1-8c4a-6f320f0809cc',
        OrchestrationCode = N'supplier-flow',
        Status = N'Failed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:22.9989261+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:23.8012654+00:00', 127)),
        InputSha256 = N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0',
        ErrorCode = N'ORCHESTRATION_RUN_FAILED'
    WHERE ID = N'1c53b4c2-90eb-4fba-bca0-1f7443a2894d' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'1c53b4c2-90eb-4fba-bca0-1f7443a2894d';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'df30433f-2d28-56ce-9e3c-962305f6a12e', N'1c53b4c2-90eb-4fba-bca0-1f7443a2894d', 0, N'step-one', N'第一步', N'2c1003cd-abad-423f-a604-19279b7a2401', N'4415f81c-29a1-4412-affd-a5161c72267b', N'Failed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:23.154491+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:23.8012654+00:00', 127)), 0, N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0', N'ORCHESTRATION_RUN_FAILED');
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'c0fb784e-3695-5146-92cc-f8074c64b734', N'1c53b4c2-90eb-4fba-bca0-1f7443a2894d', 1, N'step-two', N'第二', N'b175ca33-4aba-4d78-b8ae-6bbac3562815', N'4820b4bb-93e2-40bb-a849-b80768da34dc', N'Failed', 0, NULL, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:23.8012654+00:00', 127)), 0, N'', N'ORCHESTRATION_RUN_FAILED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'1c53b4c2-90eb-4fba-bca0-1f7443a2894d')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'1c53b4c2-90eb-4fba-bca0-1f7443a2894d');

    -- Orchestration Run 52ca49bd-d139-4b48-95e0-c471f2a176b3
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:59:36.3540295+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:59:36.3540295+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:59:36.4900701+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:59:36.4900701+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T07:59:37.2452981+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T07:59:37.2452981+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc'))) <> CONVERT(VARBINARY(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'52ca49bd-d139-4b48-95e0-c471f2a176b3'))) <> CONVERT(VARBINARY(MAX), N'52ca49bd-d139-4b48-95e0-c471f2a176b3')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'ORCHESTRATION_RUN_FAILED'))) <> CONVERT(VARBINARY(MAX), N'ORCHESTRATION_RUN_FAILED')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0'))) <> CONVERT(VARBINARY(MAX), N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-two'))) <> CONVERT(VARBINARY(MAX), N'step-two')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第二'))) <> CONVERT(VARBINARY(MAX), N'第二')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'2441a424-257a-45c1-8c4a-6f320f0809cc',
        OrchestrationCode = N'supplier-flow',
        Status = N'Failed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:36.3540295+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:37.2452981+00:00', 127)),
        InputSha256 = N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0',
        ErrorCode = N'ORCHESTRATION_RUN_FAILED'
    WHERE ID = N'52ca49bd-d139-4b48-95e0-c471f2a176b3' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'52ca49bd-d139-4b48-95e0-c471f2a176b3';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'a79cc46c-93e2-5138-b26a-9757da48a5c1', N'52ca49bd-d139-4b48-95e0-c471f2a176b3', 0, N'step-one', N'第一步', N'2c1003cd-abad-423f-a604-19279b7a2401', N'4415f81c-29a1-4412-affd-a5161c72267b', N'Failed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:36.4900701+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:37.2452981+00:00', 127)), 0, N'e412d5086c0d263946c04baf1a276569340df8ad6aa29fdc8e95b4127c132fd0', N'ORCHESTRATION_RUN_FAILED');
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'8b4274be-a759-5365-af9f-e88fefdd75cf', N'52ca49bd-d139-4b48-95e0-c471f2a176b3', 1, N'step-two', N'第二', N'b175ca33-4aba-4d78-b8ae-6bbac3562815', N'4820b4bb-93e2-40bb-a849-b80768da34dc', N'Failed', 0, NULL, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T07:59:37.2452981+00:00', 127)), 0, N'', N'ORCHESTRATION_RUN_FAILED');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'52ca49bd-d139-4b48-95e0-c471f2a176b3')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'52ca49bd-d139-4b48-95e0-c471f2a176b3');

    -- Orchestration Run 31dd7630-36f0-4acd-88f5-ab508aa91940
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'119ee882385be8065158ad3be6c143f8c57f57c8893a67dd659ef7ac46c47dbf'))) <> CONVERT(VARBINARY(MAX), N'119ee882385be8065158ad3be6c143f8c57f57c8893a67dd659ef7ac46c47dbf')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee'))) <> CONVERT(VARBINARY(MAX), N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T08:08:31.5894266+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T08:08:31.5894266+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T08:08:31.7736193+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T08:08:31.7736193+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T08:08:44.6543153+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T08:08:44.6543153+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T08:08:44.7194529+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T08:08:44.7194529+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T08:08:48.3597833+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T08:08:48.3597833+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2026-08-15T08:08:48.6790578+00:00'))) <> CONVERT(VARBINARY(MAX), N'2026-08-15T08:08:48.6790578+00:00')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc'))) <> CONVERT(VARBINARY(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'31dd7630-36f0-4acd-88f5-ab508aa91940'))) <> CONVERT(VARBINARY(MAX), N'31dd7630-36f0-4acd-88f5-ab508aa91940')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-two'))) <> CONVERT(VARBINARY(MAX), N'step-two')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第二'))) <> CONVERT(VARBINARY(MAX), N'第二')
        THROW 51823, N'Orchestration Run text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationRun SET
        OrchestrationVersionId = N'2441a424-257a-45c1-8c4a-6f320f0809cc',
        OrchestrationCode = N'supplier-flow',
        Status = N'Completed',
        StartedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T08:08:31.5894266+00:00', 127)),
        FinishedAtUtc = CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T08:08:48.6790578+00:00', 127)),
        InputSha256 = N'119ee882385be8065158ad3be6c143f8c57f57c8893a67dd659ef7ac46c47dbf',
        ErrorCode = N''
    WHERE ID = N'31dd7630-36f0-4acd-88f5-ab508aa91940' AND OrchestrationId = N'faeedeb1-74b4-43e0-9a51-64af9d4d808f';
    IF @@ROWCOUNT <> 1 THROW 51821, N'Orchestration Run source row was not found.', 1;
    DELETE FROM dbo.AgOrchestrationRunNode WHERE RunId = N'31dd7630-36f0-4acd-88f5-ab508aa91940';
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'ec03d5d2-a15d-5605-96b6-27f3e554b2d1', N'31dd7630-36f0-4acd-88f5-ab508aa91940', 0, N'step-one', N'第一步', N'2c1003cd-abad-423f-a604-19279b7a2401', N'4415f81c-29a1-4412-affd-a5161c72267b', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T08:08:31.7736193+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T08:08:44.6543153+00:00', 127)), 8, N'119ee882385be8065158ad3be6c143f8c57f57c8893a67dd659ef7ac46c47dbf', N'');
    INSERT INTO dbo.AgOrchestrationRunNode (ID, RunId, Ordinal, NodeId, NodeName, AgentId, AgentVersionId, Status, Attempts, StartedAtUtc, FinishedAtUtc, OutputCharacters, InputSha256, ErrorCode)
    VALUES (N'1071e9cf-ea5c-54db-8c1d-0b0c78313ba7', N'31dd7630-36f0-4acd-88f5-ab508aa91940', 1, N'step-two', N'第二', N'b175ca33-4aba-4d78-b8ae-6bbac3562815', N'4820b4bb-93e2-40bb-a849-b80768da34dc', N'Completed', 1, CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T08:08:44.7194529+00:00', 127)), CONVERT(datetime2(7), CONVERT(datetimeoffset(7), N'2026-08-15T08:08:48.3597833+00:00', 127)), 24, N'1c3cfed91474e264b66de9e4e8b806b0f1a4d9d362a623dc3b97003cd18cc1ee', N'');
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationRunNormalizationCheckpoint WHERE RunId = N'31dd7630-36f0-4acd-88f5-ab508aa91940')
        INSERT INTO dbo.AgOrchestrationRunNormalizationCheckpoint (RunId) VALUES (N'31dd7630-36f0-4acd-88f5-ab508aa91940');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
