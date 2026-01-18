import { Select, Form } from "antd";
import { Icon } from "@/components";

const FormItem = Form.Item;

/**
 * 1.组件选择
 * 2.组件属性设置
 */
export default ({ field, componentData, onDataChange }: any) => {
  const options = componentData?.map((item: any) => ({
    value: item.ID,
    label: (
      <>
        <Icon className="icon" name={item.icon} /> {item.label}
      </>
    )
  }));

  return (
    <>
      <FormItem label="控件类型">
        <Select
          value={field.FieldType}
          onChange={(value, option) => {
            const r = componentData?.find((s: any) => s.ID === value);
            onDataChange(value, option, r);
          }}
          options={options}
          allowClear
        />
      </FormItem>
    </>
  );
};
