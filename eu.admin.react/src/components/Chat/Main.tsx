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

import RouterGuard from "@/routers/helper/RouterGuard";
import { useNavigate } from "react-router-dom";
import { RootState, useSelector } from "@/redux";
import { LOGIN_URL } from "@/config";
import { useStyle } from "./Styles";

type BubbleDataType = {
  role: string;
  content: string;
};

export const ChatMain: React.FC = () => {
  const { styles } = useStyle();
  const abortController = useRef<AbortController | null>(null);
  const navigate = useNavigate();
  const token = useSelector((state: RootState) => state.user.token);

  // ==================== State ====================
  const [messageHistory, setMessageHistory] = useState<Record<string, any>>({});

  const [conversations, setConversations] = useState(DEFAULT_CONVERSATIONS_ITEMS);
  const [curConversation, setCurConversation] = useState(DEFAULT_CONVERSATIONS_ITEMS[0].key);

  const [attachmentsOpen, setAttachmentsOpen] = useState(false);
  const [attachedFiles, setAttachedFiles] = useState<GetProp<typeof Attachments, "items">>([]);

  const [inputValue, setInputValue] = useState("");

  /**
   * 🔔 Please replace the BASE_URL, PATH, MODEL, API_KEY with your own values.
   */

  // ==================== Runtime ====================
  const [agent] = useXAgent<BubbleDataType>({
    baseURL: "http://localhost:9291/api/Stream/chat",
    model: "deepseek-ai/DeepSeek-R1",
    dangerouslyApiKey: "Bearer sk-xxxxxxxxxxxxxxxxxxxx"
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

      if (!originMessage?.content && currentThink) {
        content = `<think>${currentThink}`;
      } else if (originMessage?.content?.includes("<think>") && !originMessage?.content.includes("</think>") && currentContent) {
        content = `${originMessage?.content}</think>${currentContent}`;
      } else {
        content = `${originMessage?.content || ""}${currentThink}${currentContent}`;
      }
      return {
        content: content,
        role: "assistant"
      };
    },
    resolveAbortController: controller => {
      abortController.current = controller;
    }
  });

  // ==================== Event ====================
  const onSubmit = (val: string) => {
    if (!val) return;

    if (loading) {
      message.error("Request is in progress, please wait for the request to complete.");
      return;
    }

    onRequest({
      stream: true,
      message: { role: "user", content: val }
    });
  };

  // ==================== Nodes ====================
  const chatSider = (
    <div className={styles.sider}>
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

  // const rolesAsObject: GetProp<typeof Bubble.List, "roles"> = {
  //   ai: {
  //     placement: "start",
  //     avatar: { icon: <UserOutlined />, style: { background: "#fde3cf" } },
  //     typing: { step: 5, interval: 20 },
  //     style: {
  //       maxWidth: 600
  //     }
  //   },
  //   user: {
  //     placement: "end",
  //     avatar: { icon: <UserOutlined />, style: { background: "#87d068" } }
  //   }
  // };
  const chatList = (
    <div className={styles.chatList}>
      {messages?.length ? (
        /* 🌟 消息列表 */
        <Bubble.List
          // roles={rolesAsObject}
          items={messages?.map((i, index) => {
            const isAI = !!(index % 2);

            return {
              ...i.message,
              key: index,
              role: isAI ? "assistant" : "user",
              classNames: {
                content: i.status === "loading" ? styles.loadingMessage : ""
              },

              typing: i.status === "loading" ? { step: 5, interval: 20, suffix: <>💗</> } : false
            };
          })}
          // items={messages?.map(i => ({
          //   ...i.message,
          //   classNames: {
          //     content: i.status === "loading" ? styles.loadingMessage : ""
          //   },

          //   typing: i.status === "loading" ? { step: 5, interval: 20, suffix: <>💗</> } : false
          // }))}
          style={{ height: "100%", paddingInline: "calc(calc(100% - 700px) /2)" }}
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
              ),
              // loadingRender: () => <Spin size="small" />,
              loadingRender: () => (
                <Space>
                  <Spin size="small" />
                  Custom loading...
                </Space>
              )
            },

            user: { placement: "end", avatar: { icon: <UserOutlined />, style: { background: "#87d068" } } }
          }}
        />
      ) : (
        <Space
          direction="vertical"
          size={16}
          style={{ paddingInline: "calc(calc(100% - 700px) /2)" }}
          className={styles.placeholder}
        >
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
      title="Upload File"
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
            ? { title: "Drop file here" }
            : {
                icon: <CloudUploadOutlined />,
                title: "Upload files",
                description: "Click or drag files to this area to upload"
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
        placeholder="Ask or input / use skills"
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
