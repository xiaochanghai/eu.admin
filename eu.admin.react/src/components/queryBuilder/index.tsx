import { Button, Space } from "antd";
import { DeleteOutlined, PlusOutlined } from "@ant-design/icons";
import { FormVo } from "@/api/Form";
import { ConditionGroup, where } from "@/dsl/base";
import { useMemo } from "react";
import Condition from "./component/Condition";

interface QueryBuilderProps {
  entityModel: FormVo;
  value: string;
  className?: string;
  style?: React.CSSProperties;
  onDataChange: (conditionJson: string) => void;
}

const emptyGroup = (): Partial<ConditionGroup> => ({ where: [{}] });

const QueryBuilder = ({ onDataChange, entityModel, className, style, value }: QueryBuilderProps) => {
  const groups = useMemo((): Partial<ConditionGroup>[] => {
    try {
      return value ? JSON.parse(value) : [emptyGroup()];
    } catch {
      return [emptyGroup()];
    }
  }, [value]);

  const updateGroups = (nextGroups: Partial<ConditionGroup>[]) => onDataChange(JSON.stringify(nextGroups));

  return (
    <div style={style} className={`${className ?? ""} space-y-3`}>
      {groups.map((group, groupIndex) => (
        <div key={`group-${groupIndex}`}>
          {groupIndex > 0 && <div className="py-2 text-center text-sm font-medium text-gray-500">或</div>}
          <div className="space-y-2 rounded border border-gray-200 bg-gray-50 p-3">
            <div className="text-xs text-gray-500">条件组 {groupIndex + 1}（以下条件同时满足）</div>
            {(group.where ?? []).map((condition, whereIndex) => (
              <div key={`where-${whereIndex}`} className="flex items-center gap-2">
                {whereIndex > 0 && <span className="w-4 text-center text-xs text-gray-500">且</span>}
                {whereIndex === 0 && <span className="w-4" />}
                <Condition
                  className="w-full"
                  where={condition}
                  formVo={entityModel}
                  onDataChange={(nextWhere: Partial<where>) => {
                    updateGroups(
                      groups.map((item, index) =>
                        index === groupIndex
                          ? { ...item, where: item.where?.map((current, index) => (index === whereIndex ? nextWhere : current)) }
                          : item
                      )
                    );
                  }}
                />
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  aria-label="删除条件"
                  onClick={() => {
                    const nextGroups = groups
                      .map((item, index) => (index === groupIndex ? { ...item, where: item.where?.filter((_, index) => index !== whereIndex) } : item))
                      .filter(item => item.where?.length);
                    updateGroups(nextGroups.length ? nextGroups : [emptyGroup()]);
                  }}
                />
              </div>
            ))}
            <Button
              type="dashed"
              icon={<PlusOutlined />}
              onClick={() => updateGroups(groups.map((item, index) => (index === groupIndex ? { ...item, where: [...(item.where ?? []), {}] } : item)))}
            >
              添加条件
            </Button>
          </div>
        </div>
      ))}
      <Space>
        <Button type="dashed" icon={<PlusOutlined />} onClick={() => updateGroups([...groups, emptyGroup()])}>
          添加条件组
        </Button>
        <span className="text-xs text-gray-500">条件组之间为“或”关系</span>
      </Space>
    </div>
  );
};

export default QueryBuilder;
