import React, { memo } from "react";
import { Button, Result } from "antd";
import { Icon } from "@/components";

interface SuccessStepProps {
  onReset: () => void;
  showResetButton?: boolean;
}

export const SuccessStep: React.FC<SuccessStepProps> = memo(({ onReset, showResetButton = true }) => {
  return (
    <Result
      style={{ padding: 20 }}
      status="success"
      title="导入成功"
      extra={
        showResetButton
          ? [
              <Button type="primary" key="reset" icon={<Icon name="RollbackOutlined" />} onClick={onReset}>
                返回
              </Button>
            ]
          : undefined
      }
    />
  );
});

SuccessStep.displayName = "SuccessStep";
