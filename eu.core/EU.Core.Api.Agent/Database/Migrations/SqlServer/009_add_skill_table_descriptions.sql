-- Add or update Chinese descriptions for normalized Skill tables.
-- SQL Server 2014+. Run after 006, 007 and Data/008.
-- The file is UTF-8. With sqlcmd, add: -f 65001

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.AgSkillDefinition', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgSkillVersion', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AgSkillVersionFile', N'U') IS NULL
    THROW 51210, N'Skill normalized tables are missing. Run 006, 007 and Data/008 first.', 1;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    CREATE TABLE #SkillDescriptions
    (
        TableName SYSNAME NOT NULL,
        ColumnName SYSNAME NULL,
        [Description] NVARCHAR(1000) NOT NULL
    );

    INSERT INTO #SkillDescriptions (TableName, ColumnName, [Description])
    VALUES
        (N'AgSkillDefinition', NULL, N'Skill 定义主表，保存 Skill 身份、名称、分类、状态和草稿修订号。'),
        (N'AgSkillDefinition', N'ID', N'Skill 主键。'),
        (N'AgSkillDefinition', N'Code', N'Skill 唯一编码。'),
        (N'AgSkillDefinition', N'DraftRevision', N'草稿修订号，用于乐观并发控制。'),
        (N'AgSkillDefinition', N'Name', N'Skill 显示名称。'),
        (N'AgSkillDefinition', N'Description', N'Skill 功能说明。'),
        (N'AgSkillDefinition', N'Category', N'Skill 分类。'),
        (N'AgSkillDefinition', N'Status', N'Skill 状态：Active 或 Archived。'),

        (N'AgSkillVersion', NULL, N'Skill 发布版本表，保存版本标识、文件清单摘要和发布时间。'),
        (N'AgSkillVersion', N'ID', N'Skill 发布版本主键。'),
        (N'AgSkillVersion', N'SkillId', N'所属 Skill 主键，对应 AgSkillDefinition.ID。'),
        (N'AgSkillVersion', N'Ordinal', N'发布版本排列顺序，从 0 开始。'),
        (N'AgSkillVersion', N'Label', N'严格 SemVer 版本标签，例如 1.0.0。'),
        (N'AgSkillVersion', N'ManifestSha256', N'发布文件清单的 SHA-256 摘要。'),
        (N'AgSkillVersion', N'PublishedAtUtc', N'UTC 发布时间。'),

        (N'AgSkillVersionFile', NULL, N'Skill 发布版本文件表，保存不可变文件清单。'),
        (N'AgSkillVersionFile', N'ID', N'Skill 发布版本文件主键。'),
        (N'AgSkillVersionFile', N'VersionId', N'所属 Skill 发布版本主键，对应 AgSkillVersion.ID。'),
        (N'AgSkillVersionFile', N'Ordinal', N'文件排列顺序，从 0 开始。'),
        (N'AgSkillVersionFile', N'Path', N'Skill 内相对文件路径。'),
        (N'AgSkillVersionFile', N'Size', N'文件字节数。'),
        (N'AgSkillVersionFile', N'Sha256', N'文件内容的 SHA-256 摘要。');

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

    INSERT INTO #SkillDescriptions (TableName, ColumnName, [Description])
    SELECT Tables.TableName, Common.ColumnName, Common.[Description]
    FROM (VALUES
        (N'AgSkillDefinition'),
        (N'AgSkillVersion'),
        (N'AgSkillVersionFile')
    ) AS Tables(TableName)
    CROSS JOIN @CommonColumns AS Common;

    IF EXISTS
    (
        SELECT 1
        FROM #SkillDescriptions AS Item
        WHERE Item.ColumnName IS NOT NULL
          AND COL_LENGTH(N'dbo.' + Item.TableName, Item.ColumnName) IS NULL
    )
        THROW 51211, N'One or more described Skill columns are missing. Verify the migration version.', 1;

    DECLARE @TableName SYSNAME;
    DECLARE @ColumnName SYSNAME;
    DECLARE @Description NVARCHAR(1000);
    DECLARE @ObjectId INT;
    DECLARE @MinorId INT;

    DECLARE DescriptionCursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT TableName, ColumnName, [Description]
        FROM #SkillDescriptions
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
    PRINT N'Skill table and column descriptions were updated successfully.';
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
      OBJECT_ID(N'dbo.AgSkillDefinition'),
      OBJECT_ID(N'dbo.AgSkillVersion'),
      OBJECT_ID(N'dbo.AgSkillVersionFile')
  )
ORDER BY ObjectName, Properties.minor_id;
GO
