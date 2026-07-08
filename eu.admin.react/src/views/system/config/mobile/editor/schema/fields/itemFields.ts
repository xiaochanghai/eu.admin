import { MobileFieldNode } from "../types";

/** Item 字段组件定义 */
const itemFieldComponents: MobileFieldNode[] = [
  {
    type: "text",
    displayName: "文本",
    props: {
      bind: "",
      role: "title",
      maxLines: 1,
      prefix: "",
      suffix: "",
      emptyText: "-"
    }
  },
  {
    type: "image",
    displayName: "图片",
    props: {
      bind: "",
      size: 48,
      radius: 8,
      placeholder: ""
    }
  },
  {
    type: "statusTag",
    displayName: "状态标签",
    props: {
      bind: "",
      map: "",
      fallback: { label: "未知", tone: "neutral" }
    }
  },
  {
    type: "metric",
    displayName: "指标",
    props: {
      bind: "",
      label: "",
      suffix: "",
      emptyText: "-"
    }
  },
  {
    type: "iconText",
    displayName: "图标文本",
    props: {
      bind: "",
      icon: "info",
      emptyText: "-",
      maxLines: 1
    }
  },
  {
    type: "divider",
    displayName: "分割线",
    props: {
      margin: 8
    }
  },
  {
    type: "spacer",
    displayName: "间距",
    props: {
      height: 8
    }
  },
  {
    type: "actionButton",
    displayName: "操作按钮",
    props: {
      text: "操作",
      action: { type: "navigate", path: "" },
      style: "link"
    }
  },
  {
    type: "row",
    displayName: "横向布局",
    props: {
      gap: 8,
      align: "center",
      justify: "start"
    }
  },
  {
    type: "column",
    displayName: "纵向布局",
    props: {
      gap: 4,
      flex: 1
    }
  }
];

export default itemFieldComponents;
