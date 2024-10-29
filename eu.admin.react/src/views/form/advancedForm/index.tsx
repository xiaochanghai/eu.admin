import React from "react";
import { Card, Typography, Row, Form, Input, Col, Button, Space, Select, DatePicker, TimePicker } from "antd";
import "./index.less";

const { RangePicker } = DatePicker;
const { Text, Title } = Typography;

const AdvancedForm: React.FC = () => {
  const [form] = Form.useForm();

  const onReset = () => {
    form.resetFields();
  };

  const onFinish = (values: any) => {
    console.log(values);
  };

  return (
    <div className="advanced-form">
      <Card className="mb10">
        <Title level={4} className="mb15">
          高级表单
        </Title>
        <Text>高级表单常见于一次性输入和提交大批量数据的场景。</Text>
      </Card>

      <Form layout="vertical" name="advanced" form={form} onFinish={onFinish}>
        <Card title="仓库管理" className="mb10">
          <Row gutter={50} justify="space-between">
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="name" label="仓库名" rules={[{ required: true }]}>
                <Input placeholder="请输入仓库名称" />
              </Form.Item>
            </Col>
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="url" label="仓库域名" rules={[{ required: true }]}>
                <Input placeholder="请输入仓库域名" addonBefore="http://" addonAfter=".com" />
              </Form.Item>
            </Col>
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="owner" label="仓库管理员" rules={[{ required: true }]}>
                <Select
                  options={[
                    { label: "付晓晓", value: "xiao" },
                    { label: "周毛毛", value: "mao" }
                  ]}
                  placeholder="请选择管理员"
                />
              </Form.Item>
            </Col>
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="approver" label="审批人" rules={[{ required: true }]}>
                <Select
                  options={[
                    { label: "付晓晓", value: "xiao" },
                    { label: "周毛毛", value: "mao" }
                  ]}
                  placeholder="请选择审批员"
                />
              </Form.Item>
            </Col>
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="dateRange" label="生效日期" rules={[{ required: true }]}>
                <RangePicker style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="type" label="仓库类型" rules={[{ required: true }]}>
                <Select
                  options={[
                    { label: "私密", value: "private" },
                    { label: "公开", value: "public" }
                  ]}
                  placeholder="请选择仓库类型"
                />
              </Form.Item>
            </Col>
          </Row>
        </Card>

        <Card title="任务管理" className="mb10">
          <Row gutter={50} justify="space-between">
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="task" label="任务名" rules={[{ required: true }]}>
                <Input placeholder="请输入任务名" />
              </Form.Item>
            </Col>
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="describe" label="任务描述" rules={[{ required: true }]}>
                <Input placeholder="请输入任务描述" />
              </Form.Item>
            </Col>
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="executor" label="执行人" rules={[{ required: true }]}>
                <Select
                  options={[
                    { label: "付晓晓", value: "xiao" },
                    { label: "周毛毛", value: "mao" }
                  ]}
                  placeholder="请选择执行人"
                />
              </Form.Item>
            </Col>
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="responsible" label="责任人" rules={[{ required: true }]}>
                <Select
                  options={[
                    { label: "付晓晓", value: "xiao" },
                    { label: "周毛毛", value: "mao" }
                  ]}
                  placeholder="请选择责任人"
                />
              </Form.Item>
            </Col>
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="dateRange2" label="生效日期" rules={[{ required: true }]}>
                <TimePicker style={{ width: "100%" }} />
              </Form.Item>
            </Col>
            <Col xxl={7} md={11} xs={24}>
              <Form.Item name="type2" label="任务类型" rules={[{ required: true }]}>
                <Select
                  options={[
                    { label: "私密", value: "private" },
                    { label: "公开", value: "public" }
                  ]}
                  placeholder="请选择任务类型"
                />
              </Form.Item>
            </Col>
          </Row>
        </Card>

        <Card title="填写备注">
          <Form.Item name="remark" label="备注信息" rules={[{ required: true }]}>
            <Input.TextArea rows={4} showCount maxLength={300} placeholder="请输入备注信息" />
          </Form.Item>
        </Card>

        <div className="form-footer">
          <Space>
            <Button htmlType="button" onClick={onReset}>
              重置
            </Button>
            <Button type="primary" htmlType="submit">
              提交
            </Button>
          </Space>
        </div>
      </Form>
    </div>
  );
};

export default AdvancedForm;
