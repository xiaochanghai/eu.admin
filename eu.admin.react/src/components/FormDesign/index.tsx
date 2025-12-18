import { useState } from "react";
import { Button, Space, Tooltip, Divider } from "antd";
import {
  EyeOutlined,
  ImportOutlined,
  ExportOutlined,
  DeleteOutlined,
  QuestionCircleOutlined,
  CloseOutlined,
  DesktopOutlined,
  MobileOutlined
} from "@ant-design/icons";
import Left from "./Left";
import Right from "./Right";
import Canvas from "./Canvas";
import SaveBtn from "./SaveBtn";
import ShowCodeBtn from "./ShowCodeBtn";
import { HTML5Backend } from "react-dnd-html5-backend";
import { DndProvider } from "react-dnd";

function Editor() {
  const [checked, setChecked] = useState(false);

  return (
    <DndProvider backend={HTML5Backend}>
      <div className="h-screen flex flex-col bg-gray-50">
        {/* 顶部工具栏 - 参考图片风格 */}
        <header className="h-14 px-4 bg-white border-b border-gray-200 flex-shrink-0 flex justify-between items-center">
          {/* 左侧：标题 */}
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 bg-blue-500 rounded flex items-center justify-center">
              <svg className="w-5 h-5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                />
              </svg>
            </div>
            <span className="text-base font-medium text-gray-800">表单设计</span>
          </div>

          {/* 中间：操作按钮组 */}
          <Space size="small">
            <SaveBtn />

            <Tooltip title="预览">
              <Button icon={<EyeOutlined />}>预览</Button>
            </Tooltip>

            <Tooltip title="导入JSON">
              <Button icon={<ImportOutlined />}>导入JSON</Button>
            </Tooltip>

            <Tooltip title="生成JSON">
              <Button icon={<ExportOutlined />}>生成JSON</Button>
            </Tooltip>

            <ShowCodeBtn />

            <Tooltip title="清空">
              <Button icon={<DeleteOutlined />} danger>
                清空
              </Button>
            </Tooltip>

            <Divider type="vertical" style={{ height: "24px", margin: "0 8px" }} />

            {/* 视图切换 */}
            <Tooltip title={checked ? "移动视图" : "桌面视图"}>
              <Button
                icon={checked ? <MobileOutlined /> : <DesktopOutlined />}
                type={checked ? "primary" : "default"}
                onClick={() => setChecked(!checked)}
              >
                {checked ? "移动" : "桌面"}
              </Button>
            </Tooltip>
          </Space>

          {/* 右侧：帮助和关闭 */}
          <Space size="small">
            <Tooltip title="帮助文档">
              <Button type="text" icon={<QuestionCircleOutlined />} />
            </Tooltip>
            <Tooltip title="关闭">
              <Button type="text" icon={<CloseOutlined />} />
            </Tooltip>
          </Space>
        </header>

        {/* 主内容区 */}
        <main className="flex-1 overflow-hidden flex">
          <Left />
          <Canvas mobile={checked} />
          <Right />
        </main>
      </div>
    </DndProvider>
  );
}

export default Editor;
