import { MobileEditField } from "../types";

/** 页面级组件属性编辑定义 */
const pageEditFields: Record<string, MobileEditField[]> = {
  searchBar: [
    { key: "placeholder", name: "占位文本", type: "Text" },
    { key: "fields", name: "搜索字段（逗号分隔）", type: "Text", placeholder: "Field1,Field2" }
  ],
  tabs: [
    { key: "field", name: "绑定字段", type: "Text" },
    { key: "items", name: "选项", type: "OptionEditor" }
  ],
  statRow: [
    { key: "items", name: "统计项", type: "OptionEditor" }
  ],
  list: [
    { key: "keyField", name: "主键字段", type: "Text" },
    { key: "template", name: "卡片模板", type: "Select", options: [
      { label: "自定义卡片", value: "customCard" },
      { label: "简单列表", value: "simpleList" }
    ]}
  ],
  emptyState: [
    { key: "text", name: "提示文案", type: "Text" },
    { key: "icon", name: "图标", type: "Text" }
  ],
  floatingAction: [
    { key: "icon", name: "图标", type: "Text" },
    { key: "label", name: "按钮文案", type: "Text" },
    { key: "action.type", name: "动作类型", type: "Select", options: [
      { label: "跳转", value: "navigate" },
      { label: "弹窗", value: "modal" }
    ]}
  ]
};

export default pageEditFields;
