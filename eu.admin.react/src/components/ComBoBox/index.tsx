import { Select, SelectProps } from "antd";
import { useState, useEffect, useMemo, useCallback } from "react";
import { getLovData } from "@/api/modules/module";
import { SmLovData } from "@/typings";

interface ComBoBoxProps extends Omit<SelectProps, "onChange"> {
  id: string;
  value?: string | number;
  defaultValue?: string | number;
  onChange?: (value: any, option: any, record?: SmLovData[]) => void;
}

const ComBoBox: React.FC<ComBoBoxProps> = props => {
  const { onChange, defaultValue, id, value, ...restProps } = props;

  const [comboValue, setComboValue] = useState<string | number | undefined>(value);
  const [options, setOptions] = useState<SmLovData[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>("");

  // 获取 LOV 数据
  useEffect(() => {
    if (!id) {
      setError("ComBoBox id is required");
      return;
    }

    const fetchLovData = async () => {
      setLoading(true);
      setError("");
      try {
        const { Data } = await getLovData(id);
        setOptions(Data || []);
      } catch (err) {
        console.error("Failed to load ComBoBox data:", err);
        setError("Failed to load data");
        setOptions([]);
      } finally {
        setLoading(false);
      }
    };

    fetchLovData();
  }, [id]);

  // 同步外部 value 变化
  useEffect(() => {
    setComboValue(value);
  }, [value]);

  // 使用 useMemo 缓存过滤逻辑
  const findOptionRecord = useMemo(() => {
    return (selectedValue: any): SmLovData[] | undefined => {
      if (!options || options.length === 0 || selectedValue == null) {
        return undefined;
      }
      return options.filter((item: SmLovData) => item.value === selectedValue);
    };
  }, [options]);

  // 使用 useCallback 优化 onChange 处理
  const handleChange = useCallback(
    (selectedValue: any, option: any) => {
      setComboValue(selectedValue);

      if (onChange) {
        const record = findOptionRecord(selectedValue);
        onChange(selectedValue, option, record);
      }
    },
    [onChange, findOptionRecord]
  );

  return (
    <Select
      allowClear
      loading={loading}
      value={comboValue ?? defaultValue ?? undefined}
      placeholder={error || restProps.placeholder || "请选择"}
      disabled={!!error || restProps.disabled}
      status={error ? "error" : undefined}
      {...restProps}
      onChange={handleChange}
      options={options}
    />
  );
};

export default ComBoBox;
