import React, { useEffect, useState } from "react";
import { Modal, Tabs, Button, InputNumber } from "antd";
import { ProTable } from "@ant-design/pro-components";
import { query } from "@/api/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import { ModuleInfo } from "@/api/interface/index";
import { getModuleInfo as GetModuleInfo } from "@/api/modules/module";
import { setModuleInfo } from "@/redux/modules/module";

const { TabPane } = Tabs;

const moduleCode = "BD_MATERIAL_SELECT_MNG";
const moduleCode1 = "PO_IN_ORDER_WAIT_RETURN_MNG";
const QueryMaterial: React.FC<any> = props => {
  const dispatch = useDispatch();
  const { modalVisible, onCancel, onSubmit } = props;
  let [selectedRowKeys, setSelectedRowKeys] = useState<any>([]);
  let [QTYObject, setQTYObject] = useState<any>({});
  // let [PriceObject, setPriceObject] = useState<any>({});
  // let [CodeObject, setCodeObject] = useState<any>({});
  const [selectList, setSelectList] = useState<any>([]);
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  let moduleInfo1 = moduleInfos[moduleCode1] as ModuleInfo;
  let { columns } = moduleInfo ?? {};

  useEffect(() => {
    const getModuleInfo1 = async () => {
      let { Data } = await GetModuleInfo(moduleCode1);
      dispatch(setModuleInfo(Data));
    };
    const getModuleInfo = async () => {
      let { Data } = await GetModuleInfo(moduleCode);
      dispatch(setModuleInfo(Data));
    };
    if (!moduleInfo) getModuleInfo();
    if (!moduleInfo1) getModuleInfo1();
  }, []);
  const okHandle = async () => {
    let selectList1 = [...selectList];
    // let flag = -1;
    selectList1.forEach(record => {
      record.QTY = QTYObject[record.ID] ?? 1;
      // record.Price = PriceObject[record.ID] ?? 0;
      // record.CustomerMaterialCode = CodeObject[record.ID] ?? null;
      // if (!record.QTY || !record.Price) flag = 1;
    });

    // if (flag > -1) {
    //   message.error("请输入数量或单价！");
    //   return false;
    // }

    onSubmit(selectList1);

    setQTYObject({});
    // setPriceObject({});
    // setCodeObject({});
    setSelectedRowKeys([]);
    setSelectList([]);
    onCancel();
  };
  const onQTYChange = (value: any, record: any) => {
    let QTYObject1 = { ...QTYObject };
    if (value > 0) QTYObject1[record.ID] = value;
    setQTYObject(QTYObject1);
  };
  // const onPriceChange = (value: any, record: any) => {
  //   let PriceObject1 = { ...PriceObject };
  //   if (value > 0) PriceObject1[record.ID] = value;
  //   setPriceObject(PriceObject1);
  // };

  let hasColumns = [...(columns ?? [])];
  let hasColumns1 = [...((moduleInfo1 ? moduleInfo1.columns : []) ?? [])];
  hasColumns.push({
    title: "数量",
    dataIndex: "QTY",
    width: 160,
    render: (_text: any, record: any) => (
      <InputNumber size="small" value={QTYObject[record.ID] ?? 1} min="0" onChange={value => onQTYChange(value, record)} />
    )
  });
  // hasColumns.push({
  //   title: "单价",
  //   dataIndex: "Price",
  //   width: 160,
  //   render: (_text: any, record: any) => (
  //     <InputNumber
  //       size="small"
  //       value={PriceObject[record.ID] ?? 0}
  //       min="0"
  //       step={record.Step}
  //       onChange={value => onPriceChange(value, record)}
  //     />
  //   )
  // });
  hasColumns1.push({
    title: "数量",
    dataIndex: "QTY",
    width: 160,
    render: (_text: any, record: any) => (
      <InputNumber
        size="small"
        value={QTYObject[record.ID] ?? 1}
        min="0"
        max={record.QTY}
        onChange={value => onQTYChange(value, record)}
      />
    )
    // },
    // {
    //   title: "单价",
    //   dataIndex: "Price",
    //   width: 160,
    //   render: (_text: any, record: any) => (
    //     <InputNumber
    //       size="small"
    //       value={PriceObject[record.ID] ?? 0}
    //       min="0"
    //       step={record.Step}
    //       onChange={value => onPriceChange(value, record)}
    //     />
    //   )
  });

  const onTabChange = () => {
    setSelectedRowKeys([]);
    setSelectList([]);
  };
  return (
    <Modal
      destroyOnClose
      // title='选择物料'
      open={modalVisible}
      // onOk={this.okHandle}
      maskClosable={false}
      width={1100}
      closable={false}
      onCancel={() => onCancel()}
      // okButtonProps={{ disabled: selectList.length == 0 ? true : false }}
      footer={[
        <Button key="back" onClick={() => onCancel()}>
          取消
        </Button>,
        // <Button key="submit1" onClick={onMatchAll}>
        //   添加全部
        // </Button>,
        // <Button
        //   key="submit1"
        //   // onClick={() => {
        //   //   localStorage.setItem("tempRowId", "Y");
        //   //   window.open("/basedata/material");
        //   // }}
        // >
        //   新建物料
        // </Button>,
        <Button key="submit" disabled={selectList.length == 0 ? true : false} type="primary" onClick={okHandle}>
          确认 {selectList.length == 0 ? null : "(" + selectList.length + ")"}
        </Button>
      ]}
    >
      <Tabs defaultActiveKey="1" onChange={onTabChange}>
        <TabPane tab="物  料" key="1">
          <ProTable
            rowKey="ID"
            rowSelection={{
              fixed: "left",
              selectedRowKeys,
              onChange: (selectedRowKeys, selectedRows) => {
                setSelectedRowKeys(selectedRowKeys);
                setSelectList(selectedRows);
              }
            }}
            scroll={{ x: "max-content" }}
            columns={hasColumns}
            search={false}
            size="small"
            pagination={{
              pageSize: 10
            }}
            request={async (params, sorter, filterCondition) => {
              return await query({
                paramData: JSON.stringify(params),
                sorter: JSON.stringify(sorter),
                filter: JSON.stringify(filterCondition),
                moduleCode
              });
            }}
            options={{
              fullScreen: false,
              reload: true,
              setting: false,
              density: false,
              search: {
                name: "keyWord"
              }
            }}
          />
        </TabPane>
        <TabPane tab="请购单" key="2">
          <ProTable
            rowKey="ID"
            rowSelection={{
              fixed: "left",
              selectedRowKeys,
              onChange: (selectedRowKeys, selectedRows) => {
                setSelectedRowKeys(selectedRowKeys);
                setSelectList(selectedRows);
              }
            }}
            scroll={{ x: "max-content" }}
            columns={hasColumns1}
            search={false}
            size="small"
            pagination={{
              pageSize: 10
            }}
            request={async (params, sorter, filterCondition) => {
              return await query({
                paramData: JSON.stringify(params),
                sorter: JSON.stringify(sorter),
                filter: JSON.stringify(filterCondition),
                moduleCode: moduleCode1
              });
            }}
            options={{
              fullScreen: false,
              reload: true,
              setting: false,
              density: false,
              search: {
                name: "keyWord"
              }
            }}
          />
        </TabPane>
      </Tabs>
    </Modal>
  );
};

export default QueryMaterial;
