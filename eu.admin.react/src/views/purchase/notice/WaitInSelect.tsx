import React, { useEffect, useState } from "react";
import { Modal, Badge, Tabs, Button, InputNumber } from "antd";
import { Icon } from "@/components/Icon";
import { ProTable } from "@ant-design/pro-components";
import { query } from "@/api/modules/module";
import { RootState, useSelector } from "@/redux";
import { ModuleInfo } from "@/api/interface/index";
import { getModuleInfo } from "@/api/modules/module";
import { useDispatch } from "@/redux";
import { setModuleInfo } from "@/redux/modules/module";

const { TabPane } = Tabs;
const moduleCode = "PO_NOTICE_ORDER_WAIT_IN_MNG";
const WaitSelect: React.FC<any> = props => {
  const dispatch = useDispatch();
  const { modalVisible, onCancel, onSubmit, selectedRowIds } = props;
  let [selectedRowKeys, setSelectedRowKeys] = useState<any>([]);
  let [QTYObject, setQTYObject] = useState<any>({});
  const [selectList, setSelectList] = useState<any>([]);
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);

  let moduleInfo = moduleInfos[moduleCode] as ModuleInfo;
  let { columns } = moduleInfo ?? {};

  useEffect(() => {
    const getModuleInfo1 = async () => {
      let { Data } = await getModuleInfo(moduleCode);
      dispatch(setModuleInfo(Data));
    };
    if (!moduleInfo) getModuleInfo1();
  }, []);

  const okHandle = async () => {
    let selectList1 = [...selectList];

    selectList1.forEach(record => {
      record["InQTY"] = QTYObject[record.ID] ?? 1;
    });

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
  // let hasColumns = [...columns];
  let hasColumns = [...(columns ?? [])];
  hasColumns.push(
    {
      title: "到货数量",
      dataIndex: "InQTY",
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
      align: "center",
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
              let filter = { ...filterCondition, PoNoticeOrderId: selectedRowIds };
              return await query({
                paramData: JSON.stringify(params),
                sorter: JSON.stringify(sorter),
                filter: JSON.stringify(filter),
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

export default WaitSelect;
