import React, { useCallback, useMemo } from "react";
import { Descriptions, Skeleton, Button, Space, Typography } from "antd";
import { CopyOutlined } from "@ant-design/icons";
import { message } from "@/hooks/useMessage";
import useClipboard from "@/hooks/useClipboard";
import { RecordLogData } from "@/api/interface/index";

const { Text } = Typography;

/**
 * 模块日志组件属性接口
 */
interface ModuleLogProps {
  /** 日志数据，可能为空 */
  log: RecordLogData | null;
  /** 是否显示加载状态 */
  loading?: boolean;
  /** 自定义样式类名 */
  className?: string;
  /** 是否显示边框 */
  bordered?: boolean;
  /** 列数配置 */
  column?: number;
}

/**
 * 复制按钮组件
 */
interface CopyButtonProps {
  value: string;
  label: string;
  disabled?: boolean;
}

const CopyButton: React.FC<CopyButtonProps> = React.memo(({ value, label, disabled }) => {
  const { copyToClipboard } = useClipboard();

  const handleCopy = useCallback(async () => {
    if (!value || disabled) return;

    try {
      await copyToClipboard(value);
      message.success(`${label}复制成功！`);
    } catch (error) {
      message.error(`${label}复制失败，请重试`);
    }
  }, [value, label, disabled, copyToClipboard]);

  return (
    <Button
      type="text"
      size="small"
      icon={<CopyOutlined />}
      onClick={handleCopy}
      disabled={disabled}
      title={`复制${label}`}
      style={{ marginLeft: 8 }}
    />
  );
});

CopyButton.displayName = "CopyButton";

/**
 * 模块日志组件
 *
 * 用于显示数据记录的元数据信息，包括创建和修改的详细信息
 * 支持一键复制功能，提供良好的用户体验
 *
 * @param props 组件属性
 */
const ModuleLog: React.FC<ModuleLogProps> = ({ log, loading = false, className, bordered = true, column = 3 }) => {
  // 描述项配置
  const descriptionsItems = useMemo(() => {
    if (!log) return [];

    return [
      {
        key: "moduleCode",
        label: "模块代码",
        children: (
          <Space>
            <Text copyable={false}>{log.ModuleCode || "-"}</Text>
            <CopyButton value={log.ModuleCode} label="模块代码" disabled={!log.ModuleCode} />
          </Space>
        ),
        span: 3
      },
      {
        key: "tableName",
        label: "表名称",
        children: (
          <Space>
            <Text copyable={false}>{log.TableName || "-"}</Text>
            <CopyButton value={log.TableName} label="表名称" disabled={!log.TableName} />
          </Space>
        ),
        span: 1
      },
      {
        key: "id",
        label: "表主键",
        children: (
          <Space>
            <Text code copyable={false}>
              {log.ID || "-"}
            </Text>
            <CopyButton value={log.ID} label="主键ID" disabled={!log.ID} />
          </Space>
        ),
        span: 2
      },
      {
        key: "createdBy",
        label: "创建人",
        children: <Text>{log.CreatedBy || "-"}</Text>,
        span: 1
      },
      {
        key: "updateBy",
        label: "最后修改人",
        children: <Text>{log.UpdateBy || "-"}</Text>,
        span: 2
      },
      {
        key: "createdTime",
        label: "创建时间",
        children: <Text>{log.CreatedTime}</Text>,
        span: 1
      },
      {
        key: "updateTime",
        label: "最后修改时间",
        children: <Text>{log.UpdateTime}</Text>,
        span: 2
      }
    ];
  }, [log]);

  // 加载状态
  if (loading || !log) {
    return <Skeleton active paragraph={{ rows: 3 }} className={className} />;
  }

  return <Descriptions bordered={bordered} column={column} size="small" className={className} items={descriptionsItems} />;
};

export default React.memo(ModuleLog);
