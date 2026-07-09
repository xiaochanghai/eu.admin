import { append, moveCom, MobileNodeSchema, setFocus } from "@/redux/modules/mobileEditor";
import { useDispatch, RootState, useSelector } from "@/redux";
import { CRAD } from "../ItemTypes";
import { useDrop } from "react-dnd";
import cl from "classnames";
import Item from "./Item";
import PhoneFrame from "./PhoneFrame";

interface DragItem {
  type: string;
  data: MobileNodeSchema;
  dragParentId: string;
  dragIndex: number;
}

export default function Canvas() {
  const state = useSelector((s: RootState) => s.mobileEditor);
  const dispatch = useDispatch();

  const [{ canDrop, isOver }, drop] = useDrop<DragItem, {}, { canDrop: boolean; isOver: boolean }>(() => ({
    accept: CRAD,
    drop: (item, monitor) => {
      if (monitor.didDrop()) return;
      if (!item.data.id) {
        dispatch(append(item.data));
      } else {
        dispatch(
          moveCom({
            dragParentId: item.dragParentId,
            dragIndex: item.dragIndex,
            data: state,
            item: item.data
          })
        );
      }
      return { dropped: true };
    },
    collect: monitor => ({
      isOver: monitor.isOver({ shallow: true }),
      canDrop: monitor.canDrop()
    })
  }));

  const pageProps = state.props || {};
  const pageBackgroundColor = typeof pageProps.backgroundColor === "string" && pageProps.backgroundColor
    ? pageProps.backgroundColor
    : "#f9fafb";

  const handleSelectPage = () => {
    dispatch(setFocus({ focusId: "root" }));
  };

  return (
    <div
      className="flex-1 overflow-y-auto flex justify-center"
      onClick={handleSelectPage}
      style={{
        background: "linear-gradient(180deg, #e8ecf3 0%, #d5dbe6 100%)",
        backgroundImage: `
          radial-gradient(circle at 1px 1px, rgba(0,0,0,0.06) 1px, transparent 0)
        `,
        backgroundSize: "24px 24px",
        padding: "24px 20px"
      }}
    >
      <div ref={drop} onClick={handleSelectPage}>
        <PhoneFrame title={pageProps.title || "页面预览"}>
          <div
            className={cl("transition-colors duration-200", {
              "bg-blue-100/40": isOver && canDrop
            })}
            style={{ minHeight: 400, position: "relative", background: pageBackgroundColor }}
          >
            {state.children.map((child, index) => (
              <Item key={child.id} data={child} parentId="root" index={index} />
            ))}
            {state.children.length === 0 && (
              <div style={{
                display: "flex",
                flexDirection: "column",
                alignItems: "center",
                justifyContent: "center",
                height: 320,
                color: "#c4c9d4"
              }}>
                <div style={{ fontSize: 48, marginBottom: 12, opacity: 0.5 }}>📱</div>
                <div style={{ fontSize: 14, fontWeight: 500 }}>从左侧拖拽组件到这里</div>
                <div style={{ fontSize: 12, marginTop: 4, color: "#d1d5db" }}>支持搜索框、标签、列表等组件</div>
              </div>
            )}
            {isOver && canDrop && (
              <div style={{
                position: "absolute",
                inset: 0,
                border: "2px dashed #93c5fd",
                borderRadius: 4,
                pointerEvents: "none"
              }} />
            )}
          </div>
        </PhoneFrame>
      </div>
    </div>
  );
}
