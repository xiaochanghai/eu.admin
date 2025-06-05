import React, { useState, useCallback } from "react";
import { Button, Modal } from "antd";
import { RootState, useSelector } from "@/redux";
import http from "@/api";
import { message } from "@/hooks/useMessage";
import { Icon } from "@/components";
import TableList from "../../system/common/components/TableList";
import FormPage from "./FormPage";
import WaitShipSelect from "../salesOrder/WaitShipSelect";
import { ViewType } from "@/typings";

const { confirm } = Modal;

/**
 * 出货类型枚举
 */
enum ShipType {
  Ship = "Ship",
  ShipOut = "ShipOut"
}

/**
 * 表格操作引用接口
 */
interface TableAction {
  clearSelected?: () => void;
  reload?: () => void;
}

/**
 * 发货订单组件属性接口
 */
interface ShipOrderProps {
  // 可以根据需要添加属性
}

// 模块代码常量
const MODULE_CODE = "SD_SHIP_ORDER_MNG";

// 表格操作引用
let tableAction: TableAction = {};

/**
 * 发货订单管理组件
 * 用于管理销售发货订单的列表展示、新增、编辑、完结和出库操作
 */
const ShipOrder: React.FC<ShipOrderProps> = React.memo(() => {
  // 状态管理
  const [viewType, setViewType] = useState<ViewType>(ViewType.INDEX);
  const [formPageId, setFormPageId] = useState<string | null>("");
  const [formPageIsView, setFormPageIsView] = useState<boolean>(false);
  const [waitShipSelectVisible, setWaitShipSelectVisible] = useState<boolean>(false);
  const [selectedRowIds, setSelectedRowIds] = useState<string[]>([]);
  const [waitShipSelectType, setWaitShipSelectType] = useState<ShipType>(ShipType.Ship);

  // 从Redux获取模块信息
  const moduleInfos = useSelector((state: RootState) => state.module.moduleInfos);
  const moduleInfo = moduleInfos[MODULE_CODE];

  /**
   * 切换页面视图
   * @param value 视图类型
   * @param id 记录ID
   * @param isView 是否为查看模式
   */
  const changePage = useCallback((value: ViewType, id: string = "", isView: boolean = false) => {
    if (value === ViewType.PAGE) {
      setViewType(value);
      setFormPageId(id);
      setFormPageIsView(isView);
    } else if (value === ViewType.INDEX) {
      setViewType(value);
      setFormPageId("");
      setFormPageIsView(false);
    }
  }, []);

  /**
   * 完结发货订单
   * @param action 表格操作引用
   * @param selectedRows 选中的行数据
   * @param selectedRowKeys 选中的行键值
   */
  const handleOrderComplete = useCallback(async (action: TableAction, _selectedRows: any[], selectedRowKeys: string[]) => {
    // 验证是否选中数据
    if (selectedRowKeys.length === 0) {
      message.error("至少选中一条数据！");
      return;
    }

    // 确认对话框
    confirm({
      title: selectedRowKeys.length === 1 ? "你确定需要完结该订单吗？" : "你确定需要批量完结订单吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        try {
          message.loading("数据提交中...", 0);
          const { Success } = await http.post<{ Success: boolean }>("/api/SdShipOrder/BulkOrderComplete", selectedRowKeys);
          message.destroy();

          if (Success) {
            message.success("提交成功！");
            action?.clearSelected?.();
            action?.reload?.();
          }
        } catch (error) {
          message.destroy();
          message.error("操作失败，请重试！");
          console.error("完结订单失败:", error);
        }
      }
    });
  }, []);

  /**
   * 发货订单出库
   * @param action 表格操作引用
   * @param selectedRows 选中的行数据
   * @param selectedRowKeys 选中的行键值
   */
  const handleOrderDelivery = useCallback((action: TableAction, _selectedRows: any[], selectedRowKeys: string[]) => {
    // 验证是否选中数据
    if (selectedRowKeys.length === 0) {
      message.error("至少选中一条数据！");
      return;
    }

    // 保存表格操作引用，用于后续刷新
    tableAction = action;
    setSelectedRowIds(selectedRowKeys);
    setWaitShipSelectVisible(true);
    setWaitShipSelectType(ShipType.ShipOut);
  }, []);

  /**
   * 处理出库提交
   * @param values 出库数据
   */
  const handleShipSubmit = useCallback(async (values: any) => {
    try {
      message.loading("数据提交中...", 0);
      const { Success } = await http.post<{ Success: boolean }>("/api/SdShipOrder/BulkInsertOut", values);
      message.destroy();

      if (Success) {
        message.success("提交成功！");
        tableAction?.clearSelected?.();
        tableAction?.reload?.();
        setWaitShipSelectVisible(false);
      }
    } catch (error) {
      message.destroy();
      message.error("操作失败，请重试！");
      console.error("出库提交失败:", error);
    }
  }, []);

  /**
   * 操作栏按钮方法
   */
  const actionMethods = {
    SalesShipOrderCompleted: handleOrderComplete,
    SalesShipOrderDelivery: handleOrderDelivery
  };

  /**
   * 渲染自定义操作按钮
   */
  const renderCustomActions = useCallback(() => {
    if (!moduleInfo?.customActionData?.length) {
      return null;
    }

    return (
      <>
        {moduleInfo.customActionData.map((item: any, index: number) => (
          <Button key={`custom-action-${index}`}>{item.FunctionName}</Button>
        ))}
      </>
    );
  }, [moduleInfo]);

  return (
    <>
      {viewType === ViewType.INDEX && (
        <TableList moduleCode={MODULE_CODE} changePage={changePage} expendAction={renderCustomActions} {...actionMethods} />
      )}

      {viewType === ViewType.PAGE && (
        <FormPage moduleCode={MODULE_CODE} Id={formPageId} IsView={formPageIsView} changePage={changePage} />
      )}

      <WaitShipSelect
        modalVisible={waitShipSelectVisible}
        waitShipSelectType={waitShipSelectType}
        selectedRowIds={selectedRowIds}
        onCancel={() => setWaitShipSelectVisible(false)}
        onSubmit={handleShipSubmit}
      />
    </>
  );
});

export default ShipOrder;
