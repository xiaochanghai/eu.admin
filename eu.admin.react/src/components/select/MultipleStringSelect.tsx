import { Select } from "antd";
import { VfBaseProps } from "@/dsl/component";

/**
 * string类型字段的多选
 * 逗号分隔
 */
export interface MultipleTreeSelectProps extends VfBaseProps<string> {
  optionList: any[];
}

export default (props: MultipleTreeSelectProps) => {
  return (
    <Select
      allowClear={true}
      // filter={true}
      onClear={() => {
        props.onDataChange("");
      }}
      value={props.value && props.value?.length > 0 ? props.value?.split(",") : undefined}
      // multiple
      placeholder={"请选择"}
      // zIndex={1000}
      options={props.optionList}
      onChange={(value: any) => {
        if (typeof value === "string") {
          props.onDataChange(value);
        } else if (typeof value === "object") {
          value = (value as Array<string>)?.join(",");
          props.onDataChange(value);
        }
      }}
    />
  );
};
