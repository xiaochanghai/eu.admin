-- 新增或更新编排运行表及字段的中文说明。

SET NOCOUNT ON;
DECLARE @Items TABLE (TableName SYSNAME, ColumnName SYSNAME NULL, Description NVARCHAR(4000));
INSERT INTO @Items VALUES
(N'AgOrchestrationRun', NULL, N'编排运行汇总表'),
(N'AgOrchestrationRun', N'OrchestrationId', N'所属编排标识'),
(N'AgOrchestrationRun', N'OrchestrationVersionId', N'执行使用的已发布编排版本标识'),
(N'AgOrchestrationRun', N'OrchestrationCode', N'执行时记录的编排编码'),
(N'AgOrchestrationRun', N'Status', N'编排运行状态'),
(N'AgOrchestrationRun', N'StartedAtUtc', N'编排运行开始时间（UTC）'),
(N'AgOrchestrationRun', N'FinishedAtUtc', N'编排运行结束时间（UTC）'),
(N'AgOrchestrationRun', N'InputSha256', N'编排输入内容的 SHA-256 摘要'),
(N'AgOrchestrationRun', N'ErrorCode', N'编排运行错误码'),
(N'AgOrchestrationRunNode', NULL, N'编排运行节点汇总表'),
(N'AgOrchestrationRunNode', N'RunId', N'所属编排运行标识'),
(N'AgOrchestrationRunNode', N'Ordinal', N'节点排列顺序'),
(N'AgOrchestrationRunNode', N'NodeId', N'编排版本内节点标识'),
(N'AgOrchestrationRunNode', N'NodeName', N'节点显示名称'),
(N'AgOrchestrationRunNode', N'AgentId', N'节点使用的 Agent 标识'),
(N'AgOrchestrationRunNode', N'AgentVersionId', N'节点使用的 Agent 版本标识'),
(N'AgOrchestrationRunNode', N'Status', N'节点执行状态'),
(N'AgOrchestrationRunNode', N'Attempts', N'节点执行尝试次数'),
(N'AgOrchestrationRunNode', N'StartedAtUtc', N'节点开始时间（UTC）'),
(N'AgOrchestrationRunNode', N'FinishedAtUtc', N'节点结束时间（UTC）'),
(N'AgOrchestrationRunNode', N'OutputCharacters', N'节点输出字符数'),
(N'AgOrchestrationRunNode', N'InputSha256', N'节点输入内容的 SHA-256 摘要'),
(N'AgOrchestrationRunNode', N'ErrorCode', N'节点执行错误码'),
(N'AgOrchestrationRunDetail', NULL, N'编排运行输入输出明细表'),
(N'AgOrchestrationRunDetail', N'RunId', N'所属编排运行标识'),
(N'AgOrchestrationRunDetail', N'OrchestrationId', N'所属编排标识'),
(N'AgOrchestrationRunDetail', N'InputText', N'编排原始输入内容'),
(N'AgOrchestrationRunDetail', N'OutputText', N'编排最终输出内容'),
(N'AgOrchestrationNodeAttempt', NULL, N'编排节点执行尝试明细表'),
(N'AgOrchestrationNodeAttempt', N'RunId', N'所属编排运行标识'),
(N'AgOrchestrationNodeAttempt', N'NodeId', N'编排版本内节点标识'),
(N'AgOrchestrationNodeAttempt', N'Attempt', N'节点重试序号'),
(N'AgOrchestrationNodeAttempt', N'Sequence', N'运行内执行排列顺序'),
(N'AgOrchestrationNodeAttempt', N'AgentRunId', N'关联的 Agent 运行标识'),
(N'AgOrchestrationNodeAttempt', N'InputText', N'本次尝试输入内容'),
(N'AgOrchestrationNodeAttempt', N'InputSha256', N'本次尝试输入摘要'),
(N'AgOrchestrationNodeAttempt', N'OutputText', N'本次尝试输出内容'),
(N'AgOrchestrationNodeAttempt', N'OutputSha256', N'本次尝试输出摘要'),
(N'AgOrchestrationNodeAttempt', N'Status', N'本次尝试状态'),
(N'AgOrchestrationNodeAttempt', N'StartedAtUtc', N'本次尝试开始时间（UTC）'),
(N'AgOrchestrationNodeAttempt', N'FinishedAtUtc', N'本次尝试结束时间（UTC）'),
(N'AgOrchestrationNodeAttempt', N'ErrorCode', N'本次尝试错误码'),
(N'AgOrchestrationToolCall', NULL, N'编排节点工具调用明细表'),
(N'AgOrchestrationToolCall', N'ToolCallId', N'工具调用业务标识'),
(N'AgOrchestrationToolCall', N'RunId', N'所属编排运行标识'),
(N'AgOrchestrationToolCall', N'NodeId', N'所属节点标识'),
(N'AgOrchestrationToolCall', N'Attempt', N'所属节点重试序号'),
(N'AgOrchestrationToolCall', N'Sequence', N'尝试内工具调用顺序'),
(N'AgOrchestrationToolCall', N'AgentRunId', N'关联的 Agent 运行标识'),
(N'AgOrchestrationToolCall', N'ToolVersionId', N'调用的工具版本标识'),
(N'AgOrchestrationToolCall', N'ToolName', N'工具名称'),
(N'AgOrchestrationToolCall', N'Status', N'工具调用状态'),
(N'AgOrchestrationToolCall', N'ArgumentsJson', N'工具调用参数 JSON'),
(N'AgOrchestrationToolCall', N'ResultContent', N'工具调用结果内容'),
(N'AgOrchestrationToolCall', N'ResultSha256', N'工具调用结果摘要'),
(N'AgOrchestrationToolCall', N'ResultCharacters', N'工具调用结果字符数'),
(N'AgOrchestrationToolCall', N'StartedAtUtc', N'工具调用开始时间（UTC）'),
(N'AgOrchestrationToolCall', N'FinishedAtUtc', N'工具调用结束时间（UTC）'),
(N'AgOrchestrationToolCall', N'ErrorCode', N'工具调用错误码');

DECLARE @Common TABLE (ColumnName SYSNAME, Description NVARCHAR(4000));
INSERT INTO @Common VALUES
(N'ID', N'主键'), (N'IsDeleted', N'软删除标记'), (N'IsActive', N'基础启用标记'),
(N'ImportDataId', N'导入数据标识'), (N'ModificationNum', N'修改次数'),
(N'Tag', N'通用标签'), (N'GroupId', N'集团标识'), (N'CompanyId', N'公司标识'),
(N'AuditStatus', N'审核状态'), (N'CurrentNode', N'当前流程节点'),
(N'CreatedBy', N'创建人标识'), (N'CreatedTime', N'创建时间'),
(N'UpdateBy', N'更新人标识'), (N'UpdateTime', N'更新时间');
INSERT INTO @Items
SELECT tables.TableName, common.ColumnName, common.Description
FROM (VALUES (N'AgOrchestrationRun'), (N'AgOrchestrationRunNode'),
             (N'AgOrchestrationRunDetail'), (N'AgOrchestrationNodeAttempt'),
             (N'AgOrchestrationToolCall')) tables(TableName)
CROSS JOIN @Common common;

DECLARE @TableName SYSNAME, @ColumnName SYSNAME, @Description NVARCHAR(4000), @Exists BIT;
DECLARE descriptions CURSOR LOCAL FAST_FORWARD FOR SELECT TableName, ColumnName, Description FROM @Items;
OPEN descriptions;
FETCH NEXT FROM descriptions INTO @TableName, @ColumnName, @Description;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NOT NULL
       AND (@ColumnName IS NULL OR COL_LENGTH(N'dbo.' + @TableName, @ColumnName) IS NOT NULL)
    BEGIN
        SELECT @Exists = CASE WHEN EXISTS (
            SELECT 1 FROM sys.extended_properties
            WHERE major_id = OBJECT_ID(N'dbo.' + @TableName)
              AND minor_id = CASE WHEN @ColumnName IS NULL THEN 0 ELSE COLUMNPROPERTY(OBJECT_ID(N'dbo.' + @TableName), @ColumnName, 'ColumnId') END
              AND name = N'MS_Description') THEN 1 ELSE 0 END;
        IF @ColumnName IS NULL
        BEGIN
            IF @Exists = 1 EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName;
            ELSE EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName;
        END
        ELSE
        BEGIN
            IF @Exists = 1 EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName,
                @level2type=N'COLUMN', @level2name=@ColumnName;
            ELSE EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName,
                @level2type=N'COLUMN', @level2name=@ColumnName;
        END
    END;
    FETCH NEXT FROM descriptions INTO @TableName, @ColumnName, @Description;
END;
CLOSE descriptions;
DEALLOCATE descriptions;
GO
