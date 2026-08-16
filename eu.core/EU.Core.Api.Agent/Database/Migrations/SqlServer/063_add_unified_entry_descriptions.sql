-- Add Chinese descriptions for normalized Unified Entry tables.
SET NOCOUNT ON;

DECLARE @Items TABLE(TableName SYSNAME,ColumnName SYSNAME NULL,Description NVARCHAR(4000));
INSERT @Items VALUES
(N'AgChatConversation',NULL,N'Agent 对话会话表'),(N'AgChatMessage',NULL,N'Agent 对话消息表'),
(N'AgUnifiedEntryRun',NULL,N'统一入口运行表'),(N'AgUnifiedAgentRun',NULL,N'统一入口 Agent 运行明细表'),
(N'AgUnifiedOrchestrationLink',NULL,N'统一入口编排运行关联表'),(N'AgUnifiedToolCall',NULL,N'统一入口工具调用明细表'),
(N'AgUnifiedRunEvent',NULL,N'统一入口运行事件表'),
(N'AgChatConversation',N'ID',N'会话主键'),(N'AgChatConversation',N'Title',N'会话标题'),(N'AgChatConversation',N'CreatedAtUtc',N'创建时间（UTC）'),(N'AgChatConversation',N'UpdatedAtUtc',N'更新时间（UTC）'),(N'AgChatConversation',N'TenantId',N'租户标识'),(N'AgChatConversation',N'UserId',N'用户标识'),
(N'AgChatMessage',N'ID',N'消息主键'),(N'AgChatMessage',N'ConversationId',N'会话标识'),(N'AgChatMessage',N'Ordinal',N'消息顺序号'),(N'AgChatMessage',N'Role',N'消息角色'),(N'AgChatMessage',N'Content',N'消息内容'),(N'AgChatMessage',N'ContentSha256',N'消息内容 SHA-256'),(N'AgChatMessage',N'ContentUtf8Bytes',N'消息内容 UTF-8 字节数'),(N'AgChatMessage',N'CreatedAtUtc',N'消息创建时间（UTC）'),(N'AgChatMessage',N'Kind',N'消息类型'),(N'AgChatMessage',N'BusinessQueryId',N'业务查询标识'),(N'AgChatMessage',N'BusinessReceiptJson',N'业务查询回执 JSON'),(N'AgChatMessage',N'BusinessPresentationJson',N'业务查询展示数据 JSON'),(N'AgChatMessage',N'BusinessIntegritySha256',N'业务查询完整性 SHA-256'),
(N'AgUnifiedEntryRun',N'ID',N'统一入口运行主键'),(N'AgUnifiedEntryRun',N'ConversationId',N'会话标识'),(N'AgUnifiedEntryRun',N'CorrelationId',N'关联追踪标识'),(N'AgUnifiedEntryRun',N'MainAgentVersionId',N'主 Agent 版本标识'),(N'AgUnifiedEntryRun',N'Status',N'运行状态'),(N'AgUnifiedEntryRun',N'StartedAtUtc',N'开始时间（UTC）'),(N'AgUnifiedEntryRun',N'FinishedAtUtc',N'完成时间（UTC）'),(N'AgUnifiedEntryRun',N'DurationTicks',N'运行耗时 Tick 数'),(N'AgUnifiedEntryRun',N'InputText',N'运行输入'),(N'AgUnifiedEntryRun',N'InputSha256',N'输入 SHA-256'),(N'AgUnifiedEntryRun',N'OutputText',N'运行输出'),(N'AgUnifiedEntryRun',N'OutputSha256',N'输出 SHA-256'),(N'AgUnifiedEntryRun',N'ErrorCode',N'错误码'),(N'AgUnifiedEntryRun',N'PersistenceRevision',N'持久化修订号'),(N'AgUnifiedEntryRun',N'StateSha256',N'保存操作指纹 SHA-256'),(N'AgUnifiedEntryRun',N'TenantId',N'租户标识'),(N'AgUnifiedEntryRun',N'UserId',N'用户标识'),
(N'AgUnifiedAgentRun',N'ID',N'Agent 运行明细主键'),(N'AgUnifiedAgentRun',N'EntryRunId',N'统一入口运行标识'),(N'AgUnifiedAgentRun',N'Ordinal',N'明细顺序号'),(N'AgUnifiedAgentRun',N'ParentRunId',N'父运行标识'),(N'AgUnifiedAgentRun',N'Kind',N'Agent 运行类型'),(N'AgUnifiedAgentRun',N'AgentId',N'Agent 标识'),(N'AgUnifiedAgentRun',N'AgentVersionId',N'Agent 版本标识'),(N'AgUnifiedAgentRun',N'Depth',N'调用深度'),(N'AgUnifiedAgentRun',N'Status',N'运行状态'),(N'AgUnifiedAgentRun',N'StartedAtUtc',N'开始时间（UTC）'),(N'AgUnifiedAgentRun',N'FinishedAtUtc',N'完成时间（UTC）'),(N'AgUnifiedAgentRun',N'DurationTicks',N'运行耗时 Tick 数'),(N'AgUnifiedAgentRun',N'InputText',N'运行输入'),(N'AgUnifiedAgentRun',N'InputSha256',N'输入 SHA-256'),(N'AgUnifiedAgentRun',N'OutputText',N'运行输出'),(N'AgUnifiedAgentRun',N'OutputSha256',N'输出 SHA-256'),(N'AgUnifiedAgentRun',N'ErrorCode',N'错误码'),
(N'AgUnifiedOrchestrationLink',N'ID',N'编排运行关联主键'),(N'AgUnifiedOrchestrationLink',N'EntryRunId',N'统一入口运行标识'),(N'AgUnifiedOrchestrationLink',N'Ordinal',N'明细顺序号'),(N'AgUnifiedOrchestrationLink',N'ParentRunId',N'父运行标识'),(N'AgUnifiedOrchestrationLink',N'OrchestrationRunId',N'编排运行标识'),(N'AgUnifiedOrchestrationLink',N'OrchestrationVersionId',N'编排版本标识'),(N'AgUnifiedOrchestrationLink',N'Depth',N'调用深度'),(N'AgUnifiedOrchestrationLink',N'Status',N'运行状态'),(N'AgUnifiedOrchestrationLink',N'StartedAtUtc',N'开始时间（UTC）'),(N'AgUnifiedOrchestrationLink',N'FinishedAtUtc',N'完成时间（UTC）'),(N'AgUnifiedOrchestrationLink',N'DurationTicks',N'运行耗时 Tick 数'),(N'AgUnifiedOrchestrationLink',N'InputText',N'运行输入'),(N'AgUnifiedOrchestrationLink',N'InputSha256',N'输入 SHA-256'),(N'AgUnifiedOrchestrationLink',N'OutputText',N'运行输出'),(N'AgUnifiedOrchestrationLink',N'OutputSha256',N'输出 SHA-256'),(N'AgUnifiedOrchestrationLink',N'ErrorCode',N'错误码'),
(N'AgUnifiedToolCall',N'ID',N'工具调用明细主键'),(N'AgUnifiedToolCall',N'EntryRunId',N'统一入口运行标识'),(N'AgUnifiedToolCall',N'Ordinal',N'明细顺序号'),(N'AgUnifiedToolCall',N'ParentRunId',N'父运行标识'),(N'AgUnifiedToolCall',N'ToolVersionId',N'工具版本标识'),(N'AgUnifiedToolCall',N'Depth',N'调用深度'),(N'AgUnifiedToolCall',N'Status',N'调用状态'),(N'AgUnifiedToolCall',N'StartedAtUtc',N'开始时间（UTC）'),(N'AgUnifiedToolCall',N'FinishedAtUtc',N'完成时间（UTC）'),(N'AgUnifiedToolCall',N'DurationTicks',N'运行耗时 Tick 数'),(N'AgUnifiedToolCall',N'ArgumentsJson',N'调用参数 JSON'),(N'AgUnifiedToolCall',N'ArgumentsSha256',N'调用参数 SHA-256'),(N'AgUnifiedToolCall',N'ResultContent',N'工具调用结果'),(N'AgUnifiedToolCall',N'ResultSha256',N'工具结果 SHA-256'),(N'AgUnifiedToolCall',N'ErrorCode',N'错误码'),
(N'AgUnifiedRunEvent',N'ID',N'运行事件主键'),(N'AgUnifiedRunEvent',N'EntryRunId',N'统一入口运行标识'),(N'AgUnifiedRunEvent',N'Sequence',N'事件序号'),(N'AgUnifiedRunEvent',N'CorrelationId',N'关联追踪标识'),(N'AgUnifiedRunEvent',N'Kind',N'事件类型'),(N'AgUnifiedRunEvent',N'OccurredAtUtc',N'发生时间（UTC）'),(N'AgUnifiedRunEvent',N'ParentRunId',N'父运行标识'),(N'AgUnifiedRunEvent',N'Depth',N'调用深度'),(N'AgUnifiedRunEvent',N'PayloadJson',N'事件载荷 JSON'),(N'AgUnifiedRunEvent',N'PayloadSha256',N'事件载荷 SHA-256');

DECLARE @Tables TABLE(TableName SYSNAME);
INSERT @Tables VALUES(N'AgChatConversation'),(N'AgChatMessage'),(N'AgUnifiedEntryRun'),(N'AgUnifiedAgentRun'),(N'AgUnifiedOrchestrationLink'),(N'AgUnifiedToolCall'),(N'AgUnifiedRunEvent');
INSERT @Items
SELECT tables.TableName,common.ColumnName,common.Description
FROM @Tables tables CROSS JOIN (VALUES
 (N'IsDeleted',N'软删除标记'),(N'IsActive',N'基础启用标记'),(N'ImportDataId',N'导入数据标识'),(N'ModificationNum',N'修改次数'),
 (N'Tag',N'通用标签'),(N'GroupId',N'集团标识'),(N'CompanyId',N'公司标识'),(N'AuditStatus',N'审核状态'),(N'CurrentNode',N'当前流程节点'),
 (N'CreatedBy',N'创建人标识'),(N'CreatedTime',N'创建时间'),(N'UpdateBy',N'更新人标识'),(N'UpdateTime',N'更新时间')) common(ColumnName,Description);

DECLARE @TableName SYSNAME,@ColumnName SYSNAME,@Description NVARCHAR(4000),@Exists BIT;
DECLARE descriptions CURSOR LOCAL FAST_FORWARD FOR SELECT TableName,ColumnName,Description FROM @Items;
OPEN descriptions; FETCH NEXT FROM descriptions INTO @TableName,@ColumnName,@Description;
WHILE @@FETCH_STATUS=0
BEGIN
    IF OBJECT_ID(N'dbo.'+@TableName,N'U') IS NULL THROW 52320,N'Unified Entry table is missing.',1;
    IF @ColumnName IS NOT NULL AND COL_LENGTH(N'dbo.'+@TableName,@ColumnName) IS NULL THROW 52321,N'Unified Entry column is missing.',1;
    SELECT @Exists=CASE WHEN EXISTS(SELECT 1 FROM sys.extended_properties WHERE major_id=OBJECT_ID(N'dbo.'+@TableName) AND minor_id=CASE WHEN @ColumnName IS NULL THEN 0 ELSE COLUMNPROPERTY(OBJECT_ID(N'dbo.'+@TableName),@ColumnName,'ColumnId') END AND name=N'MS_Description') THEN 1 ELSE 0 END;
    IF @ColumnName IS NULL
    BEGIN
        IF @Exists=1 EXEC sys.sp_updateextendedproperty @name=N'MS_Description',@value=@Description,@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=@TableName;
        ELSE EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=@Description,@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=@TableName;
    END
    ELSE
    BEGIN
        IF @Exists=1 EXEC sys.sp_updateextendedproperty @name=N'MS_Description',@value=@Description,@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=@TableName,@level2type=N'COLUMN',@level2name=@ColumnName;
        ELSE EXEC sys.sp_addextendedproperty @name=N'MS_Description',@value=@Description,@level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=@TableName,@level2type=N'COLUMN',@level2name=@ColumnName;
    END;
    FETCH NEXT FROM descriptions INTO @TableName,@ColumnName,@Description;
END;
CLOSE descriptions; DEALLOCATE descriptions;
GO
