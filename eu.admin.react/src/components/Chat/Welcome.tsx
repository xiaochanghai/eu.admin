import React from "react";
import { EllipsisOutlined, ShareAltOutlined } from "@ant-design/icons";
import { Welcome as Welcome1 } from "@ant-design/x";
import { Button, Space } from "antd";
const APP_TITLE = import.meta.env.VITE_GLOB_APP_TITLE;

export const Welcome: React.FC<any> = React.memo(() => {
  return (
    <Welcome1
      variant="borderless"
      icon="https://mdn.alipayobjects.com/huamei_iwk9zp/afts/img/A*s5sNRo5LjfQAAAAAAAAAAAAADgCCAQ/fmt.webp"
      title={`你好, 我是 ${APP_TITLE} AI`}
      description="Base on Ant Design, AGI product interface solution, create a better intelligent vision~"
      extra={
        <Space>
          <Button icon={<ShareAltOutlined />} />
          <Button icon={<EllipsisOutlined />} />
        </Space>
      }
    />
  );
});
