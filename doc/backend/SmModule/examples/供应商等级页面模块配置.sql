-- SmModule 元数据聚合脚本示例：供应商等级
-- 仅用于说明 SmModules、SmModuleSql、SmModuleColumn 的关联与部署顺序。
-- 未包含事务、缓存刷新和权限配置；未经明确授权不得直接连接数据库执行。
-- 重要：此文件保留用户提供的原始值序列。最后一条“创建时间”栏位为 74 列/73 值，当前不可执行；
-- 必须从源环境重新导出或确认缺失字段后才能生成可执行版本，禁止凭猜测补 NULL。
-- 2026-08-07 已只读核对现库对应模块，但本文件仍保留原始快照，不据此手工修补为部署脚本。
-- 生成时间：2026-08-07 09:57:38

DELETE FROM `SmModuleColumn` WHERE (`SmModuleId` = N'708e1816-f087-42c0-bbc0-14af438a8838');
DELETE FROM `SmModuleSql` WHERE (`ModuleId` = N'708e1816-f087-42c0-bbc0-14af438a8838');
DELETE FROM `SmModules` WHERE (`ID` = N'708e1816-f087-42c0-bbc0-14af438a8838');

INSERT INTO `SmModules`
    (`ModuleCode`,`ModuleName`,`TaxisNo`,`Icon`,`RoutePath`,`ParentId`,`IsParent`,`ApiUrl`,`IsShowAdd`,`IsShowBatchDelete`,`IsShowDelete`,`IsShowUpdate`,`IsShowView`,`IsDetail`,`BelongModuleId`,`IsShowSubmit`,`DefaultSort`,`DefaultSortOrder`,`IsShowAudit`,`IsShowGoBack`,`IsExecQuery`,`IsSum`,`OpenType`,`FormPage`,`ModuleType`,`FormPageWidth`,`Element`,`IsFull`,`IsExportExcel`,`IsImportExcel`,`IsShowRowSelection`,`IsRoleDataScope`,`IsWorkflow`,`IsDeleted`,`IsActive`,`ImportDataId`,`ModificationNum`,`Tag`,`GroupId`,`CompanyId`,`AuditStatus`,`CurrentNode`,`CreatedBy`,`CreatedTime`,`ID`)
VALUES
    (N'BD_SUPPLIER_LEVEL_MNG',N'供应商等级',800,N'menu-supply-chain-level',N'/supplychain/supplier_level',N'93944c80-c20e-4a1c-ab6d-151e85e9f512',null,N'/api/Common/BD_SUPPLIER_LEVEL_MNG',null,null,null,null,null,null,null,null,null,null,null,null,null,null,N'Drawer',null,null,null,N'/basedata/supplierLevel/index',null,null,null,null,null,null,0,1,null,8,1,N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'Add',null,N'60ef2323-8ec7-4c84-b08e-7894b5cd6f7e',NOW(6),N'708e1816-f087-42c0-bbc0-14af438a8838');
SELECT LAST_INSERT_ID();

INSERT INTO `SmModuleSql`
    (`ModuleId`,`PrimaryTableName`,`TableNames`,`TableAliasNames`,`PrimaryKey`,`SqlSelect`,`SqlSelectBrw`,`JoinType`,`SqlJoinTable`,`SqlJoinTableAlias`,`SqlJoinCondition`,`SqlDefaultCondition`,`SqlRecycleCondition`,`SqlQueryCondition`,`DefaultSortField`,`DefaultSortDirection`,`GroupBy`,`Description`,`FullSql`,`Remark`,`ID1`,`IsDeleted`,`IsActive`,`ImportDataId`,`ModificationNum`,`Tag`,`GroupId`,`CompanyId`,`AuditStatus`,`CurrentNode`,`CreatedBy`,`CreatedTime`,`ID`)
VALUES
    (N'708e1816-f087-42c0-bbc0-14af438a8838',N'BdSupplierLevel',N'BdSupplierLevel',N'A',null,N'SELECT A.*,A.ID AS DELETE_CONFIRM_MSG',null,null,null,null,null,N'A.IsActive = ''true'' AND A.IsDeleted = ''false''',N'A.IsActive = ''true'' AND A.IsDeleted = ''true''',null,N'CreatedTime',N'DESC',null,null,null,null,null,0,1,null,0,1,N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'Add',null,N'60ef2323-8ec7-4c84-b08e-7894b5cd6f7e',NOW(6),N'5a8fbd9f-2dcf-4269-ae7e-5f331e939749');
SELECT LAST_INSERT_ID();

INSERT INTO `SmModuleColumn`
    (`SmModuleId`,`Title`,`DataIndex`,`ValueType`,`Width`,`HideInTable`,`Sorter`,`filters`,`filterMultiple`,`IsExport`,`TaxisNo`,`IsLovCode`,`IsBool`,`QueryValue`,`QueryValueType`,`HideInSearch`,`DataFormate`,`Align`,`TableAlias`,`IsSum`,`FormTaxisNo`,`DefaultValue`,`HideInForm`,`Required`,`Disabled`,`Validator`,`ValidPattern`,`IsUnique`,`MaxLength`,`MinLength`,`Maximum`,`Minimum`,`CreateHide`,`ModifyDisabled`,`GridSpan`,`FormTitle`,`FieldType`,`Placeholder`,`DataSourceType`,`DataSource`,`IsMasterId`,`LabelCol`,`WrapperCol`,`MinRows`,`Remark`,`FromFieldGroup`,`IsTableEditable`,`IsAutoCode`,`ColumnMode`,`IsCopy`,`IsTooltip`,`TooltipContent`,`Color`,`IsThemeColor`,`AllowClear`,`IsMultiple`,`MultipleMaxCount`,`IsRedirect`,`RedirectUrl`,`ModifyHide`,`Accept`,`MaxFileSize`,`IsDeleted`,`IsActive`,`ImportDataId`,`ModificationNum`,`Tag`,`GroupId`,`CompanyId`,`AuditStatus`,`CurrentNode`,`CreatedBy`,`CreatedTime`,`ID`)
VALUES
    (N'708e1816-f087-42c0-bbc0-14af438a8838',N'备注',N'Remark',NULL,NULL,0,NULL,NULL,NULL,NULL,N'400',NULL,NULL,NULL,NULL,1,NULL,NULL,N'A',NULL,NULL,NULL,0,0,0,NULL,NULL,0,NULL,NULL,NULL,NULL,0,0,NULL,NULL,N'Input',NULL,NULL,NULL,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,0,1,NULL,N'0',N'1',N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'Add',NULL,N'60ef2323-8ec7-4c84-b08e-7894b5cd6f7e',NOW(6),N'06d57390-7bd3-4f2b-807b-a16225c965f8'),
    (N'708e1816-f087-42c0-bbc0-14af438a8838',N'等级名称',N'LevelName',NULL,NULL,NULL,NULL,NULL,NULL,NULL,N'300',NULL,NULL,NULL,NULL,NULL,NULL,NULL,N'A',NULL,NULL,NULL,0,1,0,NULL,NULL,0,NULL,NULL,NULL,NULL,0,0,NULL,NULL,N'Input',NULL,NULL,NULL,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,0,1,NULL,N'0',N'1',N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'Add',NULL,N'60ef2323-8ec7-4c84-b08e-7894b5cd6f7e',NOW(6),N'17c407b3-af9a-4ea9-8cf5-1d9d96a75484'),
    (N'708e1816-f087-42c0-bbc0-14af438a8838',N'等级编号',N'LevelNo',NULL,NULL,NULL,NULL,NULL,NULL,NULL,N'200',NULL,NULL,NULL,NULL,NULL,NULL,NULL,N'A',NULL,NULL,NULL,0,1,0,NULL,NULL,1,NULL,NULL,NULL,NULL,0,0,NULL,NULL,N'Input',NULL,NULL,NULL,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,0,1,NULL,N'0',N'1',N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'Add',NULL,N'60ef2323-8ec7-4c84-b08e-7894b5cd6f7e',NOW(6),N'66c5028e-997f-4e69-b512-63aa9c6e4938'),
    (N'708e1816-f087-42c0-bbc0-14af438a8838',N'创建时间',N'CreatedTime',N'dateTime',NULL,0,NULL,NULL,NULL,NULL,N'100',NULL,NULL,NULL,NULL,1,NULL,NULL,N'A',NULL,NULL,1,0,0,NULL,NULL,0,NULL,NULL,NULL,NULL,0,0,NULL,NULL,NULL,NULL,NULL,NULL,0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,0,1,NULL,N'0',N'1',N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'e26f359a-4983-42d8-8769-19ddec5b7d23',N'Add',NULL,N'60ef2323-8ec7-4c84-b08e-7894b5cd6f7e',NOW(6),N'db9798e8-b0c4-4d05-bd0f-31a4055e31ee');
SELECT @@IDENTITY;
