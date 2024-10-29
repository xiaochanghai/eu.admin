import { Button, Card, message } from "antd";
import { clearCache } from "@/api/modules/module";

const ClearCacheView: React.FC = () => {
  const ClearCache = async () => {
    message.loading("缓存清理中...", 0);
    let { Success, Message } = await clearCache();
    message.destroy();
    if (Success) message.success(Message);
  };
  return (
    <Card size="small" bordered={false}>
      <Button type="primary" onClick={ClearCache}>
        清空缓存
      </Button>
      <p style={{ marginTop: 10 }}>把所有缓存数据删除</p>
    </Card>
  );
};

export default ClearCacheView;
