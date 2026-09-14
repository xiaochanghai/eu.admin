-- AgModelConfig 系统模块（SQL Server，针对已存在的业务表）。
-- 仅创建模块元数据，不变更业务表、不读取密钥、不自动分配角色权限。
-- 重复执行：同一模块已存在则退出；其他 ID 占用模块代码则报错。
-- 部署后通过系统“清理缓存”刷新模块/菜单，再为目标角色配置查询权限并重新登录。
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @ModuleId uniqueidentifier = 'c37d1814-0ba2-47df-9428-51c71f86a1f0';
DECLARE @ModuleCode varchar(64) = 'AG_MODEL_CONFIG_MNG';
DECLARE @ParentId uniqueidentifier;
SELECT @ParentId=ID FROM dbo.SmModules WHERE ModuleCode='AG_AGENT_MNG' AND IsDeleted=0;
IF @ParentId IS NULL
    THROW 51730, 'Agent parent module is missing.', 1;
IF OBJECT_ID(N'dbo.AgModelConfig', N'U') IS NULL
    THROW 51731, 'AgModelConfig table is missing.', 1;
IF EXISTS (SELECT 1 FROM dbo.SmModules WITH (UPDLOCK,HOLDLOCK) WHERE ModuleCode=@ModuleCode AND ID<>@ModuleId)
    THROW 51732, 'Model configuration module code is already in use.', 1;
IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ID=@ModuleId)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.SmModules WHERE ID=@ModuleId AND ModuleCode=@ModuleCode AND IsDeleted=0)
        THROW 51733, 'Module ID is occupied or the module is deleted.', 1;
    COMMIT TRANSACTION;
    RETURN;
END;

-- 三条查询路径均显式列出安全字段，禁止 SELECT A.*，不包含 ApiKeyCiphertext。
DECLARE @Select nvarchar(max) = N'SELECT A.ID,A.ProfileCode,A.DisplayName,A.Provider,A.Endpoint,A.ModelName,A.Enabled,A.TimeoutSeconds,A.EnableThinking,A.CredentialRevision,A.LogicalRevision,A.CreatedTime,A.UpdateTime';
DECLARE @FullSql nvarchar(max) = @Select + N' FROM AgModelConfig A WHERE A.IsDeleted=0';
-- 编译验证字段映射，不取回任何业务数据。
DECLARE @ValidateSql nvarchar(max) = @Select + N' FROM AgModelConfig A WHERE 1=0';
EXEC sys.sp_executesql @ValidateSql;

INSERT INTO dbo.SmModules
(ID,ModuleCode,ModuleName,ParentId,TaxisNo,Icon,RoutePath,IsParent,ApiUrl,
 IsShowAdd,IsShowBatchDelete,IsShowDelete,IsShowUpdate,IsShowView,
 IsDetail,IsShowSubmit,IsShowAudit,IsShowGoBack,IsExecQuery,IsSum,
 OpenType,ModuleType,Element,IsFull,IsExportExcel,IsImportExcel,
 IsShowRowSelection,IsRoleDataScope,IsWorkflow,OptionPosition,IsAllowCustomColumn,
 IsDeleted,IsActive,ModificationNum,Tag,AuditStatus,CreatedTime)
VALUES
(@ModuleId,@ModuleCode,N'模型配置',@ParentId,950,'SettingOutlined','/agent/model-config',0,'/api/SmModule',
 0,0,0,0,0,0,0,0,0,1,0,'Drawer','Form','/agent/modelConfig/index',0,0,0,
 0,0,0,'right',1,0,1,0,1,'Add',SYSUTCDATETIME());

INSERT INTO dbo.SmModuleSql
(ID,ModuleId,PrimaryTableName,TableNames,TableAliasNames,PrimaryKey,
 SqlSelect,SqlSelectBrw,SqlDefaultCondition,DefaultSortField,DefaultSortDirection,
 FullSql,Description,IsDeleted,IsActive,ModificationNum,Tag,AuditStatus,CreatedTime)
VALUES
('3d427005-756c-472e-a292-0d34f3a112c7',@ModuleId,'AgModelConfig','AgModelConfig','A','ID',
 @Select,@Select,'A.IsDeleted=0','CreatedTime','DESC',@FullSql,
 N'模型配置只读列表；不输出 API 密钥密文，不接入模型运行或保存接口。',
 0,1,0,1,'Add',SYSUTCDATETIME());

DECLARE @Columns TABLE
(Title nvarchar(32),DataIndex varchar(32),ValueType varchar(32),Width int,
 TaxisNo int,HideInSearch bit,IsBool bit);
INSERT INTO @Columns VALUES
(N'配置编码','ProfileCode',NULL,180,100,0,0),
(N'显示名称','DisplayName',NULL,180,200,0,0),
(N'协议提供商','Provider',NULL,160,300,0,0),
(N'服务地址','Endpoint',NULL,260,400,1,0),
(N'模型名称','ModelName',NULL,180,500,0,0),
(N'启用','Enabled',NULL,90,600,1,1),
(N'超时（秒）','TimeoutSeconds','digit',110,700,1,0),
(N'思考开关','EnableThinking',NULL,110,800,1,1),
(N'凭据修订号','CredentialRevision','digit',110,900,1,0),
(N'配置修订号','LogicalRevision','digit',110,1000,1,0),
(N'创建时间','CreatedTime','dateTime',170,1100,1,0),
(N'更新时间','UpdateTime','dateTime',170,1200,1,0);

INSERT INTO dbo.SmModuleColumn
(ID,SmModuleId,Title,DataIndex,ValueType,Width,HideInTable,Sorter,
 filters,filterMultiple,IsExport,TaxisNo,FormTaxisNo,IsLovCode,IsBool,
 HideInSearch,Align,TableAlias,IsSum,HideInForm,Required,Disabled,
 ColumnMode,IsDeleted,IsActive,ModificationNum,Tag,AuditStatus,CreatedTime)
SELECT NEWID(),@ModuleId,Title,DataIndex,ValueType,Width,0,1,
 0,0,0,TaxisNo,TaxisNo,0,IsBool,HideInSearch,'left','A',0,1,0,1,
 'list',0,1,0,1,'Add',SYSUTCDATETIME()
FROM @Columns;

COMMIT TRANSACTION;
SELECT ModuleCode,ModuleName,RoutePath,Element FROM dbo.SmModules WHERE ID=@ModuleId;
SELECT COUNT(*) AS ColumnCount FROM dbo.SmModuleColumn WHERE SmModuleId=@ModuleId AND IsDeleted=0;
