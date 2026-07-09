import React from "react";

/** 文本预览 */
export const TextPreview: React.FC<{ bind?: string; role?: string; prefix?: string; suffix?: string; [key: string]: any }> = ({ bind, role = "title", prefix, suffix }) => {
  const styles: Record<string, React.CSSProperties> = {
    title: { fontSize: 14, fontWeight: 700, color: "#111827", lineHeight: 1.4 },
    subtitle: { fontSize: 12, color: "#6b7280", marginTop: 2, lineHeight: 1.4 },
    description: { fontSize: 12, color: "#9ca3af", marginTop: 4, lineHeight: 1.5 }
  };
  return (
    <span style={{ ...(styles[role] || styles.title), display: "block" }}>
      {prefix}{bind || "字段名"}{suffix}
    </span>
  );
};

/** 图片预览 */
export const ImagePreview: React.FC<{ size?: number; radius?: number; [key: string]: any }> = ({ size = 48, radius = 10 }) => (
  <div style={{
    width: size,
    height: size,
    borderRadius: radius,
    background: "linear-gradient(135deg, #e5e7eb, #d1d5db)",
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    fontSize: 10,
    color: "#9ca3af",
    flexShrink: 0,
    fontWeight: 600
  }}>
    IMG
  </div>
);

/** 状态标签预览 */
export const StatusTagPreview: React.FC<{ bind?: string; [key: string]: any }> = ({ bind }) => (
  <span style={{
    display: "inline-flex",
    alignItems: "center",
    gap: 4,
    padding: "3px 10px",
    borderRadius: 999,
    fontSize: 11,
    fontWeight: 600,
    background: "#ecfdf5",
    color: "#059669"
  }}>
    <span style={{ width: 5, height: 5, borderRadius: "50%", background: "#059669" }} />
    {bind || "状态"}
  </span>
);

/** 指标预览 */
export const MetricPreview: React.FC<{ label?: string; suffix?: string; [key: string]: any }> = ({ label, suffix }) => (
  <div style={{ textAlign: "center", flex: 1 }}>
    <div style={{ fontSize: 16, fontWeight: 700, color: "#111827" }}>--</div>
    <div style={{ fontSize: 11, color: "#9ca3af", marginTop: 1 }}>
      {label || "指标"}{suffix}
    </div>
  </div>
);

/** 图标文本预览 */
export const IconTextPreview: React.FC<{ bind?: string; [key: string]: any }> = ({ bind }) => (
  <div style={{ display: "flex", alignItems: "center", gap: 5, fontSize: 12, color: "#6b7280" }}>
    <span style={{
      width: 18,
      height: 18,
      borderRadius: 4,
      background: "#f3f4f6",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      fontSize: 10
    }}>📌</span>
    <span>{bind || "字段"}</span>
  </div>
);

/** 分割线预览 */
export const DividerPreview: React.FC<{ margin?: number; [key: string]: any }> = ({ margin = 8 }) => (
  <div style={{ margin: `${margin}px 14px`, borderTop: "1px solid #f0f0f0" }} />
);

/** 间距预览 */
export const SpacerPreview: React.FC<{ height?: number; [key: string]: any }> = ({ height = 8 }) => (
  <div style={{
    height,
    background: "repeating-linear-gradient(90deg, transparent, transparent 4px, #f0f0f0 4px, #f0f0f0 5px)",
    opacity: 0.6
  }} />
);

/** 操作按钮预览 */
export const ActionButtonPreview: React.FC<{ text?: string; [key: string]: any }> = ({ text }) => (
  <span style={{
    display: "inline-block",
    color: "#2563eb",
    fontSize: 13,
    fontWeight: 600,
    padding: "4px 0",
    borderBottom: "1px solid transparent"
  }}>
    {text || "操作"} →
  </span>
);

/** 横向布局预览 */
export const RowPreview: React.FC<{ gap?: number; align?: string; justify?: string; children?: React.ReactNode; [key: string]: any }> = ({ gap = 8, align = "center", justify, children }) => (
  <div style={{
    display: "flex",
    gap,
    alignItems: align,
    justifyContent: justify === "space-between" ? "space-between" : justify || "start",
    padding: "6px 0"
  }}>
    {children}
  </div>
);

/** 纵向布局预览 */
export const ColumnPreview: React.FC<{ gap?: number; flex?: number; children?: React.ReactNode; [key: string]: any }> = ({ gap = 4, flex = 1, children }) => (
  <div style={{
    display: "flex",
    flexDirection: "column",
    gap,
    flex
  }}>
    {children}
  </div>
);
