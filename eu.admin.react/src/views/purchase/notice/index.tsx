import React, { useState } from "react";
import TableList from "../../system/common/components/TableList";
import FormPage from "./FormPage";
import { Modal } from "antd";
import { message } from "@/hooks/useMessage";
// import { RootState, useSelector } from "@/redux";
import WaitSelect from "./WaitInSelect";
import http from "@/api";
import { Icon } from "@/components";
const { confirm } = Modal;

let tableAction: any = {};
const Index: React.FC<any> = () => {
  let moduleCode = "PO_ARRIVAL_ORDER_MNG";
  const [viewType, setViewType] = useState("FormIndex");
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");
  const [waitSelectVisible, setShipSelectVisible] = useState(false);
  const [selectedRowIds, setSelectedRowIds] = useState([]);
  // const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  // let moduleInfo = moduleInfos[moduleCode] as any;

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

  const PurchaseNoticeOrderCompleted = async (_action: any, _selectedRows: any, selectedRowKeys: any) => {
    if (selectedRowKeys.length == 0) {
      message.error("至少选中一条数据！");
      return;
    }

    confirm({
      title: selectedRowKeys.length == 1 ? "你确定需要完结该订单吗？" : "你确定需要批量完结订单吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        message.loading("数据提交中...", 0);
        let { Success } = await http.post<any>("/api/PoArrivalOrder/BulkOrderComplete", selectedRowKeys);
        message.destroy();
        if (Success) {
          message.success("提交成功！");
          _action?.clearSelected();
          _action?.reload();
        }
      },
      onCancel() {}
    });
  };
  const PurchaseNoticeOrderIn = (_action: any, _selectedRows: any, selectedRowKeys: any) => {
    if (selectedRowKeys.length == 0) {
      message.error("至少选中一条数据！");
      return;
    }
    tableAction = _action;
    setSelectedRowIds(selectedRowKeys);
    setShipSelectVisible(true);
  };
  //#region 操作栏按钮方法
  const action = {
    PurchaseNoticeOrderCompleted,
    PurchaseNoticeOrderIn
  };

  return (
    <>
      {viewType == "FormIndex" ? (
        <TableList
          moduleCode={moduleCode}
          changePage={changePage}
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
        />
      ) : null}
      {viewType == "FormPage" ? (
        <FormPage moduleCode={moduleCode} Id={formPageId} IsView={formPageIsView} changePage={changePage} />
      ) : null}

      <WaitSelect
        modalVisible={waitSelectVisible}
        selectedRowIds={selectedRowIds}
        onCancel={() => setShipSelectVisible(false)}
        onSubmit={async (values: any) => {
          message.loading("数据提交中...", 0);
          let { Success } = await http.post<any>("/api/PoArrivalOrder/BulkInsertIn", values);
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
