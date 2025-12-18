import React, { CSSProperties, ReactNode, useMemo } from "react";
import { Icon } from "@/components";

export type ResultStatusType = "success" | "error" | "info" | "warning" | "404" | "403" | "500";

export interface ResultProps {
  /** 结果的状态，决定图标和颜色 */
  status?: ResultStatusType;
  /** 自定义图标 */
  icon?: ReactNode;
  /** 标题文字 */
  title?: ReactNode;
  /** 补充描述 */
  subTitle?: ReactNode;
  /** 操作区域 */
  extra?: ReactNode;
  /** 自定义样式 */
  style?: CSSProperties;
  /** 自定义类名 */
  className?: string;
  /** 图标大小 */
  iconSize?: number;
}

// SVG 图标组件
const NotFoundIcon: React.FC<{ size?: number }> = ({ size = 48 }) => (
  <svg viewBox="0 0 24 24" fill="none" width={size} height={size}>
    <text x="50%" y="50%" textAnchor="middle" dy=".3em" fill="currentColor" fontSize="10" fontWeight="bold">
      404
    </text>
  </svg>
);

const ForbiddenIcon: React.FC<{ size?: number }> = ({ size = 48 }) => (
  <svg viewBox="0 0 24 24" fill="none" width={size} height={size}>
    <rect x="5" y="11" width="14" height="10" rx="2" stroke="currentColor" strokeWidth="2" />
    <path d="M7 11V7C7 4.79086 8.79086 3 11 3H13C15.2091 3 17 4.79086 17 7V11" stroke="currentColor" strokeWidth="2" />
  </svg>
);

const ServerErrorIcon: React.FC<{ size?: number }> = ({ size = 48 }) => (
  <svg viewBox="0 0 24 24" fill="none" width={size} height={size}>
    <text x="50%" y="50%" textAnchor="middle" dy=".3em" fill="currentColor" fontSize="10" fontWeight="bold">
      500
    </text>
  </svg>
);

// 状态配置类型
interface StatusConfig {
  color: string;
  iconName?: string;
  customIcon?: (size: number) => ReactNode;
}

// 状态配置映射
const STATUS_CONFIG_MAP: Record<ResultStatusType, StatusConfig> = {
  success: {
    color: "#52c41a",
    iconName: "CheckCircleFilled"
  },
  error: {
    color: "#ff4d4f",
    iconName: "CloseCircleFilled"
  },
  warning: {
    color: "#faad14",
    iconName: "WarningFilled"
  },
  info: {
    color: "#1890ff",
    iconName: "InfoCircleFilled"
  },
  404: {
    color: "#1890ff",
    customIcon: size => <NotFoundIcon size={size} />
  },
  403: {
    color: "#ff4d4f",
    customIcon: size => <ForbiddenIcon size={size} />
  },
  500: {
    color: "#ff4d4f",
    customIcon: size => <ServerErrorIcon size={size} />
  }
};

export const Result: React.FC<ResultProps> = ({
  status = "info",
  icon,
  title,
  subTitle,
  extra,
  style,
  className,
  iconSize = 48
}) => {
  const config = STATUS_CONFIG_MAP[status];

  // 使用 useMemo 优化图标渲染
  const displayIcon = useMemo(() => {
    if (icon !== undefined) return icon;

    if (config.customIcon) {
      return <div style={{ color: config.color }}>{config.customIcon(iconSize)}</div>;
    }

    if (config.iconName) {
      return <Icon name={config.iconName} style={{ fontSize: iconSize, color: config.color }} />;
    }

    return null;
  }, [icon, config, iconSize]);

  // 计算是否显示头部
  const hasHeader = displayIcon || title;

  return (
    <div style={{ ...styles.container, ...style }} className={className + " qqqqqq"} role="alert" aria-live="polite">
      {/* 图标和标题 */}
      {hasHeader && (
        <div style={styles.headerRow}>
          {displayIcon && <div style={styles.iconWrapper}>{displayIcon}</div>}
          {title && (
            <div style={styles.title} className="result-title">
              {title}
            </div>
          )}
        </div>
      )}

      {/* 描述信息 */}
      {subTitle && (
        <div style={styles.subTitle} className="result-subtitle">
          {subTitle}
        </div>
      )}

      {/* 操作区域 */}
      {extra && (
        <div style={styles.extra} className="result-extra">
          {extra}
        </div>
      )}
    </div>
  );
};

const styles: Record<string, CSSProperties> = {
  container: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    justifyContent: "center",
    padding: 10,
    textAlign: "center"
  },
  headerRow: {
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    gap: 12,
    marginBottom: 12
  },
  iconWrapper: {
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    flexShrink: 0
  },
  title: {
    fontSize: 20,
    fontWeight: 500,
    color: "rgba(0, 0, 0, 0.85)",
    lineHeight: 1.4,
    margin: 0
  },
  subTitle: {
    fontSize: 14,
    color: "rgba(0, 0, 0, 0.45)",
    lineHeight: 1.6,
    maxWidth: 500,
    marginBottom: 10
  },
  extra: {
    marginTop: 8
  }
};

Result.displayName = "Result";
