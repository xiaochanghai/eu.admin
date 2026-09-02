-- Add the React Agent operation-audit module. Review and execute this script separately;
-- it is intentionally not executed by the application or this change.
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ModuleId uniqueidentifier = 'A51EF6D1-1F28-44D6-96E3-FC93721C0963';
DECLARE @ModuleCode nvarchar(50) = N'AG_OPERATION_AUDIT_MNG';

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ModuleCode = @ModuleCode AND ID <> @ModuleId)
    THROW 51720, 'AG_OPERATION_AUDIT_MNG already exists with a different module ID.', 1;
IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ID = @ModuleId AND ModuleCode <> @ModuleCode)
    THROW 51721, 'The Agent operation audit module ID is already used by another module code.', 1;

BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.SmModules WHERE ID = @ModuleId)
BEGIN
    INSERT INTO dbo.SmModules
    (ID, ModuleCode, ModuleName, TaxisNo, Icon, RoutePath, IsParent, ApiUrl, IsShowAdd, IsShowBatchDelete, IsShowDelete, IsShowUpdate, IsShowView, IsDetail, IsShowSubmit, IsShowAudit, IsShowGoBack, IsExecQuery, IsSum, OpenType, FormPage, ModuleType, FormPageWidth, Element, IsFull, IsExportExcel, IsImportExcel, IsShowRowSelection, IsRoleDataScope, IsWorkflow, QueryApiUrl, OptionPosition, IsAllowCustomColumn, IsDeleted, IsActive, ModificationNum, Tag, AuditStatus, CreatedTime)
    VALUES
    (@ModuleId, @ModuleCode, N'操作审计', 970, N'AuditOutlined', N'/agent/audit', 0, N'/api/audit/operations', 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, N'Drawer', NULL, N'Form', 1280, N'/agent/audit/index', 0, 0, 0, 0, 0, 0, NULL, N'right', 0, 0, 1, 0, 1, N'Add', SYSUTCDATETIME());
END;

UPDATE dbo.SmModules
SET ModuleName = N'操作审计', TaxisNo = 970, Icon = N'AuditOutlined', RoutePath = N'/agent/audit', IsParent = 0, ApiUrl = N'/api/audit/operations', IsShowAdd = 0, IsShowBatchDelete = 0, IsShowDelete = 0, IsShowUpdate = 0, IsShowView = 1, IsExecQuery = 1, OpenType = N'Drawer', FormPage = NULL, ModuleType = N'Form', FormPageWidth = 1280, Element = N'/agent/audit/index', IsAllowCustomColumn = 0, IsDeleted = 0, IsActive = 1
WHERE ID = @ModuleId;
COMMIT TRANSACTION;

SELECT ID, ModuleCode, ModuleName, TaxisNo, Icon, RoutePath, Element, ApiUrl, IsActive FROM dbo.SmModules WHERE ID = @ModuleId;

/* Optional rollback (review and execute separately):
BEGIN TRANSACTION;
DELETE FROM dbo.SmRoleFunction WHERE SmModuleId = 'A51EF6D1-1F28-44D6-96E3-FC93721C0963';
DELETE FROM dbo.SmRoleModule WHERE SmModuleId = 'A51EF6D1-1F28-44D6-96E3-FC93721C0963';
DELETE FROM dbo.SmModules WHERE ID = 'A51EF6D1-1F28-44D6-96E3-FC93721C0963';
COMMIT TRANSACTION;
*/
