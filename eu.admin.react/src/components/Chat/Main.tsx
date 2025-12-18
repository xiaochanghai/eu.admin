import React, { useState, useCallback, useRef } from "react";
import type { BubbleListProps, ThoughtChainItemProps } from "@ant-design/x";
import { Icon } from "@/components";
import { Attachments, Bubble, Conversations, Prompts, Sender, ThoughtChain } from "@ant-design/x";
import XMarkdown from "@ant-design/x-markdown";
import type { DefaultMessageInfo } from "@ant-design/x-sdk";
import { AbstractChatProvider, useXChat, useXConversations, XModelParams, XRequest, XRequestOptions } from "@ant-design/x-sdk";
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
import ChatContent from "./Content";
import { uploadFile } from "@/api/modules/module";
import type { UploadFile } from "antd/es/upload/interface";
// import { Footer } from "./Footer";

// ==================== Custom Provider ====================
type TransformMessage<ChatMessage, Output> = {
  originMessage?: ChatMessage;
  chunk: Output;
  chunks: Output[];
  status: "local" | "loading" | "updating" | "success" | "error" | "abort";
};

class CustomChatProvider<
  ChatMessage extends { role: string; content: string | { text: string; type: string } } = {
    role: string;
    content: string | { text: string; type: string };
  },
  Input extends XModelParams = XModelParams,
  Output = any
> extends AbstractChatProvider<ChatMessage, Input, Output> {
  transformParams(requestParams: Partial<Input>, options: XRequestOptions<Input, Output>): Input {
    return {
      ...(options?.params || {}),
      ...(requestParams || {})
    } as Input;
  }

  transformLocalMessage(requestParams: Partial<Input>): ChatMessage {
    const messages = requestParams.messages || [];
    const lastMessage = messages[messages.length - 1];
    return {
      content: lastMessage?.content || "",
      role: "user"
    } as ChatMessage;
  }

  transformMessage(info: TransformMessage<ChatMessage, Output>): ChatMessage {
    const { originMessage, chunk, status } = info || {};

    console.log("transformMessage called:", { chunk, status });

    // 获取原始 content
    const getContent = (msg?: ChatMessage): string => {
      if (!msg) return "";
      if (typeof msg.content === "string") return msg.content;
      if (typeof msg.content === "object" && "text" in msg.content) return msg.content.text;
      return "";
    };

    // 如果没有 chunk，返回原始消息
    if (!chunk) {
      return {
        content: getContent(originMessage),
        role: "assistant"
      } as ChatMessage;
    }

    try {
      // 解析 SSE 数据
      const event = (chunk as any)?.event;
      const data = (chunk as any)?.data;

      console.log("transformMessage SSE data:", { event, data });

      if ((event === "message" || event === " message") && data) {
        const trimmedData = data.trim();

        // 跳过 [DONE] 标记
        if (trimmedData.includes("[DONE]")) {
          console.log("Received [DONE], keeping originMessage");
          return {
            content: getContent(originMessage),
            role: "assistant"
          } as ChatMessage;
        }

        // 解析数据
        const parsed = JSON.parse(trimmedData);
        const newContent = parsed?.content || "";

        console.log("Parsed content:", newContent);

        // 累积内容（如果是流式传输，需要累积）
        const accumulatedContent = getContent(originMessage);
        const finalContent = status === "updating" ? accumulatedContent + newContent : newContent;

        return {
          content: finalContent,
          role: "assistant"
        } as ChatMessage;
      }
    } catch (error) {
      console.error("transformMessage error:", error);
    }

    // 默认返回原始消息
    return {
      content: getContent(originMessage),
      role: "assistant"
    } as ChatMessage;
  }
}

// ==================== Chat Provider ====================
/**
 * 🔔 Please replace the BASE_URL, MODEL with your own values.
 */
const providerCaches = new Map<string, CustomChatProvider>();

const providerFactory = (conversationKey: string, _baseURL: string) => {
  if (!providerCaches.get(conversationKey)) {
    providerCaches.set(
      conversationKey,
      new CustomChatProvider({
        request: XRequest<XModelParams, any>(_baseURL, {
          manual: true,
          headers: { authorization: `Bearer ${store.getState().user.token}` },
          params: {
            stream: true,
            thinking: {
              type: "disabled"
            },
            model: "glm-4.5-flash"
          }
        })
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
      console.log("content:" + content);
      if (!content) return null;

      // 检查是否是 JSON 格式
      let isJSON = false;
      let jsonContent = null;
      try {
        jsonContent = JSON.parse(content);
        isJSON = true;
      } catch (e) {
        // 不是 JSON，继续使用 markdown 渲染
      }

      // 如果是 JSON 格式，直接显示
      if (isJSON && jsonContent) {
        return <ChatContent content={content} />;
      }

      // 否则使用 markdown 渲染
      const newContent = content.replace("/\n\n/g", "<br/><br/>");
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
  const [fileId, setFileId] = useState<string>("");

  // 添加：监听消息变化，确保用户消息包含文件信息
  const lastSubmittedFiles = useRef<UploadFile[]>([]);
  const lastSubmittedFileId = useRef<string>("");

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
  const { onRequest, messages, isRequesting, abort, onReload, setMessage, setMessages } = useXChat<ChatMessage>({
    provider: providerFactory(activeConversationKey, baseURL1),
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
    }
  });
  console.log(messages);
  // ==================== Event ====================
  const onSubmit = (val: string) => {
    if (!val) return;
    onRequest({
      messages: [{ role: "user", content: val, fileId }]
    });
    setActiveConversationKey(activeConversationKey);
  };

  /**
   * 上传附件
   * @param action 表格操作对象
   */
  const uploadFileAttachment = useCallback(
    async (files: UploadFile[]) => {
      if (files.length == 0) return;
      // 检查必要条件
      // if (!file || !MasterId) return false;

      // // 防止重复上传
      // if (!uploadFlag) return false;

      // uploadFlag = false;

      try {
        // 准备表单数据
        const formData = new FormData();
        formData.append("file", files[0].originFileObj as any);
        // formData.append("masterId", MasterId);
        formData.append("filePath", "Chat");
        // formData.append("imageType", imageType ?? filePath);
        // formData.append("isUnique", String(isUnique));
        // 显示上传中提示
        message.loading("附件上传中..", 0);
        // 上传文件
        const { Success, Data } = await uploadFile("/api/Stream/Upload", formData);
        // 关闭加载提示
        message.destroy();
        if (Success) {
          let { FileId, IsTemplate, ImportDataId, TemplateId, Message } = Data;
          if (IsTemplate) {
            if (Message) message.error(Message);

            setMessages(prev => [
              ...prev,
              {
                message: { role: "user", content: "上传文件", files: files, fileId: FileId }
              } as any
            ]);
            setMessages(prev => [
              ...prev,
              {
                id: createUuid(),
                message: {
                  role: "assistant",
                  content: `{ "type": "ExcelResult",  "ImportDataId": "${ImportDataId}",  "TemplateId": "${TemplateId}" }`
                },
                status: "success"
              } as any
            ]);
            setAttachmentsOpen(false);
          } else {
            setFileId(FileId);
            setAttachedFiles(files);
            lastSubmittedFiles.current = files;
            lastSubmittedFileId.current = FileId;

            message.success("附件上传成功！");
          }
        } else {
        }
      } catch (error) {
        console.error("上传附件失败:", error);
        message.error("上传附件失败");
      } finally {
        // uploadFlag = true; // 重置上传标志
      }
    },
    [attachedFiles]
  );
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
        onChange={info => uploadFileAttachment(info.fileList)}
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
