-- 新增或更新 Agent 运行审计表及字段的中文说明。

SET NOCOUNT ON;
DECLARE @Items TABLE (TableName SYSNAME, ColumnName SYSNAME NULL, Description NVARCHAR(4000));
INSERT INTO @Items VALUES
(N'AgAgentRunAudit', NULL, N'Agent 运行审计汇总表'),
(N'AgAgentRunAudit', N'ID', N'Agent 运行标识（主键）'),
(N'AgAgentRunAudit', N'AgentId', N'执行的 Agent 标识'),
(N'AgAgentRunAudit', N'AgentVersionId', N'执行使用的已发布 Agent 版本标识'),
(N'AgAgentRunAudit', N'AgentCode', N'执行时记录的 Agent 编码'),
(N'AgAgentRunAudit', N'Status', N'Agent 运行状态'),
(N'AgAgentRunAudit', N'StartedAtUtc', N'Agent 运行开始时间（UTC）'),
(N'AgAgentRunAudit', N'FinishedAtUtc', N'Agent 运行结束时间（UTC）'),
(N'AgAgentRunAudit', N'InputSha256', N'运行输入内容的 SHA-256 摘要'),
(N'AgAgentRunAudit', N'OutputCharacters', N'运行输出字符数'),
(N'AgAgentRunAudit', N'ToolCallCount', N'工具调用次数'),
(N'AgAgentRunAudit', N'ErrorCode', N'运行错误码'),
(N'AgAgentToolCallAudit', NULL, N'Agent 工具调用审计明细表'),
(N'AgAgentToolCallAudit', N'ID', N'主键'),
(N'AgAgentToolCallAudit', N'RunId', N'所属 Agent 运行标识'),
(N'AgAgentToolCallAudit', N'Ordinal', N'工具调用排列顺序'),
(N'AgAgentToolCallAudit', N'ToolVersionId', N'调用的工具版本标识'),
(N'AgAgentToolCallAudit', N'ToolName', N'工具名称'),
(N'AgAgentToolCallAudit', N'Risk', N'工具风险等级'),
(N'AgAgentToolCallAudit', N'Status', N'工具调用结果状态'),
(N'AgAgentToolCallAudit', N'StartedAtUtc', N'工具调用开始时间（UTC）'),
(N'AgAgentToolCallAudit', N'FinishedAtUtc', N'工具调用结束时间（UTC）'),
(N'AgAgentToolCallAudit', N'ErrorCode', N'工具调用错误码');

DECLARE @Common TABLE (ColumnName SYSNAME, Description NVARCHAR(4000));
INSERT INTO @Common VALUES
(N'IsDeleted', N'软删除标记'), (N'IsActive', N'基础启用标记'),
(N'ImportDataId', N'导入数据标识'), (N'ModificationNum', N'修改次数'),
(N'Tag', N'通用标签'), (N'GroupId', N'集团标识'), (N'CompanyId', N'公司标识'),
(N'AuditStatus', N'审核状态'), (N'CurrentNode', N'当前流程节点'),
(N'CreatedBy', N'创建人标识'), (N'CreatedTime', N'创建时间'),
(N'UpdateBy', N'更新人标识'), (N'UpdateTime', N'更新时间');
INSERT INTO @Items
SELECT tables.TableName, common.ColumnName, common.Description
FROM (VALUES (N'AgAgentRunAudit'), (N'AgAgentToolCallAudit')) tables(TableName)
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
        END;
    END;
    FETCH NEXT FROM descriptions INTO @TableName, @ColumnName, @Description;
END;
CLOSE descriptions;
DEALLOCATE descriptions;
GO
