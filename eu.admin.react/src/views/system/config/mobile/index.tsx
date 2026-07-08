import React, { useState, useEffect, useCallback } from "react";
import { Table, Button, Space, Tag, Popconfirm, Modal, Form, Input, Select, message } from "antd";
import { PlusOutlined, EditOutlined, DeleteOutlined, SendOutlined } from "@ant-design/icons";
import { useNavigate } from "react-router-dom";
import {
  getMobilePageList,
  createMobilePage,
  deleteMobilePage,
  publishMobilePage,
  SmMobilePageConfig
} from "@/api/modules/mobileConfig";

const { Option } = Select;

const MobileConfigList: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<SmMobilePageConfig[]>([]);
  const [total, setTotal] = useState(0);
  const [current, setCurrent] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [modalVisible, setModalVisible] = useState(false);
  const [confirmLoading, setConfirmLoading] = useState(false);
  const [form] = Form.useForm();

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const res = await getMobilePageList({
        paramData: JSON.stringify({ page: current, limit: pageSize }),
        sorter: "{}",
        filter: "{}"
      });
      if (res?.Data) {
        setData(res.Data);
        setTotal(res.Data?.length || 0);
      }
    } catch (err) {
      console.error("获取数据失败", err);
    } finally {
      setLoading(false);
    }
  }, [current, pageSize]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleCreate = async () => {
    try {
      const values = await form.validateFields();
      setConfirmLoading(true);
      const res = await createMobilePage({
        ...values,
        PageType: "list",
        Version: 0,
        IsPublished: false,
        ConfigJson: "{}"
      });
      if (res?.Success) {
        message.success("创建成功");
        setModalVisible(false);
        form.resetFields();
        fetchData();
        if (res.Data) {
          navigate(`/system/config/mobile/editor/${res.Data}`);
        }
      }
    } catch (err) {
      console.error("创建失败", err);
    } finally {
      setConfirmLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    try {
      const res = await deleteMobilePage(id);
      if (res?.Success) {
        message.success("删除成功");
        fetchData();
      }
    } catch (err) {
      console.error("删除失败", err);
    }
  };

  const handlePublish = async (id: string) => {
    try {
      const res = await publishMobilePage(id);
      if (res?.Success) {
        message.success("发布成功");
        fetchData();
      }
    } catch (err) {
      console.error("发布失败", err);
    }
  };

  const handleEdit = (record: SmMobilePageConfig) => {
    navigate(`/system/config/mobile/editor/${record.ID}`);
  };

  const columns = [
    {
      title: "页面编码",
      dataIndex: "PageCode",
      key: "PageCode",
      width: 180
    },
    {
      title: "页面名称",
      dataIndex: "PageName",
      key: "PageName",
      width: 150
    },
    {
      title: "应用范围",
      dataIndex: "AppScope",
      key: "AppScope",
      width: 120,
      render: (v: string) => v ? <Tag>{v}</Tag> : <Tag color="default">全部</Tag>
    },
    {
      title: "页面标题",
      dataIndex: "Title",
      key: "Title",
      width: 120
    },
    {
      title: "版本",
      dataIndex: "Version",
      key: "Version",
      width: 80,
      render: (v: number) => v ? `v${v}` : "-"
    },
    {
      title: "状态",
      dataIndex: "IsPublished",
      key: "IsPublished",
      width: 100,
      render: (v: boolean) => v ? <Tag color="green">已发布</Tag> : <Tag color="default">草稿</Tag>
    },
    {
      title: "更新时间",
      dataIndex: "UpdateTime",
      key: "UpdateTime",
      width: 180
    },
    {
      title: "操作",
      key: "action",
      width: 200,
      render: (_: any, record: SmMobilePageConfig) => (
        <Space>
          <Button type="link" size="small" icon={<EditOutlined />} onClick={() => handleEdit(record)}>
            编辑
          </Button>
          <Popconfirm title="确认发布该配置？" onConfirm={() => handlePublish(record.ID)} okText="确定" cancelText="取消">
            <Button type="link" size="small" icon={<SendOutlined />}>
              发布
            </Button>
          </Popconfirm>
          <Popconfirm title="确认删除该配置？" onConfirm={() => handleDelete(record.ID)} okText="确定" cancelText="取消">
            <Button type="link" size="small" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      )
    }
  ];

  return (
    <div className="main-container">
      <div className="card-header" style={{ marginBottom: 16 }}>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setModalVisible(true)}>
          新建配置
        </Button>
      </div>

      <Table
        columns={columns}
        dataSource={data}
        loading={loading}
        rowKey="ID"
        pagination={{
          current,
          pageSize,
          total,
          showSizeChanger: true,
          showTotal: (t: number) => `共 ${t} 条`,
          onChange: (page, size) => {
            setCurrent(page);
            setPageSize(size);
          }
        }}
      />

      <Modal
        title="新建移动端页面配置"
        open={modalVisible}
        onOk={handleCreate}
        onCancel={() => {
          setModalVisible(false);
          form.resetFields();
        }}
        confirmLoading={confirmLoading}
        destroyOnClose
      >
        <Form form={form} layout="vertical" preserve={false}>
          <Form.Item
            name="PageCode"
            label="页面编码"
            rules={[{ required: true, message: "请输入页面编码" }]}
          >
            <Input placeholder="例如: EQUIPMENT_LIST" />
          </Form.Item>
          <Form.Item
            name="PageName"
            label="页面名称"
            rules={[{ required: true, message: "请输入页面名称" }]}
          >
            <Input placeholder="例如: 设备列表" />
          </Form.Item>
          <Form.Item name="AppScope" label="应用范围">
            <Select placeholder="选择应用范围" allowClear>
              <Option value="">全部</Option>
              <Option value="admin">管理端</Option>
              <Option value="repair">维修端</Option>
              <Option value="operator">操作端</Option>
            </Select>
          </Form.Item>
          <Form.Item name="Title" label="页面标题">
            <Input placeholder="例如: 设备" />
          </Form.Item>
          <Form.Item name="Remark" label="备注">
            <Input.TextArea rows={3} placeholder="备注说明" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default MobileConfigList;
