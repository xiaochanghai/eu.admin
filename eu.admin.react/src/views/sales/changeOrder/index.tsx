import React, { useState } from "react";
import TableList from "../../system/common/components/TableList";
import FormPage from "./FormPage";
import { Button, Modal } from "antd";
import { message } from "@/hooks/useMessage";
import { Icon } from "@/components/Icon/index";
import { RootState, useSelector } from "@/redux";
import http from "@/api";
const { confirm } = Modal;

const Index: React.FC<any> = () => {
  let moduleCode = "SD_CHANGE_ORDER_MNG";
  const [viewType, setViewType] = useState("FormIndex");
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");
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

  const SalesChangeOrderSubmit = async (_action: any, _selectedRows: any, selectedRowKeys: any) => {
    if (selectedRowKeys.length == 0) {
      message.error("至少选中一条数据！");
      return;
    }

    confirm({
      title: selectedRowKeys.length == 1 ? "你确定需要执行变更该订单吗？" : "你确定需要批量执行变更订单吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        message.loading("数据提交中...", 0);
        let { Success } = await http.post<any>("/api/SdChangeOrder/BulkOrderChange", selectedRowKeys);
        message.destroy();
        if (Success) {
          message.success("提交成功！");
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
  //#region 操作栏按钮方法
  const action = {
    SalesChangeOrderSubmit
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
    </>
  );
};

export default Index;
