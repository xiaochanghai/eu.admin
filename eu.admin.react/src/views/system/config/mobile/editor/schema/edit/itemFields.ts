import { MobileEditField } from "../types";

/** Item 字段组件属性编辑定义 */
const itemFieldEditFields: Record<string, MobileEditField[]> = {
  text: [
    { key: "bind", name: "绑定字段", type: "Text" },
    { key: "role", name: "角色", type: "Select", options: [
      { label: "标题", value: "title" },
      { label: "副标题", value: "subtitle" },
      { label: "描述", value: "description" }
    ]},
    { key: "prefix", name: "前缀", type: "Text" },
    { key: "suffix", name: "后缀", type: "Text" },
    { key: "emptyText", name: "空值显示", type: "Text" },
    { key: "maxLines", name: "最大行数", type: "Number", min: 1, max: 5 }
  ],
  image: [
    { key: "bind", name: "绑定字段", type: "Text" },
    { key: "size", name: "尺寸", type: "Select", options: [
      { label: "40", value: 40 },
      { label: "48", value: 48 },
      { label: "56", value: 56 },
      { label: "64", value: 64 }
    ]},
    { key: "radius", name: "圆角", type: "Number", min: 0, max: 32 },
    { key: "placeholder", name: "占位图", type: "Text" }
  ],
  statusTag: [
    { key: "bind", name: "绑定字段", type: "Text" },
    { key: "map", name: "状态映射名", type: "Text" }
  ],
  metric: [
    { key: "bind", name: "绑定字段", type: "Text" },
    { key: "label", name: "标签", type: "Text" },
    { key: "suffix", name: "后缀", type: "Text" },
    { key: "emptyText", name: "空值显示", type: "Text" }
  ],
  iconText: [
    { key: "bind", name: "绑定字段", type: "Text" },
    { key: "icon", name: "图标", type: "Text" },
    { key: "emptyText", name: "空值显示", type: "Text" },
    { key: "maxLines", name: "最大行数", type: "Number", min: 1, max: 5 }
  ],
  divider: [
    { key: "margin", name: "间距", type: "Number", min: 0, max: 32 }
  ],
  spacer: [
    { key: "height", name: "高度", type: "Number", min: 0, max: 64 }
  ],
  actionButton: [
    { key: "text", name: "按钮文案", type: "Text" },
    { key: "style", name: "样式", type: "Select", options: [
      { label: "链接", value: "link" },
      { label: "主要", value: "primary" },
      { label: "默认", value: "default" }
    ]}
  ],
  row: [
    { key: "gap", name: "间距", type: "Number", min: 0, max: 32 },
    { key: "align", name: "对齐", type: "Select", options: [
      { label: "起始", value: "start" },
      { label: "居中", value: "center" },
      { label: "末尾", value: "end" },
      { label: "两端", value: "space-between" }
    ]},
    { key: "justify", name: "排列", type: "Select", options: [
      { label: "起始", value: "start" },
      { label: "居中", value: "center" },
      { label: "末尾", value: "end" },
      { label: "两端", value: "space-between" }
    ]}
  ],
  column: [
    { key: "gap", name: "间距", type: "Number", min: 0, max: 32 },
    { key: "flex", name: "弹性比", type: "Number", min: 0, max: 10 }
  ]
};

export default itemFieldEditFields;
