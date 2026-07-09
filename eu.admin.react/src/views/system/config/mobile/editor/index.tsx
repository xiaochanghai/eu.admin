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
      <div style={{ height: "100vh", display: "flex", flexDirection: "column", background: "#f0f2f5" }}>
        {/* 顶部工具栏 */}
        <header style={{
          height: 56,
          padding: "0 20px",
          background: "#fff",
          borderBottom: "1px solid #e5e7eb",
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          flexShrink: 0,
          boxShadow: "0 1px 4px rgba(0,0,0,0.04)"
        }}>
          {/* 左侧：返回 + 标题 */}
          <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
            <Tooltip title="返回列表">
              <Button
                type="text"
                icon={<ArrowLeftOutlined style={{ fontSize: 18 }} />}
                onClick={() => navigate("/system/config/mobile")}
                style={{ borderRadius: 8 }}
              />
            </Tooltip>
            <div style={{ width: 1, height: 24, background: "#e5e7eb" }} />
            <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <div style={{
                width: 32,
                height: 32,
                borderRadius: 8,
                background: "linear-gradient(135deg, #2563eb, #7c3aed)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center"
              }}>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="white" strokeWidth="2.5">
                  <rect x="5" y="2" width="14" height="20" rx="2" ry="2" />
                  <line x1="12" y1="18" x2="12.01" y2="18" />
                </svg>
              </div>
              <div>
                <div style={{ fontSize: 14, fontWeight: 600, color: "#111827", lineHeight: 1.2 }}>{pageTitle}</div>
                <div style={{ fontSize: 11, color: "#9ca3af" }}>移动端列表页配置</div>
              </div>
            </div>
          </div>

          {/* 中间：操作按钮 */}
          <Space size={8}>
            <SaveBtn configId={configId} />
            <JsonPreviewBtn />
            <Button
              icon={<SendOutlined />}
              loading={publishing}
              onClick={handlePublish}
              style={{ borderRadius: 6, borderColor: "#059669", color: "#059669" }}
            >
              发布
            </Button>
            <Divider orientation="vertical" style={{ height: 24, margin: "0 4px" }} />
            <Tooltip title="清空画布">
              <Button
                icon={<DeleteOutlined />}
                danger
                type="text"
                onClick={handleClear}
                style={{ borderRadius: 6 }}
              />
            </Tooltip>
          </Space>

          {/* 右侧占位保持居中 */}
          <div style={{ width: 200 }} />
        </header>

        {/* 主内容区 */}
        <main style={{ flex: 1, overflow: "hidden", display: "flex" }}>
          <Left />
          <Canvas />
          <Right />
        </main>
      </div>
    </DndProvider>
  );
}
