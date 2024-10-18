import { Card, Typography } from "antd";
import React from "react";

const { Text, Title } = Typography;

const AdvancedDetails: React.FC = () => {
  return (
    <React.Fragment>
      <Card className="mb10">
        <Title level={4} className="mb15">
          高级详情页
        </Title>
        <Text>高级详情页通常用于更复杂和深入的信息展示。</Text>
      </Card>
      <Card>高级详情页</Card>
    </React.Fragment>
  );
};

export default AdvancedDetails;
