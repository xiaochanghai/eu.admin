import { useEffect, useState } from "react";
import { Tree } from "antd";
import http from "@/api";

interface FieldSelectProps {
  className?: string;
  fields: any[]; //参与排序的字段
  currentField?: string; //外部选中字段
  onDataChange: (ang: any[]) => void; //数据返回出去
  onSelect: (field: string) => void; //当前选中字段
}
// const FieldSelect = ({ fields, mode, onSelect, className, onDataChange }: FieldSelectProps) => {
const FieldSelect = ({ className }: FieldSelectProps) => {
  const [treeData, setTreeData] = useState<any>([]);

  useEffect(() => {
    const getAllModuleList = async () => {
      let { Data, Success } = await http.get<any>("/api/SmModule/QueryAllModuleList");
      if (Success) setTreeData([Data]);
    };
    getAllModuleList();
  }, []);

  return (
    <div className={`${className}  bottom-1 border-black p-4`}>
      <Tree
        defaultExpandedKeys={["All"]}
        // defaultExpandParent={true}
        // checkedKeys={checkedModuleKeys}
        // onCheck={onModuleCheck}
        // checkable
        // onSelect={selectModule}
        treeData={treeData}
      />
    </div>
  );
};

export default FieldSelect;
