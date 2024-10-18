import { Select } from "antd";
import { useState, useEffect } from "react";
import { getLovData } from "@/api/modules/module";
import { SmLovData } from "@/api/interface/index";

const ComBoBox: React.FC<any> = props => {
  const [comboValue, setComboValue] = useState<string>("");
  const [options, setOptions] = useState<SmLovData[]>([]);
  let { onChange, defaultValue, id, value } = props;
  useEffect(() => {
    const GetLovData = async () => {
      let { Data } = await getLovData(id);
      setOptions(Data);
    };
    GetLovData();
    setComboValue(value);
  }, []);
  return (
    <Select
      allowClear
      value={comboValue ?? defaultValue ?? null}
      {...props}
      onChange={(value, Option) => {
        let r = null;
        if (options && options.length > 0)
          r = options.filter(function (s: any) {
            return s.value === value; // 注意：IE9以下的版本没有trim()方法
          });
        if (onChange) onChange(value, Option, r);
      }}
      options={options}
    />
  );
};

export default ComBoBox;
