import React, { useCallback } from "react";
import { Descriptions, Skeleton } from "antd";
import { message } from "@/hooks/useMessage";
import useClipboard from "@/hooks/useClipboard";
import { Icon } from "@/components/Icon";

/**
 * 模块日志数据接口
 */
interface ModuleLogData {
  /** 表名称 */
  TableName: string;
  /** 创建人 */
  CreatedBy: string;
  /** 记录ID */
  ID: string;
  /** 更新人 */
  UpdateBy: string;
  /** 创建时间 */
  CreatedTime: string;
  /** 更新时间 */
  UpdateTime: string;
}

/**
 * 模块日志组件属性接口
 */
interface ModuleLogProps {
  /** 日志数据，可能为空 */
  log: ModuleLogData | null;
}

/**
 * 模块日志组件
 *
 * 用于显示数据记录的元数据信息，包括创建和修改的详细信息
 *
 * @param props 组件属性
 */
const ModuleLog: React.FC<ModuleLogProps> = ({ log }) => {
  const { copyToClipboard } = useClipboard();

  /**
   * 复制记录ID到剪贴板
   */
  const handleCopyId = useCallback(() => {
    if (log?.ID) {
      copyToClipboard(log.ID);
      message.success("复制成功！");
    }
  }, [log, copyToClipboard]);

  /**
   * 复制表名称到剪贴板
   */
  const handleCopyTableName = useCallback(() => {
    if (log?.TableName) {
      copyToClipboard(log.TableName);
      message.success("复制成功！");
    }
  }, [log, copyToClipboard]);

  /**
   * 渲染日志详情
   */
  const renderLogDetails = () => {
    if (!log) return <Skeleton active />;

    return (
      <Descriptions bordered>
        <Descriptions.Item label="表名称">
          {log.TableName}
          <a className="ml-5" onClick={handleCopyTableName} title="复制表名称">
            <Icon name="CopyOutlined" />
          </a>
        </Descriptions.Item>
        <Descriptions.Item label="表主键" span={2}>
          {log.ID}
          <a className="ml-5" onClick={handleCopyId} title="复制ID">
            <Icon name="CopyOutlined" />
          </a>
        </Descriptions.Item>
        <Descriptions.Item label="创建人">{log.CreatedBy}</Descriptions.Item>
        <Descriptions.Item label="最后修改人" span={2}>
          {log.UpdateBy}
        </Descriptions.Item>
        <Descriptions.Item label="创建时间">{log.CreatedTime}</Descriptions.Item>
        <Descriptions.Item label="最后修改时间" span={2}>
          {log.UpdateTime}
        </Descriptions.Item>
      </Descriptions>
    );
  };

  return renderLogDetails();
};

export default React.memo(ModuleLog);
