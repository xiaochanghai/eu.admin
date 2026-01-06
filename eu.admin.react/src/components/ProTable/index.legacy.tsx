import React, { useRef, useState, useMemo } from "react";
import { Button, Dropdown, Tag, Space, Modal, Tooltip, Switch } from "antd";
import { pagination } from "@/config/proTable";
import { ProTable } from "@ant-design/pro-components";
import { ActionType, MenuProps } from "@/typings";

import { message } from "@/hooks/useMessage";
import {
  queryByFilter,
  exportExcel,
  getModuleLogInfo,
  singleDelete,
  batchDelete,
  recordUserModuleColumn,
  batchAudit,
  batchRevocation
} from "@/api/modules/module";
import { ModuleInfoBeforeAction, RecordLogData } from "@/api/interface/index";
import { setTableParam, setSearchVisible, setModuleInfo } from "@/redux/modules/module";
import { useDispatch, RootState, useSelector } from "@/redux";
import { UploadExcel, ModuleLog, ComboGrid, Icon } from "@/components";
import { downloadFile } from "@/utils";

const { confirm } = Modal;

/**
 * 操作按钮组件
 * @param icon 图标名称
 * @param onClick 点击事件
 * @param disabled 是否禁用
 * @param tooltip 提示文字
 */
interface ActionButtonProps {
  icon: string;
  onClick: () => void;
  disabled?: boolean;
  tooltip?: string;
}

const ActionButton: React.FC<ActionButtonProps> = React.memo(({ icon, onClick, disabled = false, tooltip }) => {
  const button = (
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

  return tooltip ? <Tooltip title={tooltip}>{button}</Tooltip> : button;
});

const SmProTable: React.FC<any> = React.memo(props => {
  let tableAction = useRef<ActionType | null>(null);
  const dispatch = useDispatch();
  const [moreToolBarVisible, setMoreToolBarVisible] = useState(false);
  const [recordLogVisible, setRecordLogVisible] = useState(false);
  const [recordLogData, setRecordLogData] = useState<RecordLogData | null>(null);
  const [uploadExcelVisible, setUploadExcelVisible] = useState(false);
  let {
    moduleInfo,
    IsView,
    onEdit,
    moduleInfo: {
      columns,
      moduleCode,
      actions,
      beforeActions,
      url,
      isDetail,
      UserModuleColumn,
      moduleId,
      masterColumn,
      dropActions,
      IsShowAudit
    },
    expendHideAction,
    expendAction,
    masterId,
    formRef
  } = props;
  const searchVisibles = useSelector((state: RootState) => state.module.searchVisibles);
  const tableParams = useSelector((state: RootState) => state.module.tableParams);
  const tableParam = tableParams[moduleCode] as any;
  let currentParams: any = null;
  let searchVisible = searchVisibles[moduleCode] ?? false;
  const [columnsStateMap, setColumnsStateMap] = useState(() => {
    return UserModuleColumn;
  });

  const handleOnChangeColumn = async (map: any) => {
    await recordUserModuleColumn({ moduleId, map });
    setColumnsStateMap(map);
    const updatedModuleInfo = { ...moduleInfo, UserModuleColumn: map };
    dispatch(setModuleInfo(updatedModuleInfo));
  };

  const hasExportExcel = actions?.includes("ExportExcel");

  const getMoreToolBarItems = (action: any): MenuProps["items"] => {
    const items: MenuProps["items"] = [];

    if (hasExportExcel) {
      items.push({
        key: "ExportExcel",
        icon: <Icon name="excel-export" />,
        label: "导出Excel",
        onClick: () => moreToolBarMenuClick("ExportExcel", action)
      });
    }

    return items;
  };
  const actionAuthButton: { [key: string]: boolean } = {};
  actions?.forEach((item: any) => {
    actionAuthButton[item] = true;
  });

  const optionAuthButton: { [key: string]: boolean } = {};
  beforeActions?.forEach((item: any) => (optionAuthButton[item.id] = true));

  const dropActionAuthButton: { [key: string]: boolean } = {};
  dropActions?.forEach((item: any) => (dropActionAuthButton[item.id] = true));

  const getDropActions = (record: any, action: any): MenuProps["items"] => {
    return dropActions
      .map((item: ModuleInfoBeforeAction) => {
        // 处理修改操作
        if (item.id === "Update" && !IsView) {
          return {
            key: "dropActionUpdate",
            label: "修改",
            onClick: () => onOptionEdit(record)
          };
        }

        // 处理查看操作
        if (item.id === "View") {
          return {
            key: "dropActionView",
            label: "查看",
            icon: <Icon name="EyeOutlined" />,
            onClick: () => onOptionView(record)
          };
        }

        // 处理删除操作
        if (item.id === "Delete" && !IsView) {
          return {
            key: "dropActionDelete",
            label: "删除",
            icon: <Icon name="DeleteOutlined" />,
            onClick: () => onOptionDelete(action, record)
          };
        }

        // 处理自定义操作
        const customAction = moduleInfo.actionData?.find((data: any) => data.ID === item.id);
        if (customAction) {
          return {
            key: `dropAction${item.id}`,
            label: customAction.FunctionName,
            icon: customAction.Icon ? <Icon name={customAction.Icon} /> : undefined,
            onClick: () => props[customAction.FunctionCode](record.ID, action, record)
          };
        }

        return null;
      })
      .filter(Boolean);
  };

  const actionColumn: any =
    moduleInfo?.Success === true && moduleInfo.actionCount > 0
      ? {
          title: "操作",
          dataIndex: "option",
          fixed: "left",
          valueType: "option",
          width: 100,
          align: "center",
          render: (_: any, record: { ID: string }, index: number, action: any) => (
            <>
              {record.ID !== "SumRowID" && (
                <>
                  {optionAuthButton.Update && !IsView && (
                    // <Button
                    //   type="dashed"
                    //   key={"Update" + index}
                    //   size="small"
                    //   icon={<Icon name="EditOutlined" />}
                    //   onClick={() => onOptionEdit(record)}
                    //   style={{ border: 0, background: "transparent", boxShadow: "0 0px 0 rgb(255 255 255 / 2%)" }}
                    // ></Button>
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
                    ></Button>
                  )}
                  {optionAuthButton.Delete && !IsView && (
                    <Button
                      key={"Delete" + index}
                      type="dashed"
                      size="small"
                      icon={<Icon name="DeleteOutlined" />}
                      onClick={() => onOptionDelete(action, record)}
                      style={{ border: 0, background: "transparent", boxShadow: "0 0px 0 rgb(255 255 255 / 2%)" }}
                    ></Button>
                  )}
                  {dropActions.length > 0 && (
                    <Dropdown
                      placement="bottom"
                      arrow
                      menu={{
                        items: getDropActions(record, action)
                      }}
                    >
                      <Button
                        type="dashed"
                        size="small"
                        icon={<Icon name="MoreOutlined" />}
                        style={{ border: 0, background: "transparent", boxShadow: "0 0px 0 rgb(255 255 255 / 2%)" }}
                      ></Button>
                    </Dropdown>
                  )}
                </>
              )}
            </>
          )
        }
      : [];
  if (columns) {
    const existingOptionColumn = columns.filter((s: any) => s.dataIndex === "option");
    if (actionColumn && actionColumn.dataIndex) {
      if (existingOptionColumn.length > 0) columns[columns.length - 1] = actionColumn;
      else columns = [...columns, actionColumn];
    }
  }

  const submitAudit = async (action: ActionType, selectedRows: any[]) => {
    const response = await props.submitAudit(moduleInfo.moduleId, selectedRows);
    if (response.Success) {
      message.success(response.message);
      action.reload();
    } else message.error(response.message);
  };

  const moreToolBarMenuClick = async (key: string, action: any) => {
    setMoreToolBarVisible(false);

    switch (key) {
      case "1":
        action.reload();
        break;
      case "ExportExcel":
        {
          message.success("后台处理中，处理完成将自动下载！");
          const filter = tableParam.filter;
          const { Success, Data } = await exportExcel(moduleCode, {}, filter);
          if (Success) downloadFile(Data, Data);
        }
        break;
      default:
        break;
    }
  };
  const showLogRecord = async (selectedRows: any) => {
    setRecordLogVisible(true);
    let { Data } = await getModuleLogInfo(moduleCode, selectedRows[0].ID);
    setRecordLogData(Data);
  };
  const showLogRecordCancel = () => {
    setRecordLogVisible(false);
    setRecordLogData(null);
  };
  const showDeleteConfirm = async (action: any, record: any) => {
    confirm({
      title: "是否确定删除记录?",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        const hideLoading = message.loading("数据提交中...", 0);
        try {
          if (props.delete) props.delete(record);
          else {
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
  const handleBatchOperation = async ({
    action,
    selectedRows,
    operationType,
    confirmTitle,
    apiFunc
  }: {
    action: any;
    selectedRows: any[];
    operationType: "delete" | "audit" | "revocation";
    confirmTitle: string;
    apiFunc: (params: any) => Promise<any>;
  }) => {
    confirm({
      title: confirmTitle,
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        const hideLoading = message.loading("数据提交中...", 0);
        try {
          if (operationType === "delete" && props.batchDelete) {
            const ids = selectedRows.map((item: any) => item.ID);
            await props.batchDelete(ids);
          } else {
            const { Success, Message } = await apiFunc({
              moduleCode,
              Ids: selectedRows.map((item: any) => item.ID),
              url
            });
            if (Success) {
              action.clearSelected();
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

  const batchDeleteConfirm = (action: any, selectedRows: any) =>
    handleBatchOperation({
      action,
      selectedRows,
      operationType: "delete",
      confirmTitle: "你确定需要批量删除所选数据吗？",
      apiFunc: batchDelete
    });
  const handlerToolBarVisibleChange = (flag: any) => setMoreToolBarVisible(flag);
  const onSearchVisible = () => {
    searchVisible = searchVisible ?? false;
    dispatch(setSearchVisible({ value: !searchVisible, moduleCode }));
  };

  const batchAuditConfirm = (action: any, selectedRows: any, selectedRowKeys: any) => {
    if (selectedRows.length === 0) {
      message.error("至少选中一条数据！");
      return;
    }
    confirm({
      title: selectedRows.length === 1 ? "你确定需要提交该数据吗？" : "你确定需要批量提交所选数据吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        const hideLoading = message.loading("数据提交中...", 0);
        try {
          const { Success, Message } = await batchAudit({ moduleCode, Ids: selectedRowKeys, url });
          if (Success) {
            action.clearSelected();
            action.reload();
            message.success(Message);
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
  const batchRevocationConfirm = (action: any, selectedRows: any, selectedRowKeys: any) => {
    if (selectedRows.length === 0) {
      message.error("至少选中一条数据！");
      return;
    }
    confirm({
      title: selectedRows.length === 1 ? "你确定需要撤销该数据吗？" : "你确定需要批量撤销所选数据吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        const hideLoading = message.loading("数据提交中...", 0);
        try {
          const { Success, Message } = await batchRevocation({ moduleCode, Ids: selectedRowKeys, url });
          if (Success) {
            action.clearSelected();
            action.reload();
            message.success(Message);
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
  const onOptionEdit = (record: any) => onEdit(record.ID, IsView);
  const onOptionView = (record: any) => onEdit(record.ID, true);
  const onOptionDelete = (action: { reload: any }, record: any) => {
    let { deleteConfirm } = props;
    if (deleteConfirm) deleteConfirm(action, record);
    else showDeleteConfirm(action, record);
  };
  const onOptionAdd = () => onEdit(null);

  const onReset = () => dispatch(setTableParam({ moduleCode }));

  const toolBarRender = (action: any, { selectedRows, selectedRowKeys }: any) => {
    const isModuleSuccess = moduleInfo?.Success === true;
    const hasSelectedRows = selectedRows?.length > 0;
    const hasSingleSelection = selectedRows?.length === 1;

    return [
      <Space style={{ display: "flex", justifyContent: "center" }}>
        {isModuleSuccess && !IsView && (
          <>
            {moduleInfo.actions.includes("Add") && (
              <Button type="primary" icon={<Icon name="PlusOutlined" />} onClick={onOptionAdd} disabled={isDetail && !masterId}>
                新建
              </Button>
            )}
            {moduleInfo.actions.includes("ImportExcel") && (
              <Button
                icon={<Icon name="excel-import" />}
                onClick={() => {
                  tableAction = action;
                  setUploadExcelVisible(true);
                }}
              >
                Excel导入
              </Button>
            )}
          </>
        )}
        {IsShowAudit && (
          <>
            {actionAuthButton.Audit && (
              <Button
                icon={<Icon name="CheckCircleOutlined" />}
                color="primary"
                onClick={() => batchAuditConfirm(action, selectedRows, selectedRowKeys)}
              >
                审核
              </Button>
            )}
            {actionAuthButton.Revocation && (
              <Button
                type="primary"
                icon={<Icon name="RollbackOutlined" />}
                danger
                onClick={() => batchRevocationConfirm(action, selectedRows, selectedRowKeys)}
              >
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
              onClick={() => props[item.FunctionCode](action, selectedRows, selectedRowKeys)}
            >
              {item.FunctionName}
            </Button>
          ))}
      </Space>,

      hasExportExcel && (
        <Dropdown
          onOpenChange={handlerToolBarVisibleChange}
          open={moreToolBarVisible}
          menu={{ items: getMoreToolBarItems(action) }}
        >
          <Button type="text" style={{ paddingLeft: 5, paddingRight: 5 }}>
            更多 <Icon name="DownOutlined" />
          </Button>
        </Dropdown>
      ),

      hasSingleSelection && (
        <Button onClick={() => showLogRecord(selectedRows)}>
          <Icon name="UnorderedListOutlined" /> 日志
        </Button>
      ),

      hasSelectedRows && (
        <Space style={{ display: "flex", justifyContent: "center" }}>
          {isModuleSuccess &&
            moduleInfo.hideMenu?.length > 0 &&
            moduleInfo.hideMenu.map((item: any) => (
              <Button key={item.ID} onClick={() => props[item.FunctionCode](action, selectedRows, selectedRowKeys)}>
                {item.Icon && <Icon name={item.Icon} />}
                {item.FunctionName}
              </Button>
            ))}
          {expendHideAction?.(action, selectedRows)}
          {isModuleSuccess && (
            <>
              {moduleInfo.actions.includes("Submit") && <Button onClick={() => submitAudit(action, selectedRows)}>提交</Button>}
              {moduleInfo.actions.includes("BatchDelete") && !IsView && (
                <Button onClick={() => batchDeleteConfirm(action, selectedRows)}>
                  <Icon name="DeleteOutlined" /> 批量删除
                </Button>
              )}
            </>
          )}
        </Space>
      ),

      expendAction?.(action, selectedRows, selectedRowKeys),

      <Button type="dashed" onClick={onSearchVisible} style={{ border: 0, padding: 0, boxShadow: "none" }}>
        <Tooltip placement="top" title="查询">
          <Icon name="SearchOutlined" className="font-size16" />
        </Tooltip>
      </Button>
    ];
  };

  const actionRef = useRef<ActionType>();

  const enhancedColumns = useMemo(() => {
    if (!columns) return [];

    return columns.map((item: any) => {
      const columnEnhancements: any = {};

      // 处理 ComboGrid 类型
      if (!item.hideInSearch && item.fieldType === "ComboGrid") {
        columnEnhancements.renderFormItem = () => <ComboGrid code={item.dataSource} />;
      }

      // 处理标签显示
      if (item.isTagDisplay === true) {
        columnEnhancements.render = (_: any, record: any) => {
          const valueEnum = item.valueEnum?.[record[item.dataIndex]];
          if (valueEnum) {
            return (
              <Tag
                color={valueEnum.tagColor}
                icon={valueEnum.tagIcon ? <Icon name={valueEnum.tagIcon} /> : null}
                variant={valueEnum.tagVariant}
              >
                {valueEnum.text}
              </Tag>
            );
          }
          return record[item.dataIndex];
        };
      }

      // 根据 valueType 处理不同类型
      switch (item.valueType) {
        case "icon":
          columnEnhancements.render = (_: any, record: any) => {
            return record[item.dataIndex] ? <Icon name={record[item.dataIndex]} /> : "";
          };
          break;

        case "switch":
          columnEnhancements.render = (_: any, record: any) => {
            return <Switch disabled checked={record[item.dataIndex] === "true"} />;
          };
          break;

        case "fontColor":
          if (item.isFollowThemeColor === true) {
            columnEnhancements.renderText = (val: string) => <span style={{ color: "var(--hooks-colorPrimary)" }}>{val}</span>;
          } else if (item.color) {
            columnEnhancements.renderText = (val: string) => <span style={{ color: item.color }}>{val}</span>;
          }
          break;
      }

      // 如果有任何增强，合并到原列配置中
      return Object.keys(columnEnhancements).length > 0 ? { ...item, ...columnEnhancements } : item;
    });
  }, [columns]);
  return (
    <>
      <ProTable
        rowKey="ID"
        tableAlertRender={false}
        columns={enhancedColumns}
        toolBarRender={toolBarRender}
        onRow={record => ({
          onDoubleClick: () => {
            // 忽略汇总行
            if (record.ID === "SumRowID" || !moduleInfo?.Success || IsView) return;

            // 检查是否有编辑权限（在 beforeActions 或 dropActions 中）
            const hasUpdatePermission =
              moduleInfo.beforeActions?.some((item: any) => item.id === "Update") ||
              moduleInfo.dropActions?.some((item: any) => item.id === "Update");

            if (hasUpdatePermission) {
              onOptionEdit(record);
            }
          }
        })}
        className="ant-pro-table-scroll"
        rowSelection={{
          fixed: "left",
          getCheckboxProps: record => ({
            disabled: record.ID === "SumRowID"
          })
        }}
        onReset={onReset}
        actionRef={actionRef}
        // bordered
        // cardBordered
        scroll={{ scrollToFirstRowOnChange: true, x: columns.length * 100, y: "100%" }}
        onLoad={() => {
          if (!formRef.current) return;

          if (currentParams) {
            formRef.current.setFieldsValue(currentParams);
          } else if (tableParam?.params) {
            formRef.current.setFieldsValue({ ...tableParam.params });
          }
        }}
        pagination={
          tableParam?.params ? { current: tableParam.params.current, pageSize: tableParam.params.pageSize } : pagination
        }
        request={async (params, sorter, _filterCondition) => {
          // 合并表格参数
          if (tableParam?.params && !params._timestamp) {
            params = { ...tableParam.params, ...params };
          }
          if (tableParam?.sorter) {
            sorter = { ...tableParam.sorter, ...sorter };
          }

          currentParams = params;

          // 构建过滤条件
          const filter = {
            PageIndex: params.current,
            PageSize: params.pageSize,
            sorter,
            params,
            Conditions: isDetail && masterColumn && masterId ? `A.${masterColumn} = '${masterId}'` : isDetail ? "1 != 1" : ""
          };

          dispatch(setTableParam({ params, sorter, moduleCode, filter }));

          // 处理明细表且无主表 ID 的情况
          if (isDetail && !masterId) {
            return { data: [], success: true, total: 0 };
          }

          // 加载数据
          return await queryByFilter(moduleCode, {}, filter);
        }}
        // columnsState={{
        //   persistenceKey: "use-pro-table-key",
        //   persistenceType: "localStorage"
        // }}
        search={searchVisible}
        columnsState={{
          //列设置-操作
          value: columnsStateMap, //列状态的值，支持受控模式
          onChange: handleOnChangeColumn //列状态的值发生改变之后触发
          // persistenceKey: 'user_columns_' + moduleCode, //持久化列的 key，用于判断是否是同一个 table,会存在缓存里去
          // persistenceType: 'localStorage' //持久化列的类类型， localStorage 设置在关闭浏览器后也是存在的，sessionStorage 关闭浏览器后会丢失
        }}
        // search={{ labelWidth: "auto" }}
        dateFormatter="string"
        columnEmptyText={"-"}
        {...props}
        // headerTitle="使用 ProTable"
      />
      {moduleInfo?.Success === true && (
        <>
          <Modal title="日志" open={recordLogVisible} width={1000} footer={null} onCancel={showLogRecordCancel}>
            <ModuleLog log={recordLogData} />
          </Modal>

          {moduleInfo.actions.includes("ImportExcel") && (
            <Modal
              destroyOnHidden
              title={`${moduleInfo.moduleName}-导入`}
              open={uploadExcelVisible}
              maskClosable={false}
              width={1000}
              footer={null}
              onCancel={() => setUploadExcelVisible(false)}
            >
              <UploadExcel
                moduleInfo={moduleInfo}
                onCancel={() => setUploadExcelVisible(false)}
                onReload={() => tableAction.current?.reload()}
              />
            </Modal>
          )}
        </>
      )}
    </>
  );
});

export default React.memo(SmProTable);
