import React from "react";

interface Props {
  title?: string;
  children: React.ReactNode;
}

export default function PhoneFrame({ title, children }: Props) {
  return (
    <div style={{ display: "flex", flexDirection: "column", alignItems: "center", paddingTop: 20 }}>
      {/* 手机外壳 */}
      <div style={{
        width: 375,
        borderRadius: 40,
        background: "#1a1a2e",
        padding: "12px 10px",
        boxShadow: "0 25px 80px rgba(0,0,0,0.25), 0 0 0 1px rgba(255,255,255,0.1) inset",
        position: "relative"
      }}>
        {/* 听筒 */}
        <div style={{
          position: "absolute",
          top: 6,
          left: "50%",
          transform: "translateX(-50%)",
          width: 80,
          height: 6,
          background: "#0f0f1a",
          borderRadius: 3
        }} />
        {/* 屏幕 */}
        <div style={{
          borderRadius: 30,
          background: "#f9fafb",
          overflow: "hidden",
          position: "relative"
        }}>
          {/* 状态栏 */}
          <div style={{
            height: 44,
            background: "#fff",
            display: "flex",
            alignItems: "flex-end",
            justifyContent: "space-between",
            padding: "0 24px 6px",
            fontSize: 12,
            fontWeight: 600,
            color: "#111827"
          }}>
            <span>9:41</span>
            <div style={{ display: "flex", gap: 4, alignItems: "center" }}>
              <svg width="16" height="12" viewBox="0 0 16 12" fill="#111827">
                <rect x="0" y="5" width="3" height="7" rx="0.5" />
                <rect x="4.5" y="3" width="3" height="9" rx="0.5" />
                <rect x="9" y="1" width="3" height="11" rx="0.5" />
                <rect x="13.5" y="0" width="2.5" height="12" rx="0.5" opacity="0.3" />
              </svg>
              <svg width="16" height="12" viewBox="0 0 16 12" fill="#111827">
                <path d="M8 2C5.5 2 3.3 3 1.7 4.7L0 3C2 1 4.9 0 8 0s6 1 8 3l-1.7 1.7C12.7 3 10.5 2 8 2z" opacity="0.3"/>
                <path d="M8 5C6.3 5 4.8 5.7 3.7 6.8L2 5.1C3.5 3.7 5.6 3 8 3s4.5.7 6 2.1l-1.7 1.7C11.2 5.7 9.7 5 8 5z" opacity="0.5"/>
                <path d="M8 8c-1 0-1.9.4-2.6 1.1L8 12l2.6-2.9C9.9 8.4 9 8 8 8z"/>
              </svg>
              <div style={{ width: 22, height: 11, border: "1px solid #111827", borderRadius: 3, padding: 1, position: "relative" }}>
                <div style={{ width: "70%", height: "100%", background: "#111827", borderRadius: 1 }} />
                <div style={{ position: "absolute", right: -3, top: 3, width: 2, height: 5, background: "#111827", borderRadius: "0 1px 1px 0" }} />
              </div>
            </div>
          </div>
          {/* 标题栏 */}
          <div style={{
            height: 44,
            background: "#fff",
            borderBottom: "1px solid #e5e7eb",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            fontSize: 16,
            fontWeight: 600,
            color: "#111827",
            position: "relative"
          }}>
            <div style={{ position: "absolute", left: 16, color: "#2563eb", fontSize: 14 }}>‹</div>
            {title || "页面预览"}
          </div>
          {/* 内容区 */}
          <div style={{
            minHeight: 520,
            maxHeight: 520,
            overflowY: "auto",
            background: "#f3f4f6"
          }}>
            {children}
          </div>
          {/* 底部指示条 */}
          <div style={{
            height: 28,
            background: "#fff",
            display: "flex",
            alignItems: "center",
            justifyContent: "center"
          }}>
            <div style={{ width: 120, height: 4, background: "#d1d5db", borderRadius: 2 }} />
          </div>
        </div>
      </div>
      {/* 手机下方提示 */}
      <div style={{ marginTop: 16, fontSize: 12, color: "#9ca3af", textAlign: "center" }}>
        375 × 812 · iPhone 标准尺寸
      </div>
    </div>
  );
}
