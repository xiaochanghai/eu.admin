/** 属性编辑器类型 */
export type EditFieldType = "Text" | "Number" | "Select" | "Radio" | "TextArea" | "OptionEditor" | "Json" | "Color";

/** 移动端组件节点 */
export interface MobileFieldNode {
  type: string;
  displayName?: string;
  props: Record<string, any>;
}

/** 属性编辑字段定义 */
export interface MobileEditField {
  key: string;
  name: string;
  type: EditFieldType;
  options?: { label: string; value: any }[];
  placeholder?: string;
  min?: number;
  max?: number;
}
