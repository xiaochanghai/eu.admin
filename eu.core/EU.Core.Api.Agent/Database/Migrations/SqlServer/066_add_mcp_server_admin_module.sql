-- 新增 React 管理端 MCP Server 模块元数据。
-- 列表由 SmModules/SmModuleSql/SmModuleColumn 驱动，编辑及生命周期操作由 React 自定义页面调用 Agent API。
-- 执行后需在“模块管理”中给目标角色分配 Query/Add/Update/View 权限，并清理模块、菜单和权限缓存。

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ModuleId uniqueidentifier = '78BB3D15-24A1-42CC-AFFE-37FE47131170';
DECLARE @ModuleCode nvarchar(50) = N'AG_MCP_SERVER_MNG';
DECLARE @ModuleSqlId uniqueidentifier = '4D47D72A-07B9-4646-ACD8-8F5543A5761C';

IF EXISTS
(
    SELECT 1
    FROM dbo.SmModules
    WHERE ModuleCode = @ModuleCode
      AND ID <> @ModuleId
)
BEGIN
    THROW 51660, 'AG_MCP_SERVER_MNG already exists with a different module ID.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.SmModules
    WHERE ID = @ModuleId
      AND ModuleCode <> @ModuleCode
)
BEGIN
    THROW 51661, 'The MCP Server module ID is already used by another module code.', 1;
END;

BEGIN TRANSACTION;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.SmModules
    WHERE ID = @ModuleId
)
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
        @ModuleId, @ModuleCode, N'MCP Server', 910, N'ApiOutlined',
        N'/agent/mcp-server', 0, N'/api/mcp/servers',
        1, 0, 0, 1, 1,
        0, 0, 0, 0, 1, 0,
        N'Drawer', N'/agent/mcpServer/FormPage', N'Form', 760, N'/agent/mcpServer/index', 0,
        0, 0, 0, 0,
        0, NULL, N'right', 1,
        0, 1, 0, 1, N'Add', SYSUTCDATETIME()
    );
END;

UPDATE dbo.SmModules
SET IsExecQuery = 1,
    FormPage = N'/agent/mcpServer/FormPage',
    IsAllowCustomColumn = 1
WHERE ID = @ModuleId;

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.SmModuleSql
    WHERE ModuleId = @ModuleId
      AND IsDeleted = 0
)
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
        @ModuleSqlId, @ModuleId, N'AgMcpServerDefinition', N'AgMcpServerDefinition', N'A', N'ID',
        N'SELECT A.*', N'SELECT A.*', N'CreatedTime', N'DESC',
        N'SELECT A.ID,A.Code,A.Name,A.Description,A.Transport,CASE WHEN A.Transport=''Stdio'' THEN A.Command ELSE A.Endpoint END AS ConnectionTarget,(SELECT COUNT(1) FROM dbo.AgMcpToolVersion T WHERE T.ServerId=A.ID AND T.CurrentOrdinal IS NOT NULL AND T.IsDeleted=0) AS CurrentToolCount,A.Status,A.LogicalRevision,A.LastSyncedAtUtc,A.CreatedTime,A.UpdateTime FROM dbo.AgMcpServerDefinition A WHERE A.IsDeleted=0',
        N'MCP Server 通用管理列表；配置、同步、启停、归档及工具风险操作由 Agent API 完成。',
        0, 1, 0, 1, N'Add', SYSUTCDATETIME()
    );
END;

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
('85EC75F1-8908-4251-BDA6-F5E81A497A28', N'Server Code', N'Code', NULL, 180, 100, 0, 1),
('A115C012-1750-468E-B24A-78A0F5716328', N'名称', N'Name', NULL, 180, 200, 0, 1),
('1DC4E98D-34AD-4DDB-8ABE-3C4398D31827', N'说明', N'Description', NULL, 260, 300, 0, 0),
('D87BBADA-04F7-4D55-9119-6A993F9EA062', N'传输方式', N'Transport', NULL, 140, 400, 0, 1),
('BC1E1BC8-FFE5-40EE-9BE0-A702EEF12F6D', N'连接目标', N'ConnectionTarget', NULL, 280, 500, 1, 0),
('081F3905-F6F4-4E03-B8A9-8FE1608D4ECE', N'工具数', N'CurrentToolCount', N'digit', 90, 600, 1, 0),
('23FC4C44-5BE6-4BAD-A183-04B4673A6F0B', N'状态', N'Status', NULL, 110, 700, 0, 1),
('D599A8C6-3947-457F-98E1-B719E6FF10E3', N'修订号', N'LogicalRevision', N'digit', 90, 800, 1, 1),
('DF4E6081-869D-4EDE-8E18-B447BF83A045', N'最近同步', N'LastSyncedAtUtc', N'dateTime', 170, 900, 1, 1),
('96F72908-587B-4BE4-BD28-A067C96A35B7', N'更新时间', N'UpdateTime', N'dateTime', 170, 1000, 1, 1);

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
    SELECT 1
    FROM dbo.SmModuleColumn target
    WHERE target.SmModuleId = @ModuleId
      AND target.DataIndex = source.DataIndex
      AND target.IsDeleted = 0
);

COMMIT TRANSACTION;

SELECT
    ID,
    ModuleCode,
    ModuleName,
    RoutePath,
    Element,
    ApiUrl,
    IsActive
FROM dbo.SmModules
WHERE ID = @ModuleId;

SELECT
    ID,
    ModuleId,
    PrimaryTableName,
    TableNames,
    TableAliasNames,
    PrimaryKey,
    DefaultSortField,
    DefaultSortDirection,
    Description
FROM dbo.SmModuleSql
WHERE ModuleId = @ModuleId
  AND IsDeleted = 0;

SELECT
    DataIndex,
    Title,
    ValueType,
    Width,
    TaxisNo,
    HideInSearch,
    Sorter,
    ColumnMode
FROM dbo.SmModuleColumn
WHERE SmModuleId = @ModuleId
  AND IsDeleted = 0
ORDER BY TaxisNo;

/*
    Optional rollback (review and execute separately; this script does not run it):

    BEGIN TRANSACTION;
    DELETE FROM dbo.SmRoleFunction WHERE SmModuleId = '78BB3D15-24A1-42CC-AFFE-37FE47131170';
    DELETE FROM dbo.SmRoleModule WHERE SmModuleId = '78BB3D15-24A1-42CC-AFFE-37FE47131170';
    DELETE FROM dbo.SmModuleColumn WHERE SmModuleId = '78BB3D15-24A1-42CC-AFFE-37FE47131170';
    DELETE FROM dbo.SmModuleSql WHERE ModuleId = '78BB3D15-24A1-42CC-AFFE-37FE47131170';
    DELETE FROM dbo.SmModules WHERE ID = '78BB3D15-24A1-42CC-AFFE-37FE47131170';
    COMMIT TRANSACTION;
*/
