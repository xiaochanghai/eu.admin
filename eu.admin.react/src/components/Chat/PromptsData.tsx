import { Prompts } from "@ant-design/x";
import { type GetProp } from "antd";
import locale from './_utils/local';
import type { DefaultMessageInfo } from '@ant-design/x-sdk';

import {
  XModelMessage,
} from '@ant-design/x-sdk';
import type { ActionsFeedbackProps } from '@ant-design/x';

import { Icon } from "@/components";

export const DEFAULT_CONVERSATIONS_ITEMS = [
  {
    key: "default-0",
    label: "What is Ant Design X?",
    group: "Today"
  },
  {
    key: "default-1",
    label: "How to quickly install and import components?",
    group: "Today"
  },
  {
    key: "default-2",
    label: "New AGI Hybrid Interface",
    group: "Yesterday"
  }
];

export const HOT_TOPICS = {
  key: "1",
  // label: "Hot Topics",
  label: "热门",
  children: [
    {
      key: "1-1",
      description: "如何修改供应商？",
      icon: <span style={{ color: "#f93a4a", fontWeight: 700 }}>1</span>
    },
    {
      key: "1-2",
      description: "新的 AGI 混合接口",
      icon: <span style={{ color: "#ff6565", fontWeight: 700 }}>2</span>
    },
    {
      key: "1-3",
      description: "查询供应商列表",
      icon: <span style={{ color: "#ff8f1f", fontWeight: 700 }}>3</span>
    },
    {
      key: "1-4",
      description: "来发现 AI 时代的新设计范式。",
      icon: <span style={{ color: "#00000040", fontWeight: 700 }}>4</span>
    },
    {
      key: "1-5",
      description: "如何快速学习AI功能？",
      icon: <span style={{ color: "#00000040", fontWeight: 700 }}>5</span>
    }
  ]
};

export const DESIGN_GUIDE = {
  key: "2",
  label: "使用指南",
  children: [
    {
      key: "2-1",
      icon: <Icon name="HeartOutlined" />,
      label: "Intention",
      description: "AI understands user needs and provides solutions."
    },
    {
      key: "2-2",
      icon: <Icon name="SmileOutlined" />,
      label: "Role",
      description: "AI's public persona and image"
    },
    {
      key: "2-3",
      icon: <Icon name="CommentOutlined" />,
      label: "Chat",
      description: "How AI Can Express Itself in a Way Users Understand"
    },
    {
      key: "2-4",
      icon: <Icon name="PaperClipOutlined" />,
      label: "Interface",
      description: 'AI balances "chat" & "do" behaviors.'
    }
  ]
};

export const SENDER_PROMPTS: GetProp<typeof Prompts, "items"> = [
  {
    key: "1",
    description: "查询供应商",
    icon: <Icon name="ScheduleOutlined" />
  },
  {
    key: "2",
    description: "获取供应商模板",
    icon: <Icon name="ProductOutlined" />
    // },
    // {
    //   key: "3",
    //   description: "RICH Guide",
    //   icon: <FileSearchOutlined />
    // },
    // {
    //   key: "4",
    //   description: "Installation Introduction",
    //   icon: <AppstoreAddOutlined />
  }
];

export const THOUGHT_CHAIN_CONFIG = {
  loading: {
    title: locale.modelIsRunning,
    status: 'loading',
  },
  updating: {
    title: locale.modelIsRunning,
    status: 'loading',
  },
  success: {
    title: locale.modelExecutionCompleted,
    status: 'success',
  },
  error: {
    title: locale.executionFailed,
    status: 'error',
  },
  abort: {
    title: locale.aborted,
    status: 'abort',
  },
};
export interface ChatMessage extends XModelMessage {
  extraInfo?: {
    feedback: ActionsFeedbackProps['value'];
  };
}

export const HISTORY_MESSAGES: {
  [key: string]: DefaultMessageInfo<ChatMessage>[];
} = {
  'default-1': [
    {
      message: { role: 'user', content: locale.howToQuicklyInstallAndImportComponents },
      status: 'success',
    },
    {
      message: {
        role: 'assistant',
        content: locale.aiMessage_2,
      },
      status: 'success',
    },
  ],
  'default-2': [
    { message: { role: 'user', content: locale.newAgiHybridInterface }, status: 'success' },
    {
      message: {
        role: 'assistant',
        content: locale.aiMessage_1,
      },
      status: 'success',
    },
  ],
};