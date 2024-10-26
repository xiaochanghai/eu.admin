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

//数据明细类
export enum DataModel {
  string = "string", //基础数据类型
  number = "number", //集合数组
  date = "date", //复杂对象
  boolean = "boolean",
  image = "image", //图像类型
  icon = "icon" //图像类型
}
