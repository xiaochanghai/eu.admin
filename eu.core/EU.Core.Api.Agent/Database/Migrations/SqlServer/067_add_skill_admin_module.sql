-- 新增 React 管理端 Skill 模块元数据。
-- 列表由 SmModules/SmModuleSql/SmModuleColumn 驱动，编辑、文件和生命周期操作由 Agent API 完成。
-- 执行后需给目标角色分配 Query/Add/Update/View 权限，并清理模块、菜单和权限缓存。

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ModuleId uniqueidentifier = '6354B334-06D7-43A8-874C-04A19E2449E7';
DECLARE @ModuleCode nvarchar(50) = N'AG_SKILL_MNG';
DECLARE @ModuleSqlId uniqueidentifier = 'A90C0211-7FD4-498A-A435-C5060FB1E52E';

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ModuleCode = @ModuleCode AND ID <> @ModuleId)
    THROW 51670, 'AG_SKILL_MNG already exists with a different module ID.', 1;

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ID = @ModuleId AND ModuleCode <> @ModuleCode)
    THROW 51671, 'The Skill module ID is already used by another module code.', 1;

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.SmModules WHERE ID = @ModuleId)
BEGIN
    INSERT INTO dbo.SmModules
    (
        ID, ModuleCode, ModuleName, TaxisNo, Icon, RoutePath, IsParent, ApiUrl,
        IsShowAdd, IsShowBatchDelete, IsShowDelete, IsShowUpdate, IsShowView,
        IsDetail, IsShowSubmit, IsShowAudit, IsShowGoBack, IsExecQuery, IsSum,
        OpenType, FormPage, ModuleType, FormPageWidth, Element, IsFull,
        IsExportExcel, IsImportExcel, IsShowRowSelection, IsRoleDataScope,
        IsWorkflow, QueryApiUrl, OptionPosition, IsAllowCustomColumn,
        IsDeleted, IsActive, ModificationNum, Tag, AuditStatus, CreatedTime
    )
    VALUES
    (
        @ModuleId, @ModuleCode, N'Skill 管理', 920, N'ToolOutlined',
        N'/agent/skills', 0, N'/api/skills',
        1, 0, 0, 1, 1,
        0, 0, 0, 0, 1, 0,
        N'Drawer', N'/agent/skill/FormPage', N'Form', 1120, N'/agent/skill/index', 0,
        0, 0, 0, 0,
        0, NULL, N'left', 1,
        0, 1, 0, 1, N'Add', SYSUTCDATETIME()
    );
END;

UPDATE dbo.SmModules
SET ModuleName = N'Skill 管理',
    TaxisNo = 920,
    Icon = N'ToolOutlined',
    RoutePath = N'/agent/skills',
    ApiUrl = N'/api/skills',
    IsShowAdd = 1,
    IsShowDelete = 0,
    IsShowUpdate = 1,
    IsShowView = 1,
    IsExecQuery = 1,
    OpenType = N'Drawer',
    FormPage = N'/agent/skill/FormPage',
    ModuleType = N'Form',
    FormPageWidth = 1120,
    Element = N'/agent/skill/index',
    IsAllowCustomColumn = 1,
    IsDeleted = 0,
    IsActive = 1
WHERE ID = @ModuleId;

IF NOT EXISTS (SELECT 1 FROM dbo.SmModuleSql WHERE ModuleId = @ModuleId AND IsDeleted = 0)
BEGIN
    INSERT INTO dbo.SmModuleSql
    (
        ID, ModuleId, PrimaryTableName, TableNames, TableAliasNames, PrimaryKey,
        SqlSelect, SqlSelectBrw, DefaultSortField, DefaultSortDirection,
        FullSql, Description, IsDeleted, IsActive, ModificationNum, Tag,
        AuditStatus, CreatedTime
    )
    VALUES
    (
        @ModuleSqlId, @ModuleId, N'AgSkillDefinition', N'AgSkillDefinition', N'A', N'ID',
        N'SELECT A.*', N'SELECT A.*', N'CreatedTime', N'DESC',
        N'SELECT A.ID,A.Code,A.Name,A.Description,A.Category,A.Status,A.DraftRevision,(SELECT TOP (1) V.Label FROM dbo.AgSkillVersion V WHERE V.SkillId=A.ID AND V.IsDeleted=0 ORDER BY V.Ordinal DESC,V.ID DESC) AS CurrentPublishedLabel,(SELECT COUNT(1) FROM dbo.AgSkillVersion V WHERE V.SkillId=A.ID AND V.IsDeleted=0) AS PublishedVersionCount,A.CreatedTime,A.UpdateTime FROM dbo.AgSkillDefinition A WHERE A.IsDeleted=0',
        N'Skill 通用管理列表；基础信息、Draft 文件、发布和归档操作由 Agent API 完成。',
        0, 1, 0, 1, N'Add', SYSUTCDATETIME()
    );
END;

UPDATE dbo.SmModuleSql
SET PrimaryTableName = N'AgSkillDefinition',
    TableNames = N'AgSkillDefinition',
    TableAliasNames = N'A',
    PrimaryKey = N'ID',
    DefaultSortField = N'CreatedTime',
    DefaultSortDirection = N'DESC',
    FullSql = N'SELECT A.ID,A.Code,A.Name,A.Description,A.Category,A.Status,A.DraftRevision,(SELECT TOP (1) V.Label FROM dbo.AgSkillVersion V WHERE V.SkillId=A.ID AND V.IsDeleted=0 ORDER BY V.Ordinal DESC,V.ID DESC) AS CurrentPublishedLabel,(SELECT COUNT(1) FROM dbo.AgSkillVersion V WHERE V.SkillId=A.ID AND V.IsDeleted=0) AS PublishedVersionCount,A.CreatedTime,A.UpdateTime FROM dbo.AgSkillDefinition A WHERE A.IsDeleted=0',
    Description = N'Skill 通用管理列表；基础信息、Draft 文件、发布和归档操作由 Agent API 完成。',
    IsDeleted = 0,
    IsActive = 1
WHERE ModuleId = @ModuleId AND IsDeleted = 0;

DECLARE @Columns TABLE
(
    ID uniqueidentifier NOT NULL,
    Title nvarchar(32) NOT NULL,
    DataIndex nvarchar(32) NOT NULL,
    ValueType nvarchar(32) NULL,
    Width decimal(20,2) NULL,
    TaxisNo int NOT NULL,
    HideInSearch bit NOT NULL,
    Sorter bit NOT NULL
);

INSERT INTO @Columns (ID, Title, DataIndex, ValueType, Width, TaxisNo, HideInSearch, Sorter)
VALUES
('23779053-BD87-49D4-8980-2ECEE9E0DC70', N'Skill Code', N'Code', NULL, 180, 100, 0, 1),
('EA65CBDB-C282-4588-A1CE-07AC547EB2BD', N'名称', N'Name', NULL, 180, 200, 0, 1),
('3E1D96FD-602D-4E6C-ABBD-D661414C997E', N'说明', N'Description', NULL, 260, 300, 0, 0),
('7BB5A75A-146B-41F3-9575-32275E999562', N'分类', N'Category', NULL, 140, 400, 0, 1),
('03A34D58-5B5C-4F54-B312-243921C5444B', N'状态', N'Status', NULL, 100, 500, 0, 1),
('CB13F834-74E7-4F8F-B5BD-BDB6203DE57B', N'Draft REV', N'DraftRevision', N'digit', 100, 600, 1, 1),
('C6D5EA8F-7634-41FA-B808-F80F3EAA9C30', N'当前版本', N'CurrentPublishedLabel', NULL, 110, 700, 1, 0),
('752AE3EF-81F7-4C51-AFCE-44700D46F27E', N'版本数', N'PublishedVersionCount', N'digit', 90, 800, 1, 0),
('CEB29A68-02F0-47D9-BCA7-147CE28D8694', N'更新时间', N'UpdateTime', N'dateTime', 170, 900, 1, 1);

INSERT INTO dbo.SmModuleColumn
(
    ID, SmModuleId, Title, DataIndex, ValueType, Width, HideInTable, Sorter,
    filters, filterMultiple, IsExport, TaxisNo, IsLovCode, IsBool,
    HideInSearch, Align, TableAlias, IsSum, HideInForm, Required, Disabled,
    ColumnMode, IsDeleted, IsActive, ModificationNum, Tag, AuditStatus, CreatedTime
)
SELECT
    source.ID, @ModuleId, source.Title, source.DataIndex, source.ValueType,
    source.Width, 0, source.Sorter,
    0, 0, 0, source.TaxisNo, 0, 0,
    source.HideInSearch, N'left', N'A', 0, 1, 0, 0,
    N'list', 0, 1, 0, 1, N'Add', SYSUTCDATETIME()
FROM @Columns source
WHERE NOT EXISTS
(
    SELECT 1 FROM dbo.SmModuleColumn target
    WHERE target.SmModuleId = @ModuleId
      AND target.DataIndex = source.DataIndex
      AND target.IsDeleted = 0
);

COMMIT TRANSACTION;

SELECT ID, ModuleCode, ModuleName, RoutePath, Element, FormPage
FROM dbo.SmModules
WHERE ID = @ModuleId;

SELECT DataIndex, Title, TaxisNo, HideInSearch
FROM dbo.SmModuleColumn
WHERE SmModuleId = @ModuleId AND IsDeleted = 0
ORDER BY TaxisNo;

/*
    Optional rollback (review and execute separately; this script does not run it):
    DELETE FROM dbo.SmRoleFunction WHERE SmModuleId = '6354B334-06D7-43A8-874C-04A19E2449E7';
    DELETE FROM dbo.SmRoleModule WHERE SmModuleId = '6354B334-06D7-43A8-874C-04A19E2449E7';
    DELETE FROM dbo.SmModuleColumn WHERE SmModuleId = '6354B334-06D7-43A8-874C-04A19E2449E7';
    DELETE FROM dbo.SmModuleSql WHERE ModuleId = '6354B334-06D7-43A8-874C-04A19E2449E7';
    DELETE FROM dbo.SmModules WHERE ID = '6354B334-06D7-43A8-874C-04A19E2449E7';
*/
