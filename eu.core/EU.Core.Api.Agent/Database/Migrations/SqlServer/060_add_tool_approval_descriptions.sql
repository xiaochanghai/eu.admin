-- Add Chinese descriptions for normalized Tool Approval tables.
SET NOCOUNT ON;

DECLARE @Items TABLE (TableName SYSNAME, ColumnName SYSNAME NULL, Description NVARCHAR(4000));
INSERT @Items VALUES
(N'AgToolApprovalRequest',NULL,N'工具调用审批请求表'),(N'AgToolApprovalPayload',NULL,N'工具调用审批加密载荷表'),
(N'AgToolApprovalDecision',NULL,N'工具调用审批决策历史表'),(N'AgToolApprovalExecutionResult',NULL,N'工具调用审批执行结果表'),
(N'AgToolApprovalRequest',N'ID',N'审批请求主键'),(N'AgToolApprovalRequest',N'TenantId',N'租户标识'),(N'AgToolApprovalRequest',N'RequesterUserId',N'请求用户标识'),
(N'AgToolApprovalRequest',N'ConversationId',N'会话标识'),(N'AgToolApprovalRequest',N'EntryRunId',N'统一入口运行标识'),(N'AgToolApprovalRequest',N'AgentRunId',N'Agent 运行标识'),
(N'AgToolApprovalRequest',N'AgentVersionId',N'Agent 版本标识'),(N'AgToolApprovalRequest',N'McpServerId',N'MCP Server 标识'),(N'AgToolApprovalRequest',N'ToolVersionId',N'工具版本标识'),
(N'AgToolApprovalRequest',N'ToolName',N'工具名称'),(N'AgToolApprovalRequest',N'Risk',N'工具风险等级'),(N'AgToolApprovalRequest',N'ToolSchemaSha256',N'工具 Schema SHA-256'),
(N'AgToolApprovalRequest',N'ArgumentsSha256',N'调用参数 SHA-256'),(N'AgToolApprovalRequest',N'SafeArgumentsSummaryJson',N'安全参数摘要 JSON'),(N'AgToolApprovalRequest',N'Status',N'审批状态'),
(N'AgToolApprovalRequest',N'LogicalRevision',N'逻辑修订号'),(N'AgToolApprovalRequest',N'RequestedAtUtc',N'申请时间（UTC）'),(N'AgToolApprovalRequest',N'ExpiresAtUtc',N'过期时间（UTC）'),
(N'AgToolApprovalRequest',N'DecisionUserId',N'决策用户标识'),(N'AgToolApprovalRequest',N'DecisionReason',N'决策原因'),(N'AgToolApprovalRequest',N'DecidedAtUtc',N'决策时间（UTC）'),
(N'AgToolApprovalRequest',N'ClaimedAtUtc',N'执行领取时间（UTC）'),(N'AgToolApprovalRequest',N'FinishedAtUtc',N'完成时间（UTC）'),(N'AgToolApprovalRequest',N'ErrorCode',N'错误码'),
(N'AgToolApprovalPayload',N'ID',N'主键'),(N'AgToolApprovalPayload',N'ApprovalId',N'审批请求标识'),(N'AgToolApprovalPayload',N'ProtectedPayload',N'受保护的恢复载荷'),
(N'AgToolApprovalPayload',N'ProtectedPayloadSha256',N'受保护载荷 SHA-256'),
(N'AgToolApprovalDecision',N'ID',N'决策记录主键'),(N'AgToolApprovalDecision',N'ApprovalId',N'审批请求标识'),(N'AgToolApprovalDecision',N'TenantId',N'租户标识'),
(N'AgToolApprovalDecision',N'FromStatus',N'原状态'),(N'AgToolApprovalDecision',N'ToStatus',N'目标状态'),(N'AgToolApprovalDecision',N'DecisionUserId',N'决策用户标识'),
(N'AgToolApprovalDecision',N'DecisionReason',N'决策原因'),(N'AgToolApprovalDecision',N'DecidedAtUtc',N'决策时间（UTC）'),(N'AgToolApprovalDecision',N'ResultingLogicalRevision',N'决策后逻辑修订号'),
(N'AgToolApprovalExecutionResult',N'ID',N'主键'),(N'AgToolApprovalExecutionResult',N'ApprovalId',N'审批请求标识'),(N'AgToolApprovalExecutionResult',N'TenantId',N'租户标识'),
(N'AgToolApprovalExecutionResult',N'Succeeded',N'执行成功标记'),(N'AgToolApprovalExecutionResult',N'Blocked',N'执行阻止标记'),(N'AgToolApprovalExecutionResult',N'ProtectedContent',N'受保护的执行结果'),
(N'AgToolApprovalExecutionResult',N'ProtectedContentSha256',N'受保护结果 SHA-256'),(N'AgToolApprovalExecutionResult',N'ContentSha256',N'明文结果 SHA-256'),(N'AgToolApprovalExecutionResult',N'ErrorCode',N'错误码'),
(N'AgToolApprovalExecutionResult',N'FinishedAtUtc',N'完成时间（UTC）');

DECLARE @Tables TABLE (TableName SYSNAME);
INSERT @Tables VALUES (N'AgToolApprovalRequest'),(N'AgToolApprovalPayload'),(N'AgToolApprovalDecision'),(N'AgToolApprovalExecutionResult');
INSERT @Items
SELECT tables.TableName, common.ColumnName, common.Description
FROM @Tables tables CROSS JOIN (VALUES
 (N'IsDeleted',N'软删除标记'),(N'IsActive',N'基础启用标记'),(N'ImportDataId',N'导入数据标识'),(N'ModificationNum',N'修改次数'),
 (N'Tag',N'通用标签'),(N'GroupId',N'集团标识'),(N'CompanyId',N'公司标识'),(N'AuditStatus',N'审核状态'),(N'CurrentNode',N'当前流程节点'),
 (N'CreatedBy',N'创建人标识'),(N'CreatedTime',N'创建时间'),(N'UpdateBy',N'更新人标识'),(N'UpdateTime',N'更新时间')) common(ColumnName,Description);

DECLARE @TableName SYSNAME,@ColumnName SYSNAME,@Description NVARCHAR(4000),@Exists BIT;
DECLARE descriptions CURSOR LOCAL FAST_FORWARD FOR SELECT TableName,ColumnName,Description FROM @Items;
OPEN descriptions; FETCH NEXT FROM descriptions INTO @TableName,@ColumnName,@Description;
WHILE @@FETCH_STATUS=0
BEGIN
    IF OBJECT_ID(N'dbo.'+@TableName,N'U') IS NULL THROW 52220,N'Tool Approval table is missing.',1;
    IF @ColumnName IS NULL OR COL_LENGTH(N'dbo.'+@TableName,@ColumnName) IS NOT NULL
    BEGIN
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
        END
    END;
    FETCH NEXT FROM descriptions INTO @TableName,@ColumnName,@Description;
END;
CLOSE descriptions; DEALLOCATE descriptions;
GO
