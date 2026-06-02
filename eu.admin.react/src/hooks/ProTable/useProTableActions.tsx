import { Modal } from "antd";
import { message } from "@/hooks/useMessage";
import { singleDelete } from "@/api/modules/module";
import { ModuleInfo } from "@/api/interface/index";
import { Icon } from "@/components";
import { CONFIRM_MESSAGES, CONFIRM_BUTTON_TEXT, LOADING_MESSAGES, SUM_ROW_ID } from "@/components/ProTable/constants";
import { hasUpdatePermission, shouldResetToFirstPage } from "@/components/ProTable/utils";

const { confirm } = Modal;

/**
 * ProTable 单条记录操作 Hook
 * 负责编辑、查看、删除等单条记录操作
 *
 * @param moduleInfo 模块信息
 * @param onEdit 编辑回调函数
 * @param customDelete 自定义删除函数
 * @param customDeleteConfirm 自定义删除确认函数
 * @returns 操作相关的处理函数
 */
export const useProTableActions = (
  moduleInfo: ModuleInfo,
  onEdit?: (id: string | null, isView?: boolean) => void,
  customDelete?: (record: any) => void,
  customDeleteConfirm?: (action: any, record: any) => void
) => {
  const { moduleCode, url } = moduleInfo;

  /**
   * 显示删除确认对话框
   */
  const showDeleteConfirm = async (action: any, record: any) => {
    confirm({
      title: CONFIRM_MESSAGES.DELETE_SINGLE,
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: CONFIRM_BUTTON_TEXT.OK,
      okType: "danger",
      cancelText: CONFIRM_BUTTON_TEXT.CANCEL,
      async onOk() {
        const hideLoading = message.loading(LOADING_MESSAGES.SUBMITTING, 0);
        try {
          if (customDelete) {
            customDelete(record);
          } else {
            const { Success, Message } = await singleDelete({ moduleCode, Id: record.ID, url });
            if (Success) {
              // 仅当删除后当前页变空且不在第一页时回退到第一页，否则原地刷新
              action.reload(shouldResetToFirstPage(action.pageInfo, 1));
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
   * 编辑记录
   */
  const handleEdit = (record: any) => onEdit?.(record.ID, false);

  /**
   * 查看记录
   */
  const handleView = (record: any) => onEdit?.(record.ID, true);

  /**
   * 删除记录
   */
  const handleDelete = (action: any, record: any) => {
    if (customDeleteConfirm) {
      customDeleteConfirm(action, record);
    } else {
      showDeleteConfirm(action, record);
    }
  };

  /**
   * 双击行处理（编辑）
   */
  const handleDoubleClick = (record: any, IsView: boolean | null | undefined) => {
    // 忽略汇总行
    if (record.ID === SUM_ROW_ID || !moduleInfo?.Success || IsView) return;

    // 检查是否有编辑权限
    if (hasUpdatePermission(moduleInfo)) {
      handleEdit(record);
    }
  };

  return {
    handleEdit,
    handleView,
    handleDelete,
    handleDoubleClick
  };
};
