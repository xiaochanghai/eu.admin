import React, { useEffect, useRef, useState } from "react";
import {
  CloudUploadOutlined,
  CopyOutlined,
  DeleteOutlined,
  DislikeOutlined,
  EditOutlined,
  LikeOutlined,
  PaperClipOutlined,
  PlusOutlined,
  QuestionCircleOutlined,
  ReloadOutlined,
  UserOutlined
} from "@ant-design/icons";
import { Attachments, Bubble, Conversations, Prompts, Sender, useXAgent, useXChat } from "@ant-design/x";
import { Button, Flex, type GetProp, Space, Spin, message } from "antd";
import dayjs from "dayjs";
import { Logo, AvatarIcon, Welcome, DEFAULT_CONVERSATIONS_ITEMS, HOT_TOPICS, DESIGN_GUIDE, SENDER_PROMPTS } from "./index";
import { useTranslation } from "react-i18next";

import RouterGuard from "@/routers/helper/RouterGuard";
import { useNavigate } from "react-router-dom";
import { RootState, useSelector, store } from "@/redux";
import { LOGIN_URL } from "@/config";
import { useStyle } from "./Styles";
import { getCache, setCache, createUuid } from "@/utils";
import { BubbleDataType, ChatBubble, BubbleDataTypeContent } from "./Bubble";
let baseURL = import.meta.env.VITE_API_URL as string;

import ChatContent from "./Content";

export const ChatMain: React.FC = () => {
  const { styles } = useStyle();
  const abortController = useRef<AbortController | null>(null);
  const navigate = useNavigate();
  const token = useSelector((state: RootState) => state.user.token);

  let parentMessageId: string = "";
  // ==================== State ====================
  const [messageHistory, setMessageHistory] = useState<Record<string, any>>({});

  const [conversations, setConversations] = useState(DEFAULT_CONVERSATIONS_ITEMS);
  const [curConversation, setCurConversation] = useState(DEFAULT_CONVERSATIONS_ITEMS[0].key);

  const [attachmentsOpen, setAttachmentsOpen] = useState(false);
  const [attachedFiles, setAttachedFiles] = useState<GetProp<typeof Attachments, "items">>([]);
  // const [bubbleContents, setBubbleContents] = useState<BubbleDataTypeContent[]>([]);
  let bubbleContents: BubbleDataTypeContent[] = [];
  const [inputValue, setInputValue] = useState("");
  const { t } = useTranslation();

  // 移除 isWaitingForResponse 状态，直接使用 useXChat 提供的 loading 状态

  const chatId =
    getCache("chatId") ||
    (() => {
      const newId = createUuid();
      setCache("chatId", newId);
      return newId;
    })();
  /**
   * 🔔 Please replace the BASE_URL, PATH, MODEL, API_KEY with your own values.
   */
  let baseURL1 = `${baseURL == "/" ? "" : baseURL}/api/Stream/chat/${chatId}`;
  console.log("baseURL:" + baseURL1);
  // ==================== Runtime ====================
  const [agent] = useXAgent<BubbleDataType>({
    baseURL: baseURL1,
    // model: "deepseek-ai/DeepSeek-R1",
    dangerouslyApiKey: `Bearer ${store.getState().user.token}`
  });
  const loading = agent.isRequesting();

  const { onRequest, messages, setMessages } = useXChat({
    agent,
    requestFallback: (_, { error }) => {
      // debugger;
      if (error.name === "AbortError") {
        return {
          content: "Request is aborted",
          role: "assistant"
        };
      }
      return {
        content: "Request failed, please try again!",
        role: "assistant"
      };
    },
    transformMessage: info => {
      const { originMessage, chunk } = info || {};
      let currentContent = "";
      let currentThink = "";
      try {
        if (chunk?.data && !chunk?.data.includes("DONE")) {
          const message = JSON.parse(chunk?.data);
          // const message = JSON.parse(chunk?.data);
          // currentThink = "1212";
          currentContent = message?.content || "";
        }
      } catch (error) {
        console.error(error);
      }

      let content = "";
      let contents = [...bubbleContents];

      // if (!originMessage?.content && currentThink) {
      //   content = `<think>${currentThink}`;
      // } else if (originMessage?.content?.includes("<think>") && !originMessage?.content.includes("</think>") && currentContent) {
      //   content = `${originMessage?.content}</think>${currentContent}`;
      // } else {
      //   content = `${originMessage?.content || ""}${currentThink}${currentContent}`;
      // }

      if (chunk?.id && !chunk?.data.includes("DONE")) {
        const index = contents.findIndex(u => u.key === chunk?.id);
        if (index !== -1) {
          content = `${originMessage?.content || ""}${currentThink}${currentContent}`;
          contents[index] = {
            ...contents[index],
            content: content
          };
        } else {
          contents.push({ key: chunk?.id, content: currentContent, parentMessageId });
        }
        bubbleContents = contents;
      }
      return {
        content: content,
        contents: bubbleContents,
        parentMessageId,
        // contents: [{ key: chunk?.id, content: content }],
        role: "assistant"
      };
    },
    resolveAbortController: controller => {
      abortController.current = controller;
    }
  });

  // 自动移除 loading 气泡：当最后一条不是 loading 时，删掉 loading
  useEffect(() => {
    if (!messages?.length) return;
    const hasLoading = messages.some(m => m.id === "loading");
    const last = messages[messages.length - 1];
    if (hasLoading && last.id !== "loading") {
      setMessages(prev => prev.filter(m => m.id !== "loading"));
    }
  }, [messages, setMessages]);

  // ==================== Event ====================
  const onSubmit = (val: string) => {
    if (!val) return;
    parentMessageId = createUuid();
    if (loading) {
      message.error("请求正在进行中，请等待请求完成");
      return;
    }

    // 提交后立即插入一条 loading 气泡（避免重复显示）
    setMessages(prev => [
      ...prev,
      {
        id: "loading",
        message: { role: "assistant", content: "" },
        status: "loading",
        loading: true
      } as any
    ]);

    onRequest({
      stream: true,
      message: { role: "user", content: val }
    });
  };

  // 移除监听消息变化的 useEffect
  // useEffect(() => {
  //   if (messages.length > 0) {
  //     const lastMessage = messages[messages.length - 1];
  //     if (lastMessage.message?.role === "assistant" && !loading && isWaitingForResponse) {
  //       setIsWaitingForResponse(false);
  //     }
  //   }
  // }, [messages, loading, isWaitingForResponse]);

  // ==================== Nodes ====================
  const chatSider = (
    <div className={styles.sider} style={{ display: "none" }}>
      <Logo />

      {/* 🌟 添加会话 */}
      <Button
        onClick={() => {
          const now = dayjs().valueOf().toString();
          setConversations([
            {
              key: now,
              label: `New Conversation ${conversations.length + 1}`,
              group: "Today"
            },
            ...conversations
          ]);
          setCurConversation(now);
          setMessages([]);
          setCache("chatId", "");
        }}
        type="link"
        className={styles.addBtn}
        icon={<PlusOutlined />}
      >
        新会话
      </Button>

      {/* 🌟 会话管理 */}
      <Conversations
        items={conversations}
        className={styles.conversations}
        activeKey={curConversation}
        onActiveChange={async val => {
          abortController.current?.abort();
          // The abort execution will trigger an asynchronous requestFallback, which may lead to timing issues.
          // In future versions, the sessionId capability will be added to resolve this problem.
          setTimeout(() => {
            setCurConversation(val);
            setMessages(messageHistory?.[val] || []);
          }, 100);
        }}
        groupable
        styles={{ item: { padding: "0 8px" } }}
        menu={conversation => ({
          items: [
            {
              label: "Rename",
              key: "rename",
              icon: <EditOutlined />
            },
            {
              label: "Delete",
              key: "delete",
              icon: <DeleteOutlined />,
              danger: true,
              onClick: () => {
                const newList = conversations.filter(item => item.key !== conversation.key);
                const newKey = newList?.[0]?.key;
                setConversations(newList);
                // The delete operation modifies curConversation and triggers onActiveChange, so it needs to be executed with a delay to ensure it overrides correctly at the end.
                // This feature will be fixed in a future version.
                setTimeout(() => {
                  if (conversation.key === curConversation) {
                    setCurConversation(newKey);
                    setMessages(messageHistory?.[newKey] || []);
                  }
                }, 200);
              }
            }
          ]
        })}
      />

      <div className={styles.siderFooter}>
        <AvatarIcon height={24} />
        <Button type="text" icon={<QuestionCircleOutlined />} />
      </div>
    </div>
  );

  const chatList = (
    <div className={styles.chatList}>
      {messages?.length ? (
        /* 🌟 消息列表 */
        <Bubble.List
          items={[
            ...(messages?.map((i, index) => {
              const isAI = !!(index % 2);
              let message1 = {
                ...i.message,
                content:
                  i.message.contents && i.message.contents.length > 0
                    ? i.message.contents.map(list => {
                        return {
                          key: list.key,
                          description: <ChatContent content={list.content!} />
                        };
                      })
                    : i.message.content
              };
              return {
                ...message1,
                key: index,
                role: isAI ? "assistant" : "user",
                // loading 气泡不显示 footer
                footer: i.id === "loading" ? null : undefined,
                messageRender: (content: any) => {
                  // 统一在这里渲染 loading
                  if (i.id === "loading" || i.status === "loading") {
                    return (
                      <Space>
                        <Spin size="small" />
                        <span style={{ color: "#666" }}>{t("chat.loadingMessage")}</span>
                      </Space>
                    );
                  }
                  if (i.message.role === "user") {
                    return <ChatBubble message={i} content={content} />;
                  }
                  return Array.isArray(content) ? (
                    <Prompts items={content} className="chat-bubble-prompt" vertical />
                  ) : (
                    <ChatBubble message={i} content={content} />
                  );
                }
              };
            }) ?? [])
          ]}
          style={{
            flex: 1,
            overflow: "auto",
            paddingInline: "calc(calc(100% - 1100px) /2)",
            minHeight: 0
          }}
          roles={{
            assistant: {
              placement: "start",
              avatar: { icon: <UserOutlined />, style: { background: "#fde3cf" } },
              footer: (
                <div style={{ display: "flex" }}>
                  <Button type="text" size="small" icon={<ReloadOutlined />} />
                  <Button type="text" size="small" icon={<CopyOutlined />} />
                  <Button type="text" size="small" icon={<LikeOutlined />} />
                  <Button type="text" size="small" icon={<DislikeOutlined />} />
                </div>
              )
            },
            user: {
              placement: "end",
              avatar: { icon: <AvatarIcon />, style: { background: "transparent " } }
            }
          }}
        />
      ) : (
        <Space
          direction="vertical"
          size={16}
          style={{ paddingInline: "calc(calc(100% - 900px) /2)" }}
          className={styles.placeholder}
        >
          {/* ... 其余保持不变 ... */}
          <Welcome />
          <Flex gap={16}>
            <Prompts
              items={[HOT_TOPICS]}
              styles={{
                list: { height: "100%" },
                item: {
                  flex: 1,
                  backgroundImage: "linear-gradient(123deg, #e5f4ff 0%, #efe7ff 100%)",
                  borderRadius: 12,
                  border: "none"
                },
                subItem: { padding: 0, background: "transparent" }
              }}
              onItemClick={info => {
                onSubmit(info.data.description as string);
              }}
              className={styles.chatPrompt}
            />

            <Prompts
              items={[DESIGN_GUIDE]}
              styles={{
                item: {
                  flex: 1,
                  backgroundImage: "linear-gradient(123deg, #e5f4ff 0%, #efe7ff 100%)",
                  borderRadius: 12,
                  border: "none"
                },
                subItem: { background: "#ffffffa6" }
              }}
              onItemClick={info => {
                onSubmit(info.data.description as string);
              }}
              className={styles.chatPrompt}
            />
          </Flex>
        </Space>
      )}
    </div>
  );
  const senderHeader = (
    <Sender.Header
      title={t("chat.senderHeaderTitle")}
      open={attachmentsOpen}
      onOpenChange={setAttachmentsOpen}
      styles={{ content: { padding: 0 } }}
    >
      <Attachments
        beforeUpload={() => false}
        items={attachedFiles}
        onChange={info => setAttachedFiles(info.fileList)}
        placeholder={type =>
          type === "drop"
            ? { title: t("chat.senderHeaderAttachmentDropTitle") }
            : {
                icon: <CloudUploadOutlined />,
                title: t("chat.senderHeaderAttachmentTitle"),
                description: t("chat.senderHeaderAttachmentDesc")
              }
        }
      />
    </Sender.Header>
  );

  const chatSender = (
    <>
      {/* 🌟 提示词 */}
      <Prompts
        items={SENDER_PROMPTS}
        onItemClick={info => {
          onSubmit(info.data.description as string);
        }}
        styles={{
          item: { padding: "6px 12px" }
        }}
        className={styles.senderPrompt}
      />
      {/* 🌟 输入框 */}
      <Sender
        value={inputValue}
        header={senderHeader}
        onSubmit={() => {
          onSubmit(inputValue);
          setInputValue("");
        }}
        onChange={setInputValue}
        onCancel={() => {
          abortController.current?.abort();
        }}
        prefix={
          <Button
            type="text"
            icon={<PaperClipOutlined style={{ fontSize: 18 }} />}
            onClick={() => setAttachmentsOpen(!attachmentsOpen)}
          />
        }
        loading={loading}
        className={styles.sender}
        allowSpeech
        actions={(_, info) => {
          const { SendButton, LoadingButton, SpeechButton } = info.components;
          return (
            <Flex gap={4}>
              <SpeechButton className={styles.speechButton} />
              {loading ? <LoadingButton type="default" /> : <SendButton type="primary" />}
            </Flex>
          );
        }}
        placeholder={t("chat.senderPlaceholder")}
      />
    </>
  );

  useEffect(() => {
    // history mock
    if (messages?.length) {
      setMessageHistory(prev => ({
        ...prev,
        [curConversation]: messages
      }));
    }
  }, [messages]);

  // Redirect to login immediately when token becomes empty (after logout)
  useEffect(() => {
    if (!token) {
      navigate(LOGIN_URL, { replace: true });
    }
  }, [token]);

  // ==================== Render =================
  return (
    <RouterGuard>
      <div className={styles.layout}>
        {chatSider}
        <div className={styles.chat}>
          {chatList}
          {chatSender}
        </div>
      </div>
    </RouterGuard>
  );
};
