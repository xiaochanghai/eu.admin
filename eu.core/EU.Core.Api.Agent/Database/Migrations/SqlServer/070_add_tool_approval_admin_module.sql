-- 新增 React 管理端审批中心模块元数据。
-- 页面调用既有 Agent API /api/tool-approvals；执行后需给目标角色分配模块权限，并清理菜单与权限缓存。

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ModuleId uniqueidentifier = '81D2393B-1B99-4A80-9DF6-8D898611BB42';
DECLARE @ModuleCode nvarchar(50) = N'AG_TOOL_APPROVAL_MNG';

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ModuleCode = @ModuleCode AND ID <> @ModuleId)
    THROW 51700, 'AG_TOOL_APPROVAL_MNG already exists with a different module ID.', 1;

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ID = @ModuleId AND ModuleCode <> @ModuleCode)
    THROW 51701, 'The Tool Approval module ID is already used by another module code.', 1;

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
        @ModuleId, @ModuleCode, N'审批中心', 950, N'SafetyCertificateOutlined',
        N'/agent/approval', 0, N'/api/tool-approvals',
        0, 0, 0, 1, 1,
        0, 0, 0, 0, 1, 0,
        N'Drawer', NULL, N'Form', 1280, N'/agent/approval/index', 0,
        0, 0, 0, 0,
        0, NULL, N'right', 0,
        0, 1, 0, 1, N'Add', SYSUTCDATETIME()
    );
END;

UPDATE dbo.SmModules
SET ModuleName = N'审批中心',
    TaxisNo = 950,
    Icon = N'SafetyCertificateOutlined',
    RoutePath = N'/agent/approval',
    IsParent = 0,
    ApiUrl = N'/api/tool-approvals',
    IsShowAdd = 0,
    IsShowBatchDelete = 0,
    IsShowDelete = 0,
    IsShowUpdate = 1,
    IsShowView = 1,
    IsExecQuery = 1,
    OpenType = N'Drawer',
    FormPage = NULL,
    ModuleType = N'Form',
    FormPageWidth = 1280,
    Element = N'/agent/approval/index',
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
    DELETE FROM dbo.SmRoleFunction WHERE SmModuleId = '81D2393B-1B99-4A80-9DF6-8D898611BB42';
    DELETE FROM dbo.SmRoleModule WHERE SmModuleId = '81D2393B-1B99-4A80-9DF6-8D898611BB42';
    DELETE FROM dbo.SmModules WHERE ID = '81D2393B-1B99-4A80-9DF6-8D898611BB42';
    COMMIT TRANSACTION;
*/
