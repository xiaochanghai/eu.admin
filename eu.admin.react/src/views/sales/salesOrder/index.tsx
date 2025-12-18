import React, { useState, useRef, useCallback, useMemo } from "react";
import { Button, Modal } from "antd";
import { RootState, useSelector } from "@/redux";
import { message } from "@/hooks/useMessage";
import http from "@/api";
import { ViewType, ActionType } from "@/typings";
import { TableList, Icon } from "@/components";
import FormPage from "./FormPage";
import WaitShipSelect from "./WaitShipSelect";

const { confirm } = Modal;

// Types
interface SalesOrderProps {}

interface ModuleInfo {
  customActionData?: Array<{
    FunctionName: string;
  }>;
}

type WaitShipSelectType = "Ship" | "Out";

// Constants
const MODULE_CODE = "SD_SALES_ORDER_MNG";
const API_ENDPOINTS = {
  BULK_ORDER_COMPLETE: "/api/SdOrder/BulkOrderComplete",
  BULK_ORDER_CHANGE: "/api/SdOrder/BulkOrderChange",
  BULK_INSERT_SHIP: "/api/SdOrder/BulkInsertShip",
  BULK_INSERT_OUT: "/api/SdOrder/BulkInsertOut"
};

const SalesOrder: React.FC<SalesOrderProps> = () => {
  // State
  const [viewType, setViewType] = useState<ViewType>(ViewType.INDEX);
  const [formPageId, setFormPageId] = useState<string>("");
  const [formPageIsView, setFormPageIsView] = useState<boolean>(false);
  const [waitShipSelectVisible, setWaitShipSelectVisible] = useState<boolean>(false);
  const [selectedRowIds, setSelectedRowIds] = useState<string[]>([]);
  const [waitShipSelectType, setWaitShipSelectType] = useState<WaitShipSelectType>("Ship");

  // Refs
  const tableRef = useRef<ActionType>();
  const tableActionRef = useRef<any>();

  // Selectors
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const moduleInfo = useMemo(() => moduleInfos[MODULE_CODE] as ModuleInfo, [moduleInfos]);

  // Utility functions
  const validateSelection = useCallback((selectedRowKeys: string[]): boolean => {
    if (selectedRowKeys.length === 0) {
      message.error("至少选中一条数据！");
      return false;
    }
    return true;
  }, []);

  const showConfirmDialog = useCallback((title: string, onConfirm: () => Promise<void>) => {
    confirm({
      title,
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      onOk: onConfirm
    });
  }, []);

  const handleApiCall = useCallback(async (endpoint: string, data: string[], action: any): Promise<void> => {
    try {
      message.loading("数据提交中...", 0);
      const response = await http.post(endpoint, data);
      message.destroy();

      if (response.Success) {
        message.success(response.Message);
        action?.clearSelected();
        action?.reload();
      }
    } catch (error) {
      message.destroy();
      message.error("操作失败，请重试");
    }
  }, []);

  // Page navigation
  const changePage = (value: ViewType, id?: string, isView?: boolean) => {
    setViewType(value);
    setFormPageId(value === ViewType.PAGE ? (id ?? "") : "");
    setFormPageIsView(value === ViewType.PAGE ? (isView ?? false) : false);
  };

  // Action handlers
  const handleSalesOrderCompleted = useCallback(
    async (action: any, _selectedRows: any, selectedRowKeys: string[]) => {
      if (!validateSelection(selectedRowKeys)) return;

      const title = selectedRowKeys.length === 1 ? "你确定需要完结该订单吗？" : "你确定需要批量完结订单吗？";

      showConfirmDialog(title, () => handleApiCall(API_ENDPOINTS.BULK_ORDER_COMPLETE, selectedRowKeys, action));
    },
    [validateSelection, showConfirmDialog, handleApiCall]
  );

  const handleSalesOrderChange = useCallback(
    async (action: any, _selectedRows: any, selectedRowKeys: string[]) => {
      if (!validateSelection(selectedRowKeys)) return;

      const title = selectedRowKeys.length === 1 ? "你确定需要变更该订单吗？" : "你确定需要批量变更订单吗？";

      showConfirmDialog(title, () => handleApiCall(API_ENDPOINTS.BULK_ORDER_CHANGE, selectedRowKeys, action));
    },
    [validateSelection, showConfirmDialog, handleApiCall]
  );

  const handleShippingAction = useCallback(
    (action: any, _selectedRows: any, selectedRowKeys: string[], type: WaitShipSelectType) => {
      if (!validateSelection(selectedRowKeys)) return;

      tableActionRef.current = action;
      setSelectedRowIds(selectedRowKeys);
      setWaitShipSelectVisible(true);
      setWaitShipSelectType(type);
    },
    [validateSelection]
  );

  const handleSalesOrderShippingNotice = useCallback(
    (action: any, selectedRows: any, selectedRowKeys: string[]) => {
      handleShippingAction(action, selectedRows, selectedRowKeys, "Ship");
    },
    [handleShippingAction]
  );

  const handleSalesOrderDelivery = useCallback(
    (action: any, selectedRows: any, selectedRowKeys: string[]) => {
      handleShippingAction(action, selectedRows, selectedRowKeys, "Out");
    },
    [handleShippingAction]
  );
  // Action object for TableList
  const actions = useMemo(
    () => ({
      SalesOrderCompleted: handleSalesOrderCompleted,
      SalesOrderChange: handleSalesOrderChange,
      SalesOrderShippingNotice: handleSalesOrderShippingNotice,
      SalesOrderDelivery: handleSalesOrderDelivery
    }),
    [handleSalesOrderCompleted, handleSalesOrderChange, handleSalesOrderShippingNotice, handleSalesOrderDelivery]
  );

  // Reload handler
  const handleReload = useCallback(() => {
    tableRef.current?.reload();
  }, []);

  // Custom action buttons renderer
  const renderCustomActions = useCallback(() => {
    if (!moduleInfo?.customActionData) return null;

    return (
      <>
        {moduleInfo.customActionData.map((item, index) => (
          <Button key={`${item.FunctionName}-${index}`}>{item.FunctionName}</Button>
        ))}
      </>
    );
  }, [moduleInfo?.customActionData]);

  // WaitShipSelect submit handler
  const handleWaitShipSelectSubmit = useCallback(
    async (values: any) => {
      try {
        message.loading("数据提交中...", 0);
        const endpoint = waitShipSelectType === "Ship" ? API_ENDPOINTS.BULK_INSERT_SHIP : API_ENDPOINTS.BULK_INSERT_OUT;

        const response = await http.post(endpoint, values);
        message.destroy();

        if (response.Success) {
          message.success("提交成功！");
          tableActionRef.current?.clearSelected();
          tableActionRef.current?.reload();
          setWaitShipSelectVisible(false);
        }
      } catch (error) {
        message.destroy();
        message.error("提交失败，请重试");
      }
    },
    [waitShipSelectType]
  );

  const handleWaitShipSelectCancel = useCallback(() => {
    setWaitShipSelectVisible(false);
  }, []);

  return (
    <>
      <div style={{ display: viewType == ViewType.INDEX ? "block" : "none" }}>
        <TableList
          moduleCode={MODULE_CODE}
          changePage={changePage}
          expendAction={renderCustomActions}
          tableActionRef={tableRef}
          {...actions}
        />
      </div>

      {viewType === ViewType.PAGE && (
        <FormPage
          moduleCode={MODULE_CODE}
          Id={formPageId}
          IsView={formPageIsView}
          changePage={changePage}
          onReload={handleReload}
        />
      )}

      <WaitShipSelect
        modalVisible={waitShipSelectVisible}
        waitShipSelectType={waitShipSelectType}
        selectedRowIds={selectedRowIds}
        onCancel={handleWaitShipSelectCancel}
        onSubmit={handleWaitShipSelectSubmit}
      />
    </>
  );
};

export default SalesOrder;
