import { useMemo } from "react";
import { Popconfirm } from "antd";
import { CONFIRM_CONFIG } from "@/components/ProTableEditable/constants";

/**
 * 构建可编辑表格操作列的配置
 *
 * @param editableKeys 当前可编辑的行键
 * @param editCallBack 编辑回调（可选）
 * @param handleDelete 删除处理函数
 * @returns 操作列配置对象
 */
export const useEditableActionColumn = (
  editableKeys: React.Key[],
  editCallBack: (() => void) | undefined,
  handleDelete: (record: any) => void
) => {
  const actionColumn = useMemo(
    () => ({
      title: "操作",
      dataIndex: "option",
      fixed: "right",
      valueType: "option",
      width: 150,
      render: (_text: any, record: any, _: any, action: any) => [
        <a
          key="editable"
          onClick={() => {
            if (editableKeys.length > 0) action?.saveEditable?.(editableKeys[0]);
            action?.startEditable?.(record.ID);
            if (editCallBack) editCallBack();
          }}
        >
          编辑
        </a>,
        <Popconfirm
          key="delete"
          title={CONFIRM_CONFIG.title}
          description={CONFIRM_CONFIG.description}
          onConfirm={() => handleDelete(record)}
          okType={CONFIRM_CONFIG.okType}
          okText={CONFIRM_CONFIG.okText}
          cancelText={CONFIRM_CONFIG.cancelText}
        >
          <a>删除</a>
        </Popconfirm>
      ]
    }),
    [editableKeys, editCallBack, handleDelete]
  );

  return actionColumn;
};
