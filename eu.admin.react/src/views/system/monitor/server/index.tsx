import { useEffect, useState } from "react";
import { Card, Col, Row, Spin, Alert } from "antd";
import type { CSSProperties } from "react";
import http from "@/api";

// 类型定义
interface ServerBase {
  HostName?: string;
  SystemOs?: string;
  OsArchitecture?: string;
  ProcessorCount?: number;
  SysRunTime?: string;
  RemoteIp?: string;
  LocalIp?: string;
  FrameworkDescription?: string;
  Wwwroot?: string;
  Environment?: string;
  Stage?: string;
}

interface ServerUsed {
  StartTime?: string;
  RunTime?: string;
}

interface InfoItem {
  label: string;
  key: keyof ServerBase | keyof ServerUsed;
  dataSource: "base" | "used";
}

// 样式常量
const ROW_STYLE: CSSProperties = {
  borderBottom: "1px solid #e8e8e8",
  padding: "5px 0"
};

const COL_CENTER_STYLE: CSSProperties = {
  textAlign: "center"
};

// 信息配置
const SYSTEM_INFO_CONFIG: InfoItem[] = [
  { label: "主机名称", key: "HostName", dataSource: "base" },
  { label: "操作系统", key: "SystemOs", dataSource: "base" },
  { label: "系统架构", key: "OsArchitecture", dataSource: "base" },
  { label: "CPU核数", key: "ProcessorCount", dataSource: "base" },
  { label: "运行时长", key: "SysRunTime", dataSource: "base" },
  { label: "外网地址", key: "RemoteIp", dataSource: "base" },
  { label: "内网地址", key: "LocalIp", dataSource: "base" },
  { label: "运行框架", key: "FrameworkDescription", dataSource: "base" }
];

const USAGE_INFO_CONFIG: InfoItem[] = [
  { label: "启动时间", key: "StartTime", dataSource: "used" },
  { label: "运行时长", key: "RunTime", dataSource: "used" },
  { label: "网站目录", key: "Wwwroot", dataSource: "base" },
  { label: "开发环境", key: "Environment", dataSource: "base" },
  { label: "环境变量", key: "Stage", dataSource: "base" }
];

// 信息行组件
interface InfoRowProps {
  label: string;
  value: string | number | null | undefined;
}

const InfoRow: React.FC<InfoRowProps> = ({ label, value }) => (
  <Row style={ROW_STYLE}>
    <Col span={6} style={COL_CENTER_STYLE}>
      {label}：
    </Col>
    <Col span={18} style={COL_CENTER_STYLE}>
      {value ?? "-"}
    </Col>
  </Row>
);

const Server: React.FC = () => {
  const [serverBase, setServerBase] = useState<ServerBase>({});
  const [serverUsed, setServerUsed] = useState<ServerUsed>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>("");

  useEffect(() => {
    const fetchServerData = async () => {
      try {
        setLoading(true);
        setError("");

        // 并行请求两个接口
        const [baseRes, usedRes] = await Promise.all([
          http.get<ServerBase>("/api/Server/GetServerBase"),
          http.get<ServerUsed>("/api/Server/GetServerUsed")
        ]);

        setServerBase(baseRes.Data);
        setServerUsed(usedRes.Data);
      } catch (err) {
        setError(err instanceof Error ? err.message : "加载服务器信息失败");
      } finally {
        setLoading(false);
      }
    };

    fetchServerData();
  }, []);

  // 获取数据值的辅助函数
  const getValue = (item: InfoItem): string | number | null | undefined => {
    if (item.dataSource === "base") {
      return serverBase[item.key as keyof ServerBase];
    }
    return serverUsed[item.key as keyof ServerUsed];
  };

  if (error) {
    return <Alert message="错误" description={error} type="error" showIcon />;
  }

  return (
    <Spin spinning={loading} tip="加载中...">
      <Row gutter={16}>
        <Col span={12}>
          <Card title="系统信息" variant="outlined">
            {SYSTEM_INFO_CONFIG.map(item => (
              <InfoRow key={item.key} label={item.label} value={getValue(item)} />
            ))}
          </Card>
        </Col>
        <Col span={12}>
          <Card title="使用信息" variant="outlined">
            {USAGE_INFO_CONFIG.map(item => (
              <InfoRow key={item.key} label={item.label} value={getValue(item)} />
            ))}
          </Card>
        </Col>
      </Row>
    </Spin>
  );
};

export default Server;
