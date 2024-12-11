import { DatabaseOutlined, ProductOutlined } from "@ant-design/icons";
import { Mode } from "./dsl/base";

//显示的依赖定义
export type deps = {
  field: string; //依赖的属性
  value: any[]; //依赖的值 满足其中之一即可
};
export interface designProp {
  name: string; //title
  icon?: any;
  type: "select" | "input" | "switch" | "buttonGroup" | "form" | "textArea" | "comboGrid" | "inputNumber" | "colorPicker"; //设置组件的类型
  tag?: "basic" | "layout"; //所在分组标签
  mode?: Mode[] | Mode; //使用场景
  deps?: deps | deps[]; //字段显示依赖,如果是数组都需要满足
  tooltip?: string; //提示语 label
  comboGridCode?: string; //提示语 label
  items?: {
    //多选时的内容
    icon?: any;
    tooltip?: string; //提示语 label
    label: string;
    default?: boolean; //是否默认值
    value?: any; //嵌套一个，尤它进行值得选择
    mode?: Mode;
    deps?: deps | deps[]; // 满足得一项，或者多项都满足
  }[]; //子项显示依赖
}
export interface SchemaClz {
  [key: string]: designProp; //扩展字段
}
export const formTypes: { title: string; value: string; icon?: any }[] = [
  { title: "数据属性", value: "basic", icon: DatabaseOutlined },
  { title: "布局样式", value: "layout", icon: ProductOutlined }
];
export const listTypes: { title: string; value: string; icon?: any }[] = [
  { title: "数据属性", value: "basic", icon: DatabaseOutlined }
];

export const schemaDef: SchemaClz = {
  FormTitle: {
    name: "标题",
    type: "input",
    mode: Mode.form,
    tag: "basic"
  },
  DataIndex: {
    name: "表栏位字段",
    type: "input",
    tag: "basic"
  },
  DefaultValue: {
    name: "默认值",
    type: "input",
    tag: "basic",
    deps: { field: "FieldType", value: ["Input", "InputNumber", "TextArea"] },
    mode: Mode.form
  },
  HideInForm: {
    name: "隐藏",
    type: "switch",
    tag: "layout",
    mode: Mode.form
  },
  Required: {
    name: "必填",
    type: "switch",
    tag: "basic",
    mode: Mode.form
  },
  Disabled: {
    name: "只读",
    type: "switch",
    tag: "layout",
    mode: Mode.form
  },
  // listHide: {
  //   name: "列表",
  //   type: "switch",
  //   tag: "layout",
  // },
  Validator: {
    name: "限定输入格式",
    type: "select",
    mode: Mode.form,
    tag: "basic",
    deps: { field: "FieldType", value: ["Input", "TextArea"] },
    items: [
      { label: "email", value: "email" },
      { label: "phone", value: "phone" },
      { label: "number", value: "number" },
      { label: "idcard", value: "idcard" },
      { label: "采用正则校验", value: "pattern" }
    ]
  },
  ValidPattern: {
    name: "正则表达式",
    type: "input",
    tag: "basic",
    mode: Mode.form,
    tooltip: "必须是正则表达式",
    deps: { field: "Validator", value: ["pattern"] }
  },
  vlife_message: {
    name: "校验提醒",
    type: "input",
    tag: "basic",
    mode: Mode.form,
    deps: { field: "Validator", value: ["pattern"] }
  },
  IsUnique: {
    name: "不允许重复",
    type: "switch",
    mode: Mode.form,
    tag: "basic",
    deps: { field: "FieldType", value: ["Input"] }
  },
  MaxLength: {
    name: "最大长度",
    type: "input",
    tag: "basic",
    mode: Mode.form,
    deps: { field: "FieldType", value: ["Input", "TextArea"] }
  },
  MinLength: {
    name: "最小长度",
    type: "input",
    tag: "basic",
    mode: Mode.form,
    deps: { field: "FieldType", value: ["Input", "TextArea"] }
  },
  Maximum: {
    name: "最大值",
    type: "inputNumber",
    tag: "basic",
    mode: Mode.form,
    deps: { field: "FieldType", value: ["InputNumber"] }
  },
  Minimum: {
    name: "最小值",
    type: "inputNumber",
    tag: "basic",
    mode: Mode.form,
    deps: { field: "FieldType", value: ["InputNumber"] }
  },
  Placeholder: {
    name: "填写占位符",
    type: "textArea",
    mode: Mode.form,
    tag: "basic",
    deps: { field: "FieldType", value: ["Input", "InputNumber", "TextArea"] }
  },
  CreateHide: {
    //保存时才会触发数据产生，无法实时预览
    name: "新增时隐藏",
    tooltip: "保存后生效",
    type: "switch",
    mode: Mode.form,
    tag: "basic"
  },
  ModifyDisabled: {
    name: "修改时只读",
    tooltip: "保存后生效",
    type: "switch",
    mode: Mode.form,
    tag: "basic"
  },
  // hideLabel: {
  //   name: "标签隐藏",
  //   type: "switch",
  //   tag: "layout",
  // },
  // divider: {
  //   name: "分组名称",
  //   type: "switch",
  //   tag: "layout",
  // },
  // dividerLabel: {
  //   name: "",//分组名称
  //   type: "input",
  //   tag: "layout",
  //   deps: { field: "divider", value: [true] },
  // },
  // x_decorator_props$layout: {
  //   name: "标签位置",
  //   type: "buttonGroup",
  //   tag: "layout",
  //   items: [
  //     {
  //       label: "顶部",
  //       value: "vertical",
  //     },
  //     {
  //       label: "水平",
  //       value: "horizontal",
  //     },
  //   ],
  // },
  // x_decorator_props$labelAlign: {
  //   name: "标签对齐",
  //   type: "buttonGroup",
  //   tag: "layout",
  //   deps: { field: "x_decorator_props$layout", value: ["vertical"] },
  //   items: [
  //     {
  //       label: "居左",
  //       value: "left",
  //     },
  //     {
  //       label: "居右",
  //       value: "right",
  //     },
  //   ],
  // },
  formTabCode: {
    name: "所在页签",
    type: "buttonGroup",
    mode: Mode.form,
    tag: "layout",
    // deps://有数量才显示
    // deps: { field: "x_decorator_props$layout", value: ["vertical"] },
    items: []
  },
  GridSpan: {
    name: "字段占比",
    type: "buttonGroup",
    tag: "layout",
    mode: Mode.form
    // items: [
    //   { value: 25, label: "25" },
    //   { value: 50, label: "50" },
    //   { value: 100, label: "100" }
    // ]
  },
  ComboBoxDataSource: {
    name: "数据来源",
    type: "comboGrid",
    tag: "basic",
    comboGridCode: "SmLov",
    deps: { field: "FieldType", value: ["ComboBox"] },
    mode: Mode.form
  },
  ComboGridDataSource: {
    name: "数据来源",
    type: "comboGrid",
    tag: "basic",
    comboGridCode: "SmCommonListSql",
    deps: { field: "FieldType", value: ["ComboGrid"] },
    mode: Mode.form
  },
  Title: {
    name: "标题",
    type: "input",
    tag: "basic",
    mode: Mode.list
  },
  // ValueType: {
  //   name: "列表数据类型",
  //   type: "buttonGroup",
  //   tag: "layout",
  //   mode: Mode.list,
  //   items: [
  //     { value: 25, label: "25" },
  //     { value: 50, label: "50" },
  //     { value: 100, label: "100" }
  //   ]
  // },
  ValueType: {
    name: "数据类型",
    type: "select",
    tag: "basic",
    mode: Mode.list,
    items: [
      { value: null, label: " " },
      { value: "text", label: "文本框" },
      { value: "date", label: "日期" },
      { value: "dateTime", label: "日期和时间" },
      { value: "dateRange", label: "日期区间" },
      { value: "dateTimeRange", label: "日期和时间区间" },
      { value: "time", label: "时间" },
      { value: "timeRange", label: "时间区间" },
      { value: "digit", label: "数字" },
      { value: "dateWeek", label: "周" },
      { value: "dateMonth", label: "月" },
      { value: "dateQuarter", label: "季度输入" },
      { value: "dateYear", label: "金额" },
      { value: "money", label: "金额" },
      { value: "switch", label: "开关" },
      { value: "color", label: "颜色选择器" },
      { value: "icon", label: "图标" }
    ]
  },
  Width: {
    name: "宽度",
    type: "inputNumber",
    tag: "basic",
    mode: Mode.list,
    deps: { field: "FieldType", value: ["InputNumber"] }
  },
  HideInTable: {
    name: "列表中隐藏",
    // tooltip: "保存后生效",
    type: "switch",
    mode: Mode.list,
    tag: "basic"
  },
  Sorter: {
    name: "是否排序",
    type: "switch",
    mode: Mode.list,
    tag: "basic"
  },
  IsExport: {
    name: "是否导出Excel",
    type: "switch",
    mode: Mode.list,
    tag: "basic"
  },
  IsLovCode: {
    name: "是否参数",
    type: "switch",
    mode: Mode.list,
    tag: "basic"
  },
  IsBool: {
    name: "是否bool",
    type: "switch",
    mode: Mode.list,
    tag: "basic"
  },
  HideInSearch: {
    name: "查询中隐藏",
    type: "switch",
    mode: Mode.list,
    tag: "basic"
  },
  Align: {
    name: "对齐方式",
    type: "inputNumber",
    tag: "basic",
    mode: Mode.list,
    deps: { field: "FieldType", value: ["InputNumber"] }
  },
  IsSum: {
    name: "是否合计",
    type: "switch",
    mode: Mode.list,
    tag: "basic"
  },
  ColumnMode: {
    name: "栏位模式",
    type: "select",
    tag: "basic",
    items: [
      { value: null, label: "通用" },
      { value: Mode.list, label: "列表" },
      { value: Mode.form, label: "表单" }
    ]
  },
  IsThemeColor: {
    name: "跟随主题颜色",
    tooltip: "跟随主题色自动切换",
    type: "switch",
    mode: Mode.list,
    tag: "basic",
    deps: { field: "IsLovCode", value: [false] }
  },
  Color: {
    name: "文字颜色",
    type: "colorPicker",
    tag: "basic",
    deps: [
      { field: "IsLovCode", value: [false] },
      { field: "IsThemeColor", value: [false] }
    ]
  },
  IsCopy: {
    name: "支持复制",
    type: "switch",
    mode: Mode.list,
    tag: "basic"
  },
  IsTooltip: {
    name: "显示提示",
    type: "switch",
    tag: "basic"
  },
  TooltipContent: {
    name: "提示内容",
    type: "input",
    tag: "basic",
    deps: [{ field: "IsTooltip", value: [true] }]
  },
  Remark: {
    name: "备注",
    type: "input",
    tag: "basic"
  }
};
export default schemaDef;
