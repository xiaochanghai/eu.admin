export enum OptEnum {
  eq = "eq",
  like = "like",
  notLike = "notLike",
  startsWith = "startsWith",
  endsWith = "endsWith",
  between = "between",
  notBetween = "notBetween",
  gt = "gt",
  goe = "goe",
  lt = "lt",
  loe = "loe",
  in = "in",
  ne = "ne",
  isNotNull = "isNotNull",
  isNull = "isNull",
  notIn = "notIn",
  dynamic = "dynamic", //日期类型动态范围
  fix = "fix"
}

export enum Mode {
  list = "list",
  filter = "filter",
  form = "form"
}
//数据大类
export enum DataType {
  basic = "basic", //基础数据类型
  array = "array", //集合数组
  object = "object", //复杂对象
  event = "event", //事件数据类型
  //是否下列的能够移植到 tsType
  icon = "Icon", //图标类型
  image = "image", //图像类型
  page = "page" //分页数据
}

export interface where {
  fieldId: string;
  /** 搜索条件来源 */
  from?: "searchInput" | "coulumnFilter" | "queryBuilder";
  /** 字段 */
  fieldName: string;
  /** 所在实体类 */
  entityName?: string;
  /** 字段类型 */
  fieldType?: string;
  /** 匹配方式 */
  opt: OptEnum;
  /** 转换函数 */
  tran: string;
  /** 匹配值 */
  value: any;
  /** 动态值的code */
  fixCode: string;
  /** 条件中文描述 */
  desc: { fieldName?: string; opt?: string; value?: any };
}
//简单条件分组 where内部是and(且条件), where之间是or(或条件)
export interface ConditionGroup {
  entityName?: string;
  where: Partial<where>[];
}

export type andOr = "and" | "or";

//复杂条件分组，每组内部由orAnd决定联合方式
export interface Conditions {
  orAnd: andOr;
  where: Partial<where>[];
  conditions: Partial<Conditions>[];
}
