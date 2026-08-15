-- Add or update Chinese descriptions for normalized MCP tables and business columns.
-- SQL Server 2014+; idempotent.

SET NOCOUNT ON;
DECLARE @Items TABLE (TableName SYSNAME, ColumnName SYSNAME NULL, Description NVARCHAR(4000));
INSERT INTO @Items VALUES
(N'AgMcpServerDefinition', NULL, N'MCP Server 定义主表'),
(N'AgMcpServerDefinition', N'ID', N'主键'),
(N'AgMcpServerDefinition', N'IsDeleted', N'软删除标记'),
(N'AgMcpServerDefinition', N'IsActive', N'基础启用标记'),
(N'AgMcpServerDefinition', N'ImportDataId', N'导入数据标识'),
(N'AgMcpServerDefinition', N'ModificationNum', N'修改次数'),
(N'AgMcpServerDefinition', N'Tag', N'通用标签'),
(N'AgMcpServerDefinition', N'GroupId', N'集团标识'),
(N'AgMcpServerDefinition', N'CompanyId', N'公司标识'),
(N'AgMcpServerDefinition', N'AuditStatus', N'审核状态'),
(N'AgMcpServerDefinition', N'CurrentNode', N'当前流程节点'),
(N'AgMcpServerDefinition', N'CreatedBy', N'创建人'),
(N'AgMcpServerDefinition', N'CreatedTime', N'创建时间'),
(N'AgMcpServerDefinition', N'UpdateBy', N'更新人'),
(N'AgMcpServerDefinition', N'UpdateTime', N'更新时间'),
(N'AgMcpServerDefinition', N'Code', N'MCP Server 唯一编码'),
(N'AgMcpServerDefinition', N'Name', N'MCP Server 显示名称'),
(N'AgMcpServerDefinition', N'Description', N'MCP Server 说明'),
(N'AgMcpServerDefinition', N'Transport', N'传输类型：StreamableHttp、Sse 或 Stdio'),
(N'AgMcpServerDefinition', N'Endpoint', N'HTTP 或 SSE 端点'),
(N'AgMcpServerDefinition', N'Command', N'Stdio 启动命令'),
(N'AgMcpServerDefinition', N'CredentialAlias', N'凭据别名，不保存明文凭据'),
(N'AgMcpServerDefinition', N'Enabled', N'是否启用'),
(N'AgMcpServerDefinition', N'LogicalRevision', N'逻辑修订号，用于乐观并发控制'),
(N'AgMcpServerDefinition', N'Status', N'生命周期与同步状态'),
(N'AgMcpServerDefinition', N'LastError', N'最近同步错误'),
(N'AgMcpServerDefinition', N'LastSyncedAtUtc', N'最近同步 UTC 时间'),
(N'AgMcpServerArgument', NULL, N'MCP Server Stdio 参数表'),
(N'AgMcpServerArgument', N'ID', N'主键'),
(N'AgMcpServerArgument', N'IsDeleted', N'软删除标记'),
(N'AgMcpServerArgument', N'IsActive', N'基础启用标记'),
(N'AgMcpServerArgument', N'ImportDataId', N'导入数据标识'),
(N'AgMcpServerArgument', N'ModificationNum', N'修改次数'),
(N'AgMcpServerArgument', N'Tag', N'通用标签'),
(N'AgMcpServerArgument', N'GroupId', N'集团标识'),
(N'AgMcpServerArgument', N'CompanyId', N'公司标识'),
(N'AgMcpServerArgument', N'AuditStatus', N'审核状态'),
(N'AgMcpServerArgument', N'CurrentNode', N'当前流程节点'),
(N'AgMcpServerArgument', N'CreatedBy', N'创建人'),
(N'AgMcpServerArgument', N'CreatedTime', N'创建时间'),
(N'AgMcpServerArgument', N'UpdateBy', N'更新人'),
(N'AgMcpServerArgument', N'UpdateTime', N'更新时间'),
(N'AgMcpServerArgument', N'ServerId', N'所属 MCP Server 主键'),
(N'AgMcpServerArgument', N'Ordinal', N'参数排列顺序，从 0 开始'),
(N'AgMcpServerArgument', N'Value', N'参数值'),
(N'AgMcpToolVersion', NULL, N'MCP 工具不可变版本历史表'),
(N'AgMcpToolVersion', N'ID', N'工具版本主键'),
(N'AgMcpToolVersion', N'IsDeleted', N'软删除标记'),
(N'AgMcpToolVersion', N'IsActive', N'基础启用标记'),
(N'AgMcpToolVersion', N'ImportDataId', N'导入数据标识'),
(N'AgMcpToolVersion', N'ModificationNum', N'修改次数'),
(N'AgMcpToolVersion', N'Tag', N'通用标签'),
(N'AgMcpToolVersion', N'GroupId', N'集团标识'),
(N'AgMcpToolVersion', N'CompanyId', N'公司标识'),
(N'AgMcpToolVersion', N'AuditStatus', N'审核状态'),
(N'AgMcpToolVersion', N'CurrentNode', N'当前流程节点'),
(N'AgMcpToolVersion', N'CreatedBy', N'创建人'),
(N'AgMcpToolVersion', N'CreatedTime', N'创建时间'),
(N'AgMcpToolVersion', N'UpdateBy', N'更新人'),
(N'AgMcpToolVersion', N'UpdateTime', N'更新时间'),
(N'AgMcpToolVersion', N'ServerId', N'所属 MCP Server 主键'),
(N'AgMcpToolVersion', N'HistoryOrdinal', N'历史版本排列顺序，从 0 开始'),
(N'AgMcpToolVersion', N'CurrentOrdinal', N'当前工具排列顺序；历史版本为空'),
(N'AgMcpToolVersion', N'Name', N'工具名称'),
(N'AgMcpToolVersion', N'Description', N'工具说明'),
(N'AgMcpToolVersion', N'InputSchemaJson', N'工具输入 JSON Schema'),
(N'AgMcpToolVersion', N'Risk', N'工具风险等级'),
(N'AgMcpToolVersion', N'Sha256', N'工具版本 SHA-256 摘要'),
(N'AgMcpToolVersion', N'DiscoveredAtUtc', N'发现 UTC 时间');

DECLARE @TableName SYSNAME, @ColumnName SYSNAME, @Description NVARCHAR(4000), @Exists BIT;
DECLARE descriptions CURSOR LOCAL FAST_FORWARD FOR SELECT TableName, ColumnName, Description FROM @Items;
OPEN descriptions;
FETCH NEXT FROM descriptions INTO @TableName, @ColumnName, @Description;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(N'dbo.' + QUOTENAME(@TableName), N'U') IS NOT NULL
    BEGIN
        SELECT @Exists = CASE WHEN EXISTS (
            SELECT 1 FROM sys.extended_properties
            WHERE major_id = OBJECT_ID(N'dbo.' + @TableName)
              AND minor_id = CASE WHEN @ColumnName IS NULL THEN 0 ELSE COLUMNPROPERTY(OBJECT_ID(N'dbo.' + @TableName), @ColumnName, 'ColumnId') END
              AND name = N'MS_Description') THEN 1 ELSE 0 END;
        IF @ColumnName IS NULL
        BEGIN
            IF @Exists = 1
                EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description,
                    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName;
            ELSE
                EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName;
        END
        ELSE
        BEGIN
            IF @Exists = 1
                EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description,
                    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName,
                    @level2type=N'COLUMN', @level2name=@ColumnName;
            ELSE
                EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=@TableName,
                    @level2type=N'COLUMN', @level2name=@ColumnName;
        END;
    END;
    FETCH NEXT FROM descriptions INTO @TableName, @ColumnName, @Description;
END;
CLOSE descriptions;
DEALLOCATE descriptions;
GO
