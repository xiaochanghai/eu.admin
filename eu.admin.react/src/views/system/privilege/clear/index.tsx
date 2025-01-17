import React, { useState } from "react";
import { Button, Card } from "antd";
import { clearCache } from "@/api/modules/module";
import { message } from "@/hooks/useMessage";
import NProgress from "@/config/nprogress";

const ClearCacheView: React.FC = () => {
  const [loading, setLoading] = useState<boolean>(false);
  const ClearCache = async () => {
    message.loading("缓存清理中...", 0);
    NProgress.start();
    setLoading(true);
    let { Success, Message } = await clearCache();
    message.destroy();
    NProgress.done();
    setLoading(false);
    if (Success) message.success(Message);
  };
  return (
    <Card size="small" bordered={false}>
      <Button type="primary" onClick={ClearCache} loading={loading}>
        清空缓存
      </Button>
      <p style={{ marginTop: 10 }}>把所有缓存数据删除</p>
    </Card>
  );
};

export default ClearCacheView;
