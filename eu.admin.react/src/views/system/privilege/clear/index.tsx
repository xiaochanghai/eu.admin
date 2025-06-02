import React, { useState } from "react";
import { Button, Card, Typography } from "antd";
import { clearCache } from "@/api/modules/module";
import { message } from "@/hooks/useMessage";
import NProgress from "@/config/nprogress";

const Index: React.FC = () => {
  // 加载状态管理
  const [loading, setLoading] = useState<boolean>(false);

  /**
   * 清除缓存操作
   *
   * 调用API清除缓存，并在操作过程中显示加载状态和进度条
   * 操作完成后根据结果显示相应的成功或失败消息
   */
  const handleClearCache = async () => {
    try {
      // 开始加载状态
      message.loading("缓存清理中...", 0);
      NProgress.start();
      setLoading(true);

      // 调用清除缓存API
      const { Success, Message } = await clearCache();

      // 显示操作结果
      if (Success) message.success(Message);
      else message.error(Message);
    } catch (error) {
      // 错误处理
      console.error("清除缓存时发生错误:", error);
      message.error("清除缓存失败，请稍后重试");
    } finally {
      // 无论成功失败，都需要重置状态
      message.destroy();
      NProgress.done();
      setLoading(false);
    }
  };

  return (
    <Card size="small" bordered={false} title="系统缓存管理">
      <div style={{ display: "flex", flexDirection: "column", alignItems: "flex-start", gap: "16px" }}>
        <Button type="primary" onClick={handleClearCache} loading={loading}>
          清空缓存
        </Button>

        <Typography.Paragraph type="secondary">把所有缓存数据删除</Typography.Paragraph>
      </div>
    </Card>
  );
};

export default React.memo(Index);
