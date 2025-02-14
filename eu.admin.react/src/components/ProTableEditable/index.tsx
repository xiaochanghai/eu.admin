/* eslint-disable @typescript-eslint/no-unused-vars */
import React, { useEffect, useState } from "react";
import { Popconfirm, Button, Modal, Space } from "antd";
import { EditableProTable } from "@ant-design/pro-components";
import { getModuleLogInfo, getModuleInfo } from "@/api/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import { ModuleInfo, ModifyType, RecordLogData } from "@/api/interface/index";
import { setModuleInfo } from "@/redux/modules/module";
import http from "@/api";
import { pagination1 } from "@/config/proTable";
import { query } from "@/api/modules/module";
import { message } from "@/hooks/useMessage";
import { UploadExcel, ModuleLog, Icon, Loading } from "@/components";

const Index: React.FC<any> = props => {
  let tableAction: any;
  const dispatch = useDispatch();
  const [isLoading, setIsLoading] = useState(true);
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  let { moduleCode, IsView, modifyType, masterId, tableRef, addCallBack, editCallBack, successCallBack, failCallBack } = props;
  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  let { masterColumn, isDetail, url, actions } = moduleInfo || {};

  const [editableKeys, setEditableRowKeys] = useState<React.Key[]>([]);
  const [dataSource, setDataSource] = useState<any>([]);
  const [selectedRowKeys, setSelectedRowKeys] = React.useState([]);
  const [recordLogVisible, setRecordLogVisible] = useState(false);
  const [recordLogData, setRecordLogData] = useState<RecordLogData | null>(null);
  const [uploadExcelVisible, setUploadExcelVisible] = useState(false);

  useEffect(() => {
    if (!moduleInfo) getModuleInfo1();
    setIsLoading(false);
  }, []);
  const getModuleInfo1 = async () => {
    let { Data } = await getModuleInfo(moduleCode);
    dispatch(setModuleInfo(Data));
  };
  // 定义选择行的变化时的回调函数
  // const onSelectChange = (keys: any, rows: any) => {
  const onSelectChange = (keys: any) => setSelectedRowKeys(keys);

  let actionAuthButton: { [key: string]: boolean } = {};
  actions?.forEach((item: any) => {
    actionAuthButton[item] = true;
  });
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
        title="提醒"
        description="是否确定删除记录?"
        onConfirm={async () => {
          let { Success, Message } = await http.delete<any>(`${url}/${record.ID}`);
          if (Success) message.success(Message);
          if (tableRef.current) tableRef.current.reload();
        }}
        okType="danger"
        okText="确定1"
        cancelText="取消2"
      >
        <a key="delete">删除1</a>
      </Popconfirm>
    ]
  };
  let columns: any = [];
  if (modifyType == ModifyType.Edit) {
    if (moduleInfo && moduleInfo.columns) columns = [...moduleInfo.columns, actionColumn];
  } else if (moduleInfo && moduleInfo.columns) columns = [...moduleInfo.columns];

  const showLogRecord = async (selectedRows: any) => {
    setRecordLogVisible(true);
    let { Data } = await getModuleLogInfo({ moduleCode, id: selectedRows[0].ID });
    setRecordLogData(Data);
  };
  const showLogRecordCancel = () => {
    setRecordLogVisible(false);
    setRecordLogData(null);
  };

  const toolBarRender = (action: any, { selectedRows, selectedRowKeys }: any) => [
    <Space style={{ display: "flex", justifyContent: "center" }}>
      {addCallBack ? (
        <Button
          disabled={modifyType == ModifyType.Edit ? false : true}
          type="primary"
          icon={<Icon name="PlusOutlined" />}
          onClick={() => {
            if (!masterId) {
              message.error("请先保存主表数据！");
              return;
            }
            if (addCallBack) addCallBack();
          }}
        >
          添加
        </Button>
      ) : null}
      {actionAuthButton.ImportExcel && !IsView ? (
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
    </Space>,
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
      </Space>
    )
  ];

  return (
    <>
      {isLoading ? (
        <Loading />
      ) : (
        <>
          {moduleInfo && columns ? (
            <>
              <EditableProTable
                rowKey="ID"
                className="ant-pro-table-scroll"
                // scroll={{ x: 1000, y: "100%" }}
                scroll={{ scrollToFirstRowOnChange: true, x: columns.length * 100, y: "100%" }}
                recordCreatorProps={false}
                rowSelection={{
                  alwaysShowAlert: true,
                  onChange: onSelectChange,
                  selectedRowKeys
                  // onSelect, //用户手动选择/取消选择某行的回调
                  //   onSelectMultiple: onMulSelect, //用户使用键盘 shift 选择多行的回调
                  //   onSelectAll: onMulSelect, //用户手动选择/取消选择所有行的回调
                }}
                actionRef={tableRef}
                // tableAlertRender={({ selectedRowKeys, selectedRows, onCleanSelected }) => {

                // toolBarRender={() => [
                //   <Space size={0}>
                //     <Button
                //       disabled={modifyType == ModifyType.Edit ? false : true}
                //       onClick={() => {
                //         if (!id) {
                //           message.error("请先保存主表数据！");
                //           return;
                //         }
                //         setMaterialQueryVisible(true);
                //       }}
                //     >
                //       添加
                //     </Button>
                //   </Space>
                // ]}
                // toolBarRender={() => [
                //   <Space style={{ display: "flex", justifyContent: "center" }}>
                //     {addCallBack ? (
                //       <Button
                //         disabled={modifyType == ModifyType.Edit ? false : true}
                //         type="primary"
                //         icon={<Icon name="PlusOutlined" />}
                //         onClick={() => {
                //           if (!masterId) {
                //             message.error("请先保存主表数据！");
                //             return;
                //           }
                //           if (addCallBack) addCallBack();
                //         }}
                //       >
                //         添加
                //       </Button>
                //     ) : null}
                //   </Space>
                // ]}
                toolBarRender={toolBarRender}
                tableAlertRender={false}
                tableAlertOptionRender={false}
                columns={columns}
                request={async (_params, sorter, filterCondition) => {
                  if (isDetail) {
                    if (masterId) {
                      if (masterColumn) filterCondition = { ...filterCondition, [masterColumn]: masterId };
                      return await query({
                        paramData: JSON.stringify(_params),
                        sorter: JSON.stringify(sorter),
                        filter: JSON.stringify(filterCondition),
                        moduleCode
                      });
                    } else
                      return {
                        data: [],
                        success: true,
                        total: 0
                      };
                  } else {
                    return await query({
                      paramData: JSON.stringify(_params),
                      sorter: JSON.stringify(sorter),
                      filter: JSON.stringify(filterCondition),
                      moduleCode
                    });
                  }
                }}
                pagination={pagination1}
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
                    let params = { ...data, ModuleCode: moduleCode, masterId };
                    let { Success, Data } = await http.put<any>(`${url}/UpdateReturn/${rowKey}`, params);
                    if (Success) {
                      if (successCallBack) data = successCallBack(data, Data);
                    } else if (failCallBack) data = failCallBack();
                  },
                  onChange: setEditableRowKeys
                }}
                {...props}
              />
              {moduleInfo && moduleInfo.Success == true ? (
                <Modal title="日志" open={recordLogVisible} width={1000} footer={null} onCancel={showLogRecordCancel}>
                  <ModuleLog log={recordLogData} />
                </Modal>
              ) : null}
              {actionAuthButton.ImportExcel ? (
                <Modal
                  destroyOnClose
                  title={moduleInfo.moduleName + "-导入"}
                  open={uploadExcelVisible}
                  maskClosable={false}
                  width={1000}
                  onCancel={() => {
                    setUploadExcelVisible(false);
                  }}
                  footer={[
                    <Button
                      key="back"
                      onClick={() => {
                        setUploadExcelVisible(false);
                      }}
                    >
                      关闭
                    </Button>
                  ]}
                >
                  <UploadExcel
                    moduleInfo={moduleInfo}
                    onCancel={() => setUploadExcelVisible(false)}
                    onReload={() => {
                      tableAction?.reload();
                    }}
                  />
                </Modal>
              ) : null}
            </>
          ) : null}
        </>
      )}
    </>
  );
};

export default Index;
