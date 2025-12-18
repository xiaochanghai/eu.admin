import React, { useState } from "react";
import type { BubbleListProps, ThoughtChainItemProps } from "@ant-design/x";
import { Icon } from "@/components";
import { Attachments, Bubble, Conversations, Prompts, Sender, ThoughtChain } from "@ant-design/x";
import XMarkdown from "@ant-design/x-markdown";
import type { DefaultMessageInfo } from "@ant-design/x-sdk";
import {
  DeepSeekChatProvider,
  SSEFields,
  useXChat,
  useXConversations,
  XModelParams,
  XModelResponse,
  XRequest
} from "@ant-design/x-sdk";
import { Button, Flex, type GetProp, message } from "antd";
import dayjs from "dayjs";
import "@ant-design/x-markdown/themes/light.css";
import "@ant-design/x-markdown/themes/dark.css";
import { useMarkdownTheme } from "./x-markdown/_utils";
import {
  Logo,
  AvatarIcon,
  Welcome,
  DEFAULT_CONVERSATIONS_ITEMS,
  HOT_TOPICS,
  DESIGN_GUIDE,
  SENDER_PROMPTS,
  THOUGHT_CHAIN_CONFIG,
  ChatMessage,
  HISTORY_MESSAGES
} from "./index";
import { useTranslation } from "react-i18next";
import RouterGuard from "@/routers/helper/RouterGuard";
import { store } from "@/redux";
import { useStyle } from "./Styles";
import { getCache, setCache, createUuid } from "@/utils";
let baseURL = import.meta.env.VITE_API_URL as string;

import { ThinkComponent } from "./ThinkComponent";
import { ChatContext } from "./ChatContext";
// import { Footer } from "./Footer";

// ==================== Chat Provider ====================
/**
 * 🔔 Please replace the BASE_URL, MODEL with your own values.
 */
const providerCaches = new Map<string, DeepSeekChatProvider>();
const providerFactory = (conversationKey: string, _baseURL: string) => {
  if (!providerCaches.get(conversationKey)) {
    // "https://api.x.ant.design/api/big_model_glm-4.5-flash"
    providerCaches.set(
      conversationKey,
      new DeepSeekChatProvider({
        request: XRequest<XModelParams, Partial<Record<SSEFields, XModelResponse>>>(
          "https://api.x.ant.design/api/big_model_glm-4.5-flash",
          {
            manual: true,
            headers: { authorization: `Bearer ${store.getState().user.token}` },
            params: {
              stream: true,
              thinking: {
                type: "disabled"
              },
              model: "glm-4.5-flash"
            },
            callbacks: {
              onSuccess: messages => {
                // setStatus('success');
                console.log("onSuccess", messages);
              },
              onError: error => {
                // setStatus('error');
                console.error("onError", error);
              },
              onUpdate: msg => {
                // setLines((pre) => [...pre, msg]);
                console.log("onUpdate", msg);
              }
            }
          }
        )
      })
    );
  }
  return providerCaches.get(conversationKey);
};

const historyMessageFactory = (conversationKey: string): DefaultMessageInfo<ChatMessage>[] => {
  return HISTORY_MESSAGES[conversationKey] || [];
};

const getRole = (className: string): BubbleListProps["role"] => ({
  assistant: {
    placement: "start",
    header: (_, { status }) => {
      const config = THOUGHT_CHAIN_CONFIG[status as keyof typeof THOUGHT_CHAIN_CONFIG];
      return config ? (
        <ThoughtChain.Item
          style={{
            marginBottom: 8
          }}
          status={config.status as ThoughtChainItemProps["status"]}
          variant="solid"
          icon={<Icon name=" GlobalOutlined" />}
          title={config.title}
        />
      ) : null;
    },
    // footer: (content, { status, key, extraInfo }) => (
    //   <Footer content={content} status={status} extraInfo={extraInfo as ChatMessage["extraInfo"]} id={key as string} />
    // ),
    contentRender: (content: any, { status }) => {
      const newContent = content.replace("/\n\n/g", "<br/><br/>");
      console.log("content:" + content);
      // debugger;
      return (
        <XMarkdown
          paragraphTag="div"
          components={{
            think: ThinkComponent
          }}
          className={className}
          streaming={{
            hasNextChunk: status === "updating",
            enableAnimation: true
          }}
        >
          {newContent}
        </XMarkdown>
      );
    }
  },
  user: { placement: "end" }
});

export const ChatMain: React.FC = () => {
  const { styles } = useStyle();
  // ==================== State ====================

  const { conversations, activeConversationKey, setActiveConversationKey, addConversation, setConversations } = useXConversations(
    {
      defaultConversations: DEFAULT_CONVERSATIONS_ITEMS,
      defaultActiveConversationKey: DEFAULT_CONVERSATIONS_ITEMS[0].key
    }
  );

  const [className] = useMarkdownTheme();
  const [messageApi, contextHolder] = message.useMessage();
  const [attachmentsOpen, setAttachmentsOpen] = useState(false);
  const [attachedFiles, setAttachedFiles] = useState<GetProp<typeof Attachments, "items">>([]);

  const [inputValue, setInputValue] = useState("");
  const { t } = useTranslation();
  const chatId =
    getCache("chatId") ||
    (() => {
      const newId = createUuid();
      setCache("chatId", newId);
      return newId;
    })();
  // ==================== Runtime ====================

  /**
   * 🔔 Please replace the BASE_URL, PATH, MODEL, API_KEY with your own values.
   */
  let baseURL1 = `${baseURL == "/" ? "" : baseURL}/api/Stream/chat/${chatId}`;
  console.log("baseURL:" + baseURL1);
  const { onRequest, messages, isRequesting, abort, onReload, setMessage } = useXChat<ChatMessage>({
    provider: providerFactory(activeConversationKey, baseURL1), // every conversation has its own provider
    conversationKey: activeConversationKey,
    defaultMessages: historyMessageFactory(activeConversationKey),
    requestPlaceholder: () => {
      return {
        content: t("chat.noData"),
        role: "assistant"
      };
    },
    requestFallback: (_, { messageInfo }) => {
      return {
        ...messageInfo?.message,
        content: messageInfo?.message?.content || t("chat.requestFailedPleaseTryAgain")
      };
    },
    parser: info => {
      const { chunk } = info || {};
      let currentContent = "";
      // debugger;
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

      return {
        content: currentContent,
        // contents: [{ key: chunk?.id, content: content }],
        role: "assistant"
      };
    }
  });

  // ==================== Event ====================
  const onSubmit = (val: string) => {
    if (!val) return;
    onRequest({
      messages: [{ role: "user", content: val }]
    });
    setActiveConversationKey(activeConversationKey);
  };

  // ==================== Nodes ====================
  const chatSide = (
    <div className={styles.side} style={{ display: "none" }}>
      <Logo />
      <Conversations
        creation={{
          onClick: () => {
            if (messages.length === 0) {
              messageApi.error(t("chat.itIsNowANewConversation"));
              return;
            }
            const now = dayjs().valueOf().toString();
            addConversation({
              key: now,
              label: `${t("chat.newConversation")} ${conversations.length + 1}`,
              group: t("chat.today")
            });
            setActiveConversationKey(now);
          }
        }}
        items={conversations.map(({ key, label, ...other }) => ({
          key,
          label: key === activeConversationKey ? `[${t("chat.curConversation")}]${label}` : label,
          ...other
        }))}
        className={styles.conversations}
        activeKey={activeConversationKey}
        onActiveChange={setActiveConversationKey}
        groupable
        styles={{ item: { padding: "0 8px" } }}
        menu={conversation => ({
          items: [
            {
              label: t("chat.rename"),
              key: "rename",
              icon: <Icon name="EditOutlined" />
            },
            {
              label: t("chat.delete"),
              key: "delete",
              icon: <Icon name="DeleteOutlined" />,
              danger: true,
              onClick: () => {
                const newList = conversations.filter(item => item.key !== conversation.key);
                const newKey = newList?.[0]?.key;
                setConversations(newList);
                if (conversation.key === activeConversationKey) {
                  setActiveConversationKey(newKey);
                }
              }
            }
          ]
        })}
      />

      <div className={styles.sideFooter}>
        <AvatarIcon height={24} />
        <Button type="text" icon={<Icon name="QuestionCircleOutlined" />} />
      </div>
    </div>
  );

  const chatList = (
    <div className={styles.chatList}>
      {messages?.length ? (
        /* 🌟 消息列表 */
        <Bubble.List
          items={messages?.map(i => ({
            ...i.message,
            key: i.id,
            status: i.status,
            loading: i.status === "loading",
            extraInfo: i.extraInfo
          }))}
          styles={{
            bubble: {
              maxWidth: 1000
            }
          }}
          role={getRole(className)}
        />
      ) : (
        <Flex
          vertical
          style={{
            maxWidth: 1000
          }}
          gap={16}
          align="center"
          className={styles.placeholder}
        >
          <Welcome />
          <Flex
            gap={16}
            justify="center"
            style={{
              width: "100%"
            }}
          >
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
        </Flex>
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
                icon: <Icon name="CloudUploadOutlined" />,
                title: t("chat.senderHeaderAttachmentTitle"),
                description: t("chat.senderHeaderAttachmentDesc")
              }
        }
      />
    </Sender.Header>
  );

  const chatSender = (
    <Flex
      vertical
      gap={12}
      align="center"
      style={{
        marginInline: 24
      }}
    >
      {/* 🌟 提示词 */}
      {!attachmentsOpen && (
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
      )}
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
          abort();
        }}
        prefix={
          <Button
            type="text"
            icon={<Icon name="PaperClipOutlined" style={{ fontSize: 18 }} />}
            onClick={() => setAttachmentsOpen(!attachmentsOpen)}
          />
        }
        loading={isRequesting}
        className={styles.sender}
        allowSpeech
        placeholder={t("chat.senderPlaceholder")}
      />
    </Flex>
  );

  // ==================== Render =================

  return (
    <RouterGuard>
      <ChatContext.Provider value={{ onReload, setMessage }}>
        {contextHolder}
        <div className={styles.layout}>
          {chatSide}
          <div className={styles.chat}>
            {chatList}
            {chatSender}
          </div>
        </div>
      </ChatContext.Provider>
    </RouterGuard>
  );
};
