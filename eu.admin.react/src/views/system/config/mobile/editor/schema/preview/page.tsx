import React from "react";

/** 搜索框预览 */
export const SearchBarPreview: React.FC<{ placeholder?: string; [key: string]: any }> = ({ placeholder }) => (
  <div style={{ padding: "10px 14px" }}>
    <div style={{
      display: "flex",
      alignItems: "center",
      padding: "9px 14px",
      borderRadius: 10,
      background: "#eef2f7",
      color: "#9ca3af",
      fontSize: 13,
      gap: 8
    }}>
      <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
        <circle cx="11" cy="11" r="8" />
        <path d="M21 21l-4.35-4.35" />
      </svg>
      {placeholder || "搜索"}
    </div>
  </div>
);

/** 筛选标签预览 */
export const TabsPreview: React.FC<{ items?: any[]; [key: string]: any }> = ({ items = [] }) => (
  <div style={{
    display: "flex",
    gap: 8,
    padding: "2px 14px 10px",
    overflow: "hidden"
  }}>
    {items.slice(0, 5).map((item: any, i: number) => (
      <span
        key={i}
        style={{
          padding: "5px 14px",
          borderRadius: 999,
          fontSize: 12,
          fontWeight: i === 0 ? 700 : 500,
          background: i === 0 ? "#dbeafe" : "#f3f4f6",
          color: i === 0 ? "#2563eb" : "#6b7280",
          whiteSpace: "nowrap"
        }}
      >
        {item.label}
      </span>
    ))}
  </div>
);

/** 统计条预览 */
export const StatRowPreview: React.FC<{ items?: any[]; [key: string]: any }> = ({ items = [] }) => (
  <div style={{
    display: "flex",
    padding: "12px 14px",
    background: "#fff",
    margin: "0 0 1px",
    borderRadius: 10,
    boxShadow: "0 1px 3px rgba(0,0,0,0.04)"
  }}>
    {items.map((item: any, i: number) => (
      <div key={i} style={{ flex: 1, textAlign: "center" }}>
        <div style={{ fontSize: 18, fontWeight: 700, color: "#111827" }}>--</div>
        <div style={{ fontSize: 11, color: "#9ca3af", marginTop: 2 }}>{item.label}</div>
      </div>
    ))}
  </div>
);

/** 列表容器预览 */
export const ListPreview: React.FC<{ children?: React.ReactNode; [key: string]: any }> = ({ children }) => (
  <div style={{ padding: "6px 12px" }}>
    <div style={{
      borderRadius: 12,
      background: "#fff",
      border: children && React.Children.count(children) > 0 ? "none" : "1.5px dashed #d1d5db",
      padding: children && React.Children.count(children) > 0 ? "10px 12px" : 16,
      minHeight: 60,
      boxShadow: children && React.Children.count(children) > 0 ? "0 1px 4px rgba(0,0,0,0.05)" : "none"
    }}>
      {children && React.Children.count(children) > 0 ? (
        children
      ) : (
        <div style={{ textAlign: "center", color: "#c4c9d4", fontSize: 12, padding: "16px 0" }}>
          <div style={{ fontSize: 24, marginBottom: 6, opacity: 0.5 }}>📋</div>
          拖入 Item 字段组件
        </div>
      )}
    </div>
  </div>
);

/** 空状态预览 */
export const EmptyStatePreview: React.FC<{ text?: string; [key: string]: any }> = ({ text }) => (
  <div style={{ textAlign: "center", padding: "48px 20px", color: "#d1d5db" }}>
    <div style={{ fontSize: 40, marginBottom: 10 }}>📭</div>
    <div style={{ fontSize: 13, color: "#9ca3af" }}>{text || "暂无数据"}</div>
  </div>
);

/** 悬浮按钮预览 */
export const FloatingActionPreview: React.FC<{ label?: string; [key: string]: any }> = () => (
  <div style={{ display: "flex", justifyContent: "flex-end", padding: "10px 16px" }}>
    <div style={{
      width: 52,
      height: 52,
      borderRadius: 16,
      background: "linear-gradient(135deg, #2563eb, #7c3aed)",
      display: "flex",
      alignItems: "center",
      justifyContent: "center",
      color: "#fff",
      fontSize: 24,
      boxShadow: "0 6px 20px rgba(37,99,235,0.35)",
      fontWeight: 300
    }}>
      +
    </div>
  </div>
);
