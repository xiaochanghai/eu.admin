import React from "react";

/** 文本预览 */
export const TextPreview: React.FC<{ bind?: string; role?: string; prefix?: string; suffix?: string; [key: string]: any }> = ({ bind, role = "title", prefix, suffix }) => {
  const styles: Record<string, React.CSSProperties> = {
    title: { fontSize: 14, fontWeight: 700, color: "#111827" },
    subtitle: { fontSize: 12, color: "#6b7280", marginTop: 2 },
    description: { fontSize: 12, color: "#9ca3af", marginTop: 4 }
  };
  return (
    <span style={styles[role] || styles.title}>
      {prefix}{bind || "字段名"}{suffix}
    </span>
  );
};

/** 图片预览 */
export const ImagePreview: React.FC<{ size?: number; radius?: number; [key: string]: any }> = ({ size = 48, radius = 8 }) => (
  <div
    style={{
      width: size,
      height: size,
      borderRadius: radius,
      background: "#e5e7eb",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      fontSize: 10,
      color: "#9ca3af",
      flexShrink: 0
    }}
  >
    IMG
  </div>
);

/** 状态标签预览 */
export const StatusTagPreview: React.FC<{ bind?: string; [key: string]: any }> = ({ bind }) => (
  <span
    style={{
      padding: "2px 8px",
      borderRadius: 999,
      fontSize: 10,
      fontWeight: 700,
      background: "#ecfdf5",
      color: "#059669"
    }}
  >
    {bind || "状态"}
  </span>
);

/** 指标预览 */
export const MetricPreview: React.FC<{ label?: string; suffix?: string; [key: string]: any }> = ({ label, suffix }) => (
  <div style={{ textAlign: "center" }}>
    <div style={{ fontSize: 14, fontWeight: 700, color: "#111827" }}>--</div>
    <div style={{ fontSize: 10, color: "#9ca3af" }}>
      {label || "指标"}{suffix}
    </div>
  </div>
);

/** 图标文本预览 */
export const IconTextPreview: React.FC<{ bind?: string; [key: string]: any }> = ({ bind }) => (
  <div style={{ display: "flex", alignItems: "center", gap: 4, fontSize: 12, color: "#6b7280" }}>
    <span style={{ fontSize: 12 }}>📌</span>
    <span>{bind || "字段"}</span>
  </div>
);

/** 分割线预览 */
export const DividerPreview: React.FC<{ margin?: number; [key: string]: any }> = ({ margin = 8 }) => (
  <div style={{ margin: `${margin}px 0`, borderTop: "1px solid #eef2f7" }} />
);

/** 间距预览 */
export const SpacerPreview: React.FC<{ height?: number; [key: string]: any }> = ({ height = 8 }) => (
  <div style={{ height, background: "transparent" }} />
);

/** 操作按钮预览 */
export const ActionButtonPreview: React.FC<{ text?: string; [key: string]: any }> = ({ text }) => (
  <span style={{ color: "#2563eb", fontSize: 12, fontWeight: 600 }}>
    {text || "操作"}
  </span>
);

/** 横向布局预览 */
export const RowPreview: React.FC<{ gap?: number; align?: string; justify?: string; children?: React.ReactNode; [key: string]: any }> = ({ gap = 8, align = "center", justify, children }) => (
  <div
    style={{
      display: "flex",
      gap,
      alignItems: align,
      justifyContent: justify === "space-between" ? "space-between" : justify || "start",
      padding: "4px 0"
    }}
  >
    {children}
  </div>
);

/** 纵向布局预览 */
export const ColumnPreview: React.FC<{ gap?: number; flex?: number; children?: React.ReactNode; [key: string]: any }> = ({ gap = 4, flex = 1, children }) => (
  <div
    style={{
      display: "flex",
      flexDirection: "column",
      gap,
      flex
    }}
  >
    {children}
  </div>
);
