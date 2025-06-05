import { Button, Space, Modal } from "antd";
import { message } from "@/hooks/useMessage";
import { ModifyType } from "@/api/interface/index";
import { batchAudit, batchRevocation } from "@/api/modules/module";
import { Icon } from "@/components";

const { confirm } = Modal;
const FormToolbar: React.FC<any> = props => {
  const { onFinishAdd, onBack, disabled, expendAction, moduleInfo, modifyType, auditStatus, masterId, onReload } = props;
  let { actions, moduleCode, url } = moduleInfo;

  let actionAuthButton: { [key: string]: boolean } = {};
  actions?.forEach((item: any) => {
    actionAuthButton[item] = true;
  });

  const batchAuditConfirm = () => {
    confirm({
      title: "你确定需要提交该数据吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        // let ids: string[] = [];
        // selectedRows.map((item: any) => {
        //   ids.push(item.ID);
        // });

        message.loading("数据提交中...", 0);
        let { Success, Message } = await batchAudit({ moduleCode, Ids: [masterId], url });
        message.destroy();
        if (Success) {
          message.success(Message);
          onReload?.();
        }
      },
      onCancel() {
        // console.log('Cancel');
      }
    });
  };
  const batchRevocationConfirm = () => {
    confirm({
      title: "你确定需要撤销该数据吗？",
      icon: <Icon name="ExclamationCircleOutlined" />,
      okText: "确定",
      okType: "danger",
      cancelText: "取消",
      async onOk() {
        // let ids: string[] = [];
        // selectedRows.map((item: any) => {
        //   ids.push(item.ID);
        // });

        message.loading("数据提交中...", 0);
        let { Success, Message } = await batchRevocation({ moduleCode, Ids: [masterId], url });
        message.destroy();
        if (Success) {
          message.success(Message);
          onReload?.();
        }
      },
      onCancel() {
        // console.log('Cancel');
      }
    });
  };
  return (
    <>
      <Space style={{ display: "flex", justifyContent: "flex-start" }}>
        <Button type="primary" disabled={disabled} htmlType="submit">
          保存
        </Button>

        <Button type="primary" disabled={disabled} onClick={onFinishAdd}>
          保存并新建
        </Button>
        {actionAuthButton.Audit && modifyType == ModifyType.Edit && auditStatus == "Add" && (
          <Button onClick={() => batchAuditConfirm()}>审核</Button>
        )}
        {actionAuthButton.Revocation && modifyType == ModifyType.AuditPass && auditStatus == "CompleteAudit" && (
          <Button type="primary" danger onClick={() => batchRevocationConfirm()}>
            撤销
          </Button>
        )}
        {expendAction ? expendAction() : null}

        <Button type="default" onClick={onBack}>
          <Icon name="ArrowLeftOutlined" /> 返回
        </Button>
      </Space>
      <div style={{ height: 10 }}></div>
    </>
  );
};

export default FormToolbar;
