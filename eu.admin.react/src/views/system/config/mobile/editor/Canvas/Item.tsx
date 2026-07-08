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
      className={cl(
        "relative border border-dashed rounded transition-colors",
        {
          "opacity-30": isDragging,
          "border-blue-500 bg-blue-50/30": isFocused,
          "border-gray-300 hover:border-blue-300": !isFocused
        }
      )}
      style={{ margin: "2px 0" }}
    >
      {/* 选中时的标签和操作 */}
      {isFocused && (
        <div className="-top-5 left-0 right-0 flex justify-between items-center z-10">
          <span className="text-xs text-blue-600 bg-blue-50 px-1 rounded">
            {data.type}
            {data.type === "list" && " (双击编辑Item)"}
          </span>
          <span
            onClick={handleRemove}
            className="px-1.5 py-0.5 bg-red-500 rounded cursor-pointer text-xs hover:bg-red-600"
          >
            ✕
          </span>
        </div>
      )}
      {/* 拖拽指示线 */}
      {isOver && canDrop && !positionDown && <div className="border-t-2 border-blue-500" />}
      {content}
      {isOver && canDrop && positionDown && <div className="border-b-2 border-blue-500" />}
    </div>
  );
}
