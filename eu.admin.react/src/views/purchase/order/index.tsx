import React, { useState } from "react";
import TableList from "../../system/common/components/TableList";
import FormPage from "./FormPage";
import { Button } from "antd";
import { RootState, useSelector } from "@/redux";
import { message, modal } from "@/hooks/useMessage";
const { confirm } = modal;
import http from "@/api";
import WaitSelect from "./WaitSelect";
import { Icon } from "@/components";

let tableAction: any = {};
const Index: React.FC<any> = () => {
  let moduleCode = "PO_ORDER_MNG";
  const [viewType, setViewType] = useState("FormIndex");
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");
  const [selectedRowIds, setSelectedRowIds] = useState([]);
  const [waitShipSelectType, setWaitShipSelectType] = useState("Notice");
  const [waitShipSelectVisible, setWaitShipSelectVisible] = useState(false);
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  let moduleInfo = moduleInfos[moduleCode] as any;

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

  // const SalesOrderCompleted = async (_action: any, _selectedRows: any, selectedRowKeys: any) => {
  //   if (selectedRowKeys.length == 0) {
  //     message.error("至少选中一条数据！");
  //     return;
  //   }

  //   confirm({
  //     title: selectedRowKeys.length == 1 ? "你确定需要完结该订单吗？" : "你确定需要批量完结订单吗？",
  //     icon: <Icon name="ExclamationCircleOutlined" />,
  //     okText: "确定",
  //     okType: "danger",
  //     cancelText: "取消",
  //     async onOk() {
  //       message.loading("数据提交中...", 0);
  //       let { Success, Message } = await http.post<any>("/api/SdOrder/BulkOrderComplete", selectedRowKeys);
  //       message.destroy();
  //       if (Success) {
  //         message.success(Message);
  //         _action?.clearSelected();
  //         _action?.reload();
  //         // if (tableRef.current) tableRef.current.reload();
  //       }
  //     },
  //     onCancel() {
  //       // console.log('Cancel');
  //     }
  //   });
  // };
  const PurchaseOrderChange = (_action: any, _selectedRows: any, selectedRowKeys: any) => {
    if (selectedRowKeys.length == 0) {
      message.error("至少选中一条数据！");
      return;
    }

    confirm({
      title: selectedRowKeys.length == 1 ? "你确定需要变更该订单吗？" : "你确定需要批量变更订单吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        message.loading("数据提交中...", 0);
        let { Success, Message } = await http.post<any>("/api/SdOrder/BulkOrderChange", selectedRowKeys);
        message.destroy();
        if (Success) {
          message.success(Message);
          _action?.clearSelected();
          _action?.reload();
          // if (tableRef.current) tableRef.current.reload();
        }
      },
      onCancel() {
        // console.log('Cancel');
      }
    });
  };
  const PurchaseOrderArriveNotice = (_action: any, _selectedRows: any, selectedRowKeys: any) => {
    if (selectedRowKeys.length == 0) {
      message.error("至少选中一条数据！");
      return;
    }
    tableAction = _action;
    setSelectedRowIds(selectedRowKeys);
    setWaitShipSelectVisible(true);
    setWaitShipSelectType("Notice");
  };
  const PurchaseOrderIn = (_action: any, _selectedRows: any, selectedRowKeys: any) => {
    if (selectedRowKeys.length == 0) {
      message.error("至少选中一条数据！");
      return;
    }
    tableAction = _action;
    setSelectedRowIds(selectedRowKeys);
    setWaitShipSelectVisible(true);
    setWaitShipSelectType("In");
  };
  //#region 操作栏按钮方法
  const action = {
    // SalesOrderCompleted,
    PurchaseOrderChange,
    PurchaseOrderArriveNotice,
    PurchaseOrderIn
  };

  return (
    <>
      {viewType == "FormIndex" ? (
        <TableList
          moduleCode={moduleCode}
          changePage={changePage}
          expendAction={() => (
            <>
              {moduleInfo &&
                moduleInfo.customActionData &&
                moduleInfo.customActionData.map((item: any) => {
                  return <Button>{item.FunctionName}</Button>;
                })}
            </>
          )}
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
      ) : null}
      {viewType == "FormPage" ? (
        <FormPage moduleCode={moduleCode} Id={formPageId} IsView={formPageIsView} changePage={changePage} />
      ) : null}

      <WaitSelect
        modalVisible={waitShipSelectVisible}
        waitShipSelectType={waitShipSelectType}
        selectedRowIds={selectedRowIds}
        onCancel={() => setWaitShipSelectVisible(false)}
        onSubmit={async (values: any) => {
          message.loading("数据提交中...", 0);
          let { Success } = await http.post<any>(
            waitShipSelectType == "Notice" ? "/api/PoOrder/BulkInsertNotice" : "/api/PoOrder/BulkInsertIn",
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
      />
    </>
  );
};

export default Index;
