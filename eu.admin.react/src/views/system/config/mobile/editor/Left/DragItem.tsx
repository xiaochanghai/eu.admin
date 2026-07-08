import cl from "classnames";
import { CRAD } from "../ItemTypes";
import { useDrag } from "react-dnd";
import { getEmptyImage } from "react-dnd-html5-backend";
import { MobileFieldNode } from "../schema/types";

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
        "p-2 border border-gray-200 text-center text-gray-600 shadow-sm rounded-sm bg-gray-50 cursor-move hover:bg-gray-100 hover:text-gray-900 hover:border-blue-500",
        {
          "opacity-50": isDragging
        }
      )}
    >
      {data.displayName || String(data.type)}
    </div>
  );
}
