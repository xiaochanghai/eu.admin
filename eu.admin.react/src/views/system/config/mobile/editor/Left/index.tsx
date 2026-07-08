import { useState } from "react";
import menus from "../schema/fields";
import cl from "classnames";

export default function Left() {
  const [activeTab, setActiveTab] = useState("page");

  return (
    <div className="w-60 flex flex-col border-r border-gray-200 bg-white">
      <div className="flex-shrink-0 h-10 leading-10 px-3 text-blue-600 border-b border-gray-200 font-medium text-sm">
        组件库
      </div>
      {/* Tab 切换 */}
      <div className="flex border-b border-gray-100">
        {menus.map(menu => (
          <div
            key={menu.key}
            onClick={() => setActiveTab(menu.key)}
            className={cl(
              "flex-1 text-center py-2 text-xs cursor-pointer border-b-2 transition-colors",
              {
                "border-blue-600 text-blue-600 font-medium": activeTab === menu.key,
                "border-transparent text-gray-500 hover:text-gray-700": activeTab !== menu.key
              }
            )}
          >
            {menu.label}
          </div>
        ))}
      </div>
      {/* 组件面板 */}
      <div className="flex-1 overflow-y-auto p-2">
        {menus.filter(m => m.key === activeTab).map(m => <div key={m.key}>{m.panel}</div>)}
      </div>
    </div>
  );
}
