import React from "react";
import { Button, Space, Tag } from "antd";
import type { ColumnsType } from "antd/es/table";

interface DataType {
  key: string;
  name: string;
  address: string;
  tags: string[];
}

export const columns: ColumnsType<DataType> = [
  {
    title: "Name",
    dataIndex: "name",
    key: "name",
    ellipsis: true
  },
  {
    title: "Address",
    dataIndex: "address",
    key: "address",
    ellipsis: true
  },
  {
    title: "Tags",
    key: "tags",
    dataIndex: "tags",
    ellipsis: true,
    render: (_, { tags }) => (
      <React.Fragment>
        {tags.map(tag => (
          <Tag color="processing" key={tag}>
            {tag}
          </Tag>
        ))}
      </React.Fragment>
    )
  },
  {
    title: "Action",
    key: "action",
    ellipsis: true,
    render: (_, record) => (
      <Space size="middle">
        <Button type="text" size="small">
          Invite {record.name}
        </Button>
        <Button type="text" size="small">
          Delete
        </Button>
      </Space>
    )
  }
];

export const data: DataType[] = [
  {
    key: "1",
    name: "John Brown",
    address: "New York No. 1 Lake Park",
    tags: ["nice", "developer"]
  },
  {
    key: "2",
    name: "Jim Green",
    address: "London No. 1 Lake Park",
    tags: ["nice", "developer"]
  },
  {
    key: "3",
    name: "Joe Black",
    address: "Sydney No. 1 Lake Park",
    tags: ["cool", "teacher"]
  },
  {
    key: "4",
    name: "John Brown",
    address: "New York No. 1 Lake Park",
    tags: ["nice", "developer"]
  },
  {
    key: "5",
    name: "Jim Green",
    address: "London No. 1 Lake Park",
    tags: ["nice", "developer"]
  },
  {
    key: "6",
    name: "Joe Black",
    address: "Sydney No. 1 Lake Park",
    tags: ["cool", "teacher"]
  },
  {
    key: "7",
    name: "Jim Green",
    address: "London No. 1 Lake Park",
    tags: ["nice", "developer"]
  }
];
