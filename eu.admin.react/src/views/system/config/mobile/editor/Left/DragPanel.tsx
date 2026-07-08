import { MobileFieldNode } from "../schema/types";
import DragItem from "./DragItem";

interface Props {
  data: MobileFieldNode[];
}

export default function DragPanel({ data }: Props) {
  return (
    <div className="space-y-2">
      {data.map((item, index) => (
        <DragItem key={index} data={item} />
      ))}
    </div>
  );
}
