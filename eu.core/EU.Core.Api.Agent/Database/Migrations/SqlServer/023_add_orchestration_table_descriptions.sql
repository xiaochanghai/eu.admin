-- Add or update Chinese descriptions for normalized Orchestration definition tables.

SET NOCOUNT ON;
DECLARE @Items TABLE (TableName SYSNAME, ColumnName SYSNAME NULL, Description NVARCHAR(4000));
INSERT INTO @Items VALUES
(N'AgOrchestrationDefinition', NULL, N'编排定义主表'),
(N'AgOrchestrationDefinition', N'Code', N'编排唯一编码'),
(N'AgOrchestrationDefinition', N'Name', N'编排显示名称'),
(N'AgOrchestrationDefinition', N'Description', N'编排说明'),
(N'AgOrchestrationDefinition', N'Status', N'生命周期状态：Enabled、Disabled 或 Archived'),
(N'AgOrchestrationDefinition', N'LogicalRevision', N'逻辑修订号，用于乐观并发控制'),
(N'AgOrchestrationVersion', NULL, N'编排草稿和发布版本表'),
(N'AgOrchestrationVersion', N'OrchestrationId', N'所属编排主键'),
(N'AgOrchestrationVersion', N'Ordinal', N'版本排列顺序；草稿固定为 0'),
(N'AgOrchestrationVersion', N'Label', N'版本标签'),
(N'AgOrchestrationVersion', N'IsDraft', N'是否为草稿版本'),
(N'AgOrchestrationVersion', N'StartNodeId', N'起始节点标识'),
(N'AgOrchestrationNode', NULL, N'编排版本节点表'),
(N'AgOrchestrationNode', N'OrchestrationId', N'所属编排主键'),
(N'AgOrchestrationNode', N'VersionId', N'所属编排版本主键'),
(N'AgOrchestrationNode', N'Ordinal', N'节点排列顺序'),
(N'AgOrchestrationNode', N'NodeId', N'版本内节点标识'),
(N'AgOrchestrationNode', N'Name', N'节点显示名称'),
(N'AgOrchestrationNode', N'AgentId', N'节点使用的 Agent 主键'),
(N'AgOrchestrationNode', N'InputMode', N'输入模式'),
(N'AgOrchestrationNode', N'InputTemplate', N'输入模板'),
(N'AgOrchestrationNode', N'MaximumRetries', N'最大重试次数'),
(N'AgOrchestrationNode', N'TimeoutSeconds', N'节点超时秒数'),
(N'AgOrchestrationEdge', NULL, N'编排版本连线表'),
(N'AgOrchestrationEdge', N'OrchestrationId', N'所属编排主键'),
(N'AgOrchestrationEdge', N'VersionId', N'所属编排版本主键'),
(N'AgOrchestrationEdge', N'Ordinal', N'连线存储顺序'),
(N'AgOrchestrationEdge', N'FromNodeId', N'源节点标识'),
(N'AgOrchestrationEdge', N'ToNodeId', N'目标节点标识'),
(N'AgOrchestrationEdge', N'Condition', N'连线条件'),
(N'AgOrchestrationEdge', N'ConditionValue', N'连线条件值'),
(N'AgOrchestrationEdge', N'SortOrder', N'条件匹配顺序'),
(N'AgOrchestrationAgentBinding', NULL, N'编排发布版本的 Agent 版本绑定表'),
(N'AgOrchestrationAgentBinding', N'OrchestrationId', N'所属编排主键'),
(N'AgOrchestrationAgentBinding', N'VersionId', N'所属发布版本主键'),
(N'AgOrchestrationAgentBinding', N'Ordinal', N'绑定排列顺序'),
(N'AgOrchestrationAgentBinding', N'AgentId', N'绑定的 Agent 主键'),
(N'AgOrchestrationAgentBinding', N'AgentVersionId', N'绑定的 Agent 发布版本主键');

DECLARE @Common TABLE (ColumnName SYSNAME, Description NVARCHAR(4000));
INSERT INTO @Common VALUES
(N'ID', N'主键'), (N'IsDeleted', N'软删除标记'), (N'IsActive', N'基础启用标记'),
(N'ImportDataId', N'导入数据标识'), (N'ModificationNum', N'修改次数'), (N'Tag', N'通用标签'),
(N'GroupId', N'集团标识'), (N'CompanyId', N'公司标识'), (N'AuditStatus', N'审核状态'),
(N'CurrentNode', N'当前流程节点'), (N'CreatedBy', N'创建人'), (N'CreatedTime', N'创建时间'),
(N'UpdateBy', N'更新人'), (N'UpdateTime', N'更新时间');
INSERT INTO @Items
SELECT tables.TableName, common.ColumnName, common.Description
FROM (VALUES
    (N'AgOrchestrationDefinition'), (N'AgOrchestrationVersion'), (N'AgOrchestrationNode'),
    (N'AgOrchestrationEdge'), (N'AgOrchestrationAgentBinding')) tables(TableName)
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
