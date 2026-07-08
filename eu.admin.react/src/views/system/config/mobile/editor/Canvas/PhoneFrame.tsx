import React from "react";

interface Props {
  title?: string;
  children: React.ReactNode;
}

export default function PhoneFrame({ title, children }: Props) {
  return (
    <div className="flex flex-col items-center">
      {/* 手机外壳 */}
      <div
        style={{
          width: 375,
          minHeight: 680,
          border: "6px solid #1f2937",
          borderRadius: 32,
          background: "#f9fafb",
          overflow: "hidden",
          boxShadow: "0 20px 60px rgba(0, 0, 0, 0.15)",
          position: "relative"
        }}
      >
        {/* 状态栏 */}
        <div
          style={{
            height: 28,
            background: "#1f2937",
            display: "flex",
            alignItems: "center",
            justifyContent: "center"
          }}
        >
          <div
            style={{
              width: 60,
              height: 16,
              background: "#374151",
              borderRadius: 10
            }}
          />
        </div>
        {/* 标题栏 */}
        <div
          style={{
            height: 44,
            background: "#fff",
            borderBottom: "1px solid #e5e7eb",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            fontSize: 16,
            fontWeight: 600,
            color: "#111827"
          }}
        >
          {title || "页面预览"}
        </div>
        {/* 内容区 */}
        <div
          style={{
            minHeight: 560,
            overflowY: "auto",
            paddingBottom: 60
          }}
        >
          {children}
        </div>
      </div>
    </div>
  );
}
