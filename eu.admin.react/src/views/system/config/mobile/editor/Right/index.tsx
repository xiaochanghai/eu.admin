import { traverse } from "@/utils";
import fields from "./fields";
import editFields from "../schema/edit";
import { updateTree, MobileNodeSchema } from "@/redux/modules/mobileEditor";
import { useDispatch, RootState, useSelector } from "@/redux";
import { MobileEditField, EditFieldType } from "../schema/types";

export default function Right() {
  const state = useSelector((s: RootState) => s.mobileEditor);
  const dispatch = useDispatch();

  // 查找当前选中的节点
  let focusComponent: MobileNodeSchema | undefined;
  traverse(state, sub => {
    if (sub.id === state.focusId) {
      focusComponent = sub;
      return false;
    }
    return true;
  });

  const handleChange = (value: any, key: string) => {
    dispatch(updateTree({ key, value }));
  };

  const renderField = (item: MobileEditField) => {
    const { key, name, type, ...rest } = item;
    const fieldType = type as EditFieldType;
    const Field = fields[fieldType];
    if (!Field) return null;

    // OptionEditor 特殊处理
    if (fieldType === "OptionEditor") {
      return (
        <div key={key} className="mb-2">
          <div className="text-xs text-gray-500 mb-1">{name}</div>
          <Field
            value={focusComponent?.props[key]}
            onChange={(val: any) => handleChange(val, key)}
          />
        </div>
      );
    }

    // Select 组件
    if (fieldType === "Select") {
      return (
        <div key={key} className="mb-2">
          <div className="text-xs text-gray-500 mb-1">{name}</div>
          <Field
            style={{ width: "100%" }}
            options={rest.options}
            value={focusComponent?.props[key]}
            onChange={(val: any) => handleChange(val, key)}
          />
        </div>
      );
    }

    // 其他组件
    return (
      <div key={key} className="mb-2">
        <div className="text-xs text-gray-500 mb-1">{name}</div>
        <Field
          {...rest}
          value={focusComponent?.props[key]}
          onChange={(e: any) => {
            const val = e?.target ? e.target.value : e;
            handleChange(val, key);
          }}
        />
      </div>
    );
  };

  const editDefs = focusComponent ? editFields[focusComponent.type] : null;

  return (
    <div className="w-72 overflow-y-auto border-l border-gray-200 bg-white flex flex-col">
      <div className="flex-shrink-0 h-10 leading-10 px-3 text-blue-600 border-b border-gray-200 font-medium text-sm">
        属性设置
      </div>
      <div className="p-3 flex-1">
        {focusComponent && editDefs ? (
          <>
            <div className="text-xs text-gray-400 mb-3 pb-2 border-b border-gray-100">
              组件类型: <span className="text-blue-600 font-medium">{focusComponent.type}</span>
            </div>
            {editDefs.map(item => renderField(item))}
          </>
        ) : (
          <div className="flex justify-center items-center h-48 text-gray-300 text-sm">
            请在画布中选择组件
          </div>
        )}
      </div>
    </div>
  );
}
