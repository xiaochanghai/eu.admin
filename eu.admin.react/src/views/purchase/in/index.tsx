import React, { useState } from "react";
import TableList from "../../system/common/components/TableList";
import FormPage from "./FormPage";
import { Modal } from "antd";
import { message } from "@/hooks/useMessage";
import { Icon } from "@/components/Icon/index";
import http from "@/api";
const { confirm } = Modal;

const Index: React.FC<any> = () => {
  let moduleCode = "PO_IN_ORDER_MNG";
  const [viewType, setViewType] = useState("FormIndex");
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState("Index");

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
        let { Success } = await http.post<any>("/api/PoInOrder/BulkOrderComplete", selectedRowKeys);
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
  const PurchaseNoticeOrderIn = (_action: any, _selectedRows: any, selectedRowKeys: any) => {
    if (selectedRowKeys.length == 0) {
      message.error("至少选中一条数据！");
      return;
    }
  };
  const action = {
    PurchaseNoticeOrderCompleted,
    PurchaseNoticeOrderIn
  };

  return (
    <>
      {viewType == "FormIndex" ? <TableList moduleCode={moduleCode} changePage={changePage} {...action} /> : null}
      {viewType == "FormPage" ? (
        <FormPage moduleCode={moduleCode} Id={formPageId} IsView={formPageIsView} changePage={changePage} />
      ) : null}
    </>
  );
};

export default Index;
