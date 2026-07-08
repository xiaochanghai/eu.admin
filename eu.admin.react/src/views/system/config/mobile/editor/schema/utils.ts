/** 判断是否为容器节点（可包含子组件） */
export function isContainerNode(type: string): boolean {
  return ["list", "row", "column"].includes(type);
}

/** 判断是否为 Item 字段组件 */
export function isItemFieldNode(type: string): boolean {
  return ["text", "image", "statusTag", "metric", "iconText", "divider", "spacer", "actionButton"].includes(type);
}

/** 判断是否为页面级组件 */
export function isPageComponent(type: string): boolean {
  return ["searchBar", "tabs", "statRow", "list", "emptyState", "floatingAction"].includes(type);
}
