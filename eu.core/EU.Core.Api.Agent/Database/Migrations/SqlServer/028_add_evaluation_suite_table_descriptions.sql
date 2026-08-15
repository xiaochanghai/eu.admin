-- Add or update descriptions for normalized Evaluation Suite tables.

SET NOCOUNT ON;
DECLARE @Items TABLE (TableName SYSNAME, ColumnName SYSNAME NULL, Description NVARCHAR(4000));
INSERT INTO @Items VALUES
(N'AgEvaluationSuite', NULL, N'评测套件定义主表'),
(N'AgEvaluationSuite', N'TenantId', N'租户标识'),
(N'AgEvaluationSuite', N'Code', N'租户内唯一编码'),
(N'AgEvaluationSuite', N'Name', N'套件显示名称'),
(N'AgEvaluationSuite', N'Description', N'套件说明'),
(N'AgEvaluationSuite', N'Status', N'生命周期状态：Active 或 Archived'),
(N'AgEvaluationSuite', N'LogicalRevision', N'逻辑修订号'),
(N'AgEvaluationSuite', N'CreatedAtUtc', N'业务创建 UTC 时间'),
(N'AgEvaluationSuite', N'UpdatedAtUtc', N'业务更新 UTC 时间'),
(N'AgEvaluationSuite', N'CreatedByUserId', N'创建用户标识'),
(N'AgEvaluationSuite', N'UpdatedByUserId', N'更新用户标识'),
(N'AgEvaluationSuiteVersion', NULL, N'评测套件草稿和发布版本表'),
(N'AgEvaluationSuiteVersion', N'SuiteId', N'所属评测套件主键'),
(N'AgEvaluationSuiteVersion', N'Ordinal', N'版本排列顺序；草稿固定为 0'),
(N'AgEvaluationSuiteVersion', N'Label', N'版本标签'),
(N'AgEvaluationSuiteVersion', N'IsDraft', N'是否为草稿版本'),
(N'AgEvaluationSuiteVersion', N'ContentSha256', N'版本内容摘要'),
(N'AgEvaluationSuiteVersion', N'PublishedAtUtc', N'发布 UTC 时间'),
(N'AgEvaluationSuiteVersion', N'PublishedByUserId', N'发布用户标识'),
(N'AgEvaluationCase', NULL, N'评测套件版本用例表'),
(N'AgEvaluationCase', N'SuiteId', N'所属评测套件主键'),
(N'AgEvaluationCase', N'VersionId', N'所属套件版本主键'),
(N'AgEvaluationCase', N'Ordinal', N'用例排列顺序'),
(N'AgEvaluationCase', N'CaseId', N'契约内用例标识'),
(N'AgEvaluationCase', N'Name', N'用例名称'),
(N'AgEvaluationCase', N'Input', N'用例输入'),
(N'AgEvaluationCase', N'TargetAgentId', N'目标 Agent 主键'),
(N'AgEvaluationCase', N'TargetAgentVersionId', N'目标 Agent 发布版本主键'),
(N'AgEvaluationCase', N'ExpectedStatus', N'预期运行状态'),
(N'AgEvaluationCase', N'MaximumToolCalls', N'最大工具调用数'),
(N'AgEvaluationCase', N'MaximumDurationMilliseconds', N'最大运行毫秒数'),
(N'AgEvaluationCaseRule', NULL, N'评测用例有序规则表'),
(N'AgEvaluationCaseRule', N'SuiteId', N'所属评测套件主键'),
(N'AgEvaluationCaseRule', N'VersionId', N'所属套件版本主键'),
(N'AgEvaluationCaseRule', N'EvaluationCaseId', N'所属用例行主键'),
(N'AgEvaluationCaseRule', N'RuleType', N'规则类型'),
(N'AgEvaluationCaseRule', N'Ordinal', N'同类型规则排列顺序'),
(N'AgEvaluationCaseRule', N'Value', N'规则内容');

DECLARE @Common TABLE (ColumnName SYSNAME, Description NVARCHAR(4000));
INSERT INTO @Common VALUES
(N'ID', N'主键'), (N'IsDeleted', N'软删除标记'), (N'IsActive', N'基础启用标记'),
(N'ImportDataId', N'导入数据标识'), (N'ModificationNum', N'修改次数'), (N'Tag', N'通用标签'),
(N'GroupId', N'集团标识'), (N'CompanyId', N'公司标识'), (N'AuditStatus', N'审核状态'),
(N'CurrentNode', N'当前流程节点'), (N'CreatedBy', N'创建人'), (N'CreatedTime', N'创建时间'),
(N'UpdateBy', N'更新人'), (N'UpdateTime', N'更新时间');
INSERT INTO @Items
SELECT tables.TableName, common.ColumnName, common.Description
FROM (VALUES (N'AgEvaluationSuite'), (N'AgEvaluationSuiteVersion'),
             (N'AgEvaluationCase'), (N'AgEvaluationCaseRule')) tables(TableName)
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
