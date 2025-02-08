/**
 * 表单组件资产配置定义
 */
import { Switch, ComboGrid, ComBoBox, InputNumber, Input } from "@/components";

export const FormComponents = [
  {
    key: 1,
    fieldType: "Input",
    component: Input,
    icon: "Input",
    label: "单行文字",
    props: {
      // className: "  !bg-white focus:!border-red-400",
    }
  },
  {
    key: 2,
    component: InputNumber,
    icon: "InputNumber",
    fieldType: "InputNumber",
    label: "数字输入框"
  },
  {
    key: 3,
    component: ComBoBox,
    icon: "ComBoBox",
    fieldType: "ComboBox",
    label: "下拉选择(字典)"
  },
  {
    key: 4,
    component: ComboGrid,
    icon: "ComBoBox",
    fieldType: "ComboGrid",
    label: "下拉选择(接口)"
  },
  {
    key: 5,
    icon: "Switch",
    component: Switch,
    fieldType: "Switch",
    label: "开关"
  },
  {
    key: 6,
    icon: "TextArea",
    fieldType: "TextArea",
    label: "多行文字"
  },
  {
    key: 7,
    icon: "DatePicker",
    fieldType: "DatePicker",
    label: "日期选择器"
  },
  {
    key: 8,
    icon: "TimePicker",
    fieldType: "TimePicker",
    label: "时间选择器"
  },
  {
    key: 9,
    icon: "DateTimePicker",
    fieldType: "DateTimePicker",
    label: "日期时间选择器"
  }
  // VfCheckbox: {
  //   component: VfCheckbox,
  //   icon: "IconFont",
  //   label: "Boolean选择",
  //   dataType: DataType.basic,
  //   dataModel: DataModel.boolean,
  // },
  // CheckboxGroup: {
  //   component: CheckboxGroup,
  //   icon: "IconFont",
  //   label: " Checkbox复选框组",
  //   dataType: DataType.array,
  //   dataModel: DataModel.string,
  //   props: {
  //     options: {
  //       label: "复选框数据",
  //       required: true,
  //       dataType: DataType.array,
  //       dataModel: "ISelect",
  //     },
  //     direction: {
  //       label: "排列方向",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.string,
  //       // options: { apiInfoKey: "dictOpenApi", match: "ISelect_TYPE" },
  //       options: [
  //         { label: "横向", value: "horizontal" },
  //         { label: "纵向", value: "vertical" },
  //       ],
  //     },
  //   },
  // },

  // },
  // DatePicker: {
  //   component: DatePicker,
  //   icon: "IconClock",
  //   label: "日期",
  //   dataModel: DataModel.date,
  // },
  // VfSelect_DICT: {
  //   component: Select,
  //   icon: "IconDescend2",
  //   label: "下拉选择(字典)",
  //   dataType: DataType.basic,
  //   dataModel: DataModel.string,
  //   props: {
  //     optionList: {
  //       label: "字典数据",
  //       apiMatch: { apiInfoKey: "dictOpenApi", match: "ISelect_ITEMS" },
  //       dataType: DataType.array,
  //       dataModel: "ISelect",
  //     },
  //     showClear: true,
  //     filter: true, //也支持已传入函数
  //     outerBottomSlot: <DictInput />,
  //   },
  // },
  // VfSelect: {
  //   component: Select,
  //   icon: "IconDescend2",
  //   label: "下拉选择(接口)",
  //   dataType: DataType.basic,
  //   dataModel: DataModel.string,
  //   props: {
  //     showClear: true,
  //     filter: true,
  //     emptyContent: "请选择",
  //     zIndex: 1000,
  //     optionList: {
  //       label: "选项数据",
  //       dataType: DataType.array,
  //       dataModel: "ISelect",
  //     },
  //     quickCreate: {
  //       label: "是否支持添加选项",
  //       remark: "选项是在CompData.tsx里配置的添加选项的数据保存接口",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.string,
  //       options: [
  //         { label: "创建视图", value: "/reportCondition/save" },
  //         // { label: "dem0客户创建", value: "/demoCustomer/save" },
  //       ],
  //     },
  //     outerBottomSlot: <QuickCreate />,
  //   },
  // },
  // RelationTagInput: {
  //   component: RelationTagInput,
  //   icon: "IconDescend2",
  //   label: "列表弹框",
  //   dataModel: DataModel.string,
  // },
  // RelationView: {
  //   component: RelationView,
  //   icon: "IconDescend2",
  //   label: "关联预览",
  //   dataModel: DataModel.string,
  //   props: {
  //     viewModel: {
  //       label: "预览模型",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.string,
  //       // options: { func: list, labelKey: "type", valueKey: "type" },
  //     },
  //   },
  // },

  // TreeSelect: {
  //   component: VfTreeInput,
  //   icon: "IconTreeTriangleDown",
  //   label: "树型下拉",
  //   dataType: DataType.basic,
  //   dataModel: DataModel.string,
  //   props: {
  //     className: "w-full",
  //     treeData: {
  //       label: "树形结构数据",
  //       dataType: DataType.array,
  //       dataModel: "ITreeData",
  //       required: true,
  //     },
  //     outerBottomSlot: <QuickCreate />,
  //     saveData: {
  //       label: "快捷创建",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.string,
  //       options: [
  //         { label: "在底部可创建菜单", value: "/sysMenu/save" },
  //         { label: "在底部可创建部门", value: "/sysDept/save" },
  //       ],
  //     },
  //   },
  // },

  // VfEditor: {
  //   component: VfEditor,
  //   icon: "IconTextRectangle",
  //   label: "富文本",
  //   dataType: DataType.basic,
  //   dataModel: DataModel.string,
  // },
  // SelectIcon: {
  //   component: SelectIcon,
  //   icon: "IconComponent",
  //   label: "图标选择",
  //   dataType: DataType.basic,
  //   dataModel: DataModel.string,
  // },
  // VfImage: {
  //   component: VfImage,
  //   icon: "IconImage",
  //   label: "图片上传",
  //   dataModel: DataModel.string,
  // },
  // SearchInput: {
  //   component: SearchInput,
  //   icon: "IconSearch",
  //   dataModel: DataModel.string,
  //   dataType: DataType.basic,
  //   label: "搜索组件",
  // },
  // SelectTag: {
  //   label: "Tag标签(字典)",
  //   component: SelectTag,
  //   icon: "IconSearch",
  //   dataModel: DataModel.string,
  //   props: {
  //     datas: {
  //       label: "字典数据",
  //       dataModel: "ISelect",
  //       dataType: DataType.array,
  //       required: true,
  //     },
  //   },
  // },
  // VfTreeSelect: {
  //   component: VfTreeSelect,
  //   label: "树型选择",
  //   dataType: DataType.basic,
  //   icon: "IconSearch",
  //   dataModel: DataModel.string,
  //   props: {
  //     datas: {
  //       label: "选项数据",
  //       dataType: DataType.array,
  //       dataModel: "ITree",
  //       required: true,
  //     },
  //     expandAll: false,
  //     valField: "code",
  //   },
  // },
  // PageSelect: {
  //   component: PageSelect,
  //   label: "分组选择组件",
  //   dataType: DataType.array,
  //   icon: "IconSearch",
  //   dataModel: DataModel.string,
  //   props: {
  //     datas: {
  //       label: "分组数据",
  //       dataType: DataType.array,
  //       dataModel: "PageSelectData",
  //       required: true,
  //     },
  //     dataEmpty: {
  //       label: "空数据时文案",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.string,
  //     },
  //   },
  // },
  // MenuResourcesSelect: {
  //   //该组件不能看做是通用组件，和tsx内部访问了接口，一般组件里属性来源于字段(fromField)的一般都不是通用组件，这里需要调整
  //   component: MenuResourcesSelect,
  //   label: "角色资源绑定模块",
  //   dataType: DataType.array,
  //   dataModel: DataModel.string,
  //   icon: "IconComponent",
  //   props: {
  //     appId: {
  //       required: true,
  //       label: "应用ID",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.string,
  //       fromField: { entity: "sysMenu", field: "id" },
  //     },
  //     roleId: {
  //       label: "角色id",
  //       required: true,
  //       dataType: DataType.basic,
  //       dataModel: DataModel.string,
  //       fromField: { entity: "sysRole", field: "id" },
  //     },
  //   },
  // },
  // // formPage:{
  // //   component: FormPage,
  // //   label:"表单",
  // //   dataType: DataType.object,
  // //   dataModel: "IModel",
  // // },
  // table: {
  //   component: FormTable,
  //   label: "1对多列表录入",
  //   icon: "IconOrderedList",
  //   dataType: DataType.array,
  //   dataModel: "IdBean", //仅支持实体模型(不支持IModel)
  //   props: {
  //     ignores: {
  //       label: "列表不展示字段",
  //       dataType: DataType.array,
  //       dataModel: DataModel.string,
  //       options: (
  //         form,
  //         field
  //       ) => {
  //         return list({ type: field?.fieldType }).then((d) => {
  //           if (d.data) {
  //             return d.data[0].fields
  //               .filter(
  //                 (f) =>
  //                   f.listHide === null ||
  //                   f.listHide === undefined ||
  //                   f.listHide === false
  //               )
  //               .map((f) => {
  //                 return { value: f.fieldName, label: f.title };
  //               });
  //           } else {
  //             return [];
  //           }
  //         });
  //       },
  //     },
  //     unRemove: {
  //       label: "禁止删除",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.boolean,
  //     },
  //     unModify: {
  //       label: "禁止修改",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.boolean,
  //     },
  //     unCreate: {
  //       label: "禁止新增",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.boolean,
  //     },
  //   },
  // },
  // VfListForm: {
  //   component: VfListForm,
  //   label: "1对多表单录入",
  //   icon: "IconOrderedList",
  //   dataType: DataType.array,
  //   dataModel: "IModel",
  //   props: {
  //     showInput: false,
  //   },
  // },
  // VfNestedSelector: {
  //   component: VfNestedSelector,
  //   label: "级联选择器",
  //   icon: "IconOrderedList",
  //   dataType: DataType.array,
  //   dataModel: DataModel.string,
  //   props: {
  //     datas: {
  //       label: "多级选择数据",
  //       dataType: DataType.array,
  //       dataModel: "ITreeData",
  //     },
  //     emptyDesc: {
  //       label: "空数据提醒",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.string,
  //     },
  //   },
  // },
  // MiniForm: {
  //   component: MiniFormList,
  //   label: "mini表单",
  //   icon: "IconOrderedList",
  //   dataType: DataType.array,
  //   dataModel: "IModel",
  //   props: {
  //     options: {
  //       label: "选项数据源",
  //       must: true,
  //       dataType: DataType.array,
  //       dataModel: "ISelect",
  //     },
  //     labelFieldName: {
  //       label: "核心显示字段",
  //       remark: "选项label会赋值的当前字段所在表单的指定字段",
  //       must: true,
  //       dataType: DataType.basic,
  //       dataModel: DataModel.string,
  //       // fromField: true,无效，却要指定当前表单，这里指定的是父表单
  //     },
  //     valueFieldName: {
  //       label: "核心意义字段",
  //       remark: "选项value会赋值的当前字段所在表单的指定字段",
  //       must: true,
  //       dataType: DataType.basic,
  //       dataModel: DataModel.string,
  //       // fromField: true,
  //     },
  //   },
  // },
  // //高级组件
  // QueryBuilder: {
  //   component: QueryBuilder,
  //   dataType: DataType.basic,
  //   dataModel: DataModel.string,
  //   label: "查询条件设计器",
  //   icon: "IconOrderedList",
  //   props: {
  //     entityModel: {
  //       label: "当前模型信息",
  //       must: true,
  //       dataType: DataType.object,
  //       dataModel: "FormVo",
  //     },
  //     subForms: {
  //       label: "关联模型集合",
  //       must: true,
  //       dataType: DataType.array,
  //       dataModel: "FormVo",
  //     },
  //   },
  // },
  // UserSelect: {
  //   component: UserSelect,
  //   label: "用户选择器",
  //   icon: "IconOrderedList",
  //   dataType: DataType.basic,
  //   dataModel: DataModel.string,
  //   props: {
  //     defCurrUser: {
  //       label: "当前用户默认选中",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.boolean,
  //     },
  //   },
  // },
  // WorkFlowEditor: {
  //   component: WorkFlowEditor,
  //   label: "流程设计器",
  //   icon: "IconOrderedList",
  //   dataType: DataType.basic,
  //   dataModel: DataModel.string,
  // },
  // MemberSelect: {
  //   component: MemberSelect,
  //   label: "流程审批对象选择",
  //   icon: "IconOrderedList",
  //   dataType: DataType.array,
  //   dataModel: "nodeUserInfo",
  // },
  // TableRadioSelect: {
  //   component: TableRadioSelect,
  //   label: "字典项数据选择",
  //   icon: "IconOrderedList",
  //   dataType: DataType.array,
  //   dataModel: "flowField",
  //   props: {
  //     //多选
  //     dicts: {
  //       label: "字典数据",
  //       must: true,
  //       dataType: DataType.array,
  //       dataModel: "SysDict",
  //     },
  //   },
  // },
  // SwitchCard: {
  //   component: SwitchCard,
  //   label: "switch卡片样式开关",
  //   icon: "IconOrderedList",
  //   dataType: DataType.basic,
  //   dataModel: DataModel.boolean,
  //   props: {
  //     divider: {
  //       label: "分割线",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.boolean,
  //     },
  //     icon: {
  //       label: "图标",
  //       dataType: DataType.basic,
  //       dataModel: DataModel.icon,
  //     },
  //   },
  // },
];
