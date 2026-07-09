import cl from "classnames";
import { CRAD } from "../ItemTypes";
import { useDrag } from "react-dnd";
import { getEmptyImage } from "react-dnd-html5-backend";
import { MobileFieldNode } from "../schema/types";

/** 组件图标映射 */
const componentIcons: Record<string, string> = {
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

export default function DragItem({ data }: { data: MobileFieldNode }) {
  const [{ isDragging }, dragRef, connectDragPreview] = useDrag(() => ({
    type: CRAD,
    item: { data: { ...data, id: "", children: [] } },
    collect: monitor => ({
      isDragging: monitor.isDragging()
    })
  }), [data]);

  connectDragPreview(getEmptyImage());

  return (
    <div
      ref={dragRef}
      className={cl(
        "transition-all duration-150",
        {
          "opacity-40 scale-95": isDragging
        }
      )}
      style={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        gap: 4,
        padding: "10px 6px",
        border: "1px solid #e5e7eb",
        borderRadius: 8,
        background: "#fafbfc",
        cursor: "grab",
        userSelect: "none",
        textAlign: "center"
      }}
      onMouseEnter={e => {
        if (!isDragging) {
          e.currentTarget.style.borderColor = "#93c5fd";
          e.currentTarget.style.background = "#eff6ff";
          e.currentTarget.style.boxShadow = "0 2px 8px rgba(37,99,235,0.1)";
        }
      }}
      onMouseLeave={e => {
        e.currentTarget.style.borderColor = "#e5e7eb";
        e.currentTarget.style.background = "#fafbfc";
        e.currentTarget.style.boxShadow = "none";
      }}
    >
      <span style={{ fontSize: 20, lineHeight: 1 }}>
        {componentIcons[data.type] || "📦"}
      </span>
      <span style={{ fontSize: 11, color: "#4b5563", fontWeight: 500, lineHeight: 1.2 }}>
        {data.displayName || String(data.type)}
      </span>
    </div>
  );
}
