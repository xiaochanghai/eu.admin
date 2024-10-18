import React, { useState } from "react";
import type { ProColumns } from "@ant-design/pro-components";
import { EditableProTable } from "@ant-design/pro-components";
import { Space } from "antd";
import { pagination } from "@/config/proTable";

const waitTime = (time: number = 100) => {
  return new Promise(resolve => {
    setTimeout(() => {
      resolve(true);
    }, time);
  });
};

type DataSourceType = {
  id: React.Key;
  title?: string;
  readonly?: string;
  decs?: string;
  state?: string;
  created_at?: string;
  update_at?: string;
  children?: DataSourceType[];
};

const defaultData: DataSourceType[] = [
  {
    id: 624748504,
    title: "活动名称一",
    readonly: "活动名称一",
    decs: "这个活动真好玩",
    state: "open",
    created_at: "1590486176000",
    update_at: "1590486176000"
  },
  {
    id: 624691228,
    title: "活动名称二",
    readonly: "活动名称二",
    decs: "这个活动真好玩",
    state: "closed",
    created_at: "1590481162000",
    update_at: "1590481162000"
  },
  {
    id: 624691229,
    title: "活动名称二",
    readonly: "活动名称二",
    decs: "这个活动真好玩",
    state: "closed",
    created_at: "1590481162000",
    update_at: "1590481162000"
  },
  {
    id: 624691230,
    title: "活动名称二",
    readonly: "活动名称二",
    decs: "这个活动真好玩",
    state: "closed",
    created_at: "1590481162000",
    update_at: "1590481162000"
  }
];

const UseEditTable: React.FC = () => {
  const [editableKeys, setEditableRowKeys] = useState<React.Key[]>([]);
  const [dataSource, setDataSource] = useState<readonly DataSourceType[]>([]);
  const [selectedRowKeys, setSelectedRowKeys] = React.useState([]);

  // 定义选择行的变化时的回调函数
  // const onSelectChange = (keys: any, rows: any) => {
  const onSelectChange = (keys: any) => {
    setSelectedRowKeys(keys);
    // 可以在这里处理选中行的数据，例如执行某些操作
  };
  const columns: ProColumns<DataSourceType>[] = [
    {
      title: "活动名称",
      dataIndex: "title",
      tooltip: "只读，使用form.getFieldValue获取不到值",
      formItemProps: (_, { rowIndex }) => {
        return {
          rules: rowIndex > 1 ? [{ required: true, message: "此项为必填项" }] : []
        };
      },
      // 第一行不允许编辑
      editable: (_text, _record, index) => {
        return index !== 0;
      },
      width: "15%"
    },
    {
      title: "活动名称二",
      dataIndex: "readonly",
      tooltip: "只读，使用form.getFieldValue可以获取到值",
      width: "15%"
    },
    {
      title: "状态",
      key: "state",
      dataIndex: "state",
      valueType: "select",
      valueEnum: {
        all: { text: "全部", status: "Default" },
        open: { text: "未解决", status: "Error" },
        closed: { text: "已解决", status: "Success" }
      }
    },
    {
      title: "描述",
      dataIndex: "decs",
      fieldProps: (form, { rowKey, rowIndex }) => {
        if (form.getFieldValue([rowKey || "", "title"]) === "不好玩") {
          return { disabled: true };
        }
        if (rowIndex > 9) {
          return { disabled: true };
        }
        return {};
      }
    },
    {
      title: "活动时间",
      dataIndex: "created_at",
      valueType: "date"
    },
    {
      title: "操作",
      valueType: "option",
      width: 200,
      render: (_text, record, _, action) => [
        <a
          key="editable"
          onClick={() => {
            action?.startEditable?.(record.id);
          }}
        >
          编辑
        </a>,
        <a
          key="delete"
          onClick={() => {
            setDataSource(dataSource.filter(item => item.id !== record.id));
          }}
        >
          删除
        </a>
      ]
    }
  ];
  return (
    <EditableProTable<DataSourceType>
      rowKey="id"
      headerTitle="使用 EditTable"
      className="ant-pro-table-scroll"
      scroll={{ x: 1000, y: "100%" }}
      // bordered
      // cardBordered
      recordCreatorProps={false}
      rowSelection={{
        alwaysShowAlert: true,
        onChange: onSelectChange,
        selectedRowKeys
        // onSelect, //用户手动选择/取消选择某行的回调
        //   onSelectMultiple: onMulSelect, //用户使用键盘 shift 选择多行的回调
        //   onSelectAll: onMulSelect, //用户手动选择/取消选择所有行的回调
      }}
      // tableAlertRender={({ selectedRowKeys, selectedRows, onCleanSelected }) => {
      tableAlertRender={({ selectedRowKeys }) => {
        // if (tableAlertRenderShow) {
        return (
          <Space size={10}>
            <a
              type="link"
              key="primarynew"
              // loading={bulkOperationLoading}
              onClick={() => {
                // bulkOperation(mySelectedRowKeys, mySelectedRow);
                // bulkOperation(mySelectedRowKeys);
              }}
            >
              批量删除
            </a>
            <a
              type="link"
              key="primarynew"
              // loading={bulkOperationLoading}
              onClick={() => {
                // bulkOperation(mySelectedRowKeys, mySelectedRow);
                // bulkOperation(mySelectedRowKeys);
              }}
            >
              批量填充
            </a>
            <span>已选{selectedRowKeys.length}/5项</span>
            <span>
              <a
                style={{ marginInlineStart: 8 }}
                onClick={() => {
                  setSelectedRowKeys([]);
                  // handleMySelectedRow([]);
                  // setBulkOperationLoading(false);
                }}
              >
                清空
              </a>
            </span>
          </Space>
        );
        // }
      }}
      tableAlertOptionRender={() => {
        return false;
      }}
      loading={false}
      columns={columns}
      request={async () => ({
        data: defaultData,
        total: 5,
        success: true
      })}
      pagination={pagination}
      value={dataSource}
      onChange={setDataSource}
      editable={{
        type: "multiple",
        editableKeys,
        onSave: async (rowKey, data, row) => {
          console.log(rowKey, data, row);
          await waitTime(2000);
        },
        onChange: setEditableRowKeys
      }}
    />
  );
};

export default UseEditTable;
