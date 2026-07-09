import { useState } from "react";
import menus from "../schema/fields";
import { AppstoreOutlined, BuildOutlined } from "@ant-design/icons";

const tabIcons: Record<string, React.ReactNode> = {
  page: <AppstoreOutlined style={{ fontSize: 16 }} />,
  itemFields: <BuildOutlined style={{ fontSize: 16 }} />
};

export default function Left() {
  const [activeTab, setActiveTab] = useState("page");

  return (
    <div style={{
      width: 240,
      display: "flex",
      flexDirection: "column",
      borderRight: "1px solid #e5e7eb",
      background: "#fff"
    }}>
      <div style={{
        flexShrink: 0,
        height: 44,
        lineHeight: "44px",
        padding: "0 16px",
        borderBottom: "1px solid #f3f4f6",
        fontWeight: 600,
        fontSize: 13,
        color: "#374151",
        display: "flex",
        alignItems: "center",
        gap: 6
      }}>
        <AppstoreOutlined style={{ color: "#2563eb" }} />
        组件库
      </div>
      {/* Tab 切换 */}
      <div style={{
        display: "flex",
        borderBottom: "1px solid #f3f4f6",
        padding: "0 8px",
        gap: 4
      }}>
        {menus.map(menu => (
          <div
            key={menu.key}
            onClick={() => setActiveTab(menu.key)}
            className="transition-all"
            style={{
              flex: 1,
              textAlign: "center",
              padding: "10px 0",
              fontSize: 12,
              cursor: "pointer",
              borderRadius: "8px 8px 0 0",
              borderBottom: activeTab === menu.key ? "2px solid #2563eb" : "2px solid transparent",
              color: activeTab === menu.key ? "#2563eb" : "#6b7280",
              fontWeight: activeTab === menu.key ? 600 : 400,
              background: activeTab === menu.key ? "#eff6ff" : "transparent",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              gap: 4
            }}
          >
            {tabIcons[menu.key]}
            {menu.label}
          </div>
        ))}
      </div>
      {/* 组件面板 */}
      <div style={{ flex: 1, overflowY: "auto", padding: "12px" }}>
        {menus.filter(m => m.key === activeTab).map(m => <div key={m.key}>{m.panel}</div>)}
      </div>
    </div>
  );
}
