import { Button, Dropdown } from "antd";
import { Icon } from "@/components";
import { ActionButton } from "./ActionButton";
import { ActionColumnProps } from "../types";
import { buildOptionAuthMap, getDropActionItems } from "../utils";
import { ACTION_COLUMN_CONFIG, SUM_ROW_ID, DEFAULT_ACTION_BUTTON_STYLE } from "../constants";

/**
 * 操作列工厂函数
 * 返回表格的操作列配置，包含编辑、查看、删除等按钮
 */
export const ActionColumn = ({ moduleInfo, IsView, onEdit, onView, onDelete, customActions = {} }: ActionColumnProps) => {
  const optionAuthButton = buildOptionAuthMap(moduleInfo.beforeActions);

  const renderActions = (_: any, record: { ID: string }, index: number, action: any) => {
    const dropdownItems = getDropActionItems(
      record,
      action,
      moduleInfo.dropActions || [],
      IsView ?? false,
      onEdit,
      onView,
      onDelete,
      moduleInfo,
      customActions
    );

    return (
      <>
        {record.ID !== SUM_ROW_ID && (
          <>
            {optionAuthButton.Update && !IsView && <ActionButton icon="EditOutlined" onClick={() => onEdit(record)} />}
            {optionAuthButton.View && (
              <Button
                type="dashed"
                key={"View" + index}
                size="small"
                icon={<Icon name="EyeOutlined" />}
                onClick={() => onView(record)}
                style={DEFAULT_ACTION_BUTTON_STYLE}
              />
            )}
            {optionAuthButton.Delete && !IsView && (
              <Button
                key={"Delete" + index}
                type="dashed"
                size="small"
                icon={<Icon name="DeleteOutlined" />}
                onClick={() => onDelete(action, record)}
                style={DEFAULT_ACTION_BUTTON_STYLE}
              />
            )}
            {moduleInfo.dropActions && moduleInfo.dropActions.length > 0 && (
              <Dropdown placement="bottom" arrow menu={{ items: dropdownItems }}>
                <Button
                  type="dashed"
                  size="small"
                  icon={<Icon name="MoreOutlined" />}
                  style={DEFAULT_ACTION_BUTTON_STYLE}
                />
              </Dropdown>
            )}
          </>
        )}
      </>
    );
  };

  return {
    ...ACTION_COLUMN_CONFIG,
    render: renderActions
  };
};
