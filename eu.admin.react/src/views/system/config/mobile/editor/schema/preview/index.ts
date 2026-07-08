import React from "react";
import {
  SearchBarPreview,
  TabsPreview,
  StatRowPreview,
  ListPreview,
  EmptyStatePreview,
  FloatingActionPreview
} from "./page";
import {
  TextPreview,
  ImagePreview,
  StatusTagPreview,
  MetricPreview,
  IconTextPreview,
  DividerPreview,
  SpacerPreview,
  ActionButtonPreview,
  RowPreview,
  ColumnPreview
} from "./itemFields";

const previewComponents: Record<string, React.ComponentType<any>> = {
  // 页面组件
  searchBar: SearchBarPreview,
  tabs: TabsPreview,
  statRow: StatRowPreview,
  list: ListPreview,
  emptyState: EmptyStatePreview,
  floatingAction: FloatingActionPreview,
  // Item 字段组件
  text: TextPreview,
  image: ImagePreview,
  statusTag: StatusTagPreview,
  metric: MetricPreview,
  iconText: IconTextPreview,
  divider: DividerPreview,
  spacer: SpacerPreview,
  actionButton: ActionButtonPreview,
  row: RowPreview,
  column: ColumnPreview
};

export default previewComponents;
