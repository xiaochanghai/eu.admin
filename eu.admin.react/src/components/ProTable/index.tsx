import React, { useMemo, useRef, useState } from "react";
import { ProTable } from "@ant-design/pro-components";
import { Button, Dropdown, Modal, Flex } from "antd";
import type { MenuProps } from "antd";
import { ActionType } from "@/typings";
import { pagination } from "@/config/proTable";
import { Icon } from "@/components";
import { ModuleInfoBeforeAction } from "@/api/interface/index";
import { message } from "@/hooks/useMessage";
import { singleDelete } from "@/api/modules/module";
import { useTranslation } from "react-i18next";

// Hooks
import { useProTableData, useProTableColumns, useProTableBatchOps, useProTableToolbar } from "@/hooks/ProTable";

// Components
import { Toolbar, RecordLogModal, UploadExcelModal } from "./components";

// Types and Utils
import { ProTableProps } from "./types";
import { calculateScrollWidth } from "./utils";
import { TABLE_SCROLL_CONFIG, SUM_ROW_ID } from "./constants";
import { RootState, useSelector } from "@/redux";

const { confirm } = Modal;

/**
 * 操作按钮组件
 */
interface ActionButtonProps {
  icon: string;
  onClick: () => void;
  disabled?: boolean;
  tooltip?: string;
}

const ActionButton: React.FC<ActionButtonProps> = React.memo(({ icon, onClick, disabled = false }) => {
  return (
    <Button
      type="dashed"
      size="small"
      icon={<Icon name={icon} />}
      onClick={onClick}
      disabled={disabled}
      style={{
        border: 0,
        background: "transparent",
        boxShadow: "0 0px 0 rgb(255 255 255 / 2%)",
        marginRight: 8
      }}
    />
  );
});

/**
 * SmProTable 主组件
 * 基于 @ant-design/pro-components 的 ProTable 封装
 * 提供模块化的表格功能，包括数据请求、操作列、工具栏、批量操作等
 */
const SmProTable: React.FC<ProTableProps> = props => {
  const { moduleInfo, IsView, onEdit, masterId, customConditions, formRef, expendHideAction, expendAction, ...restProps } = props;

  const { moduleCode, columns, url, beforeActions, dropActions, IsShowRowSelection } = moduleInfo;
  const actionRef = useRef<ActionType>();
  const language = useSelector((state: RootState) => state.global.language);
  const { t } = useTranslation();

  // ==================== 跨页选中状态管理 ====================
  // Ant Design onChange 返回的是跨页完整选中状态，直接用即可
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [selectedRows, setSelectedRows] = useState<any[]>([]);

  // ==================== 自定义 Hooks ====================

  // 数据请求和状态管理
  const { searchVisible, tableParam, latestParamsRef, handleRequest, handleReset, onSearchToggle } = useProTableData(
    moduleCode,
    moduleInfo,
    masterId,
    customConditions,
    formRef
  );

  // 列配置增强
  const { enhancedColumns, columnsStateMap, handleOnChangeColumn } = useProTableColumns(
    columns,
    moduleInfo.UserModuleColumn,
    moduleInfo.moduleId,
    moduleInfo,
    language
  );

  // 批量操作
  const { batchDeleteConfirm, batchAuditConfirm, batchRevocationConfirm } = useProTableBatchOps(
    moduleCode,
    url,
    restProps.batchDelete
  );

  // 重置搜索时同时清空选中
  const handleResetWithClearSelection = () => {
    clearSelection();
    handleReset();
  };

  // 工具栏状态管理
  const {
    moreToolBarVisible,
    uploadExcelVisible,
    recordLogVisible,
    recordLogData,
    handleMoreToolBarVisibleChange,
    handleUploadExcelOpen,
    handleUploadExcelClose,
    handleShowLog,
    handleLogClose,
    handleExportExcel
  } = useProTableToolbar(moduleCode, tableParam);

  // 清空选中状态（用于重置搜索或批量操作后）
  const clearSelection = () => {
    setSelectedRowKeys([]);
    setSelectedRows([]);
  };

  // ==================== 操作列相关 ====================

  /**
   * 构建行操作权限映射
   */
  const optionAuthButton: { [key: string]: boolean } = {};
  beforeActions?.forEach((item: any) => (optionAuthButton[item.id] = true));

  /**
   * 显示删除确认对话框
   */
  const showDeleteConfirm = async (action: any, record: any) => {
    confirm({
      title: t("proTable.deleteConfirmTitle"),
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: t("proTable.okText"),
      okType: "danger",
      cancelText: t("proTable.cancelText"),
      async onOk() {
        const hideLoading = message.loading(t("proTable.submitting"), 0);
        try {
          if (restProps.delete) {
            restProps.delete(record);
          } else {
            const { Success, Message } = await singleDelete({ moduleCode, Id: record.ID, url });
            if (Success) {
              action.reload();
              message.success(Message);
            }
          }
        } finally {
          hideLoading();
        }
      },
      onCancel() {
        //
      }
    });
  };

  /**
   * 编辑记录
   */
  const onOptionEdit = (record: any) => {
    onEdit?.(record.ID, false);
  };

  /**
   * 查看记录
   */
  const onOptionView = (record: any) => {
    onEdit?.(record.ID, true);
  };

  /**
   * 删除记录
   */
  const onOptionDelete = (action: any, record: any) => {
    if (restProps.deleteConfirm) {
      restProps.deleteConfirm(action, record);
    } else {
      showDeleteConfirm(action, record);
    }
  };

  /**
   * 获取下拉菜单项
   */
  const getDropActions = (record: any, action: any): MenuProps["items"] => {
    return dropActions
      ?.map((item: ModuleInfoBeforeAction) => {
        // 处理修改操作
        if (item.id === "Update" && !IsView) {
          return {
            key: "dropActionUpdate",
            label: t("proTable.edit"),
            onClick: () => onOptionEdit(record)
          };
        }

        // 处理查看操作
        if (item.id === "View") {
          return {
            key: "dropActionView",
            label: t("proTable.view"),
            icon: <Icon name="EyeOutlined" />,
            onClick: () => onOptionView(record)
          };
        }

        // 处理删除操作
        if (item.id === "Delete" && !IsView) {
          return {
            key: "dropActionDelete",
            label: t("proTable.delete"),
            icon: <Icon name="DeleteOutlined" />,
            onClick: () => onOptionDelete(action, record)
          };
        }

        // 处理自定义操作
        const customAction = moduleInfo.actionData?.find((data: any) => data.ID === item.id);
        if (customAction && restProps[customAction.FunctionCode]) {
          return {
            key: `dropAction${item.id}`,
            label: customAction.FunctionName,
            icon: customAction.Icon && <Icon name={customAction.Icon} style={{ marginRight: 7 }} />,
            onClick: () => restProps[customAction.FunctionCode](record.ID, action, record)
          };
        }

        return null;
      })
      .filter(Boolean);
  };
  /**
   * 获取 beforeActions 操作按钮（不含已内置的 Update/View/Delete）
   */
  const getBeforeActionActions = (record: any, action: any) => {
    return beforeActions
      ?.map((item: ModuleInfoBeforeAction) => {
        const { id } = item;

        // Update/View/Delete 已在上方单独渲染，此处跳过
        if (id === "View" || id === "Update" || id === "Delete") {
          return null;
        }

        // 处理自定义操作
        const customAction = moduleInfo.actionData?.find((data: any) => data.ID === id);
        if (customAction && restProps[customAction.FunctionCode]) {
          const handler = restProps[customAction.FunctionCode];
          return (
            <Button
              key={`${customAction.FunctionCode}${record.ID}`}
              type="text"
              size="small"
              icon={customAction.Icon ? <Icon name={customAction.Icon} /> : null}
              onClick={() => handler(record.ID, action, record)}
              style={{ color: customAction.Color ?? "inherit" }}
            >
              {customAction.FunctionName}
            </Button>
          );
        }

        return null;
      })
      .filter(Boolean);
  };

  /**
   * 构建操作列
   */
  const actionColumn: any =
    moduleInfo?.Success === true && moduleInfo.actionCount > 0
      ? {
        title: t("proTable.operation"),
        dataIndex: "option",
        fixed: "left",
        valueType: "option",
        width: 100,
        align: "center",
        render: (_: any, record: { ID: string }, index: number, action: any) => (
          <>
            {record.ID !== SUM_ROW_ID && (
              <>
                {optionAuthButton.Update && !IsView && (
                  <ActionButton icon="EditOutlined" onClick={() => onOptionEdit(record)} />
                )}
                {optionAuthButton.View && (
                  <Button
                    type="dashed"
                    key={"View" + index}
                    size="small"
                    icon={<Icon name="EyeOutlined" />}
                    onClick={() => onOptionView(record)}
                    style={{ border: 0, background: "transparent", boxShadow: "0 0px 0 rgb(255 255 255 / 2%)" }}
                  />
                )}
                {optionAuthButton.Delete && !IsView && (
                  <Button
                    key={"Delete" + index}
                    type="dashed"
                    size="small"
                    icon={<Icon name="DeleteOutlined" />}
                    onClick={() => onOptionDelete(action, record)}
                    style={{ border: 0, background: "transparent", boxShadow: "0 0px 0 rgb(255 255 255 / 2%)" }}
                  />
                )}
                <Flex gap="small" wrap>
                  {getBeforeActionActions(record, action)}
                </Flex>
                {dropActions && dropActions.length > 0 && (
                  <Dropdown placement="bottom" arrow menu={{ items: getDropActions(record, action) }}>
                    <Button
                      type="dashed"
                      size="small"
                      icon={<Icon name="MoreOutlined" />}
                      style={{ border: 0, background: "transparent", boxShadow: "0 0px 0 rgb(255 255 255 / 2%)" }}
                    />
                  </Dropdown>
                )}
              </>
            )}
          </>
        )
      }
      : [];

  /**
   * 合并操作列到列配置中
   */
  const finalColumns = useMemo(() => {
    if (!actionColumn || !actionColumn.dataIndex) return enhancedColumns;

    const hasOptionColumn = enhancedColumns.some((col: any) => col.dataIndex === "option");
    if (hasOptionColumn) {
      // 替换现有操作列
      return enhancedColumns.map((col: any) => (col.dataIndex === "option" ? actionColumn : col));
    }
    // 添加操作列到末尾
    return [...enhancedColumns, actionColumn];
  }, [enhancedColumns, actionColumn, language]);

  // ==================== 工具栏渲染器 ====================

  // 批量操作后清空选中
  const handleBatchDelete = (action: any, rows: any[]) => {
    batchDeleteConfirm(action, rows, clearSelection);
  };

  const handleBatchAudit = (action: any, rows: any[], keys: any[]) => {
    batchAuditConfirm(action, rows, keys);
    setTimeout(clearSelection, 0);
  };

  const handleBatchRevocation = (action: any, rows: any[], keys: any[]) => {
    batchRevocationConfirm(action, rows, keys);
    setTimeout(clearSelection, 0);
  };

  /**
   * 渲染工具栏
   */
  const toolBarRender = (action: any) =>
    Toolbar({
      moduleInfo,
      IsView,
      action,
      selectedRows,
      selectedRowKeys,
      masterId,
      onAdd: () => onEdit?.(null),
      onUploadExcel: handleUploadExcelOpen,
      onExportExcel: handleExportExcel,
      onShowLog: () => handleShowLog(selectedRows),
      onBatchDelete: () => handleBatchDelete(action, selectedRows),
      onBatchAudit: () => handleBatchAudit(action, selectedRows, selectedRowKeys),
      onBatchRevocation: () => handleBatchRevocation(action, selectedRows, selectedRowKeys),
      onSearchToggle,
      moreToolBarVisible,
      onMoreToolBarVisibleChange: handleMoreToolBarVisibleChange,
      customMenuActions: restProps,
      expendHideAction,
      expendAction
    });

  /**
   * 双击行处理
   */
  const handleDoubleClick = (record: any) => {
    // 忽略汇总行
    if (record.ID === SUM_ROW_ID || !moduleInfo?.Success || IsView) return;

    // 检查是否有编辑权限
    const hasUpdatePermission =
      moduleInfo.beforeActions?.some((item: any) => item.id === "Update") ||
      moduleInfo.dropActions?.some((item: any) => item.id === "Update");

    if (hasUpdatePermission) {
      onOptionEdit(record);
    }
  };

  // ==================== 渲染 ====================

  return (
    <>
      <ProTable
        rowKey="ID"
        tableAlertRender={false}
        columns={finalColumns}
        toolBarRender={toolBarRender}
        onRow={record => ({
          onDoubleClick: () => handleDoubleClick(record)
        })}
        className="ant-pro-table-scroll"
        rowSelection={IsShowRowSelection === false ? undefined : {
          fixed: "left",
          selectedRowKeys,
          preserveSelectedRowKeys: true,
          onChange: (keys: React.Key[], rows: any[]) => {
            setSelectedRowKeys(keys);
            setSelectedRows(rows);
          },
          getCheckboxProps: (record: any) => ({
            disabled: record.ID === SUM_ROW_ID
          })
        }}
        onReset={handleResetWithClearSelection}
        actionRef={actionRef}
        formRef={formRef}
        scroll={{
          scrollToFirstRowOnChange: TABLE_SCROLL_CONFIG.scrollToFirstRowOnChange,
          x: calculateScrollWidth(finalColumns),
          y: TABLE_SCROLL_CONFIG.y
        }}
        onLoad={() => {
          if (!formRef?.current) return;
          // 优先用最近一次请求实际使用的参数（同步 ref，永远最新）回填表单，
          // 避免读取慢一拍的 Redux tableParam 导致输入框回退到上一次的值；
          // 首次加载尚无请求时，回退到已保存的历史筛选条件（如从其他页返回）
          const restoreParams = latestParamsRef.current ?? tableParam?.params;
          if (restoreParams) {
            formRef.current.setFieldsValue({ ...restoreParams });
          }
        }}
        pagination={
          tableParam?.params
            ? { ...pagination, current: tableParam.params.current, pageSize: tableParam.params.pageSize }
            : pagination
        }
        request={handleRequest}
        search={
          searchVisible
            ? {
              labelWidth: "auto"
            }
            : false
        }
        columnsState={{
          value: columnsStateMap,
          onChange: handleOnChangeColumn
        }}
        dateFormatter="string"
        columnEmptyText={"-"}
        {...restProps}
      />

      {/* 弹窗组件 */}
      {moduleInfo?.Success === true && (
        <>
          <RecordLogModal visible={recordLogVisible} data={recordLogData} onCancel={handleLogClose} />

          <UploadExcelModal
            visible={uploadExcelVisible}
            moduleInfo={moduleInfo}
            onCancel={handleUploadExcelClose}
            onReload={() => actionRef.current?.reload()}
          />
        </>
      )}
    </>
  );
};

SmProTable.displayName = "SmProTable";

export default SmProTable;
