import React, { useCallback, useEffect, useRef, useState } from "react";
import { Bubble, Conversations, Sender, ThoughtChain } from "@ant-design/x";
import type { BubbleListProps } from "@ant-design/x";
import XMarkdown from "@ant-design/x-markdown";
import { useXChat } from "@ant-design/x-sdk";
import { InfoCircleOutlined } from "@ant-design/icons";
import { Button, Flex, Layout, Tag, Typography, message } from "antd";
import ToolBarRight from "@/layouts/components/Header/ToolBarRight";
import logo from "@/assets/images/logo.png";
import RouterGuard from "@/routers/helper/RouterGuard";
import {
  getUnifiedChatConversation,
  listUnifiedChatConversations,
  listUnifiedChatRunEvents,
  listUnifiedChatRuns,
  UnifiedChatProvider,
  type UnifiedChatConversation,
  type UnifiedChatRunEvent
} from "@/api/modules/agentChat";
import "./index.less";

const { Header, Sider } = Layout;
const APP_TITLE = import.meta.env.VITE_GLOB_APP_TITLE;
const terminalKinds = new Set(["completed", "failed", "cancelled"]);

type ChatMessage = {
  id: string;
  role: "user" | "assistant";
  content: string;
  status?: "streaming" | "completed" | "failed" | "cancelled";
  citations: string[];
};
type TraceItem = {
  id: string;
  title: string;
  description: string;
  tone: "success" | "error" | "loading";
};

const traceTitles: Record<string, string> = {
  "run-started": "运行已启动", "main-agent-started": "主 Agent", "route-selected": "路由选择", "skill-started": "Skill",
  "knowledge-retrieved": "知识库检索", "knowledge-citation": "知识库引用", "tool-started": "MCP 工具",
  "tool-succeeded": "MCP 工具完成", "tool-failed": "MCP 工具失败", "approval-required": "等待审批",
  completed: "运行完成", failed: "运行失败", cancelled: "运行已取消"
};
const createId = () => crypto.randomUUID();
const parsePayload = (value: string) => {
  try { return JSON.parse(value) as Record<string, unknown>; } catch { return {} as Record<string, unknown>; }
};
const getPayloadText = (payload: Record<string, unknown>) => (typeof payload.text === "string" ? payload.text : "");
const getTrace = (event: UnifiedChatRunEvent): TraceItem => {
  const payload = parsePayload(event.payloadJson);
  let description = getPayloadText(payload) || event.route || "正在处理";
  if (event.kind === "knowledge-retrieved") {
    description = `检索 ${Number(payload.knowledgeBaseCount || 0)} 个知识库，命中 ${Number(payload.knowledgeHitCount || 0)} 个分块`;
  }
  if (event.kind === "approval-required") description = "等待人工审批后继续";
  return {
    id: `${event.runId}-${event.sequence}`, title: traceTitles[event.kind] || event.kind, description,
    tone: event.kind === "failed" || event.kind === "tool-failed" ? "error" : terminalKinds.has(event.kind) ? "success" : "loading"
  };
};

const ToolBarLeft: React.FC = () => (
  <div className="logo"><img src={logo} alt="logo" className="logo-img" /><h2 className="logo-text">{APP_TITLE}</h2></div>
);

const LayoutChat: React.FC = () => {
  const [messageApi, contextHolder] = message.useMessage();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [conversationId, setConversationId] = useState<string>();
  const [conversations, setConversations] = useState<UnifiedChatConversation[]>([]);
  const [traces, setTraces] = useState<TraceItem[]>([]);
  const [isRunning, setIsRunning] = useState(false);
  const [inspectorOpen, setInspectorOpen] = useState(false);
  const sdkEventHandlerRef = useRef<(event: UnifiedChatRunEvent) => void>(() => undefined);
  const sdkProviderRef = useRef<UnifiedChatProvider>();
  if (!sdkProviderRef.current) sdkProviderRef.current = new UnifiedChatProvider(event => sdkEventHandlerRef.current(event));
  const { onRequest: requestChat, messages: sdkMessages, isRequesting: isSdkRequesting, abort: abortChat, setMessages: setSdkMessages } = useXChat({
    provider: sdkProviderRef.current,
    conversationKey: "unified-chat",
    requestPlaceholder: () => ({ role: "assistant" as const, content: "" }),
    requestFallback: (_, { messageInfo }) => ({
      role: "assistant" as const,
      content: messageInfo?.message?.content || "请求失败，请重试。"
    })
  });
  const abortChatRef = useRef(abortChat);
  abortChatRef.current = abortChat;
  const requestIdRef = useRef(0);
  const typingFrameRef = useRef<number>();
  const pendingTextRef = useRef("");
  const typingMessageIdRef = useRef<string>();
  const typingLastFrameAtRef = useRef(0);
  const typingCharacterCreditRef = useRef(0);
  const timelineRef = useRef<HTMLDivElement>(null);
  const conversationRevisionRef = useRef(0);
  const activeSdkRunRef = useRef<{
    requestId: number;
    assistantId: string;
    terminal: boolean;
    cancelRequested: boolean;
  }>();
  const sdkRequestStartedRef = useRef(false);
  const loadConversationsRef = useRef<() => Promise<void>>(async () => undefined);

  useEffect(() => {
    document.title = `AI 助手 - ${APP_TITLE}`;
    return () => {
      requestIdRef.current += 1;
      activeSdkRunRef.current = undefined;
      abortChatRef.current();
      if (typingFrameRef.current) cancelAnimationFrame(typingFrameRef.current);
      pendingTextRef.current = "";
      typingMessageIdRef.current = undefined;
    };
  }, []);
  useEffect(() => { timelineRef.current?.scrollTo({ top: timelineRef.current.scrollHeight, behavior: "smooth" }); }, [messages, sdkMessages, traces]);

  const appendAssistantText = useCallback((id: string, content: string) => {
    setMessages(current => current.map(item => (item.id === id ? { ...item, content: item.content + content } : item)));
  }, []);
  const flushTyping = useCallback(() => {
    if (typingFrameRef.current) cancelAnimationFrame(typingFrameRef.current);
    typingFrameRef.current = undefined;
    typingLastFrameAtRef.current = 0;
    typingCharacterCreditRef.current = 0;
    if (pendingTextRef.current && typingMessageIdRef.current) {
      appendAssistantText(typingMessageIdRef.current, pendingTextRef.current);
      pendingTextRef.current = "";
    }
  }, [appendAssistantText]);
  const enqueueTyping = useCallback((id: string, content: string) => {
    if (!content) return;
    typingMessageIdRef.current = id;
    pendingTextRef.current += content;
    if (typingFrameRef.current) return;
    const render = (timestamp: number) => {
      typingFrameRef.current = undefined;
      const backlog = pendingTextRef.current.length;
      if (!backlog) {
        typingLastFrameAtRef.current = 0;
        typingCharacterCreditRef.current = 0;
        return;
      }
      const charactersPerSecond = backlog > 480 ? 280 : backlog > 160 ? 120 : 52;
      if (!typingLastFrameAtRef.current) {
        typingLastFrameAtRef.current = timestamp;
        typingCharacterCreditRef.current = 1;
      } else {
        const elapsedMilliseconds = Math.min(100, timestamp - typingLastFrameAtRef.current);
        typingLastFrameAtRef.current = timestamp;
        typingCharacterCreditRef.current += elapsedMilliseconds * charactersPerSecond / 1000;
      }
      const characterCount = Math.min(backlog, Math.floor(typingCharacterCreditRef.current));
      if (characterCount > 0) {
        typingCharacterCreditRef.current -= characterCount;
        const chunk = pendingTextRef.current.slice(0, characterCount);
        pendingTextRef.current = pendingTextRef.current.slice(characterCount);
        appendAssistantText(id, chunk);
      }
      typingFrameRef.current = requestAnimationFrame(render);
    };
    typingFrameRef.current = requestAnimationFrame(render);
  }, [appendAssistantText]);
  const updateAssistant = useCallback((id: string, update: Partial<ChatMessage>) => {
    setMessages(current => current.map(item => (item.id === id ? { ...item, ...update } : item)));
  }, []);
  sdkEventHandlerRef.current = event => {
    const active = activeSdkRunRef.current;
    if (!active || active.requestId !== requestIdRef.current) return;
    if (event.conversationId) setConversationId(event.conversationId);
    const payload = parsePayload(event.payloadJson);
    if (event.kind === "message" && event.depth === 0 && payload.eventKind === "Delta") {
      enqueueTyping(active.assistantId, getPayloadText(payload));
    }
    if (event.kind === "knowledge-citation") {
      const citation = getPayloadText(payload);
      if (citation) setMessages(current => current.map(item => (item.id === active.assistantId ? { ...item, citations: [...item.citations, citation] } : item)));
    }
    if (event.kind !== "message") setTraces(current => [...current.slice(-11), getTrace(event)]);
    if (terminalKinds.has(event.kind)) {
      flushTyping();
      active.terminal = true;
      updateAssistant(active.assistantId, { status: event.kind === "completed" ? "completed" : event.kind === "cancelled" ? "cancelled" : "failed" });
      setIsRunning(false);
      void loadConversationsRef.current();
    }
  };
  useEffect(() => {
    const active = activeSdkRunRef.current;
    if (!active) return;
    if (isSdkRequesting) {
      sdkRequestStartedRef.current = true;
      return;
    }
    if (!sdkRequestStartedRef.current) return;

    sdkRequestStartedRef.current = false;
    flushTyping();
    if (!active.terminal) {
      const fallback = [...sdkMessages]
        .reverse()
        .find(item => item.message.role === "assistant")?.message.content;
      const fallbackText =
        typeof fallback === "string" && fallback.trim()
          ? fallback
          : active.cancelRequested
            ? "运行已取消。"
            : "请求失败，请重试。";
      setMessages(current =>
        current.map(item =>
          item.id === active.assistantId
            ? {
                ...item,
                content: item.content || fallbackText,
                status: active.cancelRequested ? "cancelled" : "failed"
              }
            : item
        )
      );
      void loadConversationsRef.current();
    }
    activeSdkRunRef.current = undefined;
    setIsRunning(false);
  }, [flushTyping, isSdkRequesting, sdkMessages]);
  const loadConversations = useCallback(async () => {
    const revision = ++conversationRevisionRef.current;
    try {
      const values = await listUnifiedChatConversations();
      if (revision === conversationRevisionRef.current) setConversations(values);
    } catch (error) {
      if (revision === conversationRevisionRef.current) {
        messageApi.error(error instanceof Error ? error.message : "会话列表读取失败。");
      }
    }
  }, [messageApi]);
  loadConversationsRef.current = loadConversations;
  const selectConversation = useCallback(async (id: string) => {
    if (!id || isRunning) return;
    const revision = ++conversationRevisionRef.current;
    setConversationId(id);
    setMessages([]);
    setTraces([]);
    try {
      const [detail, runs] = await Promise.all([getUnifiedChatConversation(id), listUnifiedChatRuns(id, 1)]);
      const events = runs[0] ? await listUnifiedChatRunEvents(runs[0]) : [];
      if (revision !== conversationRevisionRef.current) return;
      setMessages(
        detail.messages.map(item => ({
          id: item.id,
          role: item.role === 0 || String(item.role).toLowerCase() === "user" ? "user" : "assistant",
          content: item.content,
          citations: []
        }))
      );
      setTraces(events.filter(event => event.kind !== "message").map(getTrace));
      const citations = events
        .filter(event => event.kind === "knowledge-citation")
        .map(event => getPayloadText(parsePayload(event.payloadJson)))
        .filter(Boolean);
      if (citations.length) {
        setMessages(current => {
          const lastAssistantIndex = current.map(item => item.role).lastIndexOf("assistant");
          return current.map((item, index) => (index === lastAssistantIndex ? { ...item, citations } : item));
        });
      }
    } catch (error) {
      if (revision === conversationRevisionRef.current) {
        messageApi.error(error instanceof Error ? error.message : "会话读取失败。");
      }
    }
  }, [isRunning, messageApi]);
  useEffect(() => { void loadConversations(); }, [loadConversations]);

  const startRun = async (value: string) => {
    const inputValue = value.trim();
    if (!inputValue || isRunning) return;
    const requestId = ++requestIdRef.current;
    const assistantId = createId();
    activeSdkRunRef.current = {
      requestId,
      assistantId,
      terminal: false,
      cancelRequested: false
    };
    sdkRequestStartedRef.current = false;
    setInput(""); setIsRunning(true); setTraces([]);
    setMessages(current => [...current, { id: createId(), role: "user", content: inputValue, citations: [] }, { id: assistantId, role: "assistant", content: "", citations: [], status: "streaming" }]);
    setSdkMessages([]);
    requestChat({ messages: [{ role: "user", content: inputValue }], conversationId });
  };
  const startNewConversation = () => {
    requestIdRef.current += 1;
    activeSdkRunRef.current = undefined;
    sdkRequestStartedRef.current = false;
    abortChat();
    flushTyping();
    setSdkMessages([]);
    setConversationId(undefined);
    setMessages([]);
    setTraces([]);
    setInput("");
    setIsRunning(false);
  };
  const cancelRun = useCallback(() => {
    const active = activeSdkRunRef.current;
    if (active) active.cancelRequested = true;
    abortChat();
  }, [abortChat]);
  const bubbleRoles: BubbleListProps["role"] = {
    assistant: {
      placement: "start",
      contentRender: (content, { status }) => {
        const text = typeof content === "string" ? content : "";
        if (status === "updating") return <div style={{ whiteSpace: "pre-wrap" }}>{text}</div>;
        return <XMarkdown paragraphTag="div">{text}</XMarkdown>;
      }
    },
    user: { placement: "end" as const }
  };

  return (
    <RouterGuard>
      {contextHolder}
      <section className="layout-vertical layout-chat"><Layout>
        <Header><ToolBarLeft /><div className="agent-chat-header-actions"><Tag color={isRunning ? "processing" : "success"}>{isRunning ? "运行中" : "Unified Chat"}</Tag><Button type="text" icon={<InfoCircleOutlined />} onClick={() => setInspectorOpen(value => !value)}>{inspectorOpen ? "收起详情" : "运行详情"}</Button><ToolBarRight layout="Chat" /></div></Header>
        <main className="agent-chat-main">
          <aside className="agent-chat-sidebar"><Conversations items={conversations.map(item => ({ key: item.id, label: item.title || "未命名会话", group: "最近" }))} activeKey={conversationId} creation={{ onClick: startNewConversation }} onActiveChange={id => { if (isRunning) messageApi.warning("运行结束后才能切换会话。"); else void selectConversation(String(id)); }} className="agent-chat-conversations" /></aside>
          <section className="agent-chat-workspace">
            <div className="agent-chat-timeline" ref={timelineRef}>
              {messages.length ? <Bubble.List items={messages.map(item => ({ key: item.id, role: item.role, content: item.content, status: item.status === "streaming" ? "updating" : item.status === "failed" ? "error" : item.status === "cancelled" ? "abort" : "success", loading: item.status === "streaming" && !item.content }))} role={bubbleRoles} /> :
                <Flex className="agent-chat-welcome" vertical align="center" justify="center" gap={12}><Typography.Title level={2}>Unified Chat</Typography.Title><Typography.Text type="secondary">与已发布的主 Agent 对话，回答会结合 Skills、知识库和 MCP 工具。</Typography.Text></Flex>}
            </div>
            <div className="agent-chat-composer"><Sender value={input} onChange={setInput} onSubmit={() => void startRun(input)} onCancel={cancelRun} loading={isRunning} placeholder="输入问题，Unified Chat 会调用已配置的 Agent 能力" /></div>
          </section>
          <Sider className="agent-chat-inspector" width={340} collapsedWidth={0} collapsed={!inspectorOpen} trigger={null} theme="light">
            <div className="agent-chat-inspector-content">
              <Typography.Title level={5}>运行详情</Typography.Title>
              <section className="agent-chat-inspector-section">
                <Typography.Text strong>知识库引用</Typography.Text>
                {messages.flatMap(item => item.citations).length ? messages.flatMap(item => item.citations).map((citation, index) => <Typography.Paragraph key={`${citation}-${index}`}>{citation}</Typography.Paragraph>) : <Typography.Text type="secondary">本次对话暂无知识库引用。</Typography.Text>}
              </section>
              <section className="agent-chat-inspector-section">
                <Typography.Text strong>运行轨迹</Typography.Text>
                {traces.length ? traces.map(trace => <ThoughtChain.Item key={trace.id} title={trace.title} description={trace.description} status={trace.tone} />) : <Typography.Text type="secondary">运行后会显示调用轨迹。</Typography.Text>}
              </section>
            </div>
          </Sider>
        </main>
      </Layout></section>
    </RouterGuard>
  );
};

export default LayoutChat;
