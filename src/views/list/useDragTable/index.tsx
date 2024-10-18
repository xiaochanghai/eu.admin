import { useState } from "react";
import { message } from "@/hooks/useMessage";
import { DragSortTable } from "@ant-design/pro-components";
import type { ProColumns } from "@ant-design/pro-components";

const columns: ProColumns[] = [
  { title: "排序", dataIndex: "sort", width: 60, className: "drag-visible", hideInSearch: true },
  { title: "姓名", dataIndex: "name", className: "drag-visible" },
  { title: "年龄", dataIndex: "age" },
  { title: "地址", dataIndex: "address" }
];

const data = [
  { key: "1", name: "James Brown", age: 32, address: "New York No. 1 Lake Park", index: 0 },
  { key: "2", name: "Ava Davis", age: 42, address: "London No. 1 Lake Park", index: 1 },
  { key: "3", name: "Matthew Smith", age: 32, address: "Sidney No. 1 Lake Park", index: 2 },
  { key: "4", name: "Daniel White", age: 32, address: "London No. 2 Lake Park", index: 3 },
  { key: "5", name: "Emily White", age: 28, address: "Paris No. 1 Lake Park", index: 4 },
  { key: "6", name: "Linda Johnson", age: 37, address: "Los Angeles No. 1 Lake Park", index: 5 },
  { key: "7", name: "Michael Smith", age: 45, address: "Chicago No. 1 Lake Park", index: 6 },
  { key: "8", name: "Sophia Martinez", age: 29, address: "Berlin No. 1 Lake Park", index: 7 },
  { key: "9", name: "William Davis", age: 36, address: "Tokyo No. 1 Lake Park", index: 8 },
  { key: "10", name: "Olivia Taylor", age: 28, address: "Sydney No. 2 Lake Park", index: 9 }
];

const UseDragTable: React.FC = () => {
  const [dataSource, setDataSource] = useState(data);

  const handleDragSortEnd = (_beforeIndex: number, _afterIndex: number, newDataSource: any) => {
    console.log("排序后的数据", newDataSource);
    setDataSource(newDataSource);
    message.success("修改列表排序成功");
  };

  return (
    <DragSortTable
      className="ant-pro-table-scroll"
      headerTitle="使用 DragTable"
      columns={columns}
      bordered
      cardBordered
      scroll={{ y: "100%" }}
      rowKey="key"
      dataSource={dataSource}
      dragSortKey="sort"
      onDragSortEnd={handleDragSortEnd}
    />
  );
};

export default UseDragTable;
