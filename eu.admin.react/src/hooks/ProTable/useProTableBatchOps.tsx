import { Modal } from "antd";
import { message } from "@/hooks/useMessage";
import { batchDelete, batchAudit, batchRevocation } from "@/api/modules/module";
import { Icon } from "@/components";
import { CONFIRM_MESSAGES, CONFIRM_BUTTON_TEXT, LOADING_MESSAGES } from "@/components/ProTable/constants";
import { BatchOperationParams } from "@/components/ProTable/types";

const { confirm } = Modal;

/**
 * ProTable 批量操作 Hook
 * 负责批量删除、批量审核、批量撤销等操作
 *
 * @param moduleCode 模块代码
 * @param url API地址
 * @param customBatchDelete 自定义批量删除函数
 * @returns 批量操作相关的处理函数
 */
export const useProTableBatchOps = (moduleCode: string, url: string, customBatchDelete?: (ids: string[]) => void) => {
  /**
   * 通用批量操作处理
   */
  const handleBatchOperation = async ({
    action,
    selectedRows,
    operationType,
    confirmTitle,
    apiFunc
  }: BatchOperationParams) => {
    confirm({
      title: confirmTitle,
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: CONFIRM_BUTTON_TEXT.OK,
      okType: "danger",
      cancelText: CONFIRM_BUTTON_TEXT.CANCEL,
      async onOk() {
        const hideLoading = message.loading(LOADING_MESSAGES.SUBMITTING, 0);
        try {
          if (operationType === "delete" && customBatchDelete) {
            const ids = selectedRows.map((item: any) => item.ID);
            await customBatchDelete(ids);
          } else {
            const { Success, Message } = await apiFunc({
              moduleCode,
              Ids: selectedRows.map((item: any) => item.ID),
              url
            });
            if (Success) {
              action.clearSelected();
              action.reload();
              message.success(Message);
            }
          }
        } finally {
          hideLoading();
        }
      },
      onCancel() {
        //
      }
    });
  };

  /**
   * 批量删除确认
   */
  const batchDeleteConfirm = (action: any, selectedRows: any) =>
    handleBatchOperation({
      action,
      selectedRows,
      operationType: "delete",
      confirmTitle: CONFIRM_MESSAGES.DELETE_BATCH,
      apiFunc: batchDelete
    });

  /**
   * 批量审核确认
   */
  const batchAuditConfirm = (action: any, selectedRows: any, selectedRowKeys: any) => {
    if (selectedRows.length === 0) {
      message.error("至少选中一条数据！");
      return;
    }
    confirm({
      title: selectedRows.length === 1 ? CONFIRM_MESSAGES.AUDIT_SINGLE : CONFIRM_MESSAGES.AUDIT_BATCH,
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: CONFIRM_BUTTON_TEXT.OK,
      okType: "danger",
      cancelText: CONFIRM_BUTTON_TEXT.CANCEL,
      async onOk() {
        const hideLoading = message.loading(LOADING_MESSAGES.SUBMITTING, 0);
        try {
          const { Success, Message } = await batchAudit({ moduleCode, Ids: selectedRowKeys, url });
          if (Success) {
            action.clearSelected();
            action.reload();
            message.success(Message);
          }
        } finally {
          hideLoading();
        }
      },
      onCancel() {
        //
      }
    });
  };

  /**
   * 批量撤销确认
   */
  const batchRevocationConfirm = (action: any, selectedRows: any, selectedRowKeys: any) => {
    if (selectedRows.length === 0) {
      message.error("至少选中一条数据！");
      return;
    }
    confirm({
      title: selectedRows.length === 1 ? CONFIRM_MESSAGES.REVOCATION_SINGLE : CONFIRM_MESSAGES.REVOCATION_BATCH,
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: CONFIRM_BUTTON_TEXT.OK,
      okType: "danger",
      cancelText: CONFIRM_BUTTON_TEXT.CANCEL,
      async onOk() {
        const hideLoading = message.loading(LOADING_MESSAGES.SUBMITTING, 0);
        try {
          const { Success, Message } = await batchRevocation({ moduleCode, Ids: selectedRowKeys, url });
          if (Success) {
            action.clearSelected();
            action.reload();
            message.success(Message);
          }
        } finally {
          hideLoading();
        }
      },
      onCancel() {
        //
      }
    });
  };

  return {
    batchDeleteConfirm,
    batchAuditConfirm,
    batchRevocationConfirm
  };
};
