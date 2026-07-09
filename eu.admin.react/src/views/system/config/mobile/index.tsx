import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  App,
  Badge,
  Button,
  Card,
  Empty,
  Form,
  Input,
  Modal,
  Popconfirm,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography
} from "antd";
import type { TableColumnsType, TablePaginationConfig } from "antd";
import {
  AppstoreOutlined,
  DeleteOutlined,
  EditOutlined,
  MobileOutlined,
  PlusOutlined,
  ReloadOutlined,
  SearchOutlined,
  SendOutlined
} from "@ant-design/icons";
import { useNavigate } from "react-router-dom";
import {
  createMobilePage,
  deleteMobilePage,
  getMobilePageList,
  publishMobilePage,
  SmMobilePageConfig
} from "@/api/modules/mobileConfig";

const { Text } = Typography;

type QueryFormValues = {
  keyword?: string;
  appScope?: string;
  publishState?: "published" | "draft";
};

type CreateFormValues = {
  PageCode: string;
  PageName: string;
  AppScope?: string;
  Title?: string;
  Remark?: string;
};

const scopeColorMap: Record<string, string> = {
  admin: "blue",
  repair: "orange",
  operator: "green"
};

const scopeLabelMap: Record<string, string> = {
  admin: "管理端",
  repair: "维修端",
  operator: "操作端"
};

const getErrorMessage = (error: unknown, fallback: string) => {
  if (error instanceof Error && error.message) return error.message;
  return fallback;
};

const normalizePageCode = (value?: string) => value?.trim().toUpperCase().replace(/\s+/g, "_") || "";

const MobileConfigList: React.FC = () => {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const [queryForm] = Form.useForm<QueryFormValues>();
  const [createForm] = Form.useForm<CreateFormValues>();
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<SmMobilePageConfig[]>([]);
  const [total, setTotal] = useState(0);
  const [current, setCurrent] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [modalVisible, setModalVisible] = useState(false);
  const [confirmLoading, setConfirmLoading] = useState(false);
  const [publishingId, setPublishingId] = useState<string>();
  const [deletingId, setDeletingId] = useState<string>();
  const [query, setQuery] = useState<QueryFormValues>({});

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const filter = {
        keyword: query.keyword?.trim(),
        AppScope: query.appScope,
        IsPublished: query.publishState === "published" ? true : query.publishState === "draft" ? false : undefined
      };

      const res = await getMobilePageList({
        paramData: JSON.stringify({ page: current, limit: pageSize }),
        sorter: "{}",
        filter: JSON.stringify(filter)
      });

      const rows = Array.isArray(res?.Data) ? res.Data : [];
      setData(rows);
      setTotal(rows.length);
    } catch (error) {
      message.error(getErrorMessage(error, "获取移动端页面配置失败"));
    } finally {
      setLoading(false);
    }
  }, [current, message, pageSize, query]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const summary = useMemo(
    () => ({
      published: data.filter(item => item.IsPublished).length,
      draft: data.filter(item => !item.IsPublished).length
    }),
    [data]
  );

  const handleCreate = async () => {
    try {
      const values = await createForm.validateFields();
      setConfirmLoading(true);
      const pageCode = normalizePageCode(values.PageCode);
      const res = await createMobilePage({
        ...values,
        PageCode: pageCode,
        PageType: "list",
        Version: 0,
        IsPublished: false,
        ConfigJson: "{}"
      });

      if (res?.Success) {
        message.success("创建成功，正在进入配置器");
        setModalVisible(false);
        createForm.resetFields();
        fetchData();
        if (res.Data) navigate(`/system/config/mobile/editor?id=${res.Data}`);
      } else {
        message.error(res?.Message || "创建失败");
      }
    } catch (error) {
      if (error && typeof error === "object" && "errorFields" in error) return;
      message.error(getErrorMessage(error, "创建失败"));
    } finally {
      setConfirmLoading(false);
    }
  };

  const handleDelete = async (id: string) => {
    setDeletingId(id);
    try {
      const res = await deleteMobilePage(id);
      if (res?.Success) {
        message.success("删除成功");
        fetchData();
      } else {
        message.error(res?.Message || "删除失败");
      }
    } catch (error) {
      message.error(getErrorMessage(error, "删除失败"));
    } finally {
      setDeletingId(undefined);
    }
  };

  const handlePublish = async (id: string) => {
    setPublishingId(id);
    try {
      const res = await publishMobilePage(id);
      if (res?.Success) {
        message.success("发布成功");
        fetchData();
      } else {
        message.error(res?.Message || "发布失败");
      }
    } catch (error) {
      message.error(getErrorMessage(error, "发布失败"));
    } finally {
      setPublishingId(undefined);
    }
  };

  const handleEdit = (record: SmMobilePageConfig) => {
    navigate(`/system/config/mobile/editor?id=${record.ID}`);
  };

  const handleSearch = (values: QueryFormValues) => {
    setCurrent(1);
    setQuery(values);
  };

  const handleReset = () => {
    queryForm.resetFields();
    setCurrent(1);
    setQuery({});
  };

  const columns: TableColumnsType<SmMobilePageConfig> = [
    {
      title: "页面编码",
      dataIndex: "PageCode",
      key: "PageCode",
      width: 210,
      fixed: "left",
      render: (value: string) => (
        <Text code style={{ fontSize: 13 }}>
          {value || "-"}
        </Text>
      )
    },
    {
      title: "页面名称",
      dataIndex: "PageName",
      key: "PageName",
      width: 180,
      render: (value: string, record) => (
        <Space direction="vertical" size={0}>
          <Button type="link" size="small" style={{ padding: 0, height: 22, fontWeight: 600 }} onClick={() => handleEdit(record)}>
            {value || record.Title || "未命名页面"}
          </Button>
          <Text type="secondary" style={{ fontSize: 12 }}>
            {record.PageType === "form" ? "表单页" : "列表页"}
          </Text>
        </Space>
      )
    },
    {
      title: "应用范围",
      dataIndex: "AppScope",
      key: "AppScope",
      width: 120,
      render: (value: string) =>
        value ? (
          <Tag color={scopeColorMap[value] || "default"}>{scopeLabelMap[value] || value}</Tag>
        ) : (
          <Tag>全部</Tag>
        )
    },
    {
      title: "页面标题",
      dataIndex: "Title",
      key: "Title",
      width: 160,
      ellipsis: true,
      render: (value: string) => value || "-"
    },
    {
      title: "版本",
      dataIndex: "Version",
      key: "Version",
      width: 90,
      align: "center",
      render: (value: number) => (value ? <Text strong>v{value}</Text> : <Text type="secondary">-</Text>)
    },
    {
      title: "状态",
      dataIndex: "IsPublished",
      key: "IsPublished",
      width: 110,
      align: "center",
      render: (value: boolean) =>
        value ? <Badge status="success" text="已发布" /> : <Badge status="default" text="草稿" />
    },
    {
      title: "更新时间",
      dataIndex: "UpdateTime",
      key: "UpdateTime",
      width: 180,
      render: (value: string) => <Text type="secondary">{value || "-"}</Text>
    },
    {
      title: "操作",
      key: "action",
      width: 210,
      fixed: "right",
      render: (_, record) => (
        <Space size={2}>
          <Tooltip title="编辑配置">
            <Button type="text" size="small" icon={<EditOutlined />} onClick={() => handleEdit(record)}>
              编辑
            </Button>
          </Tooltip>
          <Popconfirm
            title="发布配置"
            description="发布后移动端会使用当前配置。"
            onConfirm={() => handlePublish(record.ID)}
            okText="发布"
            cancelText="取消"
          >
            <Button
              type="text"
              size="small"
              icon={<SendOutlined />}
              loading={publishingId === record.ID}
              style={{ color: "#059669" }}
            >
              发布
            </Button>
          </Popconfirm>
          <Popconfirm
            title="删除配置"
            description="删除后无法恢复。"
            onConfirm={() => handleDelete(record.ID)}
            okText="删除"
            cancelText="取消"
            okButtonProps={{ danger: true }}
          >
            <Button type="text" size="small" danger icon={<DeleteOutlined />} loading={deletingId === record.ID}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      )
    }
  ];

  const pagination: TablePaginationConfig = {
    current,
    pageSize,
    total,
    showSizeChanger: true,
    showQuickJumper: true,
    showTotal: value => `共 ${value} 条`,
    onChange: (page, size) => {
      setCurrent(page);
      setPageSize(size);
    }
  };

  return (
    <div style={{ padding: 24, background: "#f6f8fb", minHeight: "100%" }}>
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "flex-start",
          gap: 16,
          marginBottom: 16
        }}
      >
        <div>
          <Space align="center" size={10}>
            <MobileOutlined style={{ fontSize: 22, color: "#2563eb" }} />
            <Typography.Title level={4} style={{ margin: 0 }}>
              移动端页面配置
            </Typography.Title>
          </Space>
          <Text type="secondary">管理移动端低代码页面，支持列表页、表单页、字段绑定、发布和版本配置。</Text>
        </div>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={fetchData} loading={loading}>
            刷新
          </Button>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setModalVisible(true)}>
            新建配置
          </Button>
        </Space>
      </div>

      <Card style={{ marginBottom: 12 }} styles={{ body: { padding: 16 } }}>
        <Form form={queryForm} layout="inline" onFinish={handleSearch} style={{ rowGap: 12 }}>
          <Form.Item name="keyword" style={{ minWidth: 260 }}>
            <Input allowClear prefix={<SearchOutlined />} placeholder="搜索页面编码、名称或标题" />
          </Form.Item>
          <Form.Item name="appScope" style={{ minWidth: 150 }}>
            <Select
              allowClear
              placeholder="应用范围"
              options={[
                { label: "管理端", value: "admin" },
                { label: "维修端", value: "repair" },
                { label: "操作端", value: "operator" }
              ]}
            />
          </Form.Item>
          <Form.Item name="publishState" style={{ minWidth: 150 }}>
            <Select
              allowClear
              placeholder="发布状态"
              options={[
                { label: "已发布", value: "published" },
                { label: "草稿", value: "draft" }
              ]}
            />
          </Form.Item>
          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit" icon={<SearchOutlined />}>
                查询
              </Button>
              <Button onClick={handleReset}>重置</Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>

      <Card
        title={
          <Space size={16}>
            <span>配置列表</span>
            <Text type="secondary" style={{ fontSize: 13 }}>
              已发布 {summary.published}，草稿 {summary.draft}
            </Text>
          </Space>
        }
        styles={{ body: { padding: 0 } }}
      >
        <Table
          columns={columns}
          dataSource={data}
          loading={loading}
          rowKey="ID"
          size="middle"
          pagination={pagination}
          scroll={{ x: 1260 }}
          locale={{
            emptyText: (
              <Empty
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                description={query.keyword || query.appScope || query.publishState ? "没有匹配的配置" : "暂无移动端页面配置"}
              >
                {!query.keyword && !query.appScope && !query.publishState && (
                  <Button type="primary" icon={<PlusOutlined />} onClick={() => setModalVisible(true)}>
                    新建第一个配置
                  </Button>
                )}
              </Empty>
            )
          }}
          onRow={record => ({
            onDoubleClick: () => handleEdit(record)
          })}
        />
      </Card>

      <Modal
        title={
          <Space>
            <AppstoreOutlined style={{ color: "#2563eb" }} />
            <span>新建移动端页面配置</span>
          </Space>
        }
        open={modalVisible}
        onOk={handleCreate}
        onCancel={() => {
          setModalVisible(false);
          createForm.resetFields();
        }}
        confirmLoading={confirmLoading}
        destroyOnClose
        okText="创建并编辑"
        cancelText="取消"
      >
        <Form form={createForm} layout="vertical" preserve={false} style={{ marginTop: 16 }}>
          <Form.Item
            name="PageCode"
            label="页面编码"
            rules={[
              { required: true, message: "请输入页面编码" },
              { pattern: /^[A-Za-z][A-Za-z0-9_]*$/, message: "以字母开头，仅支持字母、数字和下划线" }
            ]}
            normalize={normalizePageCode}
            extra="唯一标识，建议使用 EQUIPMENT_LIST 这类大写下划线格式。"
          >
            <Input placeholder="例如：EQUIPMENT_LIST" autoFocus />
          </Form.Item>
          <Form.Item name="PageName" label="页面名称" rules={[{ required: true, message: "请输入页面名称" }]}>
            <Input placeholder="例如：设备列表" />
          </Form.Item>
          <Form.Item name="AppScope" label="应用范围">
            <Select
              allowClear
              placeholder="不选表示全部"
              options={[
                { label: "管理端", value: "admin" },
                { label: "维修端", value: "repair" },
                { label: "操作端", value: "operator" }
              ]}
            />
          </Form.Item>
          <Form.Item name="Title" label="页面标题">
            <Input placeholder="显示在手机导航栏的标题" />
          </Form.Item>
          <Form.Item name="Remark" label="备注">
            <Input.TextArea rows={3} placeholder="补充配置用途或注意事项" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default MobileConfigList;
