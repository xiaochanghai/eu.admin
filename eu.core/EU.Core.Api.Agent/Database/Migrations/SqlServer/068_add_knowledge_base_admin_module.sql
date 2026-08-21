-- 新增 React 管理端知识库模块元数据。
-- 页面自行调用 Agent API，不依赖 SmModuleSql/SmModuleColumn 动态列表配置。
-- 执行后需给目标角色分配模块访问权限，并清理模块、菜单和权限缓存。

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ModuleId uniqueidentifier = '4B185E42-EE03-4BD8-A3B5-2E4C5D610680';
DECLARE @ModuleCode nvarchar(50) = N'AG_KNOWLEDGE_BASE_MNG';

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ModuleCode = @ModuleCode AND ID <> @ModuleId)
    THROW 51680, 'AG_KNOWLEDGE_BASE_MNG already exists with a different module ID.', 1;

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ID = @ModuleId AND ModuleCode <> @ModuleCode)
    THROW 51681, 'The Knowledge Base module ID is already used by another module code.', 1;

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
        @ModuleId, @ModuleCode, N'知识库管理', 930, N'BookOutlined',
        N'/agent/knowledge', 0, N'/api/knowledge-bases',
        1, 0, 0, 1, 1,
        0, 0, 0, 0, 0, 0,
        N'Drawer', NULL, N'Form', 1280, N'/agent/knowledge/index', 0,
        0, 0, 0, 0,
        0, NULL, N'right', 0,
        0, 1, 0, 1, N'Add', SYSUTCDATETIME()
    );
END;

UPDATE dbo.SmModules
SET ModuleName = N'知识库管理',
    TaxisNo = 930,
    Icon = N'BookOutlined',
    RoutePath = N'/agent/knowledge',
    IsParent = 0,
    ApiUrl = N'/api/knowledge-bases',
    IsShowAdd = 1,
    IsShowBatchDelete = 0,
    IsShowDelete = 0,
    IsShowUpdate = 1,
    IsShowView = 1,
    IsExecQuery = 0,
    OpenType = N'Drawer',
    FormPage = NULL,
    ModuleType = N'Form',
    FormPageWidth = 1280,
    Element = N'/agent/knowledge/index',
    IsAllowCustomColumn = 0,
    IsDeleted = 0,
    IsActive = 1
WHERE ID = @ModuleId;

COMMIT TRANSACTION;

SELECT ID, ModuleCode, ModuleName, TaxisNo, Icon, RoutePath, Element, ApiUrl, IsActive
FROM dbo.SmModules
WHERE ID = @ModuleId;

/*
    Optional rollback (review and execute separately; this script does not run it):
    BEGIN TRANSACTION;
    DELETE FROM dbo.SmRoleFunction WHERE SmModuleId = '4B185E42-EE03-4BD8-A3B5-2E4C5D610680';
    DELETE FROM dbo.SmRoleModule WHERE SmModuleId = '4B185E42-EE03-4BD8-A3B5-2E4C5D610680';
    DELETE FROM dbo.SmModules WHERE ID = '4B185E42-EE03-4BD8-A3B5-2E4C5D610680';
    COMMIT TRANSACTION;
*/
