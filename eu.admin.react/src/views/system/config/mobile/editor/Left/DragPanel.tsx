import { MobileFieldNode } from "../schema/types";
import DragItem from "./DragItem";

interface Props {
  data: MobileFieldNode[];
}

export default function DragPanel({ data }: Props) {
  return (
    <div style={{
      display: "grid",
      gridTemplateColumns: "1fr 1fr",
      gap: 8
    }}>
      {data.map((item, index) => (
        <DragItem key={index} data={item} />
      ))}
    </div>
  );
}
