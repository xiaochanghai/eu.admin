import React, { useEffect, useState, useMemo, useRef } from "react";
import { Popconfirm, Button, Modal, Space } from "antd";
import { EditableProTable } from "@ant-design/pro-components";
import { getModuleLogInfo, getModuleInfo, recordUserModuleColumn } from "@/api/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import { ModuleInfo, RecordLogData } from "@/api/interface/index";
import http from "@/api";
import { pagination1 } from "@/config/proTable";
import { queryByFilter } from "@/api/modules/module";
import { message } from "@/hooks/useMessage";
import { UploadExcel, ModuleLog, Icon, Loading } from "@/components";
import { shouldResetToFirstPage } from "@/components/ProTable/utils";
import { setTableParam, setModuleInfo } from "@/redux/modules/module";
import { ModifyType } from "@/typings";

const Index: React.FC<any> = React.memo(props => {
  const tableActionRef = useRef<any>(null);
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const tableParams = useSelector((state: RootState) => state.module.tableParams);

  const {
    moduleCode,
    IsView,
    modifyType,
    masterId,
    tableRef,
    addCallBack,
    editCallBack,
    successCallBack,
    failCallBack,
    formRef
  } = props;

  const moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  const { masterColumn, isDetail, url, actions, UserModuleColumn, moduleId } = moduleInfo || {};
  const tableParam = tableParams[moduleCode] as any;

  let currentParams: any = null;
  const [editableKeys, setEditableRowKeys] = useState<React.Key[]>([]);
  const [dataSource, setDataSource] = useState<any>([]);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [recordLogVisible, setRecordLogVisible] = useState(false);
  const [recordLogData, setRecordLogData] = useState<RecordLogData | null>(null);
  const [uploadExcelVisible, setUploadExcelVisible] = useState(false);
  const [columnsStateMap, setColumnsStateMap] = useState(() => UserModuleColumn);

  useEffect(() => {
    if (!moduleInfo) getModuleInfo1();
    setIsLoading(false);
  }, []);

  const getModuleInfo1 = async () => {
    const { Data } = await getModuleInfo(moduleCode);
    dispatch(setModuleInfo(Data));
  };

  const handleOnChangeColumn = async (map: any) => {
    if (moduleId) {
      await recordUserModuleColumn({ moduleId, map });
      setColumnsStateMap(map);
      const updatedModuleInfo = { ...moduleInfo, UserModuleColumn: map };
      dispatch(setModuleInfo(updatedModuleInfo));
    }
  };

  const onSelectChange = (keys: React.Key[]) => setSelectedRowKeys(keys);

  // 权限管理
  const actionAuthButton: { [key: string]: boolean } = {};
  actions?.forEach((item: any) => {
    actionAuthButton[item] = true;
  });
  // 操作列配置
  const actionColumn = {
    title: "操作",
    dataIndex: "option",
    fixed: "right",
    valueType: "option",
    width: 150,
    render: (_text: any, record: any, _: any, action: any) => [
      <a
        key="editable"
        onClick={() => {
          if (editableKeys.length > 0) action?.saveEditable?.(editableKeys[0]);
          action?.startEditable?.(record.ID);
          if (editCallBack) editCallBack();
        }}
      >
        编辑
      </a>,
      <Popconfirm
        key="delete"
        title="提醒"
        description="是否确定删除记录?"
        onConfirm={async () => {
          const { Success, Message } = await http.delete<any>(`${url}/${record.ID}`);
          if (Success) {
            message.success(Message);
            // 仅当删除后当前页变空且不在第一页时回退到第一页，否则原地刷新
            if (tableRef?.current) tableRef.current.reload(shouldResetToFirstPage(tableRef.current.pageInfo, 1));
          }
        }}
        okType="danger"
        okText="确定"
        cancelText="取消"
      >
        <a>删除</a>
      </Popconfirm>
    ]
  };

  // 使用 useMemo 优化列配置
  const columns = useMemo(() => {
    if (!moduleInfo?.columns) return [];

    const baseColumns = [...moduleInfo.columns];
    if (modifyType === ModifyType.Edit) {
      return [...baseColumns, actionColumn];
    }
    return baseColumns;
  }, [moduleInfo?.columns, modifyType]);

  const showLogRecord = async (selectedRows: any[]) => {
    setRecordLogVisible(true);
    const { Data } = await getModuleLogInfo(moduleCode, selectedRows[0].ID);
    setRecordLogData(Data);
  };

  const showLogRecordCancel = () => {
    setRecordLogVisible(false);
    setRecordLogData(null);
  };

  const onReset = () => {
    dispatch(setTableParam({ moduleCode }));
  };

  const toolBarRender = (action: any, { selectedRows, selectedRowKeys }: any) => {
    const isModuleSuccess = moduleInfo?.Success === true;
    const hasSelectedRows = selectedRows?.length > 0;
    const hasSingleSelection = selectedRows?.length === 1;

    return [
      <Space key="toolbar-main" style={{ display: "flex", justifyContent: "center" }}>
        {addCallBack && (
          <Button
            disabled={modifyType !== ModifyType.Edit}
            type="primary"
            icon={<Icon name="PlusOutlined" />}
            onClick={() => {
              if (!masterId) {
                message.error("请先保存主表数据！");
                return;
              }
              addCallBack();
            }}
          >
            添加
          </Button>
        )}
        {actionAuthButton.ImportExcel && !IsView && (
          <Button
            icon={<Icon name="excel-import" />}
            onClick={() => {
              tableActionRef.current = action;
              setUploadExcelVisible(true);
            }}
          >
            Excel导入
          </Button>
        )}
        {isModuleSuccess &&
          moduleInfo.menuData?.length > 0 &&
          moduleInfo.menuData.map((item: any) => (
            <Button
              key={item.ID}
              icon={item.Icon ? <Icon name={item.Icon} /> : null}
              onClick={() => props[item.FunctionCode](action, selectedRows, selectedRowKeys)}
            >
              {item.FunctionName}
            </Button>
          ))}
      </Space>,

      hasSingleSelection && (
        <Button key="log" onClick={() => showLogRecord(selectedRows)}>
          <Icon name="UnorderedListOutlined" /> 日志
        </Button>
      ),

      hasSelectedRows && (
        <Space key="toolbar-selected" style={{ display: "flex", justifyContent: "center" }}>
          {isModuleSuccess &&
            moduleInfo.hideMenu?.length > 0 &&
            moduleInfo.hideMenu.map((item: any) => (
              <Button key={item.ID} onClick={() => props[item.FunctionCode](action, selectedRows, selectedRowKeys)}>
                {item.Icon ? <Icon name={item.Icon} /> : null}
                {item.FunctionName}
              </Button>
            ))}
        </Space>
      )
    ];
  };

  return (
    <>
      {isLoading ? (
        <Loading />
      ) : (
        <>
          {moduleInfo && columns.length > 0 && (
            <>
              <EditableProTable
                rowKey="ID"
                className="ant-pro-table-scroll"
                scroll={{ scrollToFirstRowOnChange: true, x: columns.length * 100, y: "100%" }}
                recordCreatorProps={false}
                rowSelection={{
                  alwaysShowAlert: true,
                  onChange: onSelectChange,
                  selectedRowKeys
                }}
                actionRef={tableRef}
                toolBarRender={toolBarRender}
                tableAlertRender={false}
                tableAlertOptionRender={false}
                columns={columns}
                onReset={onReset}
                onLoad={() => {
                  if (!formRef?.current) return;

                  if (currentParams) {
                    formRef.current.setFieldsValue(currentParams);
                  } else if (tableParam?.params) {
                    formRef.current.setFieldsValue({ ...tableParam.params });
                  }
                }}
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
                  let filter = {
                    PageIndex: params.current,
                    PageSize: params.pageSize,
                    sorter,
                    params,
                    Conditions: ""
                  };

                  if (isDetail) {
                    if (masterColumn && masterId) {
                      filter = { ...filter, Conditions: `A.${masterColumn} = '${masterId}'` };
                    } else {
                      filter = { ...filter, Conditions: "1 != 1" };
                    }
                  }

                  dispatch(setTableParam({ params, sorter, moduleCode, filter }));

                  // 处理明细表且无主表 ID 的情况
                  if (isDetail && !masterId) {
                    return { data: [], success: true, total: 0 };
                  }

                  // 加载数据
                  return await queryByFilter(moduleCode, {}, filter);
                }}
                pagination={
                  tableParam?.params
                    ? { ...pagination1, current: tableParam.params.current, pageSize: tableParam.params.pageSize }
                    : pagination1
                }
                options={{
                  search: true,
                  fullScreen: true,
                  reload: true,
                  setting: true,
                  density: true
                }}
                value={dataSource}
                onChange={setDataSource}
                editable={{
                  type: "multiple",
                  editableKeys,
                  onSave: async (rowKey, data: any, _row) => {
                    const params = { ...data, ModuleCode: moduleCode, masterId };
                    const { Success, Data } = await http.put<any>(`${url}/UpdateReturn/${rowKey}`, params);
                    if (Success) {
                      if (successCallBack) data = successCallBack(data, Data);
                    } else if (failCallBack) {
                      data = failCallBack();
                    }
                  },
                  onChange: setEditableRowKeys
                }}
                columnsState={{
                  value: columnsStateMap,
                  onChange: handleOnChangeColumn
                }}
                {...props}
              />

              {moduleInfo?.Success === true && (
                <>
                  <Modal title="日志" open={recordLogVisible} width={1000} footer={null} onCancel={showLogRecordCancel}>
                    <ModuleLog log={recordLogData} />
                  </Modal>

                  {actionAuthButton.ImportExcel && (
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
                        onReload={() => tableActionRef.current?.reload()}
                      />
                    </Modal>
                  )}
                </>
              )}
            </>
          )}
        </>
      )}
    </>
  );
});

export default Index;
