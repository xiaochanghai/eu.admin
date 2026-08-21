-- 新增 React 管理端编排模块元数据。
-- 页面自行调用 Agent API，不依赖 SmModuleSql/SmModuleColumn 动态列表配置。
-- 执行后需给目标角色分配模块访问权限，并清理模块、菜单和权限缓存。

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ModuleId uniqueidentifier = '942F439A-9F19-4F88-A865-77E25C420690';
DECLARE @ModuleCode nvarchar(50) = N'AG_ORCHESTRATION_MNG';

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ModuleCode = @ModuleCode AND ID <> @ModuleId)
    THROW 51690, 'AG_ORCHESTRATION_MNG already exists with a different module ID.', 1;

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ID = @ModuleId AND ModuleCode <> @ModuleCode)
    THROW 51691, 'The Orchestration module ID is already used by another module code.', 1;

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
        @ModuleId, @ModuleCode, N'编排管理', 940, N'ApartmentOutlined',
        N'/agent/orchestration', 0, N'/api/orchestrations',
        1, 0, 0, 1, 1,
        0, 0, 0, 0, 0, 0,
        N'Drawer', NULL, N'Form', 1280, N'/agent/orchestration/index', 0,
        0, 0, 0, 0,
        0, NULL, N'right', 0,
        0, 1, 0, 1, N'Add', SYSUTCDATETIME()
    );
END;

UPDATE dbo.SmModules
SET ModuleName = N'编排管理',
    TaxisNo = 940,
    Icon = N'ApartmentOutlined',
    RoutePath = N'/agent/orchestration',
    IsParent = 0,
    ApiUrl = N'/api/orchestrations',
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
    Element = N'/agent/orchestration/index',
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
    DELETE FROM dbo.SmRoleFunction WHERE SmModuleId = '942F439A-9F19-4F88-A865-77E25C420690';
    DELETE FROM dbo.SmRoleModule WHERE SmModuleId = '942F439A-9F19-4F88-A865-77E25C420690';
    DELETE FROM dbo.SmModules WHERE ID = '942F439A-9F19-4F88-A865-77E25C420690';
    COMMIT TRANSACTION;
*/
