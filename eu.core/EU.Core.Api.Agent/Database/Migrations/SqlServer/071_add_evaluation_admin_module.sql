-- React 评测中心模块；执行前请审核并为角色分配访问权限。本脚本不由应用自动执行。
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @ModuleId uniqueidentifier = 'E61539C1-3D1F-4F17-ABF6-34C2A2F19A95';
DECLARE @ModuleCode nvarchar(50) = N'AG_EVALUATION_MNG';
IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ModuleCode=@ModuleCode AND ID<>@ModuleId) THROW 51710, 'AG_EVALUATION_MNG conflict.', 1;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.SmModules WHERE ID=@ModuleId)
INSERT INTO dbo.SmModules (ID,ModuleCode,ModuleName,TaxisNo,Icon,RoutePath,IsParent,ApiUrl,IsShowAdd,IsShowBatchDelete,IsShowDelete,IsShowUpdate,IsShowView,IsDetail,IsShowSubmit,IsShowAudit,IsShowGoBack,IsExecQuery,IsSum,OpenType,ModuleType,FormPageWidth,Element,IsFull,IsExportExcel,IsImportExcel,IsShowRowSelection,IsRoleDataScope,IsWorkflow,OptionPosition,IsAllowCustomColumn,IsDeleted,IsActive,ModificationNum,Tag,AuditStatus,CreatedTime)
VALUES (@ModuleId,@ModuleCode,N'评测中心',960,N'ExperimentOutlined',N'/agent/evaluation',0,N'/api/evaluation-suites',1,0,0,1,1,0,0,0,0,1,0,N'Drawer',N'Form',1280,N'/agent/evaluation/index',0,0,0,0,0,0,N'right',0,0,1,0,1,N'Add',SYSUTCDATETIME());
UPDATE dbo.SmModules SET ModuleName=N'评测中心',RoutePath=N'/agent/evaluation',Element=N'/agent/evaluation/index',ApiUrl=N'/api/evaluation-suites',IsActive=1,IsDeleted=0 WHERE ID=@ModuleId;
COMMIT TRANSACTION;
