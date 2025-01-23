import React from "react";
import { Descriptions, Skeleton } from "antd";
import { message } from "@/hooks/useMessage";
import useClipboard from "@/hooks/useClipboard";
import { Icon } from "@/components/Icon";

type ModuleLogProps = {
  log: ModuleLogProps1 | null;
};
type ModuleLogProps1 = {
  TableName: string;
  CreatedBy: string;
  ID: string;
  UpdateBy: string;
  CreatedTime: string;
  UpdateTime: string;
};
const ModuleLog: React.FC<ModuleLogProps> = ({ log }) => {
  const { copyToClipboard } = useClipboard();
  const handleCopy = () => {
    if (log != null) copyToClipboard(log.ID);
    message.success("复制成功 ！");
  };
  const handleCopy1 = () => {
    if (log != null) copyToClipboard(log.TableName);
    message.success("复制成功 ！");
  };
  return (
    <React.Fragment>
      {log ? (
        <Descriptions bordered>
          <Descriptions.Item label="表名称">
            {log.TableName}
            <a className="ml-5" onClick={handleCopy1}>
              <Icon name="CopyOutlined" />
            </a>
          </Descriptions.Item>
          <Descriptions.Item label="表主键" span={2}>
            {log.ID}
            <a className="ml-5" onClick={handleCopy}>
              <Icon name="CopyOutlined" />
            </a>
          </Descriptions.Item>
          <Descriptions.Item label="创建人">{log.CreatedBy} </Descriptions.Item>
          <Descriptions.Item label="最后修改人" span={2}>
            {log.UpdateBy}
          </Descriptions.Item>
          <Descriptions.Item label="创建时间">{log.CreatedTime} </Descriptions.Item>
          <Descriptions.Item label="最后修改时间" span={2}>
            {log.UpdateTime}
          </Descriptions.Item>
        </Descriptions>
      ) : (
        <Skeleton active />
      )}
    </React.Fragment>
  );
};

export default React.memo(ModuleLog);
