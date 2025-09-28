import {
  // AppstoreAddOutlined,
  CommentOutlined,
  // FileSearchOutlined,
  HeartOutlined,
  PaperClipOutlined,
  // ProductOutlined,
  ScheduleOutlined,
  SmileOutlined
} from "@ant-design/icons";
import { Prompts } from "@ant-design/x";
import { type GetProp } from "antd";

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
      icon: <HeartOutlined />,
      label: "Intention",
      description: "AI understands user needs and provides solutions."
    },
    {
      key: "2-2",
      icon: <SmileOutlined />,
      label: "Role",
      description: "AI's public persona and image"
    },
    {
      key: "2-3",
      icon: <CommentOutlined />,
      label: "Chat",
      description: "How AI Can Express Itself in a Way Users Understand"
    },
    {
      key: "2-4",
      icon: <PaperClipOutlined />,
      label: "Interface",
      description: 'AI balances "chat" & "do" behaviors.'
    }
  ]
};

export const SENDER_PROMPTS: GetProp<typeof Prompts, "items"> = [
  {
    key: "1",
    description: "查询供应商",
    icon: <ScheduleOutlined />
    // },
    // {
    //   key: "2",
    //   description: "Components",
    //   icon: <ProductOutlined />
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
