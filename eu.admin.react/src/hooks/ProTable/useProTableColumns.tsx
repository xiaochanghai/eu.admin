import { useState, useMemo } from "react";
import { Tag, Switch } from "antd";
import { useDispatch } from "@/redux";
import { setModuleInfo } from "@/redux/modules/module";
import { recordUserModuleColumn } from "@/api/modules/module";
import { ModuleInfo } from "@/api/interface/index";
import { ComboGrid, Icon } from "@/components";

/**
 * ProTable 列配置管理 Hook
 * 负责列增强、自定义渲染和列状态持久化
 *
 * @param columns 原始列配置
 * @param userModuleColumn 用户列配置
 * @param moduleId 模块ID
 * @param moduleInfo 模块信息（用于更新Redux）
 * @returns 增强后的列配置和列状态管理函数
 */
export const useProTableColumns = (
  columns: any[],
  userModuleColumn: any,
  moduleId: string,
  moduleInfo: ModuleInfo
) => {
  const dispatch = useDispatch();
  const [columnsStateMap, setColumnsStateMap] = useState(() => userModuleColumn);

  /**
   * 处理列状态变更
   */
  const handleOnChangeColumn = async (map: any) => {
    await recordUserModuleColumn({ moduleId, map });
    setColumnsStateMap(map);
    const updatedModuleInfo = { ...moduleInfo, UserModuleColumn: map };
    dispatch(setModuleInfo(updatedModuleInfo));
  };

  /**
   * 增强列配置
   */
  const enhancedColumns = useMemo(() => {
    if (!columns) return [];

    return columns.map((item: any) => {
      const columnEnhancements: any = {};

      // 处理 ComboGrid 类型
      if (!item.hideInSearch && item.fieldType === "ComboGrid") {
        columnEnhancements.renderFormItem = () => <ComboGrid code={item.dataSource} />;
      }

      // 处理标签显示
      if (item.isTagDisplay === true) {
        columnEnhancements.render = (_: any, record: any) => {
          const valueEnum = item.valueEnum?.[record[item.dataIndex]];
          if (valueEnum) {
            return (
              <Tag
                color={valueEnum.tagColor}
                icon={valueEnum.tagIcon ? <Icon name={valueEnum.tagIcon} /> : null}
                variant={valueEnum.tagVariant}
              >
                {valueEnum.text}
              </Tag>
            );
          }
          return record[item.dataIndex];
        };
      }

      // 根据 valueType 处理不同类型
      switch (item.valueType) {
        case "icon":
          columnEnhancements.render = (_: any, record: any) => {
            return record[item.dataIndex] ? <Icon name={record[item.dataIndex]} /> : "";
          };
          break;

        case "switch":
          columnEnhancements.render = (_: any, record: any) => {
            return <Switch disabled checked={record[item.dataIndex] === "true"} />;
          };
          break;

        case "fontColor":
          if (item.isFollowThemeColor === true) {
            columnEnhancements.renderText = (val: string) => <span style={{ color: "var(--hooks-colorPrimary)" }}>{val}</span>;
          } else if (item.color) {
            columnEnhancements.renderText = (val: string) => <span style={{ color: item.color }}>{val}</span>;
          }
          break;
      }

      // 如果有任何增强，合并到原列配置中
      return Object.keys(columnEnhancements).length > 0 ? { ...item, ...columnEnhancements } : item;
    });
  }, [columns]);

  return {
    enhancedColumns,
    columnsStateMap,
    handleOnChangeColumn
  };
};
