-- Normalize Orchestration definitions exported from current SQL Server data.
-- Source row-set SHA-256: 0559cafd13e4080874ecde352106e6f3a4c2fbaf41ef05f1f83bb7e925790086
-- Run SQL Server 020 and 021 first, then this script, then Data/022.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'dbo.AgOrchestrationDefinition', N'DocumentJson') IS NULL
    THROW 51411, N'DocumentJson is absent; Orchestration cutover was already finalized.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.AgOrchestrationNormalizationCheckpoint', N'U') IS NULL
        CREATE TABLE dbo.AgOrchestrationNormalizationCheckpoint (OrchestrationId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);

    -- Orchestration FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N''))) <> CONVERT(VARBINARY(MAX), N'')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'0.1.0'))) <> CONVERT(VARBINARY(MAX), N'0.1.0')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'1.0.0'))) <> CONVERT(VARBINARY(MAX), N'1.0.0')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2.0.0'))) <> CONVERT(VARBINARY(MAX), N'2.0.0')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc'))) <> CONVERT(VARBINARY(MAX), N'2441a424-257a-45c1-8c4a-6f320f0809cc')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'))) <> CONVERT(VARBINARY(MAX), N'2999f08b-fcef-4d4c-ab30-f1443048b6f0')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401'))) <> CONVERT(VARBINARY(MAX), N'2c1003cd-abad-423f-a604-19279b7a2401')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'3.0.0'))) <> CONVERT(VARBINARY(MAX), N'3.0.0')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4.0.0'))) <> CONVERT(VARBINARY(MAX), N'4.0.0')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b'))) <> CONVERT(VARBINARY(MAX), N'4415f81c-29a1-4412-affd-a5161c72267b')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc'))) <> CONVERT(VARBINARY(MAX), N'4820b4bb-93e2-40bb-a849-b80768da34dc')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'5.0.0'))) <> CONVERT(VARBINARY(MAX), N'5.0.0')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'632b45a2-5145-469e-a35c-c71b656885c2'))) <> CONVERT(VARBINARY(MAX), N'632b45a2-5145-469e-a35c-c71b656885c2')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6d3e3b68-016b-416c-ba65-e6a605e7467c'))) <> CONVERT(VARBINARY(MAX), N'6d3e3b68-016b-416c-ba65-e6a605e7467c')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'6ee02027-b7c6-4cec-a038-02382e7f68ad'))) <> CONVERT(VARBINARY(MAX), N'6ee02027-b7c6-4cec-a038-02382e7f68ad')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'))) <> CONVERT(VARBINARY(MAX), N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'P8 真实验收使用的两节点顺序流程'))) <> CONVERT(VARBINARY(MAX), N'P8 真实验收使用的两节点顺序流程')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'a91dd682-8778-4e35-95af-94cf9e56680f'))) <> CONVERT(VARBINARY(MAX), N'a91dd682-8778-4e35-95af-94cf9e56680f')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815'))) <> CONVERT(VARBINARY(MAX), N'b175ca33-4aba-4d78-b8ae-6bbac3562815')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'f38eed6e-3474-48f5-b8b0-26616b769d22'))) <> CONVERT(VARBINARY(MAX), N'f38eed6e-3474-48f5-b8b0-26616b769d22')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f'))) <> CONVERT(VARBINARY(MAX), N'faeedeb1-74b4-43e0-9a51-64af9d4d808f')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'query-supplier'))) <> CONVERT(VARBINARY(MAX), N'query-supplier')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-one'))) <> CONVERT(VARBINARY(MAX), N'step-one')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'step-two'))) <> CONVERT(VARBINARY(MAX), N'step-two')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'supplier-flow'))) <> CONVERT(VARBINARY(MAX), N'supplier-flow')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'{{previous}}'))) <> CONVERT(VARBINARY(MAX), N'{{previous}}')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'供应商查询流程'))) <> CONVERT(VARBINARY(MAX), N'供应商查询流程')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'查询供应商'))) <> CONVERT(VARBINARY(MAX), N'查询供应商')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第一步'))) <> CONVERT(VARBINARY(MAX), N'第一步')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    IF CONVERT(VARBINARY(MAX), CONVERT(NVARCHAR(MAX), CONVERT(VARCHAR(MAX), N'第二'))) <> CONVERT(VARBINARY(MAX), N'第二')
        THROW 51413, N'Orchestration text cannot be represented by VARCHAR under the current database collation.', 1;
    UPDATE dbo.AgOrchestrationDefinition SET
        Name = N'供应商查询流程',
        Description = N'P8 真实验收使用的两节点顺序流程',
        Status = N'Enabled',
        LogicalRevision = 14
    WHERE ID = CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F') AND Code = N'supplier-flow';
    IF @@ROWCOUNT <> 1 THROW 51412, N'Orchestration source row was not found.', 1;
    DELETE binding FROM dbo.AgOrchestrationAgentBinding binding WHERE binding.OrchestrationId = CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F');
    DELETE edge FROM dbo.AgOrchestrationEdge edge WHERE edge.OrchestrationId = CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F');
    DELETE node FROM dbo.AgOrchestrationNode node WHERE node.OrchestrationId = CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F');
    DELETE version FROM dbo.AgOrchestrationVersion version WHERE version.OrchestrationId = CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F');
    INSERT INTO dbo.AgOrchestrationVersion (ID, OrchestrationId, Ordinal, Label, IsDraft, StartNodeId)
    VALUES (CONVERT(uniqueidentifier, N'6d3e3b68-016b-416c-ba65-e6a605e7467c'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), 0, N'0.1.0', 1, N'step-one');
    INSERT INTO dbo.AgOrchestrationNode (ID, OrchestrationId, VersionId, Ordinal, NodeId, Name, AgentId, InputMode, InputTemplate, MaximumRetries, TimeoutSeconds)
    VALUES (CONVERT(uniqueidentifier, N'3bc73fcc-7324-58e0-8c0b-e81cbfb9e1c7'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'6d3e3b68-016b-416c-ba65-e6a605e7467c'), 0, N'step-one', N'第一步', CONVERT(uniqueidentifier, N'2c1003cd-abad-423f-a604-19279b7a2401'), N'InitialInput', N'', 0, 120);
    INSERT INTO dbo.AgOrchestrationNode (ID, OrchestrationId, VersionId, Ordinal, NodeId, Name, AgentId, InputMode, InputTemplate, MaximumRetries, TimeoutSeconds)
    VALUES (CONVERT(uniqueidentifier, N'3a9344ec-1e09-5d03-a3d0-835c02fd078c'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'6d3e3b68-016b-416c-ba65-e6a605e7467c'), 1, N'step-two', N'第二', CONVERT(uniqueidentifier, N'b175ca33-4aba-4d78-b8ae-6bbac3562815'), N'PreviousOutput', N'{{previous}}', 0, 120);
    INSERT INTO dbo.AgOrchestrationEdge (ID, OrchestrationId, VersionId, Ordinal, FromNodeId, ToNodeId, Condition, ConditionValue, SortOrder)
    VALUES (CONVERT(uniqueidentifier, N'819ae462-ee39-533f-94fa-97a1ef36084a'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'6d3e3b68-016b-416c-ba65-e6a605e7467c'), 0, N'step-one', N'step-two', N'Succeeded', N'', 0);
    INSERT INTO dbo.AgOrchestrationVersion (ID, OrchestrationId, Ordinal, Label, IsDraft, StartNodeId)
    VALUES (CONVERT(uniqueidentifier, N'f38eed6e-3474-48f5-b8b0-26616b769d22'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), 1, N'1.0.0', 0, N'query-supplier');
    INSERT INTO dbo.AgOrchestrationNode (ID, OrchestrationId, VersionId, Ordinal, NodeId, Name, AgentId, InputMode, InputTemplate, MaximumRetries, TimeoutSeconds)
    VALUES (CONVERT(uniqueidentifier, N'c3f7f032-35ee-58e5-9a3f-4e1799120e0a'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'f38eed6e-3474-48f5-b8b0-26616b769d22'), 0, N'query-supplier', N'查询供应商', CONVERT(uniqueidentifier, N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'), N'InitialInput', N'', 0, 120);
    INSERT INTO dbo.AgOrchestrationAgentBinding (ID, OrchestrationId, VersionId, Ordinal, AgentId, AgentVersionId)
    VALUES (CONVERT(uniqueidentifier, N'eb62325c-2463-5905-916e-292ab1ef122c'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'f38eed6e-3474-48f5-b8b0-26616b769d22'), 0, CONVERT(uniqueidentifier, N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'), CONVERT(uniqueidentifier, N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'));
    INSERT INTO dbo.AgOrchestrationVersion (ID, OrchestrationId, Ordinal, Label, IsDraft, StartNodeId)
    VALUES (CONVERT(uniqueidentifier, N'6ee02027-b7c6-4cec-a038-02382e7f68ad'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), 2, N'2.0.0', 0, N'step-one');
    INSERT INTO dbo.AgOrchestrationNode (ID, OrchestrationId, VersionId, Ordinal, NodeId, Name, AgentId, InputMode, InputTemplate, MaximumRetries, TimeoutSeconds)
    VALUES (CONVERT(uniqueidentifier, N'1f2d14ec-841d-5606-a50e-5792193e71f6'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'6ee02027-b7c6-4cec-a038-02382e7f68ad'), 0, N'step-one', N'第一步', CONVERT(uniqueidentifier, N'2c1003cd-abad-423f-a604-19279b7a2401'), N'InitialInput', N'', 0, 120);
    INSERT INTO dbo.AgOrchestrationNode (ID, OrchestrationId, VersionId, Ordinal, NodeId, Name, AgentId, InputMode, InputTemplate, MaximumRetries, TimeoutSeconds)
    VALUES (CONVERT(uniqueidentifier, N'ad4669d7-0e2f-5b6e-aa83-83ee17a7214a'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'6ee02027-b7c6-4cec-a038-02382e7f68ad'), 1, N'step-two', N'', CONVERT(uniqueidentifier, N'b175ca33-4aba-4d78-b8ae-6bbac3562815'), N'PreviousOutput', N'{{previous}}', 0, 120);
    INSERT INTO dbo.AgOrchestrationEdge (ID, OrchestrationId, VersionId, Ordinal, FromNodeId, ToNodeId, Condition, ConditionValue, SortOrder)
    VALUES (CONVERT(uniqueidentifier, N'093e7a60-e438-5966-ba9a-a4b16a0822c3'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'6ee02027-b7c6-4cec-a038-02382e7f68ad'), 0, N'step-one', N'step-two', N'Succeeded', N'', 0);
    INSERT INTO dbo.AgOrchestrationAgentBinding (ID, OrchestrationId, VersionId, Ordinal, AgentId, AgentVersionId)
    VALUES (CONVERT(uniqueidentifier, N'8f842064-ca2b-5030-a05f-8192b36ee294'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'6ee02027-b7c6-4cec-a038-02382e7f68ad'), 0, CONVERT(uniqueidentifier, N'2c1003cd-abad-423f-a604-19279b7a2401'), CONVERT(uniqueidentifier, N'4415f81c-29a1-4412-affd-a5161c72267b'));
    INSERT INTO dbo.AgOrchestrationAgentBinding (ID, OrchestrationId, VersionId, Ordinal, AgentId, AgentVersionId)
    VALUES (CONVERT(uniqueidentifier, N'ab4e6073-e382-5acf-a417-d7b6343ba716'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'6ee02027-b7c6-4cec-a038-02382e7f68ad'), 1, CONVERT(uniqueidentifier, N'b175ca33-4aba-4d78-b8ae-6bbac3562815'), CONVERT(uniqueidentifier, N'4820b4bb-93e2-40bb-a849-b80768da34dc'));
    INSERT INTO dbo.AgOrchestrationVersion (ID, OrchestrationId, Ordinal, Label, IsDraft, StartNodeId)
    VALUES (CONVERT(uniqueidentifier, N'632b45a2-5145-469e-a35c-c71b656885c2'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), 3, N'3.0.0', 0, N'step-one');
    INSERT INTO dbo.AgOrchestrationNode (ID, OrchestrationId, VersionId, Ordinal, NodeId, Name, AgentId, InputMode, InputTemplate, MaximumRetries, TimeoutSeconds)
    VALUES (CONVERT(uniqueidentifier, N'07784f91-8c32-58e8-8181-fab920b01f7f'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'632b45a2-5145-469e-a35c-c71b656885c2'), 0, N'step-one', N'第一步', CONVERT(uniqueidentifier, N'2c1003cd-abad-423f-a604-19279b7a2401'), N'InitialInput', N'', 0, 120);
    INSERT INTO dbo.AgOrchestrationNode (ID, OrchestrationId, VersionId, Ordinal, NodeId, Name, AgentId, InputMode, InputTemplate, MaximumRetries, TimeoutSeconds)
    VALUES (CONVERT(uniqueidentifier, N'0671c733-1fe8-5a5f-bb65-f6ca7f13b8b5'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'632b45a2-5145-469e-a35c-c71b656885c2'), 1, N'step-two', N'第二', CONVERT(uniqueidentifier, N'b175ca33-4aba-4d78-b8ae-6bbac3562815'), N'PreviousOutput', N'{{previous}}', 0, 120);
    INSERT INTO dbo.AgOrchestrationEdge (ID, OrchestrationId, VersionId, Ordinal, FromNodeId, ToNodeId, Condition, ConditionValue, SortOrder)
    VALUES (CONVERT(uniqueidentifier, N'e4ea39d6-2f58-5d6b-a28e-7e0d70532229'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'632b45a2-5145-469e-a35c-c71b656885c2'), 0, N'step-one', N'step-two', N'Succeeded', N'', 0);
    INSERT INTO dbo.AgOrchestrationAgentBinding (ID, OrchestrationId, VersionId, Ordinal, AgentId, AgentVersionId)
    VALUES (CONVERT(uniqueidentifier, N'fb093cd4-156c-5f5c-a0da-0b78c68aa980'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'632b45a2-5145-469e-a35c-c71b656885c2'), 0, CONVERT(uniqueidentifier, N'2c1003cd-abad-423f-a604-19279b7a2401'), CONVERT(uniqueidentifier, N'4415f81c-29a1-4412-affd-a5161c72267b'));
    INSERT INTO dbo.AgOrchestrationAgentBinding (ID, OrchestrationId, VersionId, Ordinal, AgentId, AgentVersionId)
    VALUES (CONVERT(uniqueidentifier, N'90bf6019-3ef4-5fd1-bf25-306b7eb8de37'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'632b45a2-5145-469e-a35c-c71b656885c2'), 1, CONVERT(uniqueidentifier, N'b175ca33-4aba-4d78-b8ae-6bbac3562815'), CONVERT(uniqueidentifier, N'4820b4bb-93e2-40bb-a849-b80768da34dc'));
    INSERT INTO dbo.AgOrchestrationVersion (ID, OrchestrationId, Ordinal, Label, IsDraft, StartNodeId)
    VALUES (CONVERT(uniqueidentifier, N'a91dd682-8778-4e35-95af-94cf9e56680f'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), 4, N'4.0.0', 0, N'step-one');
    INSERT INTO dbo.AgOrchestrationNode (ID, OrchestrationId, VersionId, Ordinal, NodeId, Name, AgentId, InputMode, InputTemplate, MaximumRetries, TimeoutSeconds)
    VALUES (CONVERT(uniqueidentifier, N'dbb2991c-5374-5cba-b3ff-c822c1081695'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'a91dd682-8778-4e35-95af-94cf9e56680f'), 0, N'step-one', N'第一步', CONVERT(uniqueidentifier, N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'), N'InitialInput', N'', 0, 120);
    INSERT INTO dbo.AgOrchestrationAgentBinding (ID, OrchestrationId, VersionId, Ordinal, AgentId, AgentVersionId)
    VALUES (CONVERT(uniqueidentifier, N'a85551f0-11d8-570c-bc05-ed0093225e26'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'a91dd682-8778-4e35-95af-94cf9e56680f'), 0, CONVERT(uniqueidentifier, N'2999f08b-fcef-4d4c-ab30-f1443048b6f0'), CONVERT(uniqueidentifier, N'9b176ce9-2f46-473e-b33d-fd8dab0f63bf'));
    INSERT INTO dbo.AgOrchestrationVersion (ID, OrchestrationId, Ordinal, Label, IsDraft, StartNodeId)
    VALUES (CONVERT(uniqueidentifier, N'2441a424-257a-45c1-8c4a-6f320f0809cc'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), 5, N'5.0.0', 0, N'step-one');
    INSERT INTO dbo.AgOrchestrationNode (ID, OrchestrationId, VersionId, Ordinal, NodeId, Name, AgentId, InputMode, InputTemplate, MaximumRetries, TimeoutSeconds)
    VALUES (CONVERT(uniqueidentifier, N'13ed1b00-d601-5ad9-b250-1f9e7ebd2050'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'2441a424-257a-45c1-8c4a-6f320f0809cc'), 0, N'step-one', N'第一步', CONVERT(uniqueidentifier, N'2c1003cd-abad-423f-a604-19279b7a2401'), N'InitialInput', N'', 0, 120);
    INSERT INTO dbo.AgOrchestrationNode (ID, OrchestrationId, VersionId, Ordinal, NodeId, Name, AgentId, InputMode, InputTemplate, MaximumRetries, TimeoutSeconds)
    VALUES (CONVERT(uniqueidentifier, N'4b4b5db0-193b-55ef-8f69-60f7c01d4133'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'2441a424-257a-45c1-8c4a-6f320f0809cc'), 1, N'step-two', N'第二', CONVERT(uniqueidentifier, N'b175ca33-4aba-4d78-b8ae-6bbac3562815'), N'PreviousOutput', N'{{previous}}', 0, 120);
    INSERT INTO dbo.AgOrchestrationEdge (ID, OrchestrationId, VersionId, Ordinal, FromNodeId, ToNodeId, Condition, ConditionValue, SortOrder)
    VALUES (CONVERT(uniqueidentifier, N'8df99bb0-28a2-5679-83d8-299b6494834b'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'2441a424-257a-45c1-8c4a-6f320f0809cc'), 0, N'step-one', N'step-two', N'Succeeded', N'', 0);
    INSERT INTO dbo.AgOrchestrationAgentBinding (ID, OrchestrationId, VersionId, Ordinal, AgentId, AgentVersionId)
    VALUES (CONVERT(uniqueidentifier, N'5934401e-4cd6-51e7-ab98-92e37e7ba33f'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'2441a424-257a-45c1-8c4a-6f320f0809cc'), 0, CONVERT(uniqueidentifier, N'2c1003cd-abad-423f-a604-19279b7a2401'), CONVERT(uniqueidentifier, N'4415f81c-29a1-4412-affd-a5161c72267b'));
    INSERT INTO dbo.AgOrchestrationAgentBinding (ID, OrchestrationId, VersionId, Ordinal, AgentId, AgentVersionId)
    VALUES (CONVERT(uniqueidentifier, N'16c89229-9c9f-5669-b3db-cf0a6e9f2639'), CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'), CONVERT(uniqueidentifier, N'2441a424-257a-45c1-8c4a-6f320f0809cc'), 1, CONVERT(uniqueidentifier, N'b175ca33-4aba-4d78-b8ae-6bbac3562815'), CONVERT(uniqueidentifier, N'4820b4bb-93e2-40bb-a849-b80768da34dc'));
    IF NOT EXISTS (SELECT 1 FROM dbo.AgOrchestrationNormalizationCheckpoint WHERE OrchestrationId = CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'))
        INSERT INTO dbo.AgOrchestrationNormalizationCheckpoint (OrchestrationId) VALUES (CONVERT(uniqueidentifier, N'FAEEDEB1-74B4-43E0-9A51-64AF9D4D808F'));

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
