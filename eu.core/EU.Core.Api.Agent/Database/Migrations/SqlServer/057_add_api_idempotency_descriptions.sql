-- 新增或更新 Agent API 幂等请求记录表及字段的中文说明。
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.AgApiIdempotency', N'U') IS NULL
    THROW 52110, N'AgApiIdempotency is missing. Run 056_normalize_api_idempotency.sql first.', 1;

DECLARE @Items TABLE (ColumnName SYSNAME NULL, Description NVARCHAR(4000));
INSERT INTO @Items VALUES
(NULL, N'Agent API 幂等请求记录表'),
(N'ID', N'主键'),
(N'ScopeSha256', N'幂等请求作用域 SHA-256 哈希'),
(N'RequestSha256', N'请求内容 SHA-256 哈希'),
(N'Status', N'幂等请求状态'),
(N'ResponseStatusCode', N'缓存的 HTTP 响应状态码'),
(N'ResponseContentType', N'缓存的 HTTP 响应内容类型'),
(N'ResponseLocation', N'缓存的 HTTP Location 响应头'),
(N'ResponseBody', N'缓存的 HTTP 响应正文'),
(N'CreatedAtUtc', N'记录创建时间（UTC）'),
(N'ExpiresAtUtc', N'记录过期时间（UTC）'),
(N'IsDeleted', N'软删除标记'),
(N'IsActive', N'基础启用标记'),
(N'ImportDataId', N'导入数据标识'),
(N'ModificationNum', N'修改次数'),
(N'Tag', N'通用标签'),
(N'GroupId', N'集团标识'),
(N'CompanyId', N'公司标识'),
(N'AuditStatus', N'审核状态'),
(N'CurrentNode', N'当前流程节点'),
(N'CreatedBy', N'创建人标识'),
(N'CreatedTime', N'创建时间'),
(N'UpdateBy', N'更新人标识'),
(N'UpdateTime', N'更新时间');

DECLARE @ColumnName SYSNAME, @Description NVARCHAR(4000), @Exists BIT;
DECLARE descriptions CURSOR LOCAL FAST_FORWARD FOR SELECT ColumnName, Description FROM @Items;
OPEN descriptions;
FETCH NEXT FROM descriptions INTO @ColumnName, @Description;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF @ColumnName IS NULL OR COL_LENGTH(N'dbo.AgApiIdempotency', @ColumnName) IS NOT NULL
    BEGIN
        SELECT @Exists = CASE WHEN EXISTS (
            SELECT 1 FROM sys.extended_properties
            WHERE major_id = OBJECT_ID(N'dbo.AgApiIdempotency')
              AND minor_id = CASE WHEN @ColumnName IS NULL THEN 0 ELSE COLUMNPROPERTY(OBJECT_ID(N'dbo.AgApiIdempotency'), @ColumnName, 'ColumnId') END
              AND name = N'MS_Description') THEN 1 ELSE 0 END;
        IF @ColumnName IS NULL
        BEGIN
            IF @Exists = 1 EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'AgApiIdempotency';
            ELSE EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'AgApiIdempotency';
        END
        ELSE
        BEGIN
            IF @Exists = 1 EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'AgApiIdempotency',
                @level2type=N'COLUMN', @level2name=@ColumnName;
            ELSE EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'AgApiIdempotency',
                @level2type=N'COLUMN', @level2name=@ColumnName;
        END;
    END;
    FETCH NEXT FROM descriptions INTO @ColumnName, @Description;
END;
CLOSE descriptions;
DEALLOCATE descriptions;
GO
