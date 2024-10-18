import React from "react";
import { Button, Card, Result } from "antd";

const Info: React.FC = () => {
  return (
    <Card className="info">
      <Result
        title="Your operation has been executed"
        subTitle="This submission will be automatically transferred to the other party's account within 24 hours."
        extra={[
          <Button type="primary" key="console">
            Go Console
          </Button>,
          <Button key="buy">Buy Again</Button>
        ]}
      />
    </Card>
  );
};

export default Info;
