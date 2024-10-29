import React from "react";
import { Descriptions, Divider, Steps, Typography, Badge, Card } from "antd";
import { LoadingOutlined, SmileOutlined, SolutionOutlined, UserOutlined } from "@ant-design/icons";
import { ProTable } from "@ant-design/pro-components";
import type { ProColumns } from "@ant-design/pro-components";

const { Title, Text } = Typography;

export type BasicProgress = {
  key: string;
  time: string;
  rate: string;
  status: string;
  operator: string;
  cost: string;
};

const progressColumns: ProColumns<BasicProgress>[] = [
  { title: "时间", dataIndex: "time", key: "time" },
  { title: "当前进度", dataIndex: "rate", key: "rate" },
  {
    title: "状态",
    dataIndex: "status",
    key: "status",
    render: (text: React.ReactNode) => {
      if (text === "success") return <Badge status="success" text="成功" />;
      return <Badge status="processing" text="进行中" />;
    }
  },
  { title: "操作员ID", dataIndex: "operator", key: "operator" },
  { title: "耗时", dataIndex: "cost", key: "cost" }
];

const basicProgress: BasicProgress[] = [
  { key: "1", time: "2019-11-10 14:10", rate: "联系客户", status: "success", operator: "取货员 ID1234", cost: "5mins" },
  { key: "2", time: "2019-11-09 10:10", rate: "取货员出发", status: "processing", operator: "取货员 ID1234", cost: "1h" },
  { key: "3", time: "2019-11-08 08:10", rate: "取货员接单", status: "processing", operator: "取货员 ID1234", cost: "5mins" },
  { key: "4", time: "2019-11-07 08:10", rate: "申请审批通过", status: "success", operator: "系统", cost: "1h" },
  { key: "5", time: "2019-11-07 07:10", rate: "发起退货申请", status: "processing", operator: "用户", cost: "5mins" }
];

const BasicDetails: React.FC = () => {
  return (
    <React.Fragment>
      <Card className="mb10">
        <Title level={4} className="mb15">
          基础详情页
        </Title>
        <Text>基础详情页常用于展示特定实体或项目的详细信息。</Text>
      </Card>
      <Card>
        <Descriptions title="退款申请">
          <Descriptions.Item label="取货单号">1000000000</Descriptions.Item>
          <Descriptions.Item label="状态">已取货</Descriptions.Item>
          <Descriptions.Item label="销售单号">1234123421</Descriptions.Item>
          <Descriptions.Item label="子订单">3214321432</Descriptions.Item>
        </Descriptions>
        <Divider />
        <Descriptions title="用户信息">
          <Descriptions.Item label="用户姓名">付小小</Descriptions.Item>
          <Descriptions.Item label="联系电话">18100000000</Descriptions.Item>
          <Descriptions.Item label="常用快递">菜鸟仓储</Descriptions.Item>
          <Descriptions.Item label="取货地址">浙江省杭州市西湖区万塘路18号</Descriptions.Item>
          <Descriptions.Item label="备注">无</Descriptions.Item>
        </Descriptions>
        <Divider />
        <Title level={5} className="mb25">
          审批流程表
        </Title>
        <Steps
          className="mb30"
          items={[
            { title: "申请", status: "finish", icon: <UserOutlined /> },
            { title: "用户信息", status: "finish", icon: <SolutionOutlined /> },
            { title: "审核中", status: "process", icon: <LoadingOutlined /> },
            { title: "结束", status: "wait", icon: <SmileOutlined /> }
          ]}
        />
        <Divider />
        <Title level={5} className="mb20">
          退货商品
        </Title>
        <ProTable
          bordered
          pagination={false}
          search={false}
          options={false}
          toolBarRender={false}
          dataSource={basicProgress}
          columns={progressColumns}
        />
      </Card>
    </React.Fragment>
  );
};

export default BasicDetails;
