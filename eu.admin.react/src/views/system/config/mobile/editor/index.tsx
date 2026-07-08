import { useState, useEffect } from "react";
import { Button, Space, Tooltip, Divider, message } from "antd";
import { ArrowLeftOutlined, SendOutlined, DeleteOutlined } from "@ant-design/icons";
import { useNavigate, useSearchParams } from "react-router-dom";
import { DndProvider } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { useDispatch, useSelector, RootState } from "@/redux";
import { loadConfig, resetEditor } from "@/redux/modules/mobileEditor";
import { getMobilePageById, publishMobilePage, updateMobilePage } from "@/api/modules/mobileConfig";
import Left from "./Left";
import Canvas from "./Canvas";
import Right from "./Right";
import SaveBtn from "./SaveBtn";
import JsonPreviewBtn from "./JsonPreviewBtn";

export default function MobileEditor() {
  const [searchParams] = useSearchParams();
  const configId = searchParams.get("id") || undefined;
  const navigate = useNavigate();
  const dispatch = useDispatch();
  const state = useSelector((s: RootState) => s.mobileEditor);
  const [pageTitle, setPageTitle] = useState("移动端页面配置");
  const [publishing, setPublishing] = useState(false);

  useEffect(() => {
    if (configId) {
      getMobilePageById(configId).then(res => {
        if (res?.Success && res.Data) {
          const config = res.Data;
          setPageTitle(config.PageName || config.Title || "移动端页面配置");
          if (config.ConfigJson && config.ConfigJson !== "{}") {
            try {
              const tree = JSON.parse(config.ConfigJson);
              dispatch(loadConfig(tree));
            } catch (e) {
              console.error("解析配置JSON失败", e);
            }
          }
        }
      });
    }

    return () => {
      dispatch(resetEditor());
    };
  }, [configId]);

  const handlePublish = async () => {
    if (!configId) return;
    setPublishing(true);
    try {
      // 先保存再发布
      const configJson = JSON.stringify({
        type: state.type,
        props: state.props,
        children: state.children
      });
      await updateMobilePage(configId, { ConfigJson: configJson });
      const res = await publishMobilePage(configId);
      if (res?.Success) {
        message.success("发布成功");
      }
    } catch (err) {
      message.error("发布失败");
    } finally {
      setPublishing(false);
    }
  };

  const handleClear = () => {
    dispatch(resetEditor());
    message.info("已清空画布");
  };

  return (
    <DndProvider backend={HTML5Backend}>
      <div className="h-screen flex flex-col bg-gray-50">
        {/* 顶部工具栏 */}
        <header className="h-14 px-4 bg-white border-b border-gray-200 flex-shrink-0 flex justify-between items-center">
          {/* 左侧：返回 + 标题 */}
          <div className="flex items-center gap-3">
            <Tooltip title="返回列表">
              <Button
                type="text"
                icon={<ArrowLeftOutlined />}
                onClick={() => navigate("/system/config/mobile")}
              />
            </Tooltip>
            <div className="flex items-center gap-2">
              <div className="w-8 h-8 bg-blue-500 rounded flex items-center justify-center">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2">
                  <rect x="5" y="2" width="14" height="20" rx="2" ry="2" />
                  <line x1="12" y1="18" x2="12.01" y2="18" />
                </svg>
              </div>
              <span className="text-base font-medium text-gray-800">{pageTitle}</span>
            </div>
          </div>

          {/* 中间：操作按钮 */}
          <Space size="small">
            <SaveBtn configId={configId} />
            <JsonPreviewBtn />
            <Tooltip title="发布配置">
              <Button
                icon={<SendOutlined />}
                loading={publishing}
                onClick={handlePublish}
              >
                发布
              </Button>
            </Tooltip>
            <Divider orientation="vertical" style={{ height: 24, margin: "0 4px" }} />
            <Tooltip title="清空画布">
              <Button icon={<DeleteOutlined />} danger onClick={handleClear}>
                清空
              </Button>
            </Tooltip>
          </Space>

          {/* 右侧占位 */}
          <div style={{ width: 120 }} />
        </header>

        {/* 主内容区 */}
        <main className="flex-1 overflow-hidden flex">
          <Left />
          <Canvas />
          <Right />
        </main>
      </div>
    </DndProvider>
  );
}
