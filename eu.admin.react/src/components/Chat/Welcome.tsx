import React from "react";
import { Welcome as Welcome1 } from "@ant-design/x";
import { Button, Space } from "antd";

import { Icon } from "@/components";

export const Welcome: React.FC<any> = React.memo(() => {
  const APP_TITLE = import.meta.env.VITE_GLOB_APP_TITLE;
  return (
    <Welcome1
      variant="borderless"
      icon="https://mdn.alipayobjects.com/huamei_iwk9zp/afts/img/A*s5sNRo5LjfQAAAAAAAAAAAAADgCCAQ/fmt.webp"
      title={`你好, 我是 ${APP_TITLE} AI`}
      description="智启制造，一问即答，AI赋能，全程智联，让制造决策快人一步，从数据到决策，助力企业高效运营！"
      // description="Base on Ant Design, AGI product interface solution, create a better intelligent vision~"
      extra={
        <Space>
          <Button icon={<Icon name="ShareAltOutlined" />} />
          <Button icon={<Icon name="EllipsisOutlined" />} />
        </Space>
      }
    />
    // <Welcome
    //         style={{
    //           width: '100%',
    //         }}
    //         variant="borderless"
    //         icon="https://mdn.alipayobjects.com/huamei_iwk9zp/afts/img/A*s5sNRo5LjfQAAAAAAAAAAAAADgCCAQ/fmt.webp"
    //         title={locale.welcome}
    //         description={locale.welcomeDescription}
    //         extra={
    //           <Space>
    //             <Button icon={<ShareAltOutlined />} />
    //             <Button icon={<EllipsisOutlined />} />
    //           </Space>
    //         }
    //       />
  );
});
