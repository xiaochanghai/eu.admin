import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Checkbox, Form } from "antd";
import { getLovData } from "@/api/modules/module";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType, SmLovData } from "@/typings";

const FormItem = Form.Item;
type CheckboxGroupValue = React.ComponentProps<typeof Checkbox.Group>["value"];
type CheckboxValueType = NonNullable<CheckboxGroupValue>[number];

interface CheckboxFieldProps {
  field: FieldProps;
  disabled?: boolean;
  modifyType?: ModifyType;
  onChange?: (value: CheckboxValueType[], option?: any, record?: SmLovData[] | null) => void;
}

const parseCheckboxValue = (value?: FieldProps["DefaultValue"]): CheckboxValueType[] => {
  if (Array.isArray(value)) return value as CheckboxValueType[];
  if (value === undefined || value === null || value === "") return [];
  if (typeof value === "string") {
    return value
      .split(",")
      .map(item => item.trim())
      .filter(Boolean);
  }
  return [value as CheckboxValueType];
};

const CheckboxField: React.FC<CheckboxFieldProps> = ({ field, disabled = false, modifyType = ModifyType.Edit, onChange }) => {
  const { DefaultValue, DataIndex, Required, DataSource, Disabled, ModifyDisabled, FormTitle } = field;

  const [options, setOptions] = useState<SmLovData[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string>("");

  const isDisabled = useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);

  const initialValue = useMemo(() => parseCheckboxValue(DefaultValue), [DefaultValue]);

  useEffect(() => {
    const sourceId = DataSource ?? DataIndex;
    if (!sourceId) {
      setError("Checkbox data source is required");
      setOptions([]);
      return;
    }

    const fetchLovData = async () => {
      setLoading(true);
      setError("");
      try {
        const { Data } = await getLovData(sourceId);
        setOptions(Data || []);
      } catch (err) {
        console.error("Failed to load Checkbox data:", err);
        setError("Failed to load data");
        setOptions([]);
      } finally {
        setLoading(false);
      }
    };

    fetchLovData();
  }, [DataSource, DataIndex]);

  const validationRules = useMemo(
    () => [
      {
        validator: async (_: unknown, value?: CheckboxValueType[]) => {
          if (!Required || (Array.isArray(value) && value.length > 0)) return;
          throw new Error(`请选择${FormTitle}!`);
        }
      }
    ],
    [Required, FormTitle]
  );

  const handleChange = useCallback(
    (selectedValue: CheckboxValueType[]) => {
      const record = options.filter(item => selectedValue.includes(item.value as CheckboxValueType));
      onChange?.(selectedValue, undefined, record.length > 0 ? record : null);
    },
    [onChange, options]
  );

  return (
    <FormItem name={DataIndex} label={<FieldTitle {...field} />} rules={validationRules} initialValue={initialValue}>
      <Checkbox.Group disabled={isDisabled || !!error || loading} options={options} onChange={handleChange} />
    </FormItem>
  );
};

export default React.memo(CheckboxField);
