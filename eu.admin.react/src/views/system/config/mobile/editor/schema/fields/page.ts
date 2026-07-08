import { MobileFieldNode } from "../types";

/** 页面级组件定义 */
const pageComponents: MobileFieldNode[] = [
  {
    type: "searchBar",
    displayName: "搜索框",
    props: {
      placeholder: "搜索名称、编号",
      fields: []
    }
  },
  {
    type: "tabs",
    displayName: "筛选标签",
    props: {
      field: "Status",
      items: [
        { label: "全部", value: "" },
        { label: "启用", value: "enabled", tone: "success" },
        { label: "禁用", value: "disabled", tone: "danger" }
      ]
    }
  },
  {
    type: "statRow",
    displayName: "统计条",
    props: {
      items: [
        { label: "总数", bind: "Total", suffix: "条" },
        { label: "异常", bind: "ErrorCount", suffix: "条" }
      ]
    }
  },
  {
    type: "list",
    displayName: "列表",
    props: {
      keyField: "ID",
      template: "customCard",
      onPress: { type: "navigate", path: "" }
    }
  },
  {
    type: "emptyState",
    displayName: "空状态",
    props: {
      text: "暂无数据",
      icon: "inbox"
    }
  },
  {
    type: "floatingAction",
    displayName: "悬浮按钮",
    props: {
      icon: "plus",
      label: "新增",
      action: { type: "navigate", path: "" }
    }
  }
];

export default pageComponents;
