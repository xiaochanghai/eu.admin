import React, { useState } from "react";
import {
  Alert,
  Button,
  Card,
  Descriptions,
  DescriptionsProps,
  Divider,
  Form,
  Input,
  InputNumber,
  Result,
  Select,
  Space,
  Steps,
  Typography
} from "antd";
import "./index.less";

const { Title, Text, Paragraph } = Typography;

const FirstContent = ({ next }: any) => {
  const [form] = Form.useForm();

  const onFinish = (values: any) => {
    console.log("Success:", values);
    next();
  };

  const onFinishFailed = (errorInfo: any) => {
    console.log("Failed:", errorInfo);
  };

  return (
    <Form
      name="first"
      layout="vertical"
      className="first-form"
      form={form}
      onFinish={onFinish}
      onFinishFailed={onFinishFailed}
      initialValues={{
        payAccount: "antd",
        receiverMode: "alipay",
        receiverAccount: "test@example.com",
        receiverName: "Alex",
        amount: "500"
      }}
    >
      <Form.Item label="付款账户" name="payAccount" rules={[{ required: true }]}>
        <Select options={[{ value: "antd", label: "ant-design@alipay.com" }]} placeholder="请选择付款账户" allowClear />
      </Form.Item>
      <Form.Item label="收款账户" className="mb0">
        <Form.Item
          name="receiverMode"
          rules={[{ required: true, message: "请选择" }]}
          style={{ display: "inline-block", width: "calc(30% - 8px)" }}
        >
          <Select
            options={[
              { value: "alipay", label: "支付宝" },
              { value: "bank", label: "银行卡" }
            ]}
            placeholder="请选择"
            allowClear
          />
        </Form.Item>
        <Form.Item
          name="receiverAccount"
          rules={[{ required: true, message: "请输入收款账户" }]}
          style={{ display: "inline-block", width: "70%", marginLeft: "8px" }}
        >
          <Input allowClear placeholder="请填写收款账户" />
        </Form.Item>
      </Form.Item>
      <Form.Item label="收款人姓名" name="receiverName" rules={[{ required: true }]}>
        <Input />
      </Form.Item>
      <Form.Item label="转账金额" name="amount" rules={[{ required: true }]}>
        <InputNumber prefix="¥" style={{ width: "100%" }} />
      </Form.Item>
      <Form.Item wrapperCol={{ span: 5 }}>
        <Button type="primary" htmlType="submit">
          下一步
        </Button>
      </Form.Item>
    </Form>
  );
};

const SecondContent = ({ prev, next }: any) => {
  const [form] = Form.useForm();

  const onFinish = (values: any) => {
    console.log("Success:", values);
    next();
  };

  const onFinishFailed = (errorInfo: any) => {
    console.log("Failed:", errorInfo);
  };

  const items: DescriptionsProps["items"] = [
    { key: "1", label: "付款账户", children: "ant-design@alipay.com" },
    { key: "2", label: "收款账户", children: "test@example.com" },
    { key: "3", label: "收款人姓名", children: "Alex" },
    { key: "4", label: "转账金额", children: "500.00 元" }
  ];

  return (
    <Form name="second" layout="vertical" className="second-form" form={form} onFinish={onFinish} onFinishFailed={onFinishFailed}>
      <Alert message="确认转账后，资金将直接打入对方账户，无法退回。" type="info" showIcon />
      <Descriptions column={1} bordered items={items} className="mb20 mt20" />
      <Divider />
      <Form.Item label="支付密码" name="password" rules={[{ required: true, message: "需要支付密码才能进行支付" }]}>
        <Input.Password placeholder="请输入支付密码" />
      </Form.Item>
      <Form.Item>
        <Space>
          <Button onClick={prev}>上一步</Button>
          <Button type="primary" htmlType="submit">
            下一步
          </Button>
        </Space>
      </Form.Item>
    </Form>
  );
};

const LastContent = ({ setCurrent }: any) => {
  return (
    <Result
      status="success"
      title="Transfer successful!"
      subTitle="Expected to arrive in two hours. Please check your account carefully."
      extra={[
        <Button type="primary" onClick={() => setCurrent(0)}>
          再转一笔
        </Button>
      ]}
    >
      <div className="desc">
        <Paragraph>付款账户：ant-design@alipay.com</Paragraph>
        <Paragraph>收款账户：test@example.com</Paragraph>
        <Paragraph>收款人姓名：Alex</Paragraph>
        <Paragraph>转账金额：500.00元</Paragraph>
      </div>
    </Result>
  );
};

const StepForm: React.FC = () => {
  const [current, setCurrent] = useState(0);

  const next = () => {
    setCurrent(current + 1);
  };

  const prev = () => {
    setCurrent(current - 1);
  };

  const steps = [
    {
      title: "填写转账信息",
      content: <FirstContent next={next} />
    },
    {
      title: "确认转账信息",
      content: <SecondContent prev={prev} next={next} />
    },
    {
      title: "完成",
      content: <LastContent setCurrent={setCurrent} />
    }
  ];

  return (
    <React.Fragment>
      <Card className="mb10">
        <Title level={4} className="mb15">
          分步表单
        </Title>
        <Text>将一个冗长或用户不熟悉的表单任务分成多个步骤，指导用户完成。</Text>
      </Card>

      <Card className="setup-form">
        <Steps current={current} items={steps} />
        <div className="setup-form-content">{steps[current].content}</div>
      </Card>
    </React.Fragment>
  );
};

export default StepForm;
