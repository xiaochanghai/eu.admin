import React from "react";

/** 搜索框预览 */
export const SearchBarPreview: React.FC<{ placeholder?: string; [key: string]: any }> = ({ placeholder }) => (
  <div style={{ padding: "8px 12px" }}>
    <div
      style={{
        display: "flex",
        alignItems: "center",
        padding: "8px 12px",
        borderRadius: 8,
        background: "#eef2f7",
        color: "#9ca3af",
        fontSize: 13
      }}
    >
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={{ marginRight: 6 }}>
        <circle cx="11" cy="11" r="8" />
        <path d="M21 21l-4.35-4.35" />
      </svg>
      {placeholder || "搜索"}
    </div>
  </div>
);

/** 筛选标签预览 */
export const TabsPreview: React.FC<{ items?: any[]; [key: string]: any }> = ({ items = [] }) => (
  <div style={{ display: "flex", gap: 6, padding: "4px 12px 10px", overflow: "hidden" }}>
    {items.slice(0, 4).map((item: any, i: number) => (
      <span
        key={i}
        style={{
          padding: "4px 10px",
          borderRadius: 999,
          fontSize: 11,
          fontWeight: 600,
          background: i === 0 ? "#e0ecff" : "#f3f4f6",
          color: i === 0 ? "#2563eb" : "#6b7280"
        }}
      >
        {item.label}
      </span>
    ))}
  </div>
);

/** 统计条预览 */
export const StatRowPreview: React.FC<{ items?: any[]; [key: string]: any }> = ({ items = [] }) => (
  <div style={{ display: "flex", padding: "10px 12px", background: "#fff", borderBottom: "1px solid #f0f0f0" }}>
    {items.map((item: any, i: number) => (
      <div key={i} style={{ flex: 1, textAlign: "center" }}>
        <div style={{ fontSize: 16, fontWeight: 700, color: "#111827" }}>--</div>
        <div style={{ fontSize: 10, color: "#9ca3af" }}>{item.label}</div>
      </div>
    ))}
  </div>
);

/** 列表容器预览 */
export const ListPreview: React.FC<{ children?: React.ReactNode; [key: string]: any }> = ({ children }) => (
  <div style={{ padding: "8px 12px" }}>
    <div
      style={{
        borderRadius: 12,
        background: "#fff",
        border: "1px dashed #d9d9d9",
        padding: 12,
        minHeight: 80
      }}
    >
      {children && React.Children.count(children) > 0 ? (
        children
      ) : (
        <div style={{ textAlign: "center", color: "#d9d9d9", fontSize: 12, padding: "20px 0" }}>
          拖入 Item 字段组件
        </div>
      )}
    </div>
  </div>
);

/** 空状态预览 */
export const EmptyStatePreview: React.FC<{ text?: string; [key: string]: any }> = ({ text }) => (
  <div style={{ textAlign: "center", padding: "40px 20px", color: "#d9d9d9" }}>
    <div style={{ fontSize: 32, marginBottom: 8 }}>📭</div>
    <div style={{ fontSize: 13 }}>{text || "暂无数据"}</div>
  </div>
);

/** 悬浮按钮预览 */
export const FloatingActionPreview: React.FC<{ label?: string; [key: string]: any }> = () => (
  <div style={{ display: "flex", justifyContent: "flex-end", padding: "12px" }}>
    <div
      style={{
        width: 48,
        height: 48,
        borderRadius: 24,
        background: "#2563eb",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        color: "#fff",
        fontSize: 20,
        boxShadow: "0 4px 12px rgba(37, 99, 235, 0.3)"
      }}
    >
      +
    </div>
  </div>
);
