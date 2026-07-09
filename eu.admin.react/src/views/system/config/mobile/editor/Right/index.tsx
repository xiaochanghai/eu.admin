import { traverse } from "@/utils";
import fields from "./fields";
import editFields from "../schema/edit";
import { updateTree, MobileNodeSchema } from "@/redux/modules/mobileEditor";
import { useDispatch, RootState, useSelector } from "@/redux";
import { MobileEditField, EditFieldType } from "../schema/types";
import { SettingOutlined } from "@ant-design/icons";

/** 组件图标映射 */
const typeIcons: Record<string, string> = {
  page: "P",
  searchBar: "🔍",
  tabs: "📑",
  statRow: "📊",
  list: "📋",
  emptyState: "📭",
  floatingAction: "➕",
  text: "📝",
  image: "🖼️",
  statusTag: "🏷️",
  metric: "📈",
  iconText: "📌",
  divider: "〰️",
  spacer: "↕️",
  actionButton: "🔘",
  row: "↔️",
  column: "↕️"
};

/** 组件中文名 */
const typeLabels: Record<string, string> = {
  page: "页面配置",
  searchBar: "搜索框",
  tabs: "筛选标签",
  statRow: "统计条",
  list: "列表",
  emptyState: "空状态",
  floatingAction: "悬浮按钮",
  text: "文本",
  image: "图片",
  statusTag: "状态标签",
  metric: "指标",
  iconText: "图标文本",
  divider: "分割线",
  spacer: "间距",
  actionButton: "操作按钮",
  row: "横向布局",
  column: "纵向布局"
};

export default function Right() {
  const state = useSelector((s: RootState) => s.mobileEditor);
  const dispatch = useDispatch();

  // 查找当前选中的节点
  let focusComponent: MobileNodeSchema | undefined;
  traverse(state, sub => {
    if (sub.id === state.focusId) {
      focusComponent = sub;
      return false;
    }
    return true;
  });

  const handleChange = (value: any, key: string) => {
    dispatch(updateTree({ key, value }));
  };

  const getValueByPath = (source: Record<string, any> | undefined, path: string) => {
    if (!source) return undefined;
    return path.split(".").reduce<any>((current, key) => current?.[key], source);
  };

  const renderField = (item: MobileEditField) => {
    const { key, name, type, ...rest } = item;
    const fieldType = type as EditFieldType;
    const Field = fields[fieldType];
    if (!Field) return null;

    return (
      <div key={key} style={{ marginBottom: 14 }}>
        <div style={{
          fontSize: 12,
          color: "#6b7280",
          marginBottom: 5,
          fontWeight: 500
        }}>
          {name}
        </div>
        {fieldType === "OptionEditor" ? (
          <Field
            value={getValueByPath(focusComponent?.props, key)}
            onChange={(val: any) => handleChange(val, key)}
          />
        ) : fieldType === "Select" ? (
          <Field
            style={{ width: "100%" }}
            options={rest.options}
            value={getValueByPath(focusComponent?.props, key)}
            onChange={(val: any) => handleChange(val, key)}
          />
        ) : (
          <Field
            {...rest}
            value={getValueByPath(focusComponent?.props, key)}
            onChange={(e: any) => {
              const val = e?.target ? e.target.value : e;
              handleChange(val, key);
            }}
          />
        )}
      </div>
    );
  };

  const editDefs = focusComponent ? editFields[focusComponent.type] : null;

  return (
    <div style={{
      width: 280,
      overflowY: "auto",
      borderLeft: "1px solid #e5e7eb",
      background: "#fff",
      display: "flex",
      flexDirection: "column"
    }}>
      {/* 标题 */}
      <div style={{
        flexShrink: 0,
        height: 44,
        lineHeight: "44px",
        padding: "0 16px",
        borderBottom: "1px solid #f3f4f6",
        fontWeight: 600,
        fontSize: 13,
        color: "#374151",
        display: "flex",
        alignItems: "center",
        gap: 6
      }}>
        <SettingOutlined style={{ color: "#2563eb" }} />
        属性设置
      </div>
      <div style={{ padding: "16px", flex: 1 }}>
        {focusComponent && editDefs ? (
          <>
            {/* 组件类型信息卡 */}
            <div style={{
              display: "flex",
              alignItems: "center",
              gap: 10,
              padding: "10px 12px",
              background: "#f8fafc",
              borderRadius: 8,
              border: "1px solid #e5e7eb",
              marginBottom: 16
            }}>
              <span style={{ fontSize: 22 }}>{typeIcons[focusComponent.type] || "📦"}</span>
              <div>
                <div style={{ fontSize: 13, fontWeight: 600, color: "#111827" }}>
                  {typeLabels[focusComponent.type] || focusComponent.type}
                </div>
                <div style={{ fontSize: 11, color: "#9ca3af", fontFamily: "monospace" }}>
                  {focusComponent.type}
                </div>
              </div>
            </div>
            {/* 属性字段 */}
            <div style={{
              fontSize: 11,
              fontWeight: 600,
              color: "#9ca3af",
              textTransform: "uppercase",
              letterSpacing: 1,
              marginBottom: 12
            }}>
              组件属性
            </div>
            {editDefs.map(item => renderField(item))}
          </>
        ) : (
          <div style={{
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            height: 280,
            color: "#d1d5db"
          }}>
            <div style={{ fontSize: 40, marginBottom: 12, opacity: 0.6 }}>🖱️</div>
            <div style={{ fontSize: 13, fontWeight: 500, color: "#9ca3af" }}>请在画布中选择组件</div>
            <div style={{ fontSize: 12, marginTop: 4 }}>点击组件查看和编辑属性</div>
          </div>
        )}
      </div>
    </div>
  );
}
