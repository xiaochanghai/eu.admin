import React, { useState, useRef } from "react";
import { TableList, Icon } from "@/components";
import FormPage from "./FormPage";
// import { Button } from "antd";
import { message, modal } from "@/hooks/useMessage";
// import { RootState, useSelector } from "@/redux";
// import WaitShipSelect from "../salesOrder/WaitShipSelect";
import http from "@/api";
const { confirm } = modal;
import { ActionType } from "@/typings";

// let tableAction: any = {};
const Index: React.FC<any> = () => {
  let moduleCode = "SD_RETURN_ORDER_MNG";
  const [viewType, setViewType] = useState("FormIndex");
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");
  // const [waitShipSelectVisible, setWaitShipSelectVisible] = useState(false);
  // const [selectedRowIds, setSelectedRowIds] = useState("Ship");
  // const [waitShipSelectType, setWaitShipSelectType] = useState("Ship");
  // const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  // let moduleInfo = moduleInfos[moduleCode] as any;
  const tableRef = useRef<ActionType>();

  const changePage = (value: any, id: string, isView: any) => {
    if (value == "FormPage") {
      setViewType(value);
      setFormPageId(id);
      setFormPageIsView(isView);
    } else if (value == "FormIndex") {
      setViewType(value);
      setFormPageId("");
      setFormPageIsView("");
    }
  };

  const SalesReturnOrderCarry = async (_action: any, _selectedRows: any, selectedRowKeys: any) => {
    if (selectedRowKeys.length == 0) {
      message.error("至少选中一条数据！");
      return;
    }

    confirm({
      title: selectedRowKeys.length == 1 ? "你确定需要过账该订单吗？" : "你确定需要批量过账订单吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        message.loading("数据提交中...", 0);
        let { Success } = await http.post<any>("/api/SdReturnOrder/CarryTo", selectedRowKeys);
        message.destroy();
        if (Success) {
          message.success("提交成功！");
          _action?.clearSelected();
          _action?.reload();
        }
      },
      onCancel() {
        // console.log('Cancel');
      }
    });
  };
  const action = {
    SalesReturnOrderCarry
  };
  const onReload = () => tableRef.current?.reload();

  return (
    <>
      <div style={{ display: viewType === "FormIndex" ? "block" : "none" }}>
        <TableList
          moduleCode={moduleCode}
          changePage={changePage}
          tableActionRef={tableRef}
          // expendAction={() => (
          //   <>
          //     {moduleInfo &&
          //       moduleInfo.customActionData &&
          //       moduleInfo.customActionData.map((item: any) => {
          //         return <Button>{item.FunctionName}</Button>;
          //       })}
          //   </>
          // )}
          {...action}
          // expendAction={(action, selectedRows) => {
          // expendAction={(_action: any, _selectedRows: any, selectedRowKeys: any) => {
          // expendAction={() => {
          //   {
          //     moduleInfo &&
          //       moduleInfo.customActionData &&
          //       moduleInfo.customActionData.map((item: any) => {
          //         return <Button>{item.FunctionName}</Button>;
          //       });
          //   }
          //   // return (
          //   //   <>
          //   //     <Button>订单完结</Button>
          //   //     <Button>订单变更</Button>
          //   //     <Button>出货通知</Button>
          //   //     <Button>发货</Button>
          //   //   </>
          //   // );
          // }}
        />
      </div>
      {viewType == "FormPage" && (
        <FormPage moduleCode={moduleCode} Id={formPageId} IsView={formPageIsView} changePage={changePage} onReload={onReload} />
      )}

      {/* <WaitShipSelect
        modalVisible={waitShipSelectVisible}
        waitShipSelectType={waitShipSelectType}
        selectedRowIds={selectedRowIds}
        onCancel={() => setWaitShipSelectVisible(false)}
        onSubmit={async (values: any) => {
          message.loading("数据提交中...", 0);
          let { Success } = await http.post<any>(
            waitShipSelectType == "Ship" ? "/api/SdOrder/BulkInsertShip" : "/api/SdOrder/BulkInsertOut",
            values
          );
          message.destroy();
          if (Success) {
            message.success("提交成功！");
            tableAction?.clearSelected();
            tableAction?.reload();
            // if (tableRef.current) tableRef.current.reload();
          }
        }}
      /> */}
    </>
  );
};

export default Index;
