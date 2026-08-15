-- 新增或更新 Agent API 操作审计表及栏位的中文说明。
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.AgAgentOperationAudit', N'U') IS NULL
    THROW 52025, N'AgAgentOperationAudit is missing.', 1;
DECLARE @Items TABLE (ColumnName SYSNAME NULL, Description NVARCHAR(4000));
INSERT INTO @Items VALUES
(NULL, N'Agent API 操作审计表'), (N'ID', N'操作审计标识（主键）'),
(N'TenantId', N'租户标识'), (N'UserId', N'操作用户标识'),
(N'CorrelationId', N'请求关联标识'), (N'Policy', N'请求要求的授权策略'),
(N'Method', N'HTTP 请求方法'), (N'Path', N'匹配的 API 路由'),
(N'StatusCode', N'HTTP 响应状态码'), (N'Outcome', N'操作执行结果'),
(N'ErrorCode', N'操作错误码'), (N'DurationMilliseconds', N'操作耗时（毫秒）'),
(N'OccurredAtUtc', N'操作发生时间（UTC）'), (N'IsDeleted', N'软删除标记'),
(N'IsActive', N'基础启用标记'), (N'ImportDataId', N'导入数据标识'),
(N'ModificationNum', N'修改次数'), (N'Tag', N'通用标签'),
(N'GroupId', N'集团标识'), (N'CompanyId', N'公司标识'),
(N'AuditStatus', N'审核状态'), (N'CurrentNode', N'当前流程节点'),
(N'CreatedBy', N'创建人标识'), (N'CreatedTime', N'创建时间'),
(N'UpdateBy', N'更新人标识'), (N'UpdateTime', N'更新时间');

DECLARE @Column SYSNAME, @Description NVARCHAR(4000), @Exists BIT;
DECLARE descriptions CURSOR LOCAL FAST_FORWARD FOR SELECT ColumnName, Description FROM @Items;
OPEN descriptions; FETCH NEXT FROM descriptions INTO @Column, @Description;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF @Column IS NULL OR COL_LENGTH(N'dbo.AgAgentOperationAudit', @Column) IS NOT NULL
    BEGIN
        SELECT @Exists = CASE WHEN EXISTS (
            SELECT 1 FROM sys.extended_properties
            WHERE major_id = OBJECT_ID(N'dbo.AgAgentOperationAudit')
              AND minor_id = CASE WHEN @Column IS NULL THEN 0 ELSE COLUMNPROPERTY(OBJECT_ID(N'dbo.AgAgentOperationAudit'), @Column, 'ColumnId') END
              AND name = N'MS_Description') THEN 1 ELSE 0 END;
        IF @Column IS NULL
        BEGIN
            IF @Exists = 1 EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description, @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'AgAgentOperationAudit';
            ELSE EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description, @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'AgAgentOperationAudit';
        END
        ELSE
        BEGIN
            IF @Exists = 1 EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description, @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'AgAgentOperationAudit', @level2type=N'COLUMN', @level2name=@Column;
            ELSE EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description, @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'AgAgentOperationAudit', @level2type=N'COLUMN', @level2name=@Column;
        END;
    END;
    FETCH NEXT FROM descriptions INTO @Column, @Description;
END;
CLOSE descriptions; DEALLOCATE descriptions;
GO
