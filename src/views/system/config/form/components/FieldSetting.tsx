import { Select, Form } from "antd";

const FormItem = Form.Item;
const Option = Select.Option;

/**
 * 1.组件选择
 * 2.组件属性设置
 */
export default ({ field, compDatas, onDataChange }: any) => {
  return (
    <>
      <FormItem label="控件类型">
        <Select
          value={field.FieldType}
          onChange={(value, Option) => {
            let r = null;
            if (compDatas && compDatas.length > 0)
              r = compDatas.filter(function (s: any) {
                return s.ID === value;
              });
            onDataChange(value, Option, r);
          }}
        >
          {compDatas &&
            compDatas.length > 0 &&
            compDatas.map((item: any) => {
              return <Option key={item.component}>{item.label}</Option>;
            })}
        </Select>
      </FormItem>
    </>
  );
};
