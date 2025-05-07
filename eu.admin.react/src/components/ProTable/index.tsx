import React, { useRef, useState } from "react";
import { type MenuProps, Button, Menu, Dropdown, Tag, Space, Modal, Tooltip, Switch } from "antd";
import { pagination } from "@/config/proTable";
import { ProTable } from "@ant-design/pro-components";
import type { ActionType } from "@ant-design/pro-components";
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
  // const [moreToolBarVisible, setMoreToolBarVisible] = useState(false);
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
  let searchVisibles = useSelector((state: RootState) => state.module.searchVisibles);
  let tableParams = useSelector((state: RootState) => state.module.tableParams);
  let tableParam = tableParams[moduleCode] as any;
  let params1: any = null;
  let searchVisible = searchVisibles[moduleCode] ?? false;
  // let {
  //   columns,
  //   moduleInfo,
  //   IsView,
  //   request,
  //   onReset,
  //   onEdit,
  //   moduleInfo: { moduleCode, moduleId, beforeActions, url, openType, isDetail, UserModuleColumn },
  //   // smcommon: { searchVisibles },
  //   changePage,
  //   expendHideAction,
  //   expendAction,
  //   editPage,
  //   viewPage
  // } = props;
  const [columnsStateMap, setColumnsStateMap] = useState(() => {
    return UserModuleColumn;
  });

  const handleOnChangeColumn = async (map: any) => {
    await recordUserModuleColumn({ moduleId, map });
    setColumnsStateMap(map);
    moduleInfo = { ...moduleInfo, ...{ UserModuleColumn: map } };
    dispatch(setModuleInfo(moduleInfo));
  };

  // const FormPage = props.formPage;
  let moreToolBar = [];
  if (actions && moduleInfo.actions.includes("ExportExcel")) moreToolBar.push("ExportExcel");

  let actionAuthButton: { [key: string]: boolean } = {};
  actions?.forEach((item: any) => {
    actionAuthButton[item] = true;
  });

  let optionAuthButton: { [key: string]: boolean } = {};
  beforeActions?.forEach((item: any) => (optionAuthButton[item.id] = true));

  let dropActionAuthButton: { [key: string]: boolean } = {};
  dropActions?.forEach((item: any) => (dropActionAuthButton[item.id] = true));

  const getDropActions = (record: any, action: any) => {
    let dropActionItems: MenuProps["items"] = [];
    dropActions.map((item: ModuleInfoBeforeAction) => {
      if (item.id == "Update" && !IsView)
        dropActionItems.push({
          key: "dropActionUpdate",
          label: "修改",
          // icon: <HomeOutlined style={style} />,
          onClick: () => onOptionEdit(record)
        });
      else if (item.id == "View")
        dropActionItems.push({
          key: "dropActionView",
          label: "查看",
          icon: <Icon name="EyeOutlined" />,
          onClick: () => onOptionView(record)
        });
      else if (item.id == "Delete" && !IsView)
        dropActionItems.push({
          key: "dropActionDelete",
          label: "删除",
          icon: <Icon name="DeleteOutlined" />,
          onClick: () => onOptionDelete(action, record)
        });
      else {
        for (let i = 0; i < moduleInfo.actionData.length; i++) {
          let data = moduleInfo.actionData[i];
          if (item.id == data.ID) {
            dropActionItems.push({
              key: "dropAction" + item.id,
              label: data.FunctionName,
              icon: data.Icon ? (
                <i
                  style={{
                    marginInlineEnd: 8
                  }}
                >
                  <Icon name={data.Icon} />
                </i>
              ) : null,
              onClick: () => props[data.FunctionCode](record.ID, action, record)
            });
          }
        }
      }
    });
    return dropActionItems;
  };
  const actionColumn: any =
    moduleInfo && moduleInfo.Success == true && moduleInfo.actionCount > 0
      ? {
          title: "操作",
          dataIndex: "option",
          fixed: "left",
          valueType: "option",
          width: 100,
          align: "center",
          // render: (text, record, _, action) => component(text, record, _, action)

          render: (_: any, record: { ID: string }, index: number, action: any) => (
            <>
              {record.ID != "SumRowID" ? (
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
                </>
              ) : null}
              {dropActions.length > 0 && record.ID != "SumRowID" ? (
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
              ) : null}
            </>
          )
        }
      : [];
  if (columns) {
    let r = columns.filter(function (s: any) {
      return s.dataIndex == "option"; // 注意：IE9以下的版本没有trim()方法
    });
    if (actionColumn && actionColumn.dataIndex) {
      if (r.length > 0) columns[columns.length - 1] = actionColumn;
      else columns = [...columns, actionColumn];
    }
  }

  const submitAudit = async (action: ActionType, selectedRows: any[]) => {
    const response = await props.submitAudit(moduleInfo.moduleId, selectedRows);
    if (response.Success) {
      message.success(response.message);
      action.reload();
    } else {
      message.error(response.message);
    }
  };

  const moreToolBarMenuClick = async (e: any, action: any) => {
    setMoreToolBarVisible(false);
    // let { webUrl } = defaultSettings;

    switch (e.key) {
      case "1":
        action.reload();
        break;
      case "2":
        {
          message.success("后台处理中，处理完成将自动下载！");
          let filter = tableParam.filter;
          let { Success, Data } = await exportExcel(moduleCode, {}, filter);
          if (Success) downloadFile(Data, Data);
        }
        break;
      default:
        break;
    }
  };
  const showLogRecord = async (selectedRows: any) => {
    setRecordLogVisible(true);
    let { Data } = await getModuleLogInfo({ moduleCode, id: selectedRows[0].ID });
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
          if (props.delete) {
            props.delete(record);
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

  const batchDeleteConfirm = (action: any, selectedRows: any) => {
    handleBatchOperation({
      action,
      selectedRows,
      operationType: "delete",
      confirmTitle: "你确定需要批量删除所选数据吗？",
      apiFunc: batchDelete
    });
  };
  const handlerToolBarVisibleChange = (flag: any) => {
    setMoreToolBarVisible(flag);
  };
  const onSearchVisible = () => {
    searchVisible = searchVisible ?? false;
    dispatch(setSearchVisible({ value: !searchVisible, moduleCode }));
  };

  const batchAuditConfirm = (action: any, selectedRows: any, selectedRowKeys: any) => {
    if (selectedRows.length == 0) {
      message.error("至少选中一条数据！");
      return;
    }
    confirm({
      title: selectedRows.length == 1 ? "你确定需要提交该数据吗？" : "你确定需要批量提交所选数据吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        // let ids: string[] = [];
        // selectedRows.map((item: any) => {
        //   ids.push(item.ID);
        // });

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
    if (selectedRows.length == 0) {
      message.error("至少选中一条数据！");
      return;
    }
    confirm({
      title: selectedRows.length == 1 ? "你确定需要撤销该数据吗？" : "你确定需要批量撤销所选数据吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        // let ids: string[] = [];
        // selectedRows.map((item: any) => {
        //   ids.push(item.ID);
        // });

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

  const onReset = () => {
    dispatch(setTableParam({ moduleCode }));
  };
  const toolBarRender = (action: any, { selectedRows, selectedRowKeys }: any) => [
    <Space style={{ display: "flex", justifyContent: "center" }}>
      {moduleInfo && moduleInfo.Success == true && moduleInfo.actions.includes("Add") && !IsView ? (
        <Button
          type="primary"
          icon={<Icon name="PlusOutlined" />}
          onClick={onOptionAdd}
          disabled={isDetail && !masterId ? true : false}
        >
          新建
        </Button>
      ) : null}
      {moduleInfo && moduleInfo.Success == true && moduleInfo.actions.includes("ImportExcel") && !IsView ? (
        <Button
          icon={<Icon name="excel-import" />}
          onClick={() => {
            tableAction = action;
            setUploadExcelVisible(true);
          }}
        >
          Excel导入
        </Button>
      ) : null}

      {actionAuthButton.Audit && IsShowAudit && (
        <Button
          icon={<Icon name="CheckCircleOutlined" />}
          color="primary"
          // variant="outlined"
          // disabled={selectedRowKeys.length > 0 ? false : true}
          onClick={() => batchAuditConfirm(action, selectedRows, selectedRowKeys)}
        >
          审核
        </Button>
      )}
      {actionAuthButton.Revocation && IsShowAudit && (
        <Button
          type="primary"
          icon={<Icon name="RollbackOutlined" />}
          danger
          onClick={() => batchRevocationConfirm(action, selectedRows, selectedRowKeys)}
        >
          撤销
        </Button>
      )}
      {moduleInfo && moduleInfo.Success == true && moduleInfo.menuData.length > 0 ? (
        <>
          {moduleInfo.menuData.map((item: any) => {
            return (
              <Button
                key={item.ID}
                icon={item.Icon ? <Icon name={item.Icon} /> : null}
                onClick={() => {
                  props[item.FunctionCode](action, selectedRows, selectedRowKeys);
                }}
              >
                {item.FunctionName}
              </Button>
            );
          })}
        </>
      ) : null}
      <Button type="dashed" onClick={onSearchVisible} style={{ border: 0, padding: 0 }}>
        <Tooltip placement="top" title="查询">
          <Icon name="SearchOutlined" className="font-size16" />
        </Tooltip>
      </Button>
    </Space>,
    moreToolBar.length > 0 && (
      <Dropdown
        onVisibleChange={handlerToolBarVisibleChange}
        open={moreToolBarVisible}
        overlay={
          <>
            <Menu
              onClick={e => {
                moreToolBarMenuClick(e, action);
              }}
            >
              {/* <Menu.Item key="1" icon={global.utility.getIcon('RedoOutlined')}>刷新</Menu.Item> */}
              {moreToolBar.includes("ExportExcel") ? (
                <Menu.Item key="2" icon={<Icon name="excel-export" />} style={{ gap: 5 }}>
                  导出Excel
                </Menu.Item>
              ) : null}
            </Menu>
          </>
        }
      >
        <Button type="text">
          更多 <Icon name="DownOutlined" />
        </Button>
      </Dropdown>
    ),
    selectedRows && selectedRows.length == 1 && (
      <Button onClick={() => showLogRecord(selectedRows)}>
        <Icon name="UnorderedListOutlined" /> 日志
      </Button>
    ),
    // selectedRows && selectedRows.length == 1 && selectedRows[0].AuditStatus == "Add" && (
    //   <Button onClick={() => showLogRecord(selectedRows)}>审核</Button>
    // ),
    // selectedRows && selectedRows.length == 1 && selectedRows[0].AuditStatus == "CompleteAudit" && (
    //   <Button danger onClick={() => showLogRecord(selectedRows)}>
    //     撤销
    //   </Button>
    // ),

    selectedRows && selectedRows.length > 0 && (
      <Space style={{ display: "flex", justifyContent: "center" }}>
        {moduleInfo && moduleInfo.Success == true && moduleInfo.hideMenu.length > 0 ? (
          <>
            {moduleInfo.hideMenu.map((item: any) => {
              return (
                <Button
                  key={item.ID}
                  onClick={() => {
                    props[item.FunctionCode](action, selectedRows, selectedRowKeys);
                  }}
                >
                  {item.Icon ? <Icon name={item.Icon} /> : null}
                  {item.FunctionName}
                </Button>
              );
            })}
          </>
        ) : null}
        {expendHideAction ? expendHideAction(action, selectedRows) : null}
        {moduleInfo && moduleInfo.Success == true && moduleInfo.actions.includes("Submit") ? (
          <Button onClick={() => submitAudit(action, selectedRows)}>提交</Button>
        ) : null}
        {moduleInfo && moduleInfo.Success == true && moduleInfo.actions.includes("BatchDelete") && !IsView ? (
          <Button onClick={() => batchDeleteConfirm(action, selectedRows)}>
            <Icon name="DeleteOutlined" /> 批量删除
          </Button>
        ) : null}
      </Space>
    ),
    expendAction ? expendAction(action, selectedRows, selectedRowKeys) : null
  ];

  const actionRef = useRef<ActionType>();
  let columns1 = [...columns];
  columns1 &&
    columns1.map((item: any, index: any) => {
      let hasChange = false;
      let column = columns1[index];
      if (!item.hideInSearch && item.fieldType == "ComboGrid") {
        let renderFormItem = () => <ComboGrid code={item.dataSource} />;
        column = { ...column, renderFormItem };
        hasChange = true;
      }

      if (item.isTagDisplay === true) {
        // let render = (_: any, record: any) => {
        let render = (_: any, record: any) => {
          let valueEnum = item.valueEnum[record[item.dataIndex]];
          if (valueEnum)
            return (
              <Tag color={valueEnum.tagColor} bordered={valueEnum.tagBordered}>
                {valueEnum.text}
              </Tag>
            );
          else return record[item.dataIndex];
        };
        column = { ...column, render };
        hasChange = true;
      }

      switch (item.valueType) {
        case "icon":
          // eslint-disable-next-line no-case-declarations
          let render1 = (_: any, record: any) => {
            if (record[item.dataIndex]) return <Icon name={record[item.dataIndex]} />;
            else return "";
          };
          column = { ...column, render: render1 };
          hasChange = true;
          break;
        case "switch":
          // eslint-disable-next-line no-case-declarations
          let render = (_: any, record: any) => {
            return <Switch disabled checked={record[item.dataIndex] === "true" ? true : false} />;
          };
          column = { ...column, render };
          hasChange = true;
          break;
        case "fontColor":
          if (item.isFollowThemeColor === true) {
            let renderText = (val: string) => {
              return <span style={{ color: "var(--hooks-colorPrimary)" }}>{val}</span>;
            };
            column = { ...column, renderText };
            hasChange = true;
          } else if (item.color) {
            let renderText = (val: string) => {
              return <span style={{ color: item.color }}>{val}</span>;
            };
            column = { ...column, renderText };
            hasChange = true;
          }
          break;
      }
      if (hasChange == true) columns1[index] = { ...columns1[index], ...column };
    });
  return (
    <>
      <ProTable
        rowKey="ID"
        tableAlertRender={false}
        columns={columns1}
        toolBarRender={toolBarRender}
        onRow={record => {
          return {
            onDoubleClick: () => {
              if (moduleInfo && moduleInfo.Success == true && record.ID != "SumRowID") {
                let actions = moduleInfo.beforeActions;
                let index = actions.findIndex((item: any) => item.id === "Update");
                if (index <= -1) actions = moduleInfo.dropActions;
                index = actions.findIndex((item: any) => item.id === "Update");
                if (index > -1 && !IsView) onOptionEdit(record);
              }
            }
          };
        }}
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
          if (params1 && formRef.current) formRef.current.setFieldsValue(params1);
          else if (tableParam && tableParam.params && formRef.current) formRef.current.setFieldsValue({ ...tableParam.params });
        }}
        pagination={
          tableParam && tableParam.params
            ? { current: tableParam.params.current, pageSize: tableParam.params.pageSize }
            : pagination
        }
        // eslint-disable-next-line @typescript-eslint/no-unused-vars
        request={async (params, sorter, _filterCondition) => {
          if (tableParam && tableParam.params && !params._timestamp) params = { ...tableParam.params, ...params };
          if (tableParam && tableParam.sorter) sorter = { ...tableParam.sorter, ...sorter };
          params1 = params;
          let filter = { PageIndex: params.current, PageSize: params.pageSize, sorter, params, Conditions: "" };
          dispatch(setTableParam({ params: params, sorter, moduleCode, filter }));

          if (isDetail) {
            if (masterColumn && masterId) filter = { ...filter, Conditions: `A.${masterColumn} = '${masterId}'` };
            else filter = { ...filter, Conditions: "1 != 1" };
            // filterCondition[masterColumn] = masterId;
            if (masterId) return await queryByFilter(moduleCode, {}, filter);
            else
              return {
                data: [],
                success: true,
                total: 0
              };
          } else return await queryByFilter(moduleCode, {}, filter);
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
      {moduleInfo && moduleInfo.Success == true ? (
        <Modal title="日志" open={recordLogVisible} width={1000} footer={null} onCancel={showLogRecordCancel}>
          <ModuleLog log={recordLogData} />
        </Modal>
      ) : null}
      {moduleInfo && moduleInfo.Success == true && moduleInfo.actions.includes("ImportExcel") ? (
        <Modal
          destroyOnClose
          title={`${moduleInfo.moduleName}-导入`}
          open={uploadExcelVisible}
          maskClosable={false}
          width={1000}
          onCancel={() => {
            setUploadExcelVisible(false);
          }}
          footer={null}
        >
          <UploadExcel
            moduleInfo={moduleInfo}
            onCancel={() => setUploadExcelVisible(false)}
            onReload={() => {
              tableAction.current?.reload();
            }}
          />
        </Modal>
      ) : null}
    </>
  );
});

export default SmProTable;
