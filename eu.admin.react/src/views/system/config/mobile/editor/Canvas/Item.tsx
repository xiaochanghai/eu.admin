import React, { useRef, useState } from "react";
import { appendCom, moveCom, setFocus, removeCom, setEditingItem, MobileNodeSchema } from "@/redux/modules/mobileEditor";
import { useDispatch, RootState, useSelector } from "@/redux";
import { useDrop, useDrag } from "react-dnd";
import { getEmptyImage } from "react-dnd-html5-backend";
import previewComponents from "../schema/preview";
import cl from "classnames";
import { CRAD } from "../ItemTypes";
import { isContainerNode } from "../schema/utils";

interface Props {
  data: MobileNodeSchema;
  parentId: string;
  index: number;
}

interface DragData {
  data: MobileNodeSchema;
  dragIndex: number;
  dragParentId: string;
}

/** 组件显示名映射 */
const typeLabels: Record<string, string> = {
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

/** 组件类型颜色 */
const typeColors: Record<string, string> = {
  searchBar: "#0ea5e9",
  tabs: "#8b5cf6",
  statRow: "#f59e0b",
  list: "#2563eb",
  emptyState: "#6b7280",
  floatingAction: "#10b981",
  text: "#6366f1",
  image: "#ec4899",
  statusTag: "#14b8a6",
  metric: "#f97316",
  iconText: "#06b6d4",
  divider: "#94a3b8",
  spacer: "#94a3b8",
  actionButton: "#3b82f6",
  row: "#a855f7",
  column: "#a855f7"
};

export default function Item({ data, parentId, index }: Props) {
  const ref = useRef<HTMLDivElement | null>(null);
  const [positionDown, setPosition] = useState(true);
  const state = useSelector((s: RootState) => s.mobileEditor);
  const dispatch = useDispatch();

  const [{ canDrop, isOver }, drop] = useDrop<DragData, {}, { canDrop: boolean; isOver: boolean }>(
    () => ({
      accept: CRAD,
      drop: (item, monitor) => {
        if (monitor.didDrop()) return;
        if (!item.data.id) {
          dispatch(
            appendCom({
              hoverParentId: parentId,
              hoverIndex: index,
              data,
              item: item.data,
              positionDown
            })
          );
        } else {
          dispatch(
            moveCom({
              hoverParentId: parentId,
              hoverIndex: index,
              dragParentId: item.dragParentId,
              dragIndex: item.dragIndex,
              data,
              item: item.data,
              positionDown
            })
          );
        }
        return undefined;
      },
      hover: (_item, monitor) => {
        const didHover = monitor.isOver({ shallow: true });
        if (didHover && ref.current) {
          const rect = ref.current.getBoundingClientRect();
          const middleY = (rect.bottom - rect.top) / 2;
          const clientOffset = monitor.getClientOffset();
          if (clientOffset) {
            const clientY = clientOffset.y - rect.top;
            setPosition(clientY > middleY);
          }
        }
      },
      collect: monitor => ({
        isOver: monitor.isOver({ shallow: true }),
        canDrop: monitor.canDrop()
      })
    }),
    [data, parentId, positionDown, index]
  );

  const [{ isDragging }, drag, connectDragPreview] = useDrag(() => ({
    type: CRAD,
    item: { data, dragIndex: index, dragParentId: parentId } as DragData,
    collect: monitor => ({
      isDragging: monitor.isDragging()
    })
  }), [data, index, parentId]);

  connectDragPreview(getEmptyImage());
  drag(drop(ref));

  const CurrentTag = previewComponents[data.type] as any;

  const handleFocus = (e: React.MouseEvent) => {
    e.stopPropagation();
    dispatch(setFocus({ focusId: data.id }));
  };

  const handleRemove = (e: React.MouseEvent) => {
    e.stopPropagation();
    dispatch(removeCom({ parentId, id: data.id }));
  };

  const handleDoubleClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (data.type === "list") {
      dispatch(setEditingItem({ listNodeId: data.id }));
    }
  };

  const isFocused = state.focusId === data.id;
  const accentColor = typeColors[data.type] || "#2563eb";

  const content = (
    <CurrentTag {...data.props}>
      {isContainerNode(data.type) &&
        data.children?.map((child, i) => (
          <Item key={child.id} data={child} parentId={data.id} index={i} />
        ))}
    </CurrentTag>
  );

  return (
    <div
      ref={ref}
      onClick={handleFocus}
      onDoubleClick={handleDoubleClick}
      className={cl("transition-all duration-150", {
        "opacity-30": isDragging
      })}
      style={{
        position: "relative",
        margin: "1px 0",
        borderLeft: isFocused ? `3px solid ${accentColor}` : "3px solid transparent",
        background: isFocused ? `${accentColor}08` : "transparent",
        borderRadius: isFocused ? "0 6px 6px 0" : 0
      }}
    >
      {/* 选中时的标签和操作 */}
      {isFocused && (
        <div style={{
          position: "absolute",
          top: -1,
          right: 4,
          display: "flex",
          alignItems: "center",
          gap: 4,
          zIndex: 10
        }}>
          <span style={{
            fontSize: 10,
            fontWeight: 600,
            color: accentColor,
            background: `${accentColor}15`,
            padding: "1px 6px",
            borderRadius: 4,
            border: `1px solid ${accentColor}30`
          }}>
            {typeLabels[data.type] || data.type}
            {data.type === "list" && " · 双击编辑Item"}
          </span>
          <span
            onClick={handleRemove}
            style={{
              width: 18,
              height: 18,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              background: "#ef4444",
              color: "#fff",
              borderRadius: 4,
              cursor: "pointer",
              fontSize: 10,
              lineHeight: 1,
              transition: "background 0.15s"
            }}
            onMouseEnter={e => { (e.target as HTMLElement).style.background = "#dc2626"; }}
            onMouseLeave={e => { (e.target as HTMLElement).style.background = "#ef4444"; }}
          >
            ✕
          </span>
        </div>
      )}
      {/* 拖拽指示线 */}
      {isOver && canDrop && !positionDown && (
        <div style={{ height: 2, background: "#3b82f6", borderRadius: 1, margin: "0 8px" }} />
      )}
      {content}
      {isOver && canDrop && positionDown && (
        <div style={{ height: 2, background: "#3b82f6", borderRadius: 1, margin: "0 8px" }} />
      )}
    </div>
  );
}
