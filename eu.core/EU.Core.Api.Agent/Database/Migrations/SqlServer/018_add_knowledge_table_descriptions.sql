-- Add or update Chinese descriptions for normalized Knowledge tables. SQL Server 2014+.

SET NOCOUNT ON;
DECLARE @Items TABLE (TableName SYSNAME, ColumnName SYSNAME NULL, Description NVARCHAR(4000));
INSERT INTO @Items VALUES
(N'AgKnowledgeBaseDefinition', NULL, N'知识库定义主表'),
(N'AgKnowledgeBaseDefinition', N'Code', N'知识库唯一编码'),
(N'AgKnowledgeBaseDefinition', N'Name', N'知识库显示名称'),
(N'AgKnowledgeBaseDefinition', N'Description', N'知识库说明'),
(N'AgKnowledgeBaseDefinition', N'Status', N'生命周期状态：Enabled、Disabled 或 Archived'),
(N'AgKnowledgeBaseDefinition', N'LogicalRevision', N'逻辑修订号，用于乐观并发控制'),
(N'AgKnowledgeBaseDefinition', N'IndexedAtUtc', N'最近索引 UTC 时间'),
(N'AgKnowledgeDocument', NULL, N'知识库文档表'),
(N'AgKnowledgeDocument', N'KnowledgeBaseId', N'所属知识库主键'),
(N'AgKnowledgeDocument', N'Ordinal', N'文档排列顺序，从 0 开始'),
(N'AgKnowledgeDocument', N'FileName', N'原始文件名'),
(N'AgKnowledgeDocument', N'MediaType', N'文档媒体类型'),
(N'AgKnowledgeDocument', N'Sha256', N'文档正文 SHA-256 摘要'),
(N'AgKnowledgeDocument', N'Content', N'提取并规范化后的文档正文'),
(N'AgKnowledgeDocument', N'ImportedAtUtc', N'导入 UTC 时间'),
(N'AgKnowledgeChunk', NULL, N'知识库检索分块表'),
(N'AgKnowledgeChunk', N'KnowledgeBaseId', N'所属知识库主键'),
(N'AgKnowledgeChunk', N'DocumentId', N'所属知识文档主键'),
(N'AgKnowledgeChunk', N'Sequence', N'文档内分块序号，从 0 开始'),
(N'AgKnowledgeChunk', N'Content', N'用于词法检索的分块正文');

DECLARE @Common TABLE (ColumnName SYSNAME, Description NVARCHAR(4000));
INSERT INTO @Common VALUES
(N'ID', N'主键'), (N'IsDeleted', N'软删除标记'), (N'IsActive', N'基础启用标记'),
(N'ImportDataId', N'导入数据标识'), (N'ModificationNum', N'修改次数'), (N'Tag', N'通用标签'),
(N'GroupId', N'集团标识'), (N'CompanyId', N'公司标识'), (N'AuditStatus', N'审核状态'),
(N'CurrentNode', N'当前流程节点'), (N'CreatedBy', N'创建人'), (N'CreatedTime', N'创建时间'),
(N'UpdateBy', N'更新人'), (N'UpdateTime', N'更新时间');
INSERT INTO @Items
SELECT tables.TableName, common.ColumnName, common.Description
FROM (VALUES (N'AgKnowledgeBaseDefinition'), (N'AgKnowledgeDocument'), (N'AgKnowledgeChunk')) tables(TableName)
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
