-- 新增 React 评测中心模块元数据。
-- 本脚本只供部署人员审查和执行；执行后须向目标角色分配模块权限并刷新菜单、权限缓存。
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ModuleId uniqueidentifier = 'E61539C1-3D1F-4F17-ABF6-34C2A2F19A95';
DECLARE @ModuleCode nvarchar(50) = N'AG_EVALUATION_MNG';

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ModuleCode = @ModuleCode AND ID <> @ModuleId)
    THROW 51710, 'AG_EVALUATION_MNG already exists with a different module ID.', 1;

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ID = @ModuleId AND ModuleCode <> @ModuleCode)
    THROW 51711, 'The Evaluation Center module ID is already used by another module code.', 1;

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
        @ModuleId, @ModuleCode, N'评测中心', 960, N'ExperimentOutlined',
        N'/agent/evaluation', 0, N'/api/evaluation-suites',
        1, 0, 0, 1, 1,
        0, 0, 0, 0, 1, 0,
        N'Drawer', NULL, N'Form', 1280, N'/agent/evaluation/index', 0,
        0, 0, 0, 0,
        0, NULL, N'right', 0,
        0, 1, 0, 1, N'Add', SYSUTCDATETIME()
    );
END;

UPDATE dbo.SmModules
SET ModuleName = N'评测中心',
    TaxisNo = 960,
    Icon = N'ExperimentOutlined',
    RoutePath = N'/agent/evaluation',
    IsParent = 0,
    ApiUrl = N'/api/evaluation-suites',
    IsShowAdd = 1,
    IsShowBatchDelete = 0,
    IsShowDelete = 0,
    IsShowUpdate = 1,
    IsShowView = 1,
    IsExecQuery = 1,
    OpenType = N'Drawer',
    FormPage = NULL,
    ModuleType = N'Form',
    FormPageWidth = 1280,
    Element = N'/agent/evaluation/index',
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
    DELETE FROM dbo.SmRoleFunction WHERE SmModuleId = 'E61539C1-3D1F-4F17-ABF6-34C2A2F19A95';
    DELETE FROM dbo.SmRoleModule WHERE SmModuleId = 'E61539C1-3D1F-4F17-ABF6-34C2A2F19A95';
    DELETE FROM dbo.SmModules WHERE ID = 'E61539C1-3D1F-4F17-ABF6-34C2A2F19A95';
    COMMIT TRANSACTION;
*/
