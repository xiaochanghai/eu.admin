-- 新增或更新规范化模型评审表及字段的中文说明。

SET NOCOUNT ON;
DECLARE @Items TABLE (TableName SYSNAME, ColumnName SYSNAME NULL, Description NVARCHAR(4000));
INSERT INTO @Items VALUES
(N'AgEvaluationModelJudgement', NULL, N'评估批次模型评审报告主表'),
(N'AgEvaluationModelJudgement', N'TenantId', N'租户标识'),
(N'AgEvaluationModelJudgement', N'RequestedByUserId', N'发起模型评审的用户标识'),
(N'AgEvaluationModelJudgement', N'BatchId', N'所属评估批次标识'),
(N'AgEvaluationModelJudgement', N'SuiteId', N'评估套件标识'),
(N'AgEvaluationModelJudgement', N'SuiteVersionId', N'已发布评估套件版本标识'),
(N'AgEvaluationModelJudgement', N'SuiteVersionContentSha256', N'已发布评估套件版本内容的 SHA-256 摘要'),
(N'AgEvaluationModelJudgement', N'Provider', N'模型评审引擎提供方'),
(N'AgEvaluationModelJudgement', N'PackageVersion', N'模型评审组件包版本'),
(N'AgEvaluationModelJudgement', N'ModelProfileId', N'执行评审使用的模型配置标识'),
(N'AgEvaluationModelJudgement', N'ConfigurationSha256', N'模型评审配置的 SHA-256 摘要，用于防止相同配置重复评审'),
(N'AgEvaluationModelJudgement', N'PromptVersion', N'模型评审提示词版本'),
(N'AgEvaluationModelJudgement', N'StartedAtUtc', N'模型评审开始时间（UTC）'),
(N'AgEvaluationModelJudgement', N'FinishedAtUtc', N'模型评审结束时间（UTC）'),
(N'AgEvaluationModelJudgement', N'AdvisoryPassed', N'模型评审建议结果是否通过'),
(N'AgEvaluationModelJudgementEvaluator', NULL, N'模型评审报告使用的有序评估器表'),
(N'AgEvaluationModelJudgementEvaluator', N'JudgementId', N'所属模型评审报告标识'),
(N'AgEvaluationModelJudgementEvaluator', N'Ordinal', N'评估器排列顺序'),
(N'AgEvaluationModelJudgementEvaluator', N'Name', N'评估器名称'),
(N'AgEvaluationModelJudgementMinimumScore', NULL, N'模型评审指标最低分配置表'),
(N'AgEvaluationModelJudgementMinimumScore', N'JudgementId', N'所属模型评审报告标识'),
(N'AgEvaluationModelJudgementMinimumScore', N'Ordinal', N'最低分配置排列顺序'),
(N'AgEvaluationModelJudgementMinimumScore', N'Name', N'评估指标名称'),
(N'AgEvaluationModelJudgementMinimumScore', N'Score', N'评估指标最低通过分数'),
(N'AgEvaluationModelJudgementCase', NULL, N'模型评审用例结果表'),
(N'AgEvaluationModelJudgementCase', N'JudgementId', N'所属模型评审报告标识'),
(N'AgEvaluationModelJudgementCase', N'Ordinal', N'用例排列顺序'),
(N'AgEvaluationModelJudgementCase', N'CaseId', N'评估套件中的用例标识'),
(N'AgEvaluationModelJudgementCase', N'CaseName', N'执行时记录的用例名称'),
(N'AgEvaluationModelJudgementCase', N'UnifiedRunId', N'关联的统一运行标识'),
(N'AgEvaluationModelJudgementCase', N'InputSha256', N'用例输入内容的 SHA-256 摘要'),
(N'AgEvaluationModelJudgementCase', N'OutputSha256', N'用例输出内容的 SHA-256 摘要'),
(N'AgEvaluationModelJudgementMetric', NULL, N'模型评审用例指标结果表'),
(N'AgEvaluationModelJudgementMetric', N'JudgementId', N'所属模型评审报告标识'),
(N'AgEvaluationModelJudgementMetric', N'JudgementCaseId', N'所属模型评审用例记录标识'),
(N'AgEvaluationModelJudgementMetric', N'Ordinal', N'指标排列顺序'),
(N'AgEvaluationModelJudgementMetric', N'Name', N'评估指标名称'),
(N'AgEvaluationModelJudgementMetric', N'Score', N'模型评审实际得分'),
(N'AgEvaluationModelJudgementMetric', N'MinimumScore', N'指标最低通过分数'),
(N'AgEvaluationModelJudgementMetric', N'Passed', N'指标是否通过'),
(N'AgEvaluationModelJudgementDiagnostic', NULL, N'模型评审指标诊断码表'),
(N'AgEvaluationModelJudgementDiagnostic', N'JudgementId', N'所属模型评审报告标识'),
(N'AgEvaluationModelJudgementDiagnostic', N'JudgementMetricId', N'所属模型评审指标记录标识'),
(N'AgEvaluationModelJudgementDiagnostic', N'Ordinal', N'诊断码排列顺序'),
(N'AgEvaluationModelJudgementDiagnostic', N'Code', N'模型评审诊断码');

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
FROM (VALUES (N'AgEvaluationModelJudgement'), (N'AgEvaluationModelJudgementEvaluator'),
             (N'AgEvaluationModelJudgementMinimumScore'), (N'AgEvaluationModelJudgementCase'),
             (N'AgEvaluationModelJudgementMetric'), (N'AgEvaluationModelJudgementDiagnostic')) tables(TableName)
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
