import { ReactElement } from "react";
import pageComponents from "./page";
import itemFieldComponents from "./itemFields";
import DragPanel from "../../Left/DragPanel";

const menus: {
  key: string;
  label: string;
  panel: ReactElement;
}[] = [
  {
    key: "page",
    label: "页面组件",
    panel: <DragPanel data={pageComponents} />
  },
  {
    key: "itemFields",
    label: "Item 字段",
    panel: <DragPanel data={itemFieldComponents} />
  }
];

export default menus;
