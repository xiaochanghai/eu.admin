-- Add or update Chinese descriptions for normalized Agent tables.
-- SQL Server 2014+. Run after 003/004.
-- The file is UTF-8. With sqlcmd, add: -f 65001

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.AgAgentDefinition', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgAgentVersion', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgAgentVersionSnapshot', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgAgentVersionBinding', N'U') IS NULL
    THROW 51200, N'Agent normalized tables are missing. Run 002, 003 and 004 first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    CREATE TABLE #AgentDescriptions
    (
        TableName SYSNAME NOT NULL,
        ColumnName SYSNAME NULL,
        [Description] NVARCHAR(1000) NOT NULL
    );

    INSERT INTO #AgentDescriptions (TableName, ColumnName, [Description])
    VALUES
        (N'AgAgentDefinition', NULL, N'Agent 定义主表，保存 Agent 身份、名称、说明、运行状态和逻辑版本。'),
        (N'AgAgentDefinition', N'ID', N'Agent 主键。'),
        (N'AgAgentDefinition', N'Code', N'Agent 唯一编码。'),
        (N'AgAgentDefinition', N'Name', N'Agent 显示名称。'),
        (N'AgAgentDefinition', N'Description', N'Agent 功能说明。'),
        (N'AgAgentDefinition', N'RuntimeStatus', N'运行状态：Enabled、Disabled 或 Archived。'),
        (N'AgAgentDefinition', N'LogicalRevision', N'逻辑修订号，用于乐观并发控制。'),

        (N'AgAgentVersion', NULL, N'Agent 版本表，保存当前草稿和历次已发布版本配置。'),
        (N'AgAgentVersion', N'ID', N'Agent 版本主键。'),
        (N'AgAgentVersion', N'AgentId', N'所属 Agent 主键，对应 AgAgentDefinition.ID。'),
        (N'AgAgentVersion', N'Ordinal', N'版本排列顺序；草稿固定为 0，发布版本按顺序保存。'),
        (N'AgAgentVersion', N'Label', N'版本标签，例如 1.0.0。'),
        (N'AgAgentVersion', N'IsDraft', N'是否为草稿版本；每个 Agent 只能有一个草稿。'),
        (N'AgAgentVersion', N'Instructions', N'Agent 系统指令。'),
        (N'AgAgentVersion', N'ModelProfileId', N'模型配置标识。'),
        (N'AgAgentVersion', N'OutputMode', N'输出模式：Text 或 Structured。'),
        (N'AgAgentVersion', N'OutputJsonSchema', N'结构化输出使用的 JSON Schema。'),
        (N'AgAgentVersion', N'OutputSchemaSha256', N'输出 JSON Schema 的 SHA-256 摘要。'),

        (N'AgAgentVersionSnapshot', NULL, N'Agent 发布快照表，冻结发布时的 Agent 运行配置。'),
        (N'AgAgentVersionSnapshot', N'ID', N'快照主键；当前实现与所属版本 ID 一致。'),
        (N'AgAgentVersionSnapshot', N'VersionId', N'所属 Agent 版本主键，对应 AgAgentVersion.ID。'),
        (N'AgAgentVersionSnapshot', N'SnapshotVersionId', N'快照记录的 Agent 版本标识。'),
        (N'AgAgentVersionSnapshot', N'AgentCode', N'发布时冻结的 Agent 编码。'),
        (N'AgAgentVersionSnapshot', N'AgentName', N'发布时冻结的 Agent 名称。'),
        (N'AgAgentVersionSnapshot', N'AgentDescription', N'发布时冻结的 Agent 说明。'),
        (N'AgAgentVersionSnapshot', N'Instructions', N'发布时冻结的 Agent 系统指令。'),
        (N'AgAgentVersionSnapshot', N'ModelProfileId', N'发布时冻结的模型配置标识。'),
        (N'AgAgentVersionSnapshot', N'OutputMode', N'发布时冻结的输出模式。'),
        (N'AgAgentVersionSnapshot', N'OutputJsonSchema', N'发布时冻结的结构化输出 JSON Schema。'),

        (N'AgAgentVersionBinding', NULL, N'Agent 版本资源绑定表，统一保存 Skill、MCP 工具、知识库、子 Agent 和编排绑定。'),
        (N'AgAgentVersionBinding', N'ID', N'资源绑定记录主键。'),
        (N'AgAgentVersionBinding', N'VersionId', N'所属 Agent 版本主键，对应 AgAgentVersion.ID。'),
        (N'AgAgentVersionBinding', N'Scope', N'绑定范围：Version 表示版本配置，Snapshot 表示发布快照。'),
        (N'AgAgentVersionBinding', N'BindingType', N'绑定类型：Skill、Tool、KnowledgeBase、ChildAgent 或 Orchestration。'),
        (N'AgAgentVersionBinding', N'Ordinal', N'同一版本、范围及类型下的排列顺序。'),
        (N'AgAgentVersionBinding', N'ReferenceId', N'被绑定资源的主键。'),
        (N'AgAgentVersionBinding', N'ReferenceVersionId', N'发布时固定的资源版本主键，适用于子 Agent 和编排等资源。'),
        (N'AgAgentVersionBinding', N'LogicalRevision', N'发布时固定的资源逻辑修订号，主要用于知识库。'),
        (N'AgAgentVersionBinding', N'ReferenceCode', N'发布时冻结的被绑定资源编码。'),
        (N'AgAgentVersionBinding', N'ReferenceName', N'发布时冻结的被绑定资源名称。'),
        (N'AgAgentVersionBinding', N'ReferenceDescription', N'发布时冻结的被绑定资源说明。');

    DECLARE @CommonColumns TABLE
    (
        ColumnName SYSNAME NOT NULL,
        [Description] NVARCHAR(1000) NOT NULL
    );

    INSERT INTO @CommonColumns (ColumnName, [Description])
    VALUES
        (N'IsDeleted', N'软删除标识。'),
        (N'IsActive', N'是否启用。'),
        (N'ImportDataId', N'外部导入数据标识。'),
        (N'ModificationNum', N'修改次数。'),
        (N'Tag', N'通用数据标签。'),
        (N'GroupId', N'所属集团标识。'),
        (N'CompanyId', N'所属公司标识。'),
        (N'AuditStatus', N'审核状态。'),
        (N'CurrentNode', N'当前审核节点。'),
        (N'CreatedBy', N'创建人标识。'),
        (N'CreatedTime', N'创建时间。'),
        (N'UpdateBy', N'最后修改人标识。'),
        (N'UpdateTime', N'最后修改时间。');

    INSERT INTO #AgentDescriptions (TableName, ColumnName, [Description])
    SELECT Tables.TableName, Common.ColumnName, Common.[Description]
    FROM (VALUES
        (N'AgAgentDefinition'),
        (N'AgAgentVersion'),
        (N'AgAgentVersionSnapshot'),
        (N'AgAgentVersionBinding')
    ) AS Tables(TableName)
    CROSS JOIN @CommonColumns AS Common;

    IF EXISTS
    (
        SELECT 1
        FROM #AgentDescriptions AS Item
        WHERE Item.ColumnName IS NOT NULL
          AND COL_LENGTH(N'dbo.' + Item.TableName, Item.ColumnName) IS NULL
    )
        THROW 51201, N'One or more described Agent columns are missing. Verify the migration version.', 1;

    DECLARE @TableName SYSNAME;
    DECLARE @ColumnName SYSNAME;
    DECLARE @Description NVARCHAR(1000);
    DECLARE @ObjectId INT;
    DECLARE @MinorId INT;

    DECLARE DescriptionCursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT TableName, ColumnName, [Description]
        FROM #AgentDescriptions
        ORDER BY TableName, CASE WHEN ColumnName IS NULL THEN 0 ELSE 1 END, ColumnName;

    OPEN DescriptionCursor;
    FETCH NEXT FROM DescriptionCursor INTO @TableName, @ColumnName, @Description;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @ObjectId = OBJECT_ID(N'dbo.' + @TableName);
        SET @MinorId = CASE
            WHEN @ColumnName IS NULL THEN 0
            ELSE COLUMNPROPERTY(@ObjectId, @ColumnName, N'ColumnId')
        END;

        IF EXISTS
        (
            SELECT 1
            FROM sys.extended_properties
            WHERE class = 1
              AND major_id = @ObjectId
              AND minor_id = @MinorId
              AND name = N'MS_Description'
        )
        BEGIN
            IF @ColumnName IS NULL
                EXEC sys.sp_updateextendedproperty
                    @name = N'MS_Description', @value = @Description,
                    @level0type = N'SCHEMA', @level0name = N'dbo',
                    @level1type = N'TABLE', @level1name = @TableName;
            ELSE
                EXEC sys.sp_updateextendedproperty
                    @name = N'MS_Description', @value = @Description,
                    @level0type = N'SCHEMA', @level0name = N'dbo',
                    @level1type = N'TABLE', @level1name = @TableName,
                    @level2type = N'COLUMN', @level2name = @ColumnName;
        END
        ELSE
        BEGIN
            IF @ColumnName IS NULL
                EXEC sys.sp_addextendedproperty
                    @name = N'MS_Description', @value = @Description,
                    @level0type = N'SCHEMA', @level0name = N'dbo',
                    @level1type = N'TABLE', @level1name = @TableName;
            ELSE
                EXEC sys.sp_addextendedproperty
                    @name = N'MS_Description', @value = @Description,
                    @level0type = N'SCHEMA', @level0name = N'dbo',
                    @level1type = N'TABLE', @level1name = @TableName,
                    @level2type = N'COLUMN', @level2name = @ColumnName;
        END;

        FETCH NEXT FROM DescriptionCursor INTO @TableName, @ColumnName, @Description;
    END;

    CLOSE DescriptionCursor;
    DEALLOCATE DescriptionCursor;

    COMMIT TRANSACTION;
    PRINT N'Agent table and column descriptions were updated successfully.';
END TRY
BEGIN CATCH
    IF CURSOR_STATUS(N'local', N'DescriptionCursor') >= 0
        CLOSE DescriptionCursor;
    IF CURSOR_STATUS(N'local', N'DescriptionCursor') > -3
        DEALLOCATE DescriptionCursor;
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

SELECT
    ObjectName = OBJECT_NAME(Properties.major_id),
    ColumnName = Columns.name,
    [Description] = CONVERT(NVARCHAR(1000), Properties.value)
FROM sys.extended_properties AS Properties
LEFT JOIN sys.columns AS Columns
    ON Columns.object_id = Properties.major_id
   AND Columns.column_id = Properties.minor_id
WHERE Properties.class = 1
  AND Properties.name = N'MS_Description'
  AND Properties.major_id IN
  (
      OBJECT_ID(N'dbo.AgAgentDefinition'),
      OBJECT_ID(N'dbo.AgAgentVersion'),
      OBJECT_ID(N'dbo.AgAgentVersionSnapshot'),
      OBJECT_ID(N'dbo.AgAgentVersionBinding')
  )
ORDER BY ObjectName, Properties.minor_id;
GO
