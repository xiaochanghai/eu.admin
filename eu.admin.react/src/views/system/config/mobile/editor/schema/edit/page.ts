import { MobileEditField } from "../types";

/** 页面级组件属性编辑定义 */
const pageEditFields: Record<string, MobileEditField[]> = {
  page: [
    { key: "pageCode", name: "页面编码", type: "Text", placeholder: "EQUIPMENT_LIST" },
    { key: "title", name: "页面标题", type: "Text", placeholder: "设备" },
    { key: "pageType", name: "页面类型", type: "Select", options: [
      { label: "列表页", value: "list" },
      { label: "表单页", value: "form" }
    ]},
    { key: "dataSource.type", name: "数据源类型", type: "Select", options: [
      { label: "模块", value: "module" },
      { label: "接口", value: "api" }
    ]},
    { key: "dataSource.moduleCode", name: "模块编码", type: "Text", placeholder: "EM_EQUIPMENT_INFO_MNG" },
    { key: "dataSource.api", name: "查询接口", type: "Text", placeholder: "/api/xxx/Query" },
    { key: "dataSource.pageSize", name: "每页数量", type: "Number", min: 1, max: 100 },
    { key: "backgroundColor", name: "页面背景色", type: "Color", placeholder: "#f9fafb" },
    { key: "paddingHorizontal", name: "页面左右边距", type: "Number", min: 0, max: 40 },
    { key: "paddingTop", name: "页面顶部边距", type: "Number", min: 0, max: 40 },
    { key: "paddingBottom", name: "页面底部边距", type: "Number", min: 0, max: 80 },
    { key: "permission", name: "权限标识", type: "Text", placeholder: "mobile:equipment:list" },
    { key: "statusMap", name: "状态映射 JSON", type: "Json", placeholder: "{\n  \"running\": { \"label\": \"运行中\", \"tone\": \"success\" }\n}" }
  ],
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
    ]},
    { key: "componentPath", name: "列表组件路径", type: "Text", placeholder: "src/components/refresh-list-view.tsx" },
    { key: "marginHorizontal", name: "卡片左右外距", type: "Number", min: 0, max: 40 },
    { key: "marginBottom", name: "卡片底部外距", type: "Number", min: 0, max: 32 },
    { key: "padding", name: "卡片内边距", type: "Number", min: 8, max: 32 },
    { key: "cardRadius", name: "卡片圆角", type: "Number", min: 0, max: 24 }
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
