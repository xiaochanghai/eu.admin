import React, { useCallback, useEffect, useRef, useState } from "react";
import { Bubble, Conversations, Sender } from "@ant-design/x";
import type { BubbleListProps } from "@ant-design/x";
import { useXChat } from "@ant-design/x-sdk";
import { InfoCircleOutlined } from "@ant-design/icons";
import { Button, Flex, Layout, Tag, Typography, message } from "antd";
import ToolBarRight from "@/layouts/components/Header/ToolBarRight";
import logo from "@/assets/images/logo.png";
import RouterGuard from "@/routers/helper/RouterGuard";
import EmbeddedModuleContent, {
  extractEmbeddedModules,
  type EmbeddedModuleReference
} from "./EmbeddedModuleContent";
import {
  cancelAgentTask,
  cancelUnifiedChatRun,
  createAgentTask,
  getUnifiedChatConversation,
  getAgentTaskDetail,
  getUnifiedChatRun,
  getUnifiedChatRunDetailEvents,
  listUnifiedChatConversations,
  listAgentTasks,
  resumeAgentTaskWithUserInput,
  listUnifiedChatRunEvents,
  listUnifiedChatRuns,
  UnifiedChatProvider,
  type AgentTask,
  type AgentTaskDetail,
  type UnifiedChatConversation,
  type UnifiedChatRunEvent
} from "@/api/modules/agentChat";
import "./index.less";

const { Header, Sider } = Layout;
const APP_TITLE = import.meta.env.VITE_GLOB_APP_TITLE;
const terminalKinds = new Set(["completed", "failed", "cancelled"]);
const MAX_TRACE_ROWS = 80;
const activeTaskStatuses = new Set(["Pending", "Running", "WaitingForApproval", "WaitingForUser"]);
const taskStatusLabels: Record<string, string> = {
  Pending: "等待执行",
  Running: "执行中",
  WaitingForApproval: "等待审批",
  WaitingForUser: "等待回复",
  Completed: "已完成",
  Failed: "失败",
  Cancelled: "已取消"
};
const taskStatusColors: Record<string, string> = {
  Pending: "default",
  Running: "processing",
  WaitingForApproval: "warning",
  WaitingForUser: "warning",
  Completed: "success",
  Failed: "error",
  Cancelled: "default"
};
const taskAttemptStatusLabels: Record<string, string> = {
  Running: "运行中",
  Completed: "已完成",
  Failed: "失败",
  Cancelled: "已取消",
  Paused: "已暂停"
};

type ChatMessage = {
  id: string;
  role: "user" | "assistant";
  content: string;
  status?: "streaming" | "completed" | "failed" | "cancelled";
  citations: string[];
  modules: EmbeddedModuleReference[];
};
type TraceItem = {
  id: string;
  kind: string;
  sequence: number;
  occurredAtUtc: string;
  title: string;
  description: string;
  tone: "success" | "error" | "loading";
  payload: Record<string, unknown>;
};
type CitationReference = {
  raw: string;
  knowledgeBaseCode?: string;
  fileName?: string;
  chunkSequence?: string;
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
const getPayloadValue = (payload: Record<string, unknown>, ...keys: string[]) => {
  for (const key of keys) {
    const value = payload[key];
    if (typeof value === "string" || typeof value === "number") return String(value);
  }
  return "";
};
const parseCitation = (raw: string): CitationReference => {
  const match = /^\[kb:([^/]+)\/(.+)#(\d+)\]$/.exec(raw.trim());
  return match
    ? { raw, knowledgeBaseCode: match[1], fileName: match[2], chunkSequence: match[3] }
    : { raw };
};
const formatTraceTime = (value: string) => {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? ""
    : new Intl.DateTimeFormat("zh-CN", { hour: "2-digit", minute: "2-digit", second: "2-digit" }).format(date);
};
const formatPayloadValue = (value: unknown) => {
  if (typeof value === "string") {
    try {
      return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
      return value;
    }
  }
  return JSON.stringify(value, null, 2) ?? String(value);
};
const getTrace = (event: UnifiedChatRunEvent): TraceItem => {
  const payload = parsePayload(event.payloadJson);
  let description = getPayloadText(payload) || event.route || "正在处理";
  if (event.kind === "knowledge-retrieved") {
    description = `检索 ${Number(payload.knowledgeBaseCount || 0)} 个知识库，命中 ${Number(payload.knowledgeHitCount || 0)} 个分块`;
  }
  if (event.kind === "approval-required") description = "等待人工审批后继续";
  if (event.kind === "route-selected") description = getPayloadValue(payload, "route", "Route") || event.route || "direct";
  if (event.kind === "skill-started") {
    description = getPayloadValue(payload, "skillName", "SkillName", "skillVersionId", "SkillVersionId") || "Skill";
  }
  if (event.kind.startsWith("tool-")) {
    description = [
      getPayloadValue(payload, "toolName", "ToolName", "toolVersionId", "ToolVersionId") || "MCP 工具",
      getPayloadValue(payload, "errorCode", "ErrorCode")
    ].filter(Boolean).join(" · ");
  }
  if (event.kind.startsWith("child-agent")) {
    description = [
      getPayloadValue(payload, "agentName", "AgentName", "agentVersionId", "AgentVersionId") || "子 Agent",
      getPayloadValue(payload, "reason", "Reason", "errorCode", "ErrorCode")
    ].filter(Boolean).join(" · ");
  }
  if (event.kind.startsWith("orchestration-")) {
    description = [
      getPayloadValue(payload, "orchestrationName", "OrchestrationName", "orchestrationVersionId", "OrchestrationVersionId") || "编排",
      getPayloadValue(payload, "reason", "Reason", "errorCode", "ErrorCode")
    ].filter(Boolean).join(" · ");
  }
  if (event.kind === "failed") {
    description = [
      getPayloadValue(payload, "errorCode", "ErrorCode") || "运行失败",
      getPayloadValue(payload, "detail", "Detail")
    ].filter(Boolean).join(" · ");
  }
  return {
    id: `${event.runId}-${event.sequence}`,
    kind: event.kind,
    sequence: event.sequence,
    occurredAtUtc: event.occurredAtUtc,
    title: traceTitles[event.kind] || event.kind,
    description,
    tone: event.kind === "failed" || event.kind === "tool-failed" ? "error" : terminalKinds.has(event.kind) ? "success" : "loading",
    payload
  };
};

const ToolBarLeft: React.FC = () => (
  <div className="logo"><img src={logo} alt="logo" className="logo-img" /><h2 className="logo-text">{APP_TITLE}</h2></div>
);

const NEW_CONVERSATION_KEY = "__new-conversation__";

interface ActiveSdkRun {
  requestId: number;
  assistantId: string;
  runId?: string;
  terminal: boolean;
  cancelRequested: boolean;
}

const LayoutChat: React.FC = () => {
  const [messageApi, contextHolder] = message.useMessage();
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [conversationId, setConversationId] = useState<string>();
  const [conversations, setConversations] = useState<UnifiedChatConversation[]>([]);
  const [traces, setTraces] = useState<TraceItem[]>([]);
  const [isRunning, setIsRunning] = useState(false);
  const [agentTasks, setAgentTasks] = useState<AgentTask[]>([]);
  const [tasksLoading, setTasksLoading] = useState(false);
  const [queueingTask, setQueueingTask] = useState(false);
  const [cancellingTaskId, setCancellingTaskId] = useState<string>();
  const [resumingTaskId, setResumingTaskId] = useState<string>();
  const [selectedTaskDetail, setSelectedTaskDetail] = useState<AgentTaskDetail>();
  const [loadingTaskDetailId, setLoadingTaskDetailId] = useState<string>();
  const [inspectorOpen, setInspectorOpen] = useState(false);
  const [mobileHistoryOpen, setMobileHistoryOpen] = useState(false);
  const sdkEventHandlerRef = useRef<(event: UnifiedChatRunEvent) => void>(() => undefined);
  const sdkErrorRef = useRef("");
  const sdkProviderRef = useRef<UnifiedChatProvider>();
  if (!sdkProviderRef.current) sdkProviderRef.current = new UnifiedChatProvider(event => sdkEventHandlerRef.current(event));
  // Ant Design X owns the request lifecycle; rich messages remain the single rendered state.
  const { onRequest: requestChat, isRequesting: isSdkRequesting, abort: abortChat, setMessages: resetSdkMessages } = useXChat({
    provider: sdkProviderRef.current,
    conversationKey: "unified-chat",
    requestPlaceholder: () => ({ role: "assistant" as const, content: "" }),
    requestFallback: (_, { error, messageInfo }) => {
      sdkErrorRef.current = error.message;
      return {
        role: "assistant" as const,
        content: messageInfo?.message?.content || error.message || "请求失败，请重试。"
      };
    }
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
  const activeSdkRunRef = useRef<ActiveSdkRun>();
  const sdkRequestStartedRef = useRef(false);
  const loadConversationsRef = useRef<() => Promise<void>>(async () => undefined);
  const knownTaskConversationIdsRef = useRef<Set<string>>(new Set());
  const newConversationTaskIdsRef = useRef<Set<string>>(new Set());

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
  useEffect(() => { timelineRef.current?.scrollTo({ top: timelineRef.current.scrollHeight, behavior: "smooth" }); }, [messages, traces]);

  useEffect(() => {
    if (!inspectorOpen) return;
    let disposed = false;
    let loading = false;
    const loadTasks = async () => {
      if (loading) return;
      loading = true;
      try {
        const values = await listAgentTasks();
        if (!disposed) {
          const conversationIds = values.flatMap(task => task.conversationId ? [task.conversationId] : []);
          const discoveredConversation = conversationIds.some(id => !knownTaskConversationIdsRef.current.has(id));
          knownTaskConversationIdsRef.current = new Set(conversationIds);
          setAgentTasks(values);
          if (discoveredConversation) void loadConversationsRef.current();
        }
      } catch (error) {
        if (!disposed) messageApi.error(error instanceof Error ? error.message : "任务列表加载失败");
      } finally {
        loading = false;
        if (!disposed) setTasksLoading(false);
      }
    };
    setTasksLoading(true);
    void loadTasks();
    const timer = window.setInterval(() => void loadTasks(), 3000);
    return () => {
      disposed = true;
      window.clearInterval(timer);
    };
  }, [inspectorOpen, conversationId, messageApi]);

  const queueAgentTask = useCallback(async () => {
    const normalized = input.trim();
    if (!normalized || queueingTask || isRunning || isSdkRequesting) return;
    setQueueingTask(true);
    try {
      const task = await createAgentTask({
        title: normalized.slice(0, 80),
        input: normalized,
        conversationId,
        idempotencyKey: crypto.randomUUID()
      });
      if (!conversationId) newConversationTaskIdsRef.current.add(task.id);
      setAgentTasks(current => [task, ...current.filter(item => item.id !== task.id)]);
      setInput("");
      setInspectorOpen(true);
      setMobileHistoryOpen(false);
      messageApi.success("已加入后台任务");
    } catch (error) {
      messageApi.error(error instanceof Error ? error.message : "后台任务创建失败");
    } finally {
      setQueueingTask(false);
    }
  }, [conversationId, input, isRunning, isSdkRequesting, messageApi, queueingTask]);

  const cancelTask = useCallback(async (taskId: string) => {
    if (cancellingTaskId) return;
    setCancellingTaskId(taskId);
    try {
      const task = await cancelAgentTask(taskId);
      setAgentTasks(current => current.map(item => (item.id === task.id ? task : item)));
      messageApi.success("任务已取消");
    } catch (error) {
      messageApi.error(error instanceof Error ? error.message : "任务取消失败");
    } finally {
      setCancellingTaskId(undefined);
    }
  }, [cancellingTaskId, messageApi]);

  const resumeTask = useCallback(async (task: AgentTask) => {
    const normalized = input.trim();
    if (!normalized || resumingTaskId) return;
    setResumingTaskId(task.id);
    try {
      const resumed = await resumeAgentTaskWithUserInput(task.id, task.logicalRevision, normalized);
      setAgentTasks(current => current.map(item => (item.id === resumed.id ? resumed : item)));
      setInput("");
      messageApi.success("回复已提交，任务将继续执行");
    } catch (error) {
      messageApi.error(error instanceof Error ? error.message : "任务继续失败");
    } finally {
      setResumingTaskId(undefined);
    }
  }, [input, messageApi, resumingTaskId]);

  const loadTaskDetail = useCallback(async (taskId: string) => {
    if (loadingTaskDetailId) return;
    if (selectedTaskDetail?.task.id === taskId) {
      setSelectedTaskDetail(undefined);
      return;
    }

    setLoadingTaskDetailId(taskId);
    try {
      setSelectedTaskDetail(await getAgentTaskDetail(taskId));
    } catch (error) {
      messageApi.error(error instanceof Error ? error.message : "任务轨迹加载失败");
    } finally {
      setLoadingTaskDetailId(undefined);
    }
  }, [loadingTaskDetailId, messageApi, selectedTaskDetail?.task.id]);

  const selectedTaskId = selectedTaskDetail?.task.id;
  const selectedTaskStatus = selectedTaskDetail?.task.status;
  useEffect(() => {
    if (!inspectorOpen || !selectedTaskId || !selectedTaskStatus || !activeTaskStatuses.has(selectedTaskStatus)) return;
    let disposed = false;
    let loading = false;
    const refreshTaskDetail = async () => {
      if (loading) return;
      loading = true;
      try {
        const detail = await getAgentTaskDetail(selectedTaskId);
        if (!disposed) setSelectedTaskDetail(detail);
      } catch (error) {
        if (!disposed) messageApi.error(error instanceof Error ? error.message : "任务轨迹刷新失败");
      } finally {
        loading = false;
      }
    };
    const timer = window.setInterval(() => void refreshTaskDetail(), 3000);
    return () => {
      disposed = true;
      window.clearInterval(timer);
    };
  }, [inspectorOpen, messageApi, selectedTaskId, selectedTaskStatus]);

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
  const appendAssistantModules = useCallback((id: string, modules: EmbeddedModuleReference[]) => {
    if (!modules.length) return;
    setMessages(current => current.map(item => {
      if (item.id !== id) return item;
      const values = new Map(item.modules.map(module => [`${module.moduleCode}:${module.viewType}`, module]));
      modules.forEach(module => values.set(`${module.moduleCode}:${module.viewType}`, module));
      return { ...item, modules: Array.from(values.values()) };
    }));
  }, []);
  sdkEventHandlerRef.current = event => {
    const active = activeSdkRunRef.current;
    if (!active || active.requestId !== requestIdRef.current) return;
    if (event.runId) active.runId ||= event.runId;
    if (event.conversationId) setConversationId(event.conversationId);
    const payload = parsePayload(event.payloadJson);
    if (event.kind === "message" && event.depth === 0 && payload.eventKind === "Delta") {
      enqueueTyping(active.assistantId, getPayloadText(payload));
    }
    if (event.kind === "knowledge-citation") {
      const citation = getPayloadText(payload);
      if (citation) setMessages(current => current.map(item => (item.id === active.assistantId && !item.citations.includes(citation) ? { ...item, citations: [...item.citations, citation] } : item)));
    }
    if (event.kind === "tool-succeeded") appendAssistantModules(active.assistantId, extractEmbeddedModules(payload));
    if (event.kind !== "message") setTraces(current => [...current.slice(-(MAX_TRACE_ROWS - 1)), getTrace(event)]);
    if (terminalKinds.has(event.kind)) {
      flushTyping();
      active.terminal = true;
      updateAssistant(active.assistantId, { status: event.kind === "completed" ? "completed" : event.kind === "cancelled" ? "cancelled" : "failed" });
      void loadConversationsRef.current();
    }
  };
  const reconcileDisconnectedRun = useCallback(async (active: ActiveSdkRun) => {
    if (!active.runId) return false;
    for (let attempt = 0; attempt < 4; attempt += 1) {
      try {
        const run = await getUnifiedChatRun(active.runId);
        const status = run.status.toLowerCase();
        if (["completed", "failed", "cancelled", "blocked"].includes(status)) {
          if (activeSdkRunRef.current !== active) return true;
          const messageStatus = status === "completed" ? "completed" : status === "cancelled" ? "cancelled" : "failed";
          setMessages(current =>
            current.map(item =>
              item.id === active.assistantId
                ? {
                    ...item,
                    content: run.output || item.content || (messageStatus === "cancelled" ? "运行已取消。" : run.errorCode || "请求失败，请重试。"),
                    status: messageStatus
                  }
                : item
            )
          );
          try {
            let events: UnifiedChatRunEvent[] = await listUnifiedChatRunEvents(active.runId);
            if (!events.length) events = await getUnifiedChatRunDetailEvents(active.runId);
            if (activeSdkRunRef.current === active) {
              setTraces(events.filter(event => event.kind !== "message").map(getTrace));
            }
          } catch {
            // The terminal Run remains authoritative when trace recovery fails.
          }
          active.terminal = true;
          void loadConversationsRef.current();
          return true;
        }
      } catch {
        // Retry briefly because disconnect cleanup and persistence can race.
      }
      if (attempt < 3) await new Promise(resolve => window.setTimeout(resolve, 100 * 2 ** attempt));
    }
    return false;
  }, []);
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
    void (async () => {
      const reconciled = active.terminal ? true : await reconcileDisconnectedRun(active);
      if (activeSdkRunRef.current !== active) return;
      if (!reconciled) {
        const fallbackText =
          active.cancelRequested
            ? "运行已取消。"
            : sdkErrorRef.current.trim()
              ? sdkErrorRef.current
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
      sdkErrorRef.current = "";
      setIsRunning(false);
    })();
  }, [flushTyping, isSdkRequesting, reconcileDisconnectedRun]);
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
    if (!id || isRunning || isSdkRequesting) return;
    const revision = ++conversationRevisionRef.current;
    newConversationTaskIdsRef.current.clear();
    setSelectedTaskDetail(undefined);
    setMobileHistoryOpen(false);
    setConversationId(id);
    setMessages([]);
    setTraces([]);
    try {
      const [detail, runs] = await Promise.all([getUnifiedChatConversation(id), listUnifiedChatRuns(id, 1)]);
      const latestRun = runs[0] ? await getUnifiedChatRun(runs[0]) : undefined;
      let events: UnifiedChatRunEvent[] = runs[0] ? await listUnifiedChatRunEvents(runs[0]) : [];
      if (!events.length && runs[0]) events = await getUnifiedChatRunDetailEvents(runs[0]);
      if (revision !== conversationRevisionRef.current) return;
      const loadedMessages: ChatMessage[] = detail.messages.map(item => ({
          id: item.id,
          role: item.role === 0 || String(item.role).toLowerCase() === "user" ? "user" : "assistant",
          content: item.content,
          citations: [],
          modules: []
        }));
      const latestStatus = latestRun?.status.toLowerCase();
      if (
        latestRun?.output.trim() &&
        latestStatus &&
        ["failed", "cancelled", "blocked"].includes(latestStatus) &&
        !loadedMessages.some(item => item.role === "assistant" && item.content === latestRun.output)
      ) {
        loadedMessages.push({
          id: `recovered-${latestRun.id}`,
          role: "assistant",
          content: latestRun.output,
          citations: [],
          modules: [],
          status: latestStatus === "cancelled" ? "cancelled" : "failed"
        });
      }
      setMessages(loadedMessages);
      setTraces(events.filter(event => event.kind !== "message").map(getTrace));
      const citations = events
        .filter(event => event.kind === "knowledge-citation")
        .map(event => getPayloadText(parsePayload(event.payloadJson)))
        .filter(Boolean);
      const embeddedModules = events
        .filter(event => event.kind === "tool-succeeded")
        .flatMap(event => extractEmbeddedModules(parsePayload(event.payloadJson)));
      if (citations.length || embeddedModules.length) {
        setMessages(current => {
          const lastAssistantIndex = current.map(item => item.role).lastIndexOf("assistant");
          return current.map((item, index) => (index === lastAssistantIndex ? {
            ...item,
            citations: Array.from(new Set(citations)),
            modules: embeddedModules
          } : item));
        });
      }
    } catch (error) {
      if (revision === conversationRevisionRef.current) {
        messageApi.error(error instanceof Error ? error.message : "会话读取失败。");
      }
    }
  }, [isRunning, isSdkRequesting, messageApi]);
  useEffect(() => { void loadConversations(); }, [loadConversations]);

  const startRun = async (value: string) => {
    const inputValue = value.trim();
    if (!inputValue || isRunning || isSdkRequesting) return;
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
    setMessages(current => [
      ...current,
      { id: createId(), role: "user", content: inputValue, citations: [], modules: [] },
      { id: assistantId, role: "assistant", content: "", citations: [], modules: [], status: "streaming" }
    ]);
    sdkErrorRef.current = "";
    resetSdkMessages([]);
    requestChat({ messages: [{ role: "user", content: inputValue }], conversationId });
  };
  const startNewConversation = () => {
    const shouldAbort = isRunning || isSdkRequesting;
    requestIdRef.current += 1;
    conversationRevisionRef.current += 1;
    activeSdkRunRef.current = undefined;
    sdkRequestStartedRef.current = false;
    flushTyping();
    sdkErrorRef.current = "";
    resetSdkMessages([]);
    newConversationTaskIdsRef.current.clear();
    setSelectedTaskDetail(undefined);
    setConversationId(undefined);
    setMessages([]);
    setTraces([]);
    setInput("");
    setIsRunning(false);
    setMobileHistoryOpen(false);
    if (shouldAbort) abortChat();
  };
  const cancelRun = useCallback(async () => {
    const active = activeSdkRunRef.current;
    if (!active || active.cancelRequested) return;
    active.cancelRequested = true;
    if (!active.runId) {
      abortChat();
      return;
    }
    try {
      await cancelUnifiedChatRun(active.runId);
    } catch (error) {
      if (activeSdkRunRef.current === active && !active.terminal) {
        messageApi.warning(error instanceof Error ? error.message : "取消请求失败，已中断当前连接。");
        abortChat();
      }
    }
  }, [abortChat, messageApi]);
  const bubbleRoles: BubbleListProps["role"] = {
    assistant: { placement: "start" },
    user: { placement: "end" as const }
  };
  const citationReferences = Array.from(new Set(messages.flatMap(item => item.citations))).map(parseCitation);
  const visibleAgentTasks = agentTasks.filter(task => conversationId
    ? task.conversationId === conversationId
    : !task.conversationId || newConversationTaskIdsRef.current.has(task.id));

  return (
    <RouterGuard>
      {contextHolder}
      <section className="layout-vertical layout-chat"><Layout>
        <Header><ToolBarLeft /><div className="agent-chat-header-actions"><Tag color={isRunning ? "processing" : "success"}>{isRunning ? "运行中" : "Unified Chat"}</Tag><Button className="agent-chat-mobile-history-button" type="text" aria-controls="agent-chat-conversation-list" aria-expanded={mobileHistoryOpen} onClick={() => { setMobileHistoryOpen(value => !value); setInspectorOpen(false); }}>会话</Button><Button type="text" icon={<InfoCircleOutlined />} aria-controls="agent-chat-inspector" aria-expanded={inspectorOpen} onClick={() => { setInspectorOpen(value => !value); setMobileHistoryOpen(false); }}>{inspectorOpen ? "收起详情" : "运行详情"}</Button><ToolBarRight layout="Chat" /></div></Header>
        <main className="agent-chat-main">
          <aside className={`agent-chat-sidebar${mobileHistoryOpen ? " is-mobile-open" : ""}`} id="agent-chat-conversation-list"><Conversations items={conversations.map(item => ({ key: item.id, label: item.title || "未命名会话", group: "最近" }))} activeKey={conversationId ?? NEW_CONVERSATION_KEY} creation={{ onClick: startNewConversation }} onActiveChange={id => { if (isRunning) messageApi.warning("运行结束后才能切换会话。"); else void selectConversation(String(id)); }} className="agent-chat-conversations" /></aside>
          <section className="agent-chat-workspace">
            <div className="agent-chat-timeline" ref={timelineRef}>
              {messages.length ? <Bubble.List items={messages.map(item => ({
                key: item.id,
                role: item.role,
                content: item.role === "assistant"
                  ? <EmbeddedModuleContent content={item.content} modules={item.modules} streaming={item.status === "streaming"} />
                  : item.content,
                status: item.status === "streaming" ? "updating" : item.status === "failed" ? "error" : item.status === "cancelled" ? "abort" : "success",
                loading: item.status === "streaming" && !item.content && !item.modules.length
              }))} role={bubbleRoles} /> :
                <Flex className="agent-chat-welcome" vertical align="center" justify="center" gap={12}><Typography.Title level={2}>Unified Chat</Typography.Title><Typography.Text type="secondary">与已发布的主 Agent 对话，回答会结合 Skills、知识库和 MCP 工具。</Typography.Text></Flex>}
            </div>
            <div className="agent-chat-composer">
              <div className="agent-chat-task-actions"><Button disabled={!input.trim() || isRunning || isSdkRequesting} loading={queueingTask} onClick={() => void queueAgentTask()}>后台执行</Button></div>
              <Sender value={input} onChange={setInput} onSubmit={() => void startRun(input)} onCancel={cancelRun} loading={isRunning || isSdkRequesting} placeholder="输入问题，Unified Chat 会调用已配置的 Agent 能力" />
            </div>
          </section>
          <Sider className="agent-chat-inspector" id="agent-chat-inspector" width={340} collapsedWidth={0} collapsed={!inspectorOpen} trigger={null} theme="light">
            <div className="agent-chat-inspector-content">
              <Typography.Title level={5}>运行详情</Typography.Title>
              <section className="agent-chat-inspector-section">
                <div className="agent-chat-section-heading"><Typography.Text strong>后台任务</Typography.Text>{tasksLoading ? <Typography.Text type="secondary">加载中</Typography.Text> : null}</div>
                {visibleAgentTasks.length ? (
                  <div className="agent-chat-task-list">
                    {visibleAgentTasks.map(task => (
                      <article className="agent-chat-task-card" key={task.id}>
                        <div className="agent-chat-task-heading"><Typography.Text strong ellipsis>{task.title}</Typography.Text><Tag color={taskStatusColors[task.status]}>{taskStatusLabels[task.status] || task.status}</Tag></div>
                        <Typography.Text type="secondary">尝试 {task.attemptCount}/{task.maximumAttempts}</Typography.Text>
                        {task.lastErrorCode ? <Typography.Text type="danger">{task.lastErrorCode}{task.lastErrorMessage ? ` · ${task.lastErrorMessage}` : ""}</Typography.Text> : null}
                        {task.status === "WaitingForUser" ? <Button size="small" disabled={!input.trim()} loading={resumingTaskId === task.id} onClick={() => void resumeTask(task)}>使用当前输入继续</Button> : null}
                        <Button size="small" loading={loadingTaskDetailId === task.id} onClick={() => void loadTaskDetail(task.id)}>{selectedTaskDetail?.task.id === task.id ? "收起轨迹" : "查看轨迹"}</Button>
                        {selectedTaskDetail?.task.id === task.id ? (
                          <div className="agent-chat-task-events">
                            <Typography.Text strong>执行尝试</Typography.Text>
                            {selectedTaskDetail.attempts.length ? selectedTaskDetail.attempts.map(attempt => (
                              <div key={attempt.id} className="agent-chat-task-attempt">
                                <div><Typography.Text>第 {attempt.attemptNumber} 次</Typography.Text><Tag>{taskAttemptStatusLabels[attempt.status] || attempt.status}</Tag></div>
                                {attempt.runId ? <Typography.Text type="secondary" copyable={{ text: attempt.runId }}>Run {attempt.runId.slice(0, 8)}</Typography.Text> : null}
                                {attempt.errorCode ? <Typography.Text type="danger">{attempt.errorCode}</Typography.Text> : null}
                              </div>
                            )) : <Typography.Text type="secondary">尚未开始执行</Typography.Text>}
                            <Typography.Text strong>生命周期事件</Typography.Text>
                            {selectedTaskDetail.events.length ? selectedTaskDetail.events.map(event => (
                              <div key={event.id} className="agent-chat-task-event">
                                <Typography.Text>{event.kind}</Typography.Text>
                                <Typography.Text type="secondary">{new Date(event.occurredAtUtc).toLocaleString()} · {taskStatusLabels[event.status] || event.status}</Typography.Text>
                              </div>
                            )) : <Typography.Text type="secondary">暂无生命周期事件</Typography.Text>}
                          </div>
                        ) : null}
                        {activeTaskStatuses.has(task.status) ? <Button danger size="small" loading={cancellingTaskId === task.id} onClick={() => void cancelTask(task.id)}>取消任务</Button> : null}
                      </article>
                    ))}
                  </div>
                ) : <Typography.Text type="secondary">当前会话暂无后台任务。</Typography.Text>}
              </section>
              <section className="agent-chat-inspector-section">
                <Typography.Text strong>知识库引用</Typography.Text>
                {citationReferences.length ? <div className="agent-chat-citation-list">{citationReferences.map(citation => (
                  <article className="agent-chat-citation-card" key={citation.raw} title={citation.raw}>
                    <div className="agent-chat-citation-title">
                      <Typography.Text strong ellipsis>{citation.fileName || citation.raw}</Typography.Text>
                      {citation.chunkSequence ? <Tag>分块 {citation.chunkSequence}</Tag> : null}
                    </div>
                    {citation.knowledgeBaseCode ? <Typography.Text type="secondary">知识库：{citation.knowledgeBaseCode}</Typography.Text> : null}
                  </article>
                ))}</div> : <Typography.Text type="secondary">本次对话暂无知识库引用。</Typography.Text>}
              </section>
              <section className="agent-chat-inspector-section">
                <Typography.Text strong>运行轨迹</Typography.Text>
                {traces.length ? <div className="agent-chat-trace-list">{traces.map(trace => {
                  const argumentsValue = trace.payload.argumentsJson ?? trace.payload.ArgumentsJson;
                  const resultValue = trace.payload.text ?? trace.payload.Text ?? trace.payload.output ?? trace.payload.Output;
                  const errorCode = getPayloadValue(trace.payload, "errorCode", "ErrorCode");
                  const errorDetail = getPayloadValue(trace.payload, "detail", "Detail");
                  return <details className="agent-chat-trace-row" data-tone={trace.tone} key={trace.id}>
                      <summary>
                        <span className="agent-chat-trace-sequence">{String(trace.sequence).padStart(2, "0")}</span>
                        <span className="agent-chat-trace-copy"><strong>{trace.title}</strong><small>{trace.description}</small></span>
                        <time>{formatTraceTime(trace.occurredAtUtc)}</time>
                      </summary>
                      <div className="agent-chat-trace-detail">
                        <Typography.Text type="secondary">{trace.kind}</Typography.Text>
                        {argumentsValue !== undefined ? <section className="agent-chat-trace-block"><strong>调用参数</strong><pre>{formatPayloadValue(argumentsValue)}</pre></section> : null}
                        {resultValue !== undefined ? <section className="agent-chat-trace-block"><strong>原始结果</strong><pre>{formatPayloadValue(resultValue)}</pre></section> : null}
                        {errorCode ? <section className="agent-chat-trace-failure"><strong>{errorCode}</strong>{errorDetail ? <span>{errorDetail}</span> : null}</section> : null}
                        <details className="agent-chat-trace-raw"><summary>完整 payload</summary><pre>{formatPayloadValue(trace.payload)}</pre></details>
                      </div>
                    </details>;
                })}</div> : <Typography.Text type="secondary">运行后会显示调用轨迹。</Typography.Text>}
              </section>
            </div>
          </Sider>
          {mobileHistoryOpen || inspectorOpen ? <button className="agent-chat-mobile-backdrop" type="button" aria-label="关闭侧栏" onClick={() => { setMobileHistoryOpen(false); setInspectorOpen(false); }} /> : null}
        </main>
      </Layout></section>
    </RouterGuard>
  );
};

export default LayoutChat;
