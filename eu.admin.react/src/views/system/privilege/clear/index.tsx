import React, { useState } from "react";
import { Button, Card, Typography, Space, App } from "antd";
import { ExclamationCircleOutlined, DeleteOutlined } from "@ant-design/icons";
import { clearCache } from "@/api/modules/module";
import { message } from "@/hooks/useMessage";
import NProgress from "@/config/nprogress";
import { useTranslation } from "react-i18next";

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
  const { t } = useTranslation();
  const [loading, setLoading] = useState<boolean>(false);

  /**
   * 清除缓存操作
   * 调用API清除缓存，并在操作过程中显示加载状态和进度条
   */
  const handleClearCache = async () => {
    try {
      // 开始加载状态
      message.loading(t("cacheClear.clearing"), 0) as unknown as string;
      NProgress.start();
      setLoading(true);

      // 调用清除缓存API
      const { Success, Message } = (await clearCache()) as ClearCacheResponse;
      message.destroy();

      // 显示操作结果
      if (Success) message.success(Message || t("cacheClear.clearSuccess"));
      else message.error(Message || t("cacheClear.clearFailed"));
    } catch (error) {
      // 销毁加载消息
      message.destroy();

      // 错误处理
      console.error("清除缓存时发生错误:", error);
      message.error(t("cacheClear.clearFailedRetry"));
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
      title: t("cacheClear.confirmTitle"),
      icon: <ExclamationCircleOutlined />,
      content: (
        <Space orientation="vertical" size="small">
          <Text>{t("cacheClear.confirmContent")}</Text>
          <ul style={{ marginTop: 8, marginBottom: 0, paddingLeft: 20 }}>
            <li>{t("cacheClear.userPermissionCache")}</li>
            <li>{t("cacheClear.dataDictCache")}</li>
            <li>{t("cacheClear.configCache")}</li>
            <li>{t("cacheClear.otherTempData")}</li>
          </ul>
          {/* <Text type="warning" strong style={{ marginTop: 8, display: "block" }}>
            清除后可能需要重新登录，确定要继续吗？
          </Text> */}
        </Space>
      ),
      okText: t("cacheClear.okText"),
      okType: "danger",
      cancelText: t("cacheClear.cancelText"),
      onOk: handleClearCache
    });
  };

  return (
    <Card size="small" variant="outlined" title={t("cacheClear.title")}>
      <Space orientation="vertical" size="large" style={{ width: "100%" }}>
        <Space>
          <Button type="primary" danger icon={<DeleteOutlined />} onClick={showConfirm} loading={loading}>
            {t("cacheClear.clearButton")}
          </Button>
        </Space>

        <Space orientation="vertical" size="small">
          <Paragraph type="secondary" style={{ marginBottom: 0 }}>
            {t("cacheClear.solveProblems")}
          </Paragraph>
          <ul style={{ marginTop: 8, marginBottom: 0, color: "rgba(0, 0, 0, 0.45)" }}>
            <li>{t("cacheClear.problem1")}</li>
            <li>{t("cacheClear.problem2")}</li>
            <li>{t("cacheClear.problem3")}</li>
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
