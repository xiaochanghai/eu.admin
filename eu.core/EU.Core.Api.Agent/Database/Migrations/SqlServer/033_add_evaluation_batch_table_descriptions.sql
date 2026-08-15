-- 新增或更新规范化评估批次表及字段的中文说明。

SET NOCOUNT ON;
DECLARE @Items TABLE (TableName SYSNAME, ColumnName SYSNAME NULL, Description NVARCHAR(4000));
INSERT INTO @Items VALUES
(N'AgEvaluationBatch', NULL, N'评估批次执行汇总表'),
(N'AgEvaluationBatch', N'TenantId', N'租户标识'),
(N'AgEvaluationBatch', N'RequestedByUserId', N'发起评估批次的用户标识'),
(N'AgEvaluationBatch', N'SuiteId', N'评估套件标识'),
(N'AgEvaluationBatch', N'SuiteVersionId', N'已发布评估套件版本标识'),
(N'AgEvaluationBatch', N'SuiteVersionContentSha256', N'已发布评估套件版本内容的 SHA-256 摘要'),
(N'AgEvaluationBatch', N'Status', N'评估批次执行状态'),
(N'AgEvaluationBatch', N'LogicalRevision', N'用于乐观并发控制的逻辑修订号'),
(N'AgEvaluationBatch', N'StartedAtUtc', N'评估批次开始时间（UTC）'),
(N'AgEvaluationBatch', N'FinishedAtUtc', N'评估批次结束时间（UTC）'),
(N'AgEvaluationBatch', N'ErrorCode', N'评估批次级错误码'),
(N'AgEvaluationBatchCase', NULL, N'评估批次用例执行结果及评估报告汇总表'),
(N'AgEvaluationBatchCase', N'BatchId', N'所属评估批次标识'),
(N'AgEvaluationBatchCase', N'Ordinal', N'用例执行顺序'),
(N'AgEvaluationBatchCase', N'CaseId', N'评估套件中的用例标识'),
(N'AgEvaluationBatchCase', N'CaseName', N'执行时记录的用例显示名称'),
(N'AgEvaluationBatchCase', N'TargetAgentId', N'目标 Agent 标识'),
(N'AgEvaluationBatchCase', N'TargetAgentVersionId', N'目标 Agent 已发布版本标识'),
(N'AgEvaluationBatchCase', N'Status', N'用例执行状态'),
(N'AgEvaluationBatchCase', N'UnifiedRunId', N'关联的统一运行标识'),
(N'AgEvaluationBatchCase', N'UnifiedRunStatus', N'统一运行的实际状态'),
(N'AgEvaluationBatchCase', N'ErrorCode', N'用例级错误码'),
(N'AgEvaluationBatchCase', N'DurationMilliseconds', N'实际运行耗时（毫秒）'),
(N'AgEvaluationBatchCase', N'ToolCallCount', N'实际工具调用次数'),
(N'AgEvaluationBatchCase', N'ReportEvaluatedAtUtc', N'断言报告评估时间（UTC）'),
(N'AgEvaluationBatchCase', N'ReportPassed', N'断言报告是否通过'),
(N'AgEvaluationBatchCase', N'ReportScore', N'断言报告得分'),
(N'AgEvaluationBatchCase', N'OutputSha256', N'运行输出内容的 SHA-256 摘要'),
(N'AgEvaluationBatchCase', N'OutputUtf8Bytes', N'运行输出内容的 UTF-8 字节数'),
(N'AgEvaluationBatchCheck', NULL, N'用例评估报告的有序断言检查项表'),
(N'AgEvaluationBatchCheck', N'BatchId', N'所属评估批次标识'),
(N'AgEvaluationBatchCheck', N'BatchCaseId', N'所属评估批次用例记录标识'),
(N'AgEvaluationBatchCheck', N'Ordinal', N'检查项在报告中的顺序'),
(N'AgEvaluationBatchCheck', N'Code', N'检查项类型编码'),
(N'AgEvaluationBatchCheck', N'Passed', N'检查项是否通过'),
(N'AgEvaluationBatchCheck', N'Expected', N'检查项的预期值或预期条件'),
(N'AgEvaluationBatchCheck', N'Actual', N'检查项的实际值或实际结果'),
(N'AgEvaluationBatchObservation', NULL, N'用例运行期间记录的有序事件类型及路由观测表'),
(N'AgEvaluationBatchObservation', N'BatchId', N'所属评估批次标识'),
(N'AgEvaluationBatchObservation', N'BatchCaseId', N'所属评估批次用例记录标识'),
(N'AgEvaluationBatchObservation', N'ObservationType', N'观测类型：EventKind（事件类型）或 Route（路由）'),
(N'AgEvaluationBatchObservation', N'Ordinal', N'同类观测记录的排列顺序'),
(N'AgEvaluationBatchObservation', N'Value', N'观测到的事件类型或路由值'),
(N'AgEvaluationModelJudgement', NULL, N'评估批次模型评审报告表'),
(N'AgEvaluationModelJudgement', N'Id', N'模型评审报告标识'),
(N'AgEvaluationModelJudgement', N'TenantId', N'租户标识'),
(N'AgEvaluationModelJudgement', N'BatchId', N'所属评估批次标识'),
(N'AgEvaluationModelJudgement', N'ConfigurationSha256', N'模型评审配置的 SHA-256 摘要，用于防止相同配置重复评审'),
(N'AgEvaluationModelJudgement', N'StartedAtUtc', N'模型评审开始时间（UTC）'),
(N'AgEvaluationModelJudgement', N'DocumentJson', N'模型评审报告完整 JSON 数据');

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
FROM (VALUES (N'AgEvaluationBatch'), (N'AgEvaluationBatchCase'),
             (N'AgEvaluationBatchCheck'), (N'AgEvaluationBatchObservation')) tables(TableName)
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
