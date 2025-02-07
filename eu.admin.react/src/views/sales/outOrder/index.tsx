import React, { useState } from "react";
import TableList from "../../system/common/components/TableList";
import FormPage from "./FormPage";
import { Button, Modal } from "antd";
import { message } from "@/hooks/useMessage";
import { RootState, useSelector } from "@/redux";
// import WaitShipSelect from "../salesOrder/WaitShipSelect";
import http from "@/api";
const { confirm } = Modal;
import { Icon } from "@/components";

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

  const SalesOutOrderCarryTo = async (_action: any, _selectedRows: any, selectedRowKeys: any) => {
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
        />
      ) : null}
      {viewType == "FormPage" ? (
        <FormPage moduleCode={moduleCode} Id={formPageId} IsView={formPageIsView} changePage={changePage} />
      ) : null}
    </>
  );
};

export default Index;
