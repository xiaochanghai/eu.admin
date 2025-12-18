import React, { useState } from "react";
import { Button, Card, Typography, Space, App } from "antd";
import { ExclamationCircleOutlined, DeleteOutlined } from "@ant-design/icons";
import { clearCache } from "@/api/modules/module";
import { message } from "@/hooks/useMessage";
import NProgress from "@/config/nprogress";

const { Paragraph, Text } = Typography;

// 类型定义
interface ClearCacheResponse {
  Success: boolean;
  Message: string;
}

/**
 * 系统缓存清理组件
 * 提供清空系统缓存的功能
 */
const CacheClear: React.FC = () => {
  const { modal } = App.useApp();
  const [loading, setLoading] = useState<boolean>(false);

  /**
   * 清除缓存操作
   * 调用API清除缓存，并在操作过程中显示加载状态和进度条
   */
  const handleClearCache = async () => {
    try {
      // 开始加载状态
      message.loading("缓存清理中，请稍候...", 0) as unknown as string;
      NProgress.start();
      setLoading(true);

      // 调用清除缓存API
      const { Success, Message } = (await clearCache()) as ClearCacheResponse;
      message.destroy();

      // 显示操作结果
      if (Success) message.success(Message || "缓存清理成功");
      else message.error(Message || "缓存清理失败");
    } catch (error) {
      // 销毁加载消息
      message.destroy();

      // 错误处理
      console.error("清除缓存时发生错误:", error);
      message.error("清除缓存失败，请稍后重试");
    } finally {
      NProgress.done();
      setLoading(false);
    }
  };

  /**
   * 显示确认对话框
   */
  const showConfirm = () => {
    modal.confirm({
      title: "确认清除缓存",
      icon: <ExclamationCircleOutlined />,
      content: (
        <Space orientation="vertical" size="small">
          <Text>此操作将清除系统中的所有缓存数据，包括：</Text>
          <ul style={{ marginTop: 8, marginBottom: 0, paddingLeft: 20 }}>
            <li>用户权限缓存</li>
            <li>数据字典缓存</li>
            <li>配置信息缓存</li>
            <li>其他临时数据</li>
          </ul>
          {/* <Text type="warning" strong style={{ marginTop: 8, display: "block" }}>
            清除后可能需要重新登录，确定要继续吗？
          </Text> */}
        </Space>
      ),
      okText: "确定清除",
      okType: "danger",
      cancelText: "取消",
      onOk: handleClearCache
    });
  };

  return (
    <Card size="small" variant="outlined" title="系统缓存管理">
      <Space orientation="vertical" size="large" style={{ width: "100%" }}>
        <Space>
          <Button type="primary" danger icon={<DeleteOutlined />} onClick={showConfirm} loading={loading}>
            清空缓存
          </Button>
        </Space>

        <Space orientation="vertical" size="small">
          <Paragraph type="secondary" style={{ marginBottom: 0 }}>
            清除系统缓存可以解决以下问题：
          </Paragraph>
          <ul style={{ marginTop: 8, marginBottom: 0, color: "rgba(0, 0, 0, 0.45)" }}>
            <li>数据更新不及时</li>
            <li>权限变更未生效</li>
            <li>系统配置修改后需要刷新</li>
          </ul>
          {/* <Paragraph type="warning" style={{ marginTop: 12, marginBottom: 0 }}>
            <Text strong>注意：</Text>清除缓存后，所有用户可能需要重新登录系统。
          </Paragraph> */}
        </Space>
      </Space>
    </Card>
  );
};

export default React.memo(CacheClear);
