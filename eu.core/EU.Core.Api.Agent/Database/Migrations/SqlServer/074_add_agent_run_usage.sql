-- Additive, idempotent usage columns. Apply after normalized audit migrations 048-051.
-- Never backfill unknown historical usage with zero. No automatic startup migration.
SET XACT_ABORT ON;
SET NOCOUNT ON;
GO
IF OBJECT_ID(N'dbo.AgAgentRunAudit', N'U') IS NULL
   OR COL_LENGTH(N'dbo.AgAgentRunAudit', N'ID') IS NULL
   OR COL_LENGTH(N'dbo.AgAgentRunAudit', N'AgentVersionId') IS NULL
    THROW 51990, N'Normalize AgAgentRunAudit with migrations 048-051 first.', 1;
GO
BEGIN TRY
    BEGIN TRANSACTION;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'ModelProfileId') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD ModelProfileId varchar(256) NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'InputTokens') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD InputTokens bigint NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'OutputTokens') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD OutputTokens bigint NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'TotalTokens') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD TotalTokens bigint NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'TokenUsageStatus') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD TokenUsageStatus varchar(32) NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'ModelDurationMilliseconds') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD ModelDurationMilliseconds bigint NULL;
    IF COL_LENGTH(N'dbo.AgAgentRunAudit', N'TimeToFirstTextMilliseconds') IS NULL ALTER TABLE dbo.AgAgentRunAudit ADD TimeToFirstTextMilliseconds bigint NULL;
    DECLARE @Columns TABLE (Name SYSNAME, TypeName SYSNAME, MaxLength SMALLINT, Description NVARCHAR(256));
    INSERT INTO @Columns VALUES
        (N'ModelProfileId', N'varchar', 256, N'模型配置编码'),
        (N'InputTokens', N'bigint', 8, N'模型报告的输入 Token 数，未报告时为空'),
        (N'OutputTokens', N'bigint', 8, N'模型报告的输出 Token 数，未报告时为空'),
        (N'TotalTokens', N'bigint', 8, N'模型报告的总 Token 数，不自行推算'),
        (N'TokenUsageStatus', N'varchar', 32, N'Token 统计完整性：Unknown、Partial、Reported'),
        (N'ModelDurationMilliseconds', N'bigint', 8, N'模型循环耗时（毫秒），包含工具等待'),
        (N'TimeToFirstTextMilliseconds', N'bigint', 8, N'首段非空文本等待时间（毫秒）');
    IF EXISTS (
        SELECT 1 FROM @Columns expected
        LEFT JOIN sys.columns actual ON actual.object_id = OBJECT_ID(N'dbo.AgAgentRunAudit') AND actual.name = expected.Name
        LEFT JOIN sys.types types ON types.user_type_id = actual.user_type_id
        WHERE actual.column_id IS NULL OR types.name <> expected.TypeName OR actual.is_nullable <> 1 OR actual.max_length <> expected.MaxLength)
        THROW 51991, N'Existing usage column type or nullability is incompatible. No destructive conversion is performed.', 1;

    DECLARE @Name SYSNAME, @Description NVARCHAR(256);
    DECLARE usage_columns CURSOR LOCAL FAST_FORWARD FOR SELECT Name, Description FROM @Columns;
    OPEN usage_columns;
    FETCH NEXT FROM usage_columns INTO @Name, @Description;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.extended_properties WHERE major_id = OBJECT_ID(N'dbo.AgAgentRunAudit')
                   AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.AgAgentRunAudit'), @Name, 'ColumnId') AND name = N'MS_Description')
            EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'AgAgentRunAudit', @level2type=N'COLUMN', @level2name=@Name;
        ELSE
            EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=@Description,
                @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'AgAgentRunAudit', @level2type=N'COLUMN', @level2name=@Name;
        FETCH NEXT FROM usage_columns INTO @Name, @Description;
    END;
    CLOSE usage_columns;
    DEALLOCATE usage_columns;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
