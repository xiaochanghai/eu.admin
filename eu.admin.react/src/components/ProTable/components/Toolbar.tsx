import { Button, Space, Dropdown, Tooltip } from "antd";
import { Icon } from "@/components";
import { ToolbarProps } from "../types";
import { buildAuthButtonMap } from "../utils";

/**
 * 工具栏工厂函数
 * 返回表格工具栏按钮数组，包含新建、导入、导出、批量操作等按钮
 */
export const Toolbar = (props: ToolbarProps) => {
  const {
    moduleInfo,
    IsView,
    action,
    selectedRows,
    selectedRowKeys,
    masterId,
    onAdd,
    onUploadExcel,
    onExportExcel,
    onShowLog,
    onBatchDelete,
    onBatchAudit,
    onBatchRevocation,
    onSearchToggle,
    moreToolBarVisible,
    onMoreToolBarVisibleChange,
    customMenuActions = {},
    expendHideAction,
    expendAction
  } = props;

  const actionAuthButton = buildAuthButtonMap(moduleInfo.actions);

  const isModuleSuccess = moduleInfo?.Success === true;
  const hasSelectedRows = selectedRows?.length > 0;
  const hasSingleSelection = selectedRows?.length === 1;
  const hasExportExcel = moduleInfo.actions?.includes("ExportExcel");

  const getMoreToolBarItems = () => {
    const items: any[] = [];
    if (hasExportExcel) {
      items.push({
        key: "ExportExcel",
        icon: <Icon name="excel-export" />,
        label: "导出Excel",
        onClick: onExportExcel
      });
    }
    return items;
  };

  return [
    <Space style={{ display: "flex", justifyContent: "center" }} key="main-actions">
      {isModuleSuccess && !IsView && (
        <>
          {moduleInfo.actions?.includes("Add") && (
            <Button
              type="primary"
              icon={<Icon name="PlusOutlined" />}
              onClick={onAdd}
              disabled={moduleInfo.isDetail && !masterId}
            >
              新建
            </Button>
          )}
          {moduleInfo.actions?.includes("ImportExcel") && (
            <Button icon={<Icon name="excel-import" />} onClick={onUploadExcel}>
              Excel导入
            </Button>
          )}
        </>
      )}
      {moduleInfo.IsShowAudit && (
        <>
          {actionAuthButton.Audit && (
            <Button icon={<Icon name="CheckCircleOutlined" />} color="primary" onClick={onBatchAudit}>
              审核
            </Button>
          )}
          {actionAuthButton.Revocation && (
            <Button type="primary" icon={<Icon name="RollbackOutlined" />} danger onClick={onBatchRevocation}>
              撤销
            </Button>
          )}
        </>
      )}
      {isModuleSuccess &&
        moduleInfo.menuData?.length > 0 &&
        moduleInfo.menuData.map((item: any) => (
          <Button
            key={item.ID}
            icon={item.Icon && <Icon name={item.Icon} />}
            onClick={() => customMenuActions[item.FunctionCode]?.(action, selectedRows, selectedRowKeys)}
          >
            {item.FunctionName}
          </Button>
        ))}
    </Space>,

    hasExportExcel && (
      <Dropdown
        key="more-dropdown"
        onOpenChange={onMoreToolBarVisibleChange}
        open={moreToolBarVisible}
        menu={{ items: getMoreToolBarItems() }}
      >
        <Button type="text" style={{ paddingLeft: 5, paddingRight: 5 }}>
          更多 <Icon name="DownOutlined" />
        </Button>
      </Dropdown>
    ),

    hasSingleSelection && (
      <Button key="log-btn" onClick={onShowLog}>
        <Icon name="UnorderedListOutlined" /> 日志
      </Button>
    ),

    hasSelectedRows && (
      <Space style={{ display: "flex", justifyContent: "center" }} key="batch-actions">
        {isModuleSuccess &&
          moduleInfo.hideMenu?.length > 0 &&
          moduleInfo.hideMenu.map((item: any) => (
            <Button
              key={item.ID}
              onClick={() => customMenuActions[item.FunctionCode]?.(action, selectedRows, selectedRowKeys)}
            >
              {item.Icon && <Icon name={item.Icon} />}
              {item.FunctionName}
            </Button>
          ))}
        {expendHideAction?.(action, selectedRows)}
        {isModuleSuccess && (
          <>
            {moduleInfo.actions?.includes("Submit") && (
              <Button onClick={() => customMenuActions.submitAudit?.(action, selectedRows)}>提交</Button>
            )}
            {moduleInfo.actions?.includes("BatchDelete") && !IsView && (
              <Button onClick={onBatchDelete}>
                <Icon name="DeleteOutlined" /> 批量删除
              </Button>
            )}
          </>
        )}
      </Space>
    ),

    expendAction?.(action, selectedRows, selectedRowKeys),

    <Button
      key="search-btn"
      type="dashed"
      onClick={onSearchToggle}
      style={{ border: 0, padding: 0, boxShadow: "none" }}
    >
      <Tooltip placement="top" title="查询">
        <Icon name="SearchOutlined" className="font-size16" />
      </Tooltip>
    </Button>
  ].filter(Boolean);
};
