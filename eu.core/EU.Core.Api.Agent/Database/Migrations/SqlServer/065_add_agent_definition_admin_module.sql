-- 新增 React 管理端 Agent Definition 模块元数据。
-- 列表由 SmModule/SmModuleSql/SmModuleColumn 驱动，编辑操作由 React 自定义页面调用 Agent API。
-- 执行后需在“模块管理”中给目标角色分配 Query/Add/Update/View 权限，并清理模块权限缓存。

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ModuleId UNIQUEIDENTIFIER = '7EB69473-CB1A-4D62-A182-041213BAA53E';
DECLARE @ModuleSqlId UNIQUEIDENTIFIER = '495C513A-CE11-47F3-8BEF-3899528480D1';
DECLARE @ModuleCode VARCHAR(64) = 'AG_AGENT_DEFINITION_MNG';

IF EXISTS (SELECT 1 FROM dbo.SmModules WHERE ModuleCode = @ModuleCode AND ID <> @ModuleId)
    THROW 51650, 'ModuleCode AG_AGENT_DEFINITION_MNG is already used by another module.', 1;

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
        @ModuleId, @ModuleCode, 'Agent Definition', 900, 'RobotOutlined',
        '/agent/agent-definition', 0, '/api/agents',
        1, 0, 0, 1, 1,
        0, 0, 0, 0, 1, 0,
        'Drawer', '/agent/agentDefinition/FormPage', 'Form', 1280,
        '/agent/agentDefinition/index', 0,
        0, 0, 0, 0,
        0, NULL, 'right', 1,
        0, 1, 0, 1, 'Add', SYSUTCDATETIME()
    );
END;

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
        @ModuleSqlId, @ModuleId, 'AgAgentDefinition', 'AgAgentDefinition', 'A', 'ID',
        'SELECT A.*', 'SELECT A.*', 'CreatedTime', 'DESC',
        'SELECT A.ID,A.Code,A.Name,A.Description,A.RuntimeStatus,A.LogicalRevision,A.CreatedTime,A.UpdateTime,A.Code AS DELETE_CONFIRM_MSG,(SELECT TOP (1) V.Label FROM dbo.AgAgentVersion V WHERE V.AgentId=A.ID AND V.IsDraft=0 AND V.IsDeleted=0 ORDER BY V.Ordinal DESC,V.ID DESC) AS CurrentPublishedLabel FROM dbo.AgAgentDefinition A WHERE A.IsDeleted=0',
        'Agent Definition 管理列表，发布与生命周期操作由 Agent API 完成。',
        0, 1, 0, 1, 'Add', SYSUTCDATETIME()
    );
END;

DECLARE @Columns TABLE
(
    ID UNIQUEIDENTIFIER NOT NULL,
    Title VARCHAR(32) NOT NULL,
    DataIndex VARCHAR(32) NOT NULL,
    ValueType VARCHAR(32) NULL,
    Width DECIMAL(20,2) NULL,
    TaxisNo INT NOT NULL,
    HideInSearch BIT NOT NULL,
    Sorter BIT NOT NULL
);

INSERT INTO @Columns (ID, Title, DataIndex, ValueType, Width, TaxisNo, HideInSearch, Sorter)
VALUES
('FC457C2F-DF80-432E-A987-010C23269D72', 'Agent Code', 'Code', NULL, 180, 100, 0, 1),
('12DC6642-2679-4019-A27E-43E93199392E', '名称', 'Name', NULL, 180, 200, 0, 1),
('A9D4B653-4181-4C55-B15D-917022460936', '职责说明', 'Description', NULL, 280, 300, 0, 0),
('CA946B1C-C443-4B1D-8668-A5EE53F2D7B5', '运行状态', 'RuntimeStatus', NULL, 110, 400, 0, 1),
('35C6C41A-8D32-40E9-9E12-C9CB4CA10C03', '最新版本', 'CurrentPublishedLabel', NULL, 110, 500, 1, 0),
('C8670C42-6ADF-4733-9490-CFD956D4B97A', '修订号', 'LogicalRevision', 'digit', 90, 600, 1, 1),
('C773106A-D59D-44E0-992C-00FB39020EB0', '创建时间', 'CreatedTime', 'dateTime', 170, 700, 1, 1),
('CB9CB78F-67E6-4B2E-B25E-8630B03814F8', '更新时间', 'UpdateTime', 'dateTime', 170, 800, 1, 1);

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
    source.HideInSearch, 'left', 'A', 0, 1, 0, 0,
    'list', 0, 1, 0, 1, 'Add', SYSUTCDATETIME()
FROM @Columns source
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.SmModuleColumn target
    WHERE target.SmModuleId = @ModuleId
      AND target.DataIndex = source.DataIndex
      AND target.IsDeleted = 0
);

COMMIT TRANSACTION;

SELECT ID, ModuleCode, ModuleName, RoutePath, Element
FROM dbo.SmModules
WHERE ID = @ModuleId;

SELECT DataIndex, Title, TaxisNo, HideInSearch
FROM dbo.SmModuleColumn
WHERE SmModuleId = @ModuleId AND IsDeleted = 0
ORDER BY TaxisNo;
