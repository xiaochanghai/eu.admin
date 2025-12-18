import React, { useState, useRef } from "react";
import { TableList, Icon } from "@/components";
import FormPage from "./FormPage";
import { Button, Modal } from "antd";
import { message } from "@/hooks/useMessage";
import { RootState, useSelector } from "@/redux";
// import WaitShipSelect from "../salesOrder/WaitShipSelect";
import http from "@/api";
const { confirm } = Modal;
import { ActionType } from "@/typings";

// let tableAction: any = {};
const Index: React.FC<any> = () => {
  let moduleCode = "SD_OUT_ORDER_MNG";
  const [viewType, setViewType] = useState("FormIndex");
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");
  // const [waitShipSelectVisible, setWaitShipSelectVisible] = useState(false);
  // const [selectedRowIds, setSelectedRowIds] = useState("Ship");
  // const [waitShipSelectType, setWaitShipSelectType] = useState("Ship");
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  let moduleInfo = moduleInfos[moduleCode] as any;
  const tableRef = useRef<ActionType>();

  const changePage = (value: any, id: string, isView: any) => {
    setViewType(value);
    if (value === "FormPage") {
      setFormPageId(id);
      setFormPageIsView(isView);
    } else if (value === "FormIndex") {
      setFormPageId("");
      setFormPageIsView("");
    }
  };

  const SalesOutOrderCarryTo = async (_action: any, _selectedRows: any, selectedRowKeys: any) => {
    if (selectedRowKeys.length === 0) {
      message.error("至少选中一条数据！");
      return;
    }

    confirm({
      title: selectedRowKeys.length === 1 ? "你确定需要过账该订单吗？" : "你确定需要批量过账订单吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        message.loading("数据提交中...", 0);
        let { Success } = await http.post<any>("/api/SdOutOrder/CarryTo", selectedRowKeys);
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
    SalesOutOrderCarryTo
  };
  const onReload = () => tableRef.current?.reload();

  return (
    <>
      <div style={{ display: viewType === "FormIndex" ? "block" : "none" }}>
        <TableList
          moduleCode={moduleCode}
          changePage={changePage}
          tableActionRef={tableRef}
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
        />
      </div>
      {viewType === "FormPage" && (
        <FormPage moduleCode={moduleCode} Id={formPageId} IsView={formPageIsView} changePage={changePage} onReload={onReload} />
      )}
    </>
  );
};

export default Index;
