import { append, moveCom, MobileNodeSchema } from "@/redux/modules/mobileEditor";
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

  return (
    <div className="flex-1 p-4 overflow-y-auto bg-gray-100 flex justify-center">
      <div ref={drop}>
        <PhoneFrame title={pageProps.title || "页面预览"}>
          <div
            className={cl("min-h-[400px] transition-colors", {
              "bg-blue-50/50": isOver && canDrop
            })}
          >
            {state.children.map((child, index) => (
              <Item key={child.id} data={child} parentId="root" index={index} />
            ))}
            {state.children.length === 0 && (
              <div className="flex items-center justify-center h-full text-gray-300 text-sm absolute inset-0">
                从左侧拖拽组件到这里
              </div>
            )}
          </div>
        </PhoneFrame>
      </div>
    </div>
  );
}
