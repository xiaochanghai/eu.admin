import React, { useCallback } from "react";
import { Button, Space, Modal } from "antd";
import { message } from "@/hooks/useMessage";
import { ModifyType } from "@/typings";
import { batchAudit, batchRevocation } from "@/api/modules/module";
import { Icon } from "@/components";

const { confirm } = Modal;

// 类型定义
interface ModuleInfo {
  actions?: string[];
  moduleCode: string;
  url: string;
}

interface FormToolbarProps {
  onFinishAdd?: () => void;
  onBack?: () => void;
  disabled?: boolean;
  expendAction?: () => React.ReactNode;
  moduleInfo: ModuleInfo;
  modifyType?: ModifyType;
  auditStatus?: string;
  masterId?: string | null;
  onReload?: () => void;
}

// 常量定义
const AUDIT_STATUS = {
  ADD: "Add",
  COMPLETE_AUDIT: "CompleteAudit"
} as const;

const ACTION_TYPES = {
  AUDIT: "Audit",
  REVOCATION: "Revocation"
} as const;

const MESSAGES = {
  AUDIT_CONFIRM: "你确定需要提交该数据吗？",
  REVOCATION_CONFIRM: "你确定需要撤销该数据吗？",
  LOADING: "数据提交中...",
  CONFIRM: "确定",
  CANCEL: "取消",
  SAVE: "保存",
  SAVE_AND_NEW: "保存并新建",
  AUDIT: "审核",
  REVOCATION: "撤销",
  BACK: "返回"
} as const;

const FormToolbar: React.FC<FormToolbarProps> = ({
  onFinishAdd,
  onBack,
  disabled = false,
  expendAction,
  moduleInfo,
  modifyType = ModifyType.Add, // 提供默认值
  auditStatus = "", // 提供默认值
  masterId = null, // 提供默认值
  onReload
}) => {
  const { actions = [], moduleCode, url } = moduleInfo;

  // 生成权限按钮映射
  const actionAuthButton = actions.reduce<Record<string, boolean>>((acc, action) => {
    acc[action] = true;
    return acc;
  }, {});

  // 通用的确认操作处理函数
  const handleConfirmAction = useCallback(
    async (title: string, apiCall: (params: { moduleCode: string; Ids: string[]; url: string }) => Promise<any>) => {
      // 如果没有 masterId，不执行操作
      if (!masterId) {
        message.error("缺少必要的数据ID");
        return;
      }

      confirm({
        title,
        icon: <Icon name="ExclamationCircleOutlined" />,
        okText: MESSAGES.CONFIRM,
        okType: "danger",
        cancelText: MESSAGES.CANCEL,
        async onOk() {
          try {
            message.loading(MESSAGES.LOADING, 0);
            const { Success, Message } = await apiCall({
              moduleCode,
              Ids: [masterId],
              url
            });
            message.destroy();

            if (Success) {
              message.success(Message);
              onReload?.();
            } else {
              message.error(Message || "操作失败");
            }
          } catch (error) {
            message.destroy();
            message.error("操作失败，请重试");
            console.error("操作错误:", error);
          }
        }
      });
    },
    [moduleCode, masterId, url, onReload]
  );

  // 审核确认
  const handleAuditConfirm = useCallback(() => {
    handleConfirmAction(MESSAGES.AUDIT_CONFIRM, batchAudit);
  }, [handleConfirmAction]);

  // 撤销确认
  const handleRevocationConfirm = useCallback(() => {
    handleConfirmAction(MESSAGES.REVOCATION_CONFIRM, batchRevocation);
  }, [handleConfirmAction]);
  // 判断是否显示审核按钮
  const shouldShowAuditButton =
    actionAuthButton[ACTION_TYPES.AUDIT] && modifyType === ModifyType.Edit && auditStatus === AUDIT_STATUS.ADD && masterId; // 确保有 masterId

  // 判断是否显示撤销按钮
  const shouldShowRevocationButton =
    actionAuthButton[ACTION_TYPES.REVOCATION] &&
    modifyType === ModifyType.AuditPass &&
    auditStatus === AUDIT_STATUS.COMPLETE_AUDIT &&
    masterId; // 确保有 masterId

  return (
    <div className="pb-10">
      <Space size="middle" wrap>
        <Button type="primary" disabled={disabled} htmlType="submit">
          {MESSAGES.SAVE}
        </Button>

        {onFinishAdd && (
          <Button type="primary" disabled={disabled} onClick={onFinishAdd}>
            {MESSAGES.SAVE_AND_NEW}
          </Button>
        )}

        {shouldShowAuditButton && <Button onClick={handleAuditConfirm}>{MESSAGES.AUDIT}</Button>}

        {shouldShowRevocationButton && (
          <Button type="primary" danger onClick={handleRevocationConfirm}>
            {MESSAGES.REVOCATION}
          </Button>
        )}

        {expendAction?.()}

        {onBack && (
          <Button type="default" onClick={onBack}>
            <Icon name="ArrowLeftOutlined" />
            <span style={{ marginLeft: 4 }}>{MESSAGES.BACK}</span>
          </Button>
        )}
      </Space>
    </div>
  );
};

export default FormToolbar;
