import { memo, useState } from "react";
import { IWorkFlowNode } from "../../workflow-editor";
import { useTranslate } from "../../workflow-editor/react-locales";
import { CloseCircleOutlined } from "@ant-design/icons";
import { styled } from "styled-components";
import { Modal, Button } from "antd";
import { useWorkFlow } from "@/workflow-editor/hooks";
import { RootState, useSelector } from "@/redux";
import http from "@/api";
import { message } from "@/hooks/useMessage";

import { useUpdateEffect } from "ahooks";
const Title = styled.div`
  display: flex;
  align-items: center;
`;

const ErrorIcon = styled(CloseCircleOutlined)`
  color: red;
  font-size: 20px;
  margin-right: 8px;
`;

const Tip = styled.div`
  color: ${props => props.theme.token?.colorTextSecondary};
`;

const ErrorItem = styled.div`
  background-color: ${props => props.theme.token?.colorBorderSecondary};
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 8px 0;
  padding: 0 16px;
  border-radius: 5px;
  min-height: 48px;
`;

const ErrorCagetory = styled.div`
  color: ${props => props.theme.token?.colorTextSecondary};
  opacity: 0.8;
`;

const ErrorMessage = styled.div`
  font-size: 13px;
`;

export interface IErrorItem {
  category: string;
  message: string;
}

export const PublishButton = memo(
  ({ onValidate, iWorkFlowNode }: { iWorkFlowNode?: IWorkFlowNode; onValidate?: (result: boolean) => void }) => {
    const [errors, setErrors] = useState<IErrorItem[]>();
    const workFlow = useWorkFlow();

    const t = useTranslate();
    // const editorStore = useEditorEngine();
    const startNode = useSelector((state: RootState) => state.workflow.startNode);
    const formId = useSelector((state: RootState) => state.workflow.formId);

    useUpdateEffect(() => {
      if (iWorkFlowNode && iWorkFlowNode.childNode) {
        const result = workFlow.validate();
        if (result !== true && result !== undefined) {
          onValidate?.(false);
        } else {
          onValidate?.(true);
        }
      }
    }, [iWorkFlowNode]);

    const handleValidate = async () => {
      if (!formId) {
        message.warning("无法发布：表单信息未加载");
        return;
      }
      if (!startNode || !startNode.childNode) {
        message.warning("流程为空，请先配置流程节点");
        return;
      }
      // 先执行前端校验
      const validateResult = workFlow.validate();
      if (validateResult !== true && validateResult !== undefined) {
        // 校验失败，收集错误信息展示给用户
        const errs: IErrorItem[] = [];
        for (const nodeId of Object.keys(validateResult)) {
          const msg = validateResult[nodeId];
          errs.push({
            category: t("flowDesign"),
            message: `${nodeId}: ${msg}`
          });
        }
        setErrors(errs);
        onValidate?.(false);
        return;
      }
      // 校验通过，调用发布接口
      const { Success, Message } = await http.post<any>(`/api/SmWorkFlow/Publish/${formId}`, startNode);
      if (Success) {
        message.success(Message || "发布成功");
        onValidate?.(true);
      }
    };

    const handleOk = () => {
      setErrors(undefined);
    };

    const handleCancel = () => {
      setErrors(undefined);
    };

    return (
      <>
        <Button onClick={handleValidate}>{t("publish")}</Button>
        <Modal
          title={
            <Title>
              <ErrorIcon />
              {t("cantNotPublish")}
            </Title>
          }
          open={!!errors?.length}
          cancelText={t("gotIt")}
          okText={t("gotoEdit")}
          onOk={handleOk}
          onCancel={handleCancel}
        >
          <Tip>{t("canNotPublishTip")}</Tip>
          {errors?.map((err, index) => {
            return (
              <ErrorItem key={index}>
                <ErrorCagetory>{err.category}</ErrorCagetory>
                <ErrorMessage>{err.message}</ErrorMessage>
              </ErrorItem>
            );
          })}
        </Modal>
      </>
    );
  }
);
