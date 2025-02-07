import React, { useEffect, useState } from "react";
import { Modal, Badge, Tabs, Button, InputNumber } from "antd";
// import type { ProColumns } from "@ant-design/pro-components";
import { ProTable } from "@ant-design/pro-components";
import { query } from "@/api/modules/module";
import { RootState, useSelector } from "@/redux";
import { ModuleInfo } from "@/api/interface/index";
import { getModuleInfo } from "@/api/modules/module";
import { useDispatch } from "@/redux";
import { setModuleInfo } from "@/redux/modules/module";
import { Icon } from "@/components";

const { TabPane } = Tabs;
const moduleCode = "SD_SALES_ORDER_WAIT_SHIP_MNG";
const moduleCode1 = "SD_SALES_ORDER_WAIT_OUT_MNG";
const moduleCode2 = "SD_SALES_SHIP_ORDER_WAIT_OUT_MNG";
const WaitShipSelect: React.FC<any> = props => {
  const dispatch = useDispatch();
  const { modalVisible, onCancel, onSubmit, waitShipSelectType, selectedRowIds } = props;
  let [selectedRowKeys, setSelectedRowKeys] = useState<any>([]);
  let [QTYObject, setQTYObject] = useState<any>({});
  const [selectList, setSelectList] = useState<any>([]);
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);

  let moduleInfo = moduleInfos[
    waitShipSelectType == "Ship" ? moduleCode : waitShipSelectType == "Out" ? moduleCode1 : moduleCode2
  ] as ModuleInfo;
  let { columns } = moduleInfo ?? {};

  useEffect(() => {
    const getModuleInfo2 = async () => {
      let { Data } = await getModuleInfo(moduleCode1);
      dispatch(setModuleInfo(Data));
    };
    const getModuleInfo3 = async () => {
      let { Data } = await getModuleInfo(moduleCode2);
      dispatch(setModuleInfo(Data));
    };
    const getModuleInfo1 = async () => {
      let { Data } = await getModuleInfo(moduleCode);
      dispatch(setModuleInfo(Data));
    };
    if (!moduleInfo) {
      getModuleInfo1();
      getModuleInfo2();
      getModuleInfo3();
    }
  }, []);

  // let columns: any = [];
  // if (moduleInfo && moduleInfo.columns) columns = [...moduleInfo.columns, actionColumn];
  // const onMatchAll = async () => {};
  const okHandle = async () => {
    let selectList1 = [...selectList];
    // let flag = -1;
    selectList1.forEach(record => {
      if (waitShipSelectType == "Ship") record.ShipQTY = QTYObject[record.ID] ?? 1;
      else record.OutQTY = QTYObject[record.ID] ?? 1;
      // if (!record.QTY || !record.Price) flag = 1;
    });

    // if (flag > -1) {
    //   message.error("请输入数量或单价！");
    //   return false;
    // }

    onSubmit(selectList1);

    setQTYObject({});
    setSelectedRowKeys([]);
    setSelectList([]);
    onCancel();
  };
  const onQTYChange = (value: any, record: any) => {
    let QTYObject1 = { ...QTYObject };
    QTYObject1[record.ID] = value;
    setQTYObject(QTYObject1);
  };
  // const onPriceChange = (value: any, record: any) => {
  //   let PriceObject1 = { ...PriceObject };
  //   PriceObject1[record.ID] = value;
  //   setPriceObject(PriceObject1);
  // };
  // const onCodeChange = (value: any, record: any) => {
  //   let CodeObject1 = { ...CodeObject };
  //   CodeObject1[record.ID] = value;
  //   setCodeObject(CodeObject1);
  // };
  const onDelList = (record: any) => {
    //let tempList = selectList;
    let tempList = [...selectList];
    const index = tempList.findIndex((item: any) => item.ID === record.ID);
    if (index > -1) {
      tempList.splice(index, 1);
      selectedRowKeys = [];
      tempList.forEach((element: any) => {
        selectedRowKeys.push(element.ID);
      });
      setSelectedRowKeys(selectedRowKeys);
      setSelectList(tempList);
    }
  };
  // const columns: ProColumns[] = [
  //   {
  //     title: "物料编号",
  //     dataIndex: "MaterialNo",
  //     width: 160
  //   },
  //   {
  //     title: "物料名称",
  //     dataIndex: "MaterialNames"
  //   },
  //   {
  //     title: "规格",
  //     dataIndex: "Specifications"
  //   },
  //   {
  //     title: "描述",
  //     dataIndex: "Description"
  //   },
  //   {
  //     title: "数量",
  //     dataIndex: "QTY",
  //     width: 160,
  //     render: (_text: any, record: any) => (
  //       <InputNumber size="small" value={QTYObject[record.ID] ?? 1} min="0" onChange={value => onQTYChange(value, record)} />
  //     )
  //   },
  //   {
  //     title: "单价",
  //     dataIndex: "Price",
  //     width: 160,
  //     render: (_text: any, record: any) => (
  //       <InputNumber
  //         size="small"
  //         value={PriceObject[record.ID] ?? 0}
  //         min="0"
  //         step={record.Step}
  //         onChange={value => onPriceChange(value, record)}
  //       />
  //     )
  //   },
  //   {
  //     title: "客户物料编码",
  //     dataIndex: "CustomerMaterialCode",
  //     width: 160,
  //     render: (_text: any, record: any) => (
  //       <Input size="small" value={CodeObject[record.ID] ?? null} onChange={e => onCodeChange(e.target.value, record)} />
  //     )
  //   }
  // ];

  // let hasColumns = [...columns];
  let hasColumns = [...(columns ?? [])];
  hasColumns.push(
    {
      title: waitShipSelectType == "Ship" ? "发货数量" : "出库数量",
      dataIndex: waitShipSelectType == "Ship" ? "ShipQTY" : "OutQTY",
      align: "center",
      width: 120,
      render: (_text, record) => (
        <InputNumber
          placeholder="请输入"
          value={QTYObject[record.ID] ?? 1}
          min={0}
          max={record.QTY}
          step={1}
          onChange={value => onQTYChange(value, record)}
        />
      )
    },
    {
      title: "操作",
      dataIndex: "option",
      fixed: "right",
      valueType: "option",
      width: 60,
      render: (_text, record) => <a onClick={() => onDelList(record)}>删除</a>
    }
  );
  return (
    <Modal
      destroyOnClose
      // title='选择物料'
      open={modalVisible}
      // onOk={this.okHandle}
      maskClosable={false}
      width={1100}
      closable={false}
      onCancel={() => {
        setQTYObject({});
        setSelectedRowKeys([]);
        setSelectList([]);
        onCancel();
      }}
      // okButtonProps={{ disabled: selectList.length == 0 ? true : false }}
      footer={[
        <Button
          key="back"
          onClick={() => {
            setQTYObject({});
            setSelectedRowKeys([]);
            setSelectList([]);
            onCancel();
          }}
        >
          取消
        </Button>,
        // <Button key="submit1" onClick={onMatchAll}>
        //   添加全部
        // </Button>,
        <Button key="submit" disabled={selectList.length == 0 ? true : false} type="primary" onClick={okHandle}>
          确认
        </Button>
      ]}
    >
      <Tabs defaultActiveKey="1">
        <TabPane tab="待选" key="1">
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
            columns={columns}
            search={false}
            size="small"
            pagination={{
              pageSize: 10
            }}
            request={async (params, sorter, filterCondition) => {
              filterCondition["SalesOrderId"] = selectedRowIds;
              return await query({
                paramData: JSON.stringify(params),
                sorter: JSON.stringify(sorter),
                filter: JSON.stringify(filterCondition),
                moduleCode: waitShipSelectType == "Ship" ? moduleCode : waitShipSelectType == "Out" ? moduleCode1 : moduleCode2
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
        <TabPane
          tab={
            selectList.length == 0 ? (
              "已选"
            ) : (
              <Badge count={selectList.length} style={{ right: "-10px" }}>
                已选
              </Badge>
            )
          }
          key="2"
        >
          {selectList.length == 0 ? (
            <div style={{ textAlign: "center", padding: 20 }}>
              <div style={{ fontSize: 20 }}>
                <Icon name="ExclamationOutlined" className=".font-size" />
              </div>
              <div>暂无物料</div>
            </div>
          ) : (
            <ProTable
              rowKey="ID"
              scroll={{ x: "max-content" }}
              columns={hasColumns}
              search={false}
              dataSource={selectList}
              pagination={{
                pageSize: 10
              }}
              size="small"
              options={{
                fullScreen: false,
                reload: true,
                setting: false,
                density: false
              }}
              toolBarRender={false}
            />
          )}
        </TabPane>
      </Tabs>
    </Modal>
  );
};

export default WaitShipSelect;
