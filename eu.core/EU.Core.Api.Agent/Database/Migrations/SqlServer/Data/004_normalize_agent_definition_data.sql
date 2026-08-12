-- Generated from eu-core-agent.db for SQL Server 2014.
-- Run 002 and 003 first. Stop EU.Core.Api.Agent and back up the database.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
SET NOCOUNT ON;
BEGIN TRY
BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.AgAgentDefinition',N'DocumentJson') IS NULL THROW 51110,N'DocumentJson is absent; migration was already finalized.',1;
IF OBJECT_ID(N'dbo.AgAgentVersion',N'U') IS NULL OR OBJECT_ID(N'dbo.AgAgentVersionSnapshot',N'U') IS NULL OR OBJECT_ID(N'dbo.AgAgentVersionBinding',N'U') IS NULL THROW 51111,N'Run 003 first.',1;
IF EXISTS(SELECT 1 FROM dbo.AgAgentVersion) OR EXISTS(SELECT 1 FROM dbo.AgAgentVersionSnapshot) OR EXISTS(SELECT 1 FROM dbo.AgAgentVersionBinding) THROW 51112,N'Normalized detail tables must be empty.',1;
IF (SELECT COUNT_BIG(*) FROM dbo.AgAgentDefinition WITH (TABLOCKX,HOLDLOCK)) <> 6 THROW 51113,N'Agent count differs from the SQLite snapshot.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.AgAgentDefinition WHERE ID='7ccf46e6-b5c8-4554-959a-2b247819d389' AND Code=N'111') THROW 51114,N'Agent source identity mismatch: 111',1;
UPDATE dbo.AgAgentDefinition SET Name=N'1111',Description=N'1111',RuntimeStatus='Archived' WHERE ID='7ccf46e6-b5c8-4554-959a-2b247819d389';
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('aecfbde4-4ea1-4140-89e4-a53f05b02b37','7ccf46e6-b5c8-4554-959a-2b247819d389',0,N'0.1.0',1,N'1111111',N'qwen3.7-plus','Structured',N'{
    "type": "object",
    "properties": {
      "answer": {
        "type": "string"
      },
      "summary": {
        "type": "string"
      },
      "success": {
        "type": "boolean"
      }
    },
    "required": [
      "answer",
      "success"
    ]
  }',NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('ecd6dcfc-2e82-4aeb-a8e4-5a69456534d6','aecfbde4-4ea1-4140-89e4-a53f05b02b37','Version','Skill',0,'1ae40ad4-1669-4a55-9f63-260dae2c6a3f',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('cf9f76fd-9bd3-4d49-8779-cb8cbffc41d5','7ccf46e6-b5c8-4554-959a-2b247819d389',0,N'1.0.0',0,N'1111111',N'qwen3.7-plus','Structured',N'{"properties":{"answer":{"type":"string"},"success":{"type":"boolean"},"summary":{"type":"string"}},"required":["answer","success"],"type":"object"}',N'7b57d2d65632b5336a9c84fba95797ca0ab5b360bfb52b767f6476587e6dfaa7');
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('cf9f76fd-9bd3-4d49-8779-cb8cbffc41d5','cf9f76fd-9bd3-4d49-8779-cb8cbffc41d5','cf9f76fd-9bd3-4d49-8779-cb8cbffc41d5',N'111',N'1111111',N'qwen3.7-plus','Structured',N'{"properties":{"answer":{"type":"string"},"success":{"type":"boolean"},"summary":{"type":"string"}},"required":["answer","success"],"type":"object"}',N'1111',N'1111');
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('b9254cc6-4e12-4d82-9ecb-089bb3b5c2fc','cf9f76fd-9bd3-4d49-8779-cb8cbffc41d5','Snapshot','Skill',0,'1ae40ad4-1669-4a55-9f63-260dae2c6a3f',NULL,NULL,NULL,NULL,NULL);
IF NOT EXISTS(SELECT 1 FROM dbo.AgAgentDefinition WHERE ID='2c1003cd-abad-423f-a604-19279b7a2401' AND Code=N'flow-step-one') THROW 51114,N'Agent source identity mismatch: flow-step-one',1;
UPDATE dbo.AgAgentDefinition SET Name=N'编排步骤一',Description=N'',RuntimeStatus='Enabled' WHERE ID='2c1003cd-abad-423f-a604-19279b7a2401';
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('64775e0e-b3d2-44b8-8270-4fcba3943410','2c1003cd-abad-423f-a604-19279b7a2401',0,N'0.1.0',1,N'无论用户输入什么，只返回下面这一行，不要增加其他内容：
  NODE1_OK',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('4415f81c-29a1-4412-affd-a5161c72267b','2c1003cd-abad-423f-a604-19279b7a2401',0,N'1.0.0',0,N'无论用户输入什么，只返回下面这一行，不要增加其他内容：
  NODE1_OK',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('4415f81c-29a1-4412-affd-a5161c72267b','4415f81c-29a1-4412-affd-a5161c72267b','4415f81c-29a1-4412-affd-a5161c72267b',N'flow-step-one',N'无论用户输入什么，只返回下面这一行，不要增加其他内容：
  NODE1_OK',N'qwen3.7-plus','Text',NULL,NULL,NULL);
IF NOT EXISTS(SELECT 1 FROM dbo.AgAgentDefinition WHERE ID='b175ca33-4aba-4d78-b8ae-6bbac3562815' AND Code=N'flow-step-two') THROW 51114,N'Agent source identity mismatch: flow-step-two',1;
UPDATE dbo.AgAgentDefinition SET Name=N'编排步骤二',Description=N'',RuntimeStatus='Enabled' WHERE ID='b175ca33-4aba-4d78-b8ae-6bbac3562815';
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('effbec3a-5974-4b4d-8e51-43c9950e807f','b175ca33-4aba-4d78-b8ae-6bbac3562815',0,N'0.1.0',1,N' 你会收到上一个节点的输出。
  请原样保留收到的内容，并严格返回：
  NODE2_RECEIVED: 上一个节点的内容',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('4820b4bb-93e2-40bb-a849-b80768da34dc','b175ca33-4aba-4d78-b8ae-6bbac3562815',0,N'1.0.0',0,N' 你会收到上一个节点的输出。
  请原样保留收到的内容，并严格返回：
  NODE2_RECEIVED: 上一个节点的内容',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('4820b4bb-93e2-40bb-a849-b80768da34dc','4820b4bb-93e2-40bb-a849-b80768da34dc','4820b4bb-93e2-40bb-a849-b80768da34dc',N'flow-step-two',N' 你会收到上一个节点的输出。
  请原样保留收到的内容，并严格返回：
  NODE2_RECEIVED: 上一个节点的内容',N'qwen3.7-plus','Text',NULL,NULL,NULL);
IF NOT EXISTS(SELECT 1 FROM dbo.AgAgentDefinition WHERE ID='1e1c9aab-71e7-4e8b-9905-b34588f4515e' AND Code=N'main-agent') THROW 51114,N'Agent source identity mismatch: main-agent',1;
UPDATE dbo.AgAgentDefinition SET Name=N'主 Agent',Description=N'P8 统一入口 Main Agent',RuntimeStatus='Enabled' WHERE ID='1e1c9aab-71e7-4e8b-9905-b34588f4515e';
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('3acdabf6-0921-422f-b743-6ac73e7eca9d','1e1c9aab-71e7-4e8b-9905-b34588f4515e',0,N'0.1.0',1,N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('ab93be59-428f-4e18-afc7-783f8eb37dbd','3acdabf6-0921-422f-b743-6ac73e7eca9d','Version','Skill',0,'4c82c326-def6-4706-9960-6aefa4814dcd',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('bd362f96-14c8-4e56-9f96-6fc60186102d','3acdabf6-0921-422f-b743-6ac73e7eca9d','Version','Tool',0,'b9e74725-1170-4ec7-8cb7-125510dbd2b0',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('c8c33881-91e2-4a90-81bf-46b188e5a5e6','3acdabf6-0921-422f-b743-6ac73e7eca9d','Version','Tool',1,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('3a6f97e7-04d0-46f1-adea-0051d375d02d','3acdabf6-0921-422f-b743-6ac73e7eca9d','Version','KnowledgeBase',0,'25a850f1-a02f-4a88-a202-84fa3342e28f',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('f2f3206f-f4b2-4af4-9a35-f3eb87449a1e','1e1c9aab-71e7-4e8b-9905-b34588f4515e',0,N'1.0.0',0,N'?? EU.Core ???? Agent??????????????????',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('f2f3206f-f4b2-4af4-9a35-f3eb87449a1e','f2f3206f-f4b2-4af4-9a35-f3eb87449a1e','f2f3206f-f4b2-4af4-9a35-f3eb87449a1e',N'main-agent',N'?? EU.Core ???? Agent??????????????????',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('15b85c7c-3c7f-4b83-891b-9c55553d5d99','1e1c9aab-71e7-4e8b-9905-b34588f4515e',1,N'2.0.0',0,N'?? EU.Core ???? Agent??????????????????',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('15b85c7c-3c7f-4b83-891b-9c55553d5d99','15b85c7c-3c7f-4b83-891b-9c55553d5d99','15b85c7c-3c7f-4b83-891b-9c55553d5d99',N'main-agent',N'?? EU.Core ???? Agent??????????????????',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('db349919-372d-471e-a432-f0e9ff170622','15b85c7c-3c7f-4b83-891b-9c55553d5d99','Snapshot','Tool',0,'b4919a29-6ae3-48de-88da-1b888780afa6',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('71f32f33-d350-4839-b935-b2e5ef8c4652','15b85c7c-3c7f-4b83-891b-9c55553d5d99','Snapshot','Tool',1,'d55d9b33-8036-48b4-af5a-59a8c27d1f75',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('33c325d0-c685-4d94-834d-73d4218ee136','15b85c7c-3c7f-4b83-891b-9c55553d5d99','Snapshot','Tool',2,'832c1d85-d225-4cef-9520-f70b6d69a127',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('4acf5a90-5cd4-4068-a8ce-cb5ea14748ee','15b85c7c-3c7f-4b83-891b-9c55553d5d99','Snapshot','Tool',3,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('29aed1da-8ba7-4b7a-b938-c6335dbab2c4','15b85c7c-3c7f-4b83-891b-9c55553d5d99','Snapshot','Tool',4,'bac125bc-8e7c-46b7-9c55-d53d125b8fd9',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('035c5999-039e-44ea-9700-ded968708891','15b85c7c-3c7f-4b83-891b-9c55553d5d99','Snapshot','Tool',5,'d4359ea4-9317-4110-892a-c899a87d7b69',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('a40de0e0-3f30-4fbc-a53e-892ec68b030b','1e1c9aab-71e7-4e8b-9905-b34588f4515e',2,N'3.0.0',0,N'?? EU.Core ???? Agent??????????????????',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('a40de0e0-3f30-4fbc-a53e-892ec68b030b','a40de0e0-3f30-4fbc-a53e-892ec68b030b','a40de0e0-3f30-4fbc-a53e-892ec68b030b',N'main-agent',N'?? EU.Core ???? Agent??????????????????',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('e9446849-9fd9-47a3-a0f2-da129a497089','a40de0e0-3f30-4fbc-a53e-892ec68b030b','Snapshot','Tool',0,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('c9be0598-c504-46d8-9d94-17b596affd68','1e1c9aab-71e7-4e8b-9905-b34588f4515e',3,N'4.0.0',0,N'?? EU.Core ???? Agent??????????????????',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('c9be0598-c504-46d8-9d94-17b596affd68','c9be0598-c504-46d8-9d94-17b596affd68','c9be0598-c504-46d8-9d94-17b596affd68',N'main-agent',N'?? EU.Core ???? Agent??????????????????',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('dce2e5a4-ccec-4b77-9c38-8708f06a100f','c9be0598-c504-46d8-9d94-17b596affd68','Snapshot','Skill',0,'4c82c326-def6-4706-9960-6aefa4814dcd',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('836299f5-9ed7-4c51-a3ee-298f48d962b2','c9be0598-c504-46d8-9d94-17b596affd68','Snapshot','Tool',0,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('018e9bbb-82a1-4f89-b4e5-5d429704425d','c9be0598-c504-46d8-9d94-17b596affd68','Snapshot','KnowledgeBase',0,'25a850f1-a02f-4a88-a202-84fa3342e28f',NULL,2,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('da2be35d-ef83-4b95-a4e7-35f9ec95e7f9','1e1c9aab-71e7-4e8b-9905-b34588f4515e',4,N'5.0.0',0,N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('da2be35d-ef83-4b95-a4e7-35f9ec95e7f9','da2be35d-ef83-4b95-a4e7-35f9ec95e7f9','da2be35d-ef83-4b95-a4e7-35f9ec95e7f9',N'main-agent',N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('2dff0a76-4314-4865-bd64-3a88e3f69bb9','da2be35d-ef83-4b95-a4e7-35f9ec95e7f9','Snapshot','Skill',0,'4c82c326-def6-4706-9960-6aefa4814dcd',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('3a43a2fe-5700-4e2d-8df7-d8314f2f8afd','da2be35d-ef83-4b95-a4e7-35f9ec95e7f9','Snapshot','Tool',0,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('933a39c7-db83-4e36-87c9-c43b5b83e2fa','da2be35d-ef83-4b95-a4e7-35f9ec95e7f9','Snapshot','KnowledgeBase',0,'25a850f1-a02f-4a88-a202-84fa3342e28f',NULL,2,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('80341b93-f0df-493b-9131-9f273e0ed823','da2be35d-ef83-4b95-a4e7-35f9ec95e7f9','Snapshot','ChildAgent',0,'2999f08b-fcef-4d4c-ab30-f1443048b6f0','9b176ce9-2f46-473e-b33d-fd8dab0f63bf',NULL,N'',NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('df2e5ea3-b078-49ef-9b0b-34038fa7b220','da2be35d-ef83-4b95-a4e7-35f9ec95e7f9','Snapshot','Orchestration',0,'faeedeb1-74b4-43e0-9a51-64af9d4d808f','2441a424-257a-45c1-8c4a-6f320f0809cc',NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('cb8620c7-28df-49fb-8036-0a949b78c7e5','1e1c9aab-71e7-4e8b-9905-b34588f4515e',5,N'6.0.0',0,N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('cb8620c7-28df-49fb-8036-0a949b78c7e5','cb8620c7-28df-49fb-8036-0a949b78c7e5','cb8620c7-28df-49fb-8036-0a949b78c7e5',N'main-agent',N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,N'主 Agent',N'P8 统一入口 Main Agent');
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('54537f47-53fd-4e2e-b273-b31aa1cc5cd5','cb8620c7-28df-49fb-8036-0a949b78c7e5','Snapshot','Skill',0,'4c82c326-def6-4706-9960-6aefa4814dcd',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('2cad94ee-1fc3-419e-9b32-2c69ac9ea94b','cb8620c7-28df-49fb-8036-0a949b78c7e5','Snapshot','Tool',0,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('8e7dc6ee-8dc2-4b3e-8203-58c0081dce8b','cb8620c7-28df-49fb-8036-0a949b78c7e5','Snapshot','KnowledgeBase',0,'25a850f1-a02f-4a88-a202-84fa3342e28f',NULL,5,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('2c180359-6466-4706-95d7-bfc6e8ef2d2e','cb8620c7-28df-49fb-8036-0a949b78c7e5','Snapshot','ChildAgent',0,'2999f08b-fcef-4d4c-ab30-f1443048b6f0','9b176ce9-2f46-473e-b33d-fd8dab0f63bf',NULL,N'test',N'test',N'test1');
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('c5f4b5e7-c9e6-4177-81c1-e94c2d0ab569','cb8620c7-28df-49fb-8036-0a949b78c7e5','Snapshot','Orchestration',0,'faeedeb1-74b4-43e0-9a51-64af9d4d808f','2441a424-257a-45c1-8c4a-6f320f0809cc',NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('adb035fa-e140-47bd-9637-86be703027d1','1e1c9aab-71e7-4e8b-9905-b34588f4515e',6,N'7.0.0',0,N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('adb035fa-e140-47bd-9637-86be703027d1','adb035fa-e140-47bd-9637-86be703027d1','adb035fa-e140-47bd-9637-86be703027d1',N'main-agent',N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,N'主 Agent',N'P8 统一入口 Main Agent');
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('4f9ab2e8-0243-41ea-a102-51b9857818e5','adb035fa-e140-47bd-9637-86be703027d1','Snapshot','Skill',0,'4c82c326-def6-4706-9960-6aefa4814dcd',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('3f1fc36e-d4e3-43ed-954a-3d2445fb11df','adb035fa-e140-47bd-9637-86be703027d1','Snapshot','Tool',0,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('8586fd3f-f9ef-49ea-9a7b-6143ac164b2f','adb035fa-e140-47bd-9637-86be703027d1','Snapshot','KnowledgeBase',0,'25a850f1-a02f-4a88-a202-84fa3342e28f',NULL,5,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('fbfb37c1-0edc-4abb-aadf-31a143371c3a','adb035fa-e140-47bd-9637-86be703027d1','Snapshot','ChildAgent',0,'2999f08b-fcef-4d4c-ab30-f1443048b6f0','9b176ce9-2f46-473e-b33d-fd8dab0f63bf',NULL,N'test',N'test',N'test1');
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('b8cf2903-04c4-4b91-b55c-273138674baa','adb035fa-e140-47bd-9637-86be703027d1','Snapshot','Orchestration',0,'faeedeb1-74b4-43e0-9a51-64af9d4d808f','2441a424-257a-45c1-8c4a-6f320f0809cc',NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('c00f9b33-11c5-4c00-a159-3cf947535548','1e1c9aab-71e7-4e8b-9905-b34588f4515e',7,N'8.0.0',0,N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('c00f9b33-11c5-4c00-a159-3cf947535548','c00f9b33-11c5-4c00-a159-3cf947535548','c00f9b33-11c5-4c00-a159-3cf947535548',N'main-agent',N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,N'主 Agent',N'P8 统一入口 Main Agent');
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('bd374dbb-e1e5-4f99-a617-65278d28555a','c00f9b33-11c5-4c00-a159-3cf947535548','Snapshot','Skill',0,'4c82c326-def6-4706-9960-6aefa4814dcd',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('8477dcb1-0f57-455b-88ce-bbaf5a61093e','c00f9b33-11c5-4c00-a159-3cf947535548','Snapshot','Tool',0,'4951b525-75a8-42f3-86f1-9ddc27e94f36',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('a9d3e134-f661-4681-8c48-bda61001447c','c00f9b33-11c5-4c00-a159-3cf947535548','Snapshot','Tool',1,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('12a0069e-6099-4ef7-aa1a-b978f0e0adc4','c00f9b33-11c5-4c00-a159-3cf947535548','Snapshot','KnowledgeBase',0,'25a850f1-a02f-4a88-a202-84fa3342e28f',NULL,5,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('5926c389-fd51-4e82-8482-71ee4b557b3a','c00f9b33-11c5-4c00-a159-3cf947535548','Snapshot','ChildAgent',0,'2999f08b-fcef-4d4c-ab30-f1443048b6f0','9b176ce9-2f46-473e-b33d-fd8dab0f63bf',NULL,N'test',N'test',N'test1');
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('adeaf68f-6bbf-4f65-97a5-28ca1cd78bee','c00f9b33-11c5-4c00-a159-3cf947535548','Snapshot','Orchestration',0,'faeedeb1-74b4-43e0-9a51-64af9d4d808f','2441a424-257a-45c1-8c4a-6f320f0809cc',NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e','1e1c9aab-71e7-4e8b-9905-b34588f4515e',8,N'9.0.0',0,N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e','ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e','ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e',N'main-agent',N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,N'主 Agent',N'P8 统一入口 Main Agent');
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('be731949-5cd3-4045-ba08-c28cc57defc1','ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e','Snapshot','Skill',0,'4c82c326-def6-4706-9960-6aefa4814dcd',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('7008813d-718b-4f0f-a7b2-fb9f0a564ced','ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e','Snapshot','Tool',0,'4951b525-75a8-42f3-86f1-9ddc27e94f36',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('61c4aad6-87aa-483b-bddb-9e93d1536780','ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e','Snapshot','Tool',1,'deaa7008-be68-4449-a8ed-ffd27576dcef',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('8a1a6a3c-4942-4c3a-8fc2-be59061fb51a','ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e','Snapshot','Tool',2,'613def2c-3100-4a25-afa8-eeed522d3a17',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('39b2ad93-e9f6-49ad-aae6-7fda99913a3a','ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e','Snapshot','Tool',3,'c9e4931a-90dc-49c7-925e-588e572e5857',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('be99b496-a42b-4bea-8ec9-43659baf8c6b','ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e','Snapshot','Tool',4,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('46779b2d-971e-4e53-8094-82babc8007af','ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e','Snapshot','KnowledgeBase',0,'25a850f1-a02f-4a88-a202-84fa3342e28f',NULL,5,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('1b531cc7-f478-437c-b147-fe3d094dc12e','ed5f6528-1ce9-42e1-96bc-5d0f45a39f8e','Snapshot','ChildAgent',0,'2999f08b-fcef-4d4c-ab30-f1443048b6f0','9b176ce9-2f46-473e-b33d-fd8dab0f63bf',NULL,N'test',N'test',N'test1');
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('c28ddaec-1d54-410e-8533-8fd45e955e46','1e1c9aab-71e7-4e8b-9905-b34588f4515e',9,N'10.0.0',0,N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('c28ddaec-1d54-410e-8533-8fd45e955e46','c28ddaec-1d54-410e-8533-8fd45e955e46','c28ddaec-1d54-410e-8533-8fd45e955e46',N'main-agent',N'你是 EU.Core 平台的主 Agent，负责从统一入口处理用户请求。
普通问题直接回答。查询供应商列表时调用 get_supplier。
登录接口等平台资料问题使用已绑定的 Skill 和知识库，并在答案末尾列出 [kb:code/file#chunk] 引用。
用户明确要求子 Agent 处理时，调用 delegate_to_agent，并选择已授权的 test Agent 版本。
用户明确要求运行两节点流程时，调用 run_orchestration，并选择已授权的 supplier-flow 版本。
只使用冻结快照中授权的版本；如实保留 MCP 返回字段，不得虚构 id。',N'qwen3.7-plus','Text',NULL,N'主 Agent',N'P8 统一入口 Main Agent');
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('fadaedd4-617c-4d8a-a401-ad304602139f','c28ddaec-1d54-410e-8533-8fd45e955e46','Snapshot','Skill',0,'4c82c326-def6-4706-9960-6aefa4814dcd',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('7219dcba-b2f5-4975-ad6f-ad992fdadfc3','c28ddaec-1d54-410e-8533-8fd45e955e46','Snapshot','Tool',0,'b9e74725-1170-4ec7-8cb7-125510dbd2b0',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('7f0dab68-8a70-42c5-aa81-fd7f83b8d040','c28ddaec-1d54-410e-8533-8fd45e955e46','Snapshot','Tool',1,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('ad141cbb-5c22-4274-ae83-b8307a75de0c','c28ddaec-1d54-410e-8533-8fd45e955e46','Snapshot','KnowledgeBase',0,'25a850f1-a02f-4a88-a202-84fa3342e28f',NULL,5,NULL,NULL,NULL);
IF NOT EXISTS(SELECT 1 FROM dbo.AgAgentDefinition WHERE ID='ff84c83b-3adb-4f9f-950d-030056f4eeb6' AND Code=N'pdf-acceptance-agent-172555') THROW 51114,N'Agent source identity mismatch: pdf-acceptance-agent-172555',1;
UPDATE dbo.AgAgentDefinition SET Name=N'PDF Acceptance Agent',Description=N'Real-model grounded PDF acceptance',RuntimeStatus='Enabled' WHERE ID='ff84c83b-3adb-4f9f-950d-030056f4eeb6';
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('129b46df-c571-4a50-a5c4-af99912de568','ff84c83b-3adb-4f9f-950d-030056f4eeb6',0,N'0.1.0',1,N'Answer only from bound knowledge. If the fact is absent, say NOT FOUND. For code questions, return only the exact code.',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('8dd1195f-d9ac-4297-bc9d-6bb214202a6d','129b46df-c571-4a50-a5c4-af99912de568','Version','KnowledgeBase',0,'63b2c3d3-fd52-4406-aefa-ccd5ba476c58',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('86263d9b-e0ca-4bac-a0de-c80318dfd8f9','ff84c83b-3adb-4f9f-950d-030056f4eeb6',0,N'1.0.0',0,N'Answer only from bound knowledge. If the fact is absent, say NOT FOUND. For code questions, return only the exact code.',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('86263d9b-e0ca-4bac-a0de-c80318dfd8f9','86263d9b-e0ca-4bac-a0de-c80318dfd8f9','86263d9b-e0ca-4bac-a0de-c80318dfd8f9',N'pdf-acceptance-agent-172555',N'Answer only from bound knowledge. If the fact is absent, say NOT FOUND. For code questions, return only the exact code.',N'qwen3.7-plus','Text',NULL,N'PDF Acceptance Agent',N'Real-model grounded PDF acceptance');
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('71b74fd1-5f3b-4beb-b661-80e471ba924b','86263d9b-e0ca-4bac-a0de-c80318dfd8f9','Snapshot','KnowledgeBase',0,'63b2c3d3-fd52-4406-aefa-ccd5ba476c58',NULL,1,NULL,NULL,NULL);
IF NOT EXISTS(SELECT 1 FROM dbo.AgAgentDefinition WHERE ID='2999f08b-fcef-4d4c-ab30-f1443048b6f0' AND Code=N'test') THROW 51114,N'Agent source identity mismatch: test',1;
UPDATE dbo.AgAgentDefinition SET Name=N'test',Description=N'test1',RuntimeStatus='Disabled' WHERE ID='2999f08b-fcef-4d4c-ab30-f1443048b6f0';
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('13d23fab-15aa-4ac7-a9e6-24bbbf826547','2999f08b-fcef-4d4c-ab30-f1443048b6f0',0,N'0.1.0',1,N'test',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('a866d630-c360-4bc5-a4b6-4fefea770c34','13d23fab-15aa-4ac7-a9e6-24bbbf826547','Version','Tool',0,'b4919a29-6ae3-48de-88da-1b888780afa6',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('7f780a42-9a5b-4c79-b192-a6584efd06d3','13d23fab-15aa-4ac7-a9e6-24bbbf826547','Version','Tool',1,'d55d9b33-8036-48b4-af5a-59a8c27d1f75',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('d01d6366-3939-4b0c-bf1d-08bbd8daa3fb','13d23fab-15aa-4ac7-a9e6-24bbbf826547','Version','Tool',2,'832c1d85-d225-4cef-9520-f70b6d69a127',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('9e5d4ed5-ebbe-4568-9269-12692b52aa00','13d23fab-15aa-4ac7-a9e6-24bbbf826547','Version','Tool',3,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('98dc9c8e-891f-4d5a-a355-207160e8fbf0','13d23fab-15aa-4ac7-a9e6-24bbbf826547','Version','Tool',4,'bac125bc-8e7c-46b7-9c55-d53d125b8fd9',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('81d9a84e-d913-4e7c-9c24-b74fb3f50d8f','13d23fab-15aa-4ac7-a9e6-24bbbf826547','Version','Tool',5,'d4359ea4-9317-4110-892a-c899a87d7b69',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('791054cd-9773-4e8c-866f-6bcbad3fda3a','13d23fab-15aa-4ac7-a9e6-24bbbf826547','Version','KnowledgeBase',0,'25a850f1-a02f-4a88-a202-84fa3342e28f',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('00d84092-0ce3-4544-b84e-418e40142d8c','2999f08b-fcef-4d4c-ab30-f1443048b6f0',0,N'1.0.0',0,N'test',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('00d84092-0ce3-4544-b84e-418e40142d8c','00d84092-0ce3-4544-b84e-418e40142d8c','00d84092-0ce3-4544-b84e-418e40142d8c',N'test',N'test',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('a4439457-2f75-4115-a16b-394518693a1f','2999f08b-fcef-4d4c-ab30-f1443048b6f0',1,N'2.0.0',0,N'test',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('a4439457-2f75-4115-a16b-394518693a1f','a4439457-2f75-4115-a16b-394518693a1f','a4439457-2f75-4115-a16b-394518693a1f',N'test',N'test',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('1f94c1a5-1505-47e5-b450-0c372d8723da','2999f08b-fcef-4d4c-ab30-f1443048b6f0',2,N'3.0.0',0,N'test',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('1f94c1a5-1505-47e5-b450-0c372d8723da','1f94c1a5-1505-47e5-b450-0c372d8723da','1f94c1a5-1505-47e5-b450-0c372d8723da',N'test',N'test',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('859fc218-5f7c-4fdd-92ad-b64ce340e683','2999f08b-fcef-4d4c-ab30-f1443048b6f0',3,N'4.0.0',0,N'test',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('859fc218-5f7c-4fdd-92ad-b64ce340e683','859fc218-5f7c-4fdd-92ad-b64ce340e683','859fc218-5f7c-4fdd-92ad-b64ce340e683',N'test',N'test',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('05c65d6f-15c3-43ed-8caa-548463d67576','859fc218-5f7c-4fdd-92ad-b64ce340e683','Snapshot','Tool',0,'b4919a29-6ae3-48de-88da-1b888780afa6',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('6c4ac1f1-4be1-4826-a033-5581bd2f54a0','859fc218-5f7c-4fdd-92ad-b64ce340e683','Snapshot','Tool',1,'d55d9b33-8036-48b4-af5a-59a8c27d1f75',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('6e03478a-9cf7-474d-8c42-e6f219635342','859fc218-5f7c-4fdd-92ad-b64ce340e683','Snapshot','Tool',2,'832c1d85-d225-4cef-9520-f70b6d69a127',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('b4a8111f-6fb6-4d4d-98e1-b98f18014348','859fc218-5f7c-4fdd-92ad-b64ce340e683','Snapshot','Tool',3,'4246d104-7d19-4b42-b6c8-d7a9dd46897a',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('b20ddd0f-889d-4d6a-a1ac-f1d58d11c766','859fc218-5f7c-4fdd-92ad-b64ce340e683','Snapshot','Tool',4,'bac125bc-8e7c-46b7-9c55-d53d125b8fd9',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('190e89d9-4dc7-4e73-9ef2-b83e519b6da0','859fc218-5f7c-4fdd-92ad-b64ce340e683','Snapshot','Tool',5,'d4359ea4-9317-4110-892a-c899a87d7b69',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('1009789b-34b1-4a57-b900-0d691f335a5d','2999f08b-fcef-4d4c-ab30-f1443048b6f0',4,N'5.0.0',0,N'test',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('1009789b-34b1-4a57-b900-0d691f335a5d','1009789b-34b1-4a57-b900-0d691f335a5d','1009789b-34b1-4a57-b900-0d691f335a5d',N'test',N'test',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('0fc20e07-45e6-48fd-80f5-97e0acb3de55','1009789b-34b1-4a57-b900-0d691f335a5d','Snapshot','Tool',0,'b4919a29-6ae3-48de-88da-1b888780afa6',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('61b544bf-8be2-4fc6-8316-16cb4b5b7133','1009789b-34b1-4a57-b900-0d691f335a5d','Snapshot','Tool',1,'d55d9b33-8036-48b4-af5a-59a8c27d1f75',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('32720faf-7088-4f89-a5aa-6403e89fd848','1009789b-34b1-4a57-b900-0d691f335a5d','Snapshot','Tool',2,'832c1d85-d225-4cef-9520-f70b6d69a127',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('34c4cccc-1dd4-4840-9721-c6168565d018','1009789b-34b1-4a57-b900-0d691f335a5d','Snapshot','Tool',3,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('3e844df2-3de9-444b-bd05-625a2e8f0cc0','1009789b-34b1-4a57-b900-0d691f335a5d','Snapshot','Tool',4,'bac125bc-8e7c-46b7-9c55-d53d125b8fd9',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('f6935f42-88d3-4de2-b443-8fb963d20531','1009789b-34b1-4a57-b900-0d691f335a5d','Snapshot','Tool',5,'d4359ea4-9317-4110-892a-c899a87d7b69',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('6c8d6243-dbc2-48e0-a56d-fb69559edfce','2999f08b-fcef-4d4c-ab30-f1443048b6f0',5,N'6.0.0',0,N'test',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('6c8d6243-dbc2-48e0-a56d-fb69559edfce','6c8d6243-dbc2-48e0-a56d-fb69559edfce','6c8d6243-dbc2-48e0-a56d-fb69559edfce',N'test',N'test',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('90cc7537-33be-4312-84ce-23f1131be1a4','6c8d6243-dbc2-48e0-a56d-fb69559edfce','Snapshot','Tool',0,'b4919a29-6ae3-48de-88da-1b888780afa6',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('d810e1c4-a588-4383-b236-28a6e715bffc','6c8d6243-dbc2-48e0-a56d-fb69559edfce','Snapshot','Tool',1,'d55d9b33-8036-48b4-af5a-59a8c27d1f75',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('ed442950-04c1-413e-a74b-fd11663d7529','6c8d6243-dbc2-48e0-a56d-fb69559edfce','Snapshot','Tool',2,'832c1d85-d225-4cef-9520-f70b6d69a127',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('b3a7a633-47fc-4e38-beac-dcc714bf50e2','6c8d6243-dbc2-48e0-a56d-fb69559edfce','Snapshot','Tool',3,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('147d5909-3275-463b-8528-0c18accc71d3','6c8d6243-dbc2-48e0-a56d-fb69559edfce','Snapshot','Tool',4,'bac125bc-8e7c-46b7-9c55-d53d125b8fd9',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('ca90f607-dc2d-4213-9e87-14efc953f17f','6c8d6243-dbc2-48e0-a56d-fb69559edfce','Snapshot','Tool',5,'d4359ea4-9317-4110-892a-c899a87d7b69',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersion (ID,AgentId,Ordinal,Label,IsDraft,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,OutputSchemaSha256) VALUES ('9b176ce9-2f46-473e-b33d-fd8dab0f63bf','2999f08b-fcef-4d4c-ab30-f1443048b6f0',6,N'7.0.0',0,N'test',N'qwen3.7-plus','Text',NULL,NULL);
INSERT dbo.AgAgentVersionSnapshot (ID,VersionId,SnapshotVersionId,AgentCode,Instructions,ModelProfileId,OutputMode,OutputJsonSchema,AgentName,AgentDescription) VALUES ('9b176ce9-2f46-473e-b33d-fd8dab0f63bf','9b176ce9-2f46-473e-b33d-fd8dab0f63bf','9b176ce9-2f46-473e-b33d-fd8dab0f63bf',N'test',N'test',N'qwen3.7-plus','Text',NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('a3874ffb-9efe-4ca8-b7ae-c2ca38853029','9b176ce9-2f46-473e-b33d-fd8dab0f63bf','Snapshot','Tool',0,'b4919a29-6ae3-48de-88da-1b888780afa6',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('87dfd944-a4b9-4227-8757-8775dfffa2a7','9b176ce9-2f46-473e-b33d-fd8dab0f63bf','Snapshot','Tool',1,'d55d9b33-8036-48b4-af5a-59a8c27d1f75',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('1a05e983-01ad-444f-8bc6-c3e8ca78f992','9b176ce9-2f46-473e-b33d-fd8dab0f63bf','Snapshot','Tool',2,'832c1d85-d225-4cef-9520-f70b6d69a127',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('761b3871-4a0d-4a68-a37e-2e41dce49525','9b176ce9-2f46-473e-b33d-fd8dab0f63bf','Snapshot','Tool',3,'b65c0544-e334-4c98-a7bd-f153eb10fde8',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('3a4cdf00-2c0b-4cc0-b2a4-aa33b28859f9','9b176ce9-2f46-473e-b33d-fd8dab0f63bf','Snapshot','Tool',4,'bac125bc-8e7c-46b7-9c55-d53d125b8fd9',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('d81d2636-f2cd-4de9-9ed2-27cac8179696','9b176ce9-2f46-473e-b33d-fd8dab0f63bf','Snapshot','Tool',5,'d4359ea4-9317-4110-892a-c899a87d7b69',NULL,NULL,NULL,NULL,NULL);
INSERT dbo.AgAgentVersionBinding (ID,VersionId,Scope,BindingType,Ordinal,ReferenceId,ReferenceVersionId,LogicalRevision,ReferenceCode,ReferenceName,ReferenceDescription) VALUES ('a7f9b565-dc3c-470b-bd3c-07656490be47','9b176ce9-2f46-473e-b33d-fd8dab0f63bf','Snapshot','KnowledgeBase',0,'25a850f1-a02f-4a88-a202-84fa3342e28f',NULL,2,NULL,NULL,NULL);
IF (SELECT COUNT_BIG(*) FROM dbo.AgAgentVersion)<>27 THROW 51115,N'Version count validation failed.',1;
IF (SELECT COUNT_BIG(*) FROM dbo.AgAgentVersionSnapshot)<>21 THROW 51116,N'Snapshot count validation failed.',1;
IF (SELECT COUNT_BIG(*) FROM dbo.AgAgentVersionBinding)<>83 THROW 51117,N'Binding count validation failed.',1;
DECLARE @DropChecks NVARCHAR(MAX)=N'';
SELECT @DropChecks=@DropChecks+N'ALTER TABLE dbo.AgAgentDefinition DROP CONSTRAINT '+QUOTENAME(name)+N';' FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.AgAgentDefinition') AND definition LIKE N'%DocumentJson%';
IF LEN(@DropChecks)>0 EXEC sys.sp_executesql @DropChecks;
ALTER TABLE dbo.AgAgentDefinition ALTER COLUMN Name NVARCHAR(256) NOT NULL;
ALTER TABLE dbo.AgAgentDefinition ALTER COLUMN Description NVARCHAR(MAX) NOT NULL;
ALTER TABLE dbo.AgAgentDefinition ALTER COLUMN RuntimeStatus VARCHAR(32) NOT NULL;
IF OBJECT_ID(N'dbo.ck_ag_agent_definition_runtime_status',N'C') IS NULL ALTER TABLE dbo.AgAgentDefinition ADD CONSTRAINT ck_ag_agent_definition_runtime_status CHECK(RuntimeStatus IN('Enabled','Disabled','Archived'));
ALTER TABLE dbo.AgAgentDefinition DROP COLUMN DocumentJson;
COMMIT TRANSACTION;
PRINT N'Agent normalization completed: 6 agents, 27 versions, 21 snapshots, 83 bindings.';
END TRY
BEGIN CATCH
IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
THROW;
END CATCH;
GO
