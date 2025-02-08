import React, { useState, useEffect } from "react";
import { Modal, Badge, Tabs, Button, InputNumber, Input } from "antd";
import type { ProColumns } from "@ant-design/pro-components";
import { ProTable } from "@ant-design/pro-components";
import { query } from "@/api/modules/module";
import { RootState, useSelector, useDispatch } from "@/redux";
import { ModuleInfo } from "@/api/interface/index";
import { getModuleInfo as GetModuleInfo } from "@/api/modules/module";
import { setModuleInfo } from "@/redux/modules/module";
import { useNavigate } from "react-router-dom";
import { Icon } from "@/components";

const { TabPane } = Tabs;
const QueryMaterial: React.FC<any> = props => {
  const dispatch = useDispatch();
  const navigate = useNavigate();
  const moduleCode = "BD_MATERIAL_SELECT_MNG";
  const { modalVisible, onCancel, onSubmit, ignoreColumns } = props;
  let [selectedRowKeys, setSelectedRowKeys] = useState<any>([]);
  let [QTYObject, setQTYObject] = useState<any>({});
  let [PriceObject, setPriceObject] = useState<any>({});
  let [CodeObject, setCodeObject] = useState<any>({});
  const [selectList, setSelectList] = useState<any>([]);
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  let { columns } = moduleInfo ?? {};
  columns = columns ?? [];
  useEffect(() => {
    const getModuleInfo = async () => {
      let { Data } = await GetModuleInfo(moduleCode);
      dispatch(setModuleInfo(Data));
    };
    if (!moduleInfo) getModuleInfo();
  }, []);
  const onMatchAll = async () => {
    let params = {
      current: 1,
      pageSize: 100000
    };
    let result = await query({
      paramData: JSON.stringify(params),
      moduleCode
    });
    let tempList: any[] = [];
    let tempSelectedRowKeys: any[] = [];
    result.data.forEach((record: any) => {
      tempSelectedRowKeys.push(record.ID);
      tempList.push(record);
    });

    setSelectedRowKeys(tempSelectedRowKeys);
    setSelectList(tempList);
  };
  const okHandle = async () => {
    let selectList1 = [...selectList];
    // let flag = -1;
    selectList1.forEach(record => {
      record.MaterialId = record.ID;
      record.MaterialName = record.MaterialNames;
      record.MaterialSpecifications = record.Specifications;
      record.MaterialUnitId = record.UnitId;
      record.QTY = QTYObject[record.ID] ?? 1;
      record.Price = PriceObject[record.ID] ?? 0;
      record.CustomerMaterialCode = CodeObject[record.ID] ?? null;
      // if (!record.QTY || !record.Price) flag = 1;
    });

    // if (flag > -1) {
    //   message.error("请输入数量或单价！");
    //   return false;
    // }

    onSubmit(selectList1);

    setQTYObject({});
    setPriceObject({});
    setCodeObject({});
    setSelectedRowKeys([]);
    setSelectList([]);
    onCancel();
  };
  const onQTYChange = (value: any, record: any) => {
    let QTYObject1 = { ...QTYObject };
    if (value > 0) QTYObject1[record.ID] = value;
    setQTYObject(QTYObject1);
  };
  const onPriceChange = (value: any, record: any) => {
    let PriceObject1 = { ...PriceObject };
    if (value > 0) PriceObject1[record.ID] = value;
    setPriceObject(PriceObject1);
  };
  const onCodeChange = (value: any, record: any) => {
    let CodeObject1 = { ...CodeObject };
    CodeObject1[record.ID] = value;
    setCodeObject(CodeObject1);
  };
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
  const columns1: ProColumns[] = [
    {
      title: "数量",
      dataIndex: "QTY",
      width: 160,
      render: (_text: any, record: any) => (
        <InputNumber size="small" value={QTYObject[record.ID] ?? 1} min="0" onChange={value => onQTYChange(value, record)} />
      )
    },
    {
      title: "单价",
      dataIndex: "Price",
      width: 160,
      hideInTable: ignoreColumns && ignoreColumns.includes("Price") ? true : false,
      render: (_text: any, record: any) => (
        <InputNumber
          size="small"
          value={PriceObject[record.ID] ?? 0}
          min="0"
          step={record.Step}
          onChange={value => onPriceChange(value, record)}
        />
      )
    },
    {
      title: "客户物料编码",
      dataIndex: "CustomerMaterialCode",
      width: 160,
      hideInTable: ignoreColumns && ignoreColumns.includes("Price") ? true : false,
      render: (_text: any, record: any) => (
        <Input size="small" value={CodeObject[record.ID] ?? null} onChange={e => onCodeChange(e.target.value, record)} />
      )
    }
  ];
  columns = [...columns, ...columns1];
  let hasColumns = [...columns];
  hasColumns.push({
    title: "操作",
    dataIndex: "option",
    fixed: "right",
    valueType: "option",
    width: 60,
    render: (_text, record) => <a onClick={() => onDelList(record)}>删除</a>
  });
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
        setSelectedRowKeys([]);
        setSelectList([]);
        onCancel();
      }}
      footer={[
        <Button key="back" onClick={() => onCancel()}>
          取消
        </Button>,
        <Button key="submit1" onClick={onMatchAll}>
          添加全部
        </Button>,
        <Button
          key="submit1"
          onClick={() => {
            setSelectedRowKeys([]);
            setSelectList([]);
            onCancel();
            navigate(`/basedata/BD_MATERIAL_MNG`);
          }}
        >
          新建物料
        </Button>,
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

export default QueryMaterial;
