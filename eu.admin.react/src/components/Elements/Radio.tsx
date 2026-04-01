import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Form, Radio } from "antd";
import type { RadioChangeEvent } from "antd";
import { getLovData } from "@/api/modules/module";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType, SmLovData } from "@/typings";

const FormItem = Form.Item;

interface RadioFieldProps {
  field: FieldProps;
  disabled?: boolean;
  modifyType?: ModifyType;
  onChange?: (value: string, option?: any, record?: SmLovData[] | null) => void;
}

const RadioField: React.FC<RadioFieldProps> = ({ field, disabled = false, modifyType = ModifyType.Edit, onChange }) => {
  const { DefaultValue, DataIndex, Required, DataSource, Disabled, ModifyDisabled, FormTitle } = field;

  const [options, setOptions] = useState<SmLovData[]>([]);
  const [loading, setLoading] = useState(false);

  const isDisabled = useMemo(() => {
    return (modifyType === ModifyType.Edit && ModifyDisabled) || modifyType === ModifyType.View || Disabled || disabled;
  }, [modifyType, ModifyDisabled, Disabled, disabled]);

  useEffect(() => {
    const sourceId = DataSource ?? DataIndex;
    if (!sourceId) {
      setOptions([]);
      return;
    }

    let mounted = true;

    const fetchLovData = async () => {
      setLoading(true);
      try {
        const { Data } = await getLovData(sourceId);
        debugger
        if (mounted) {
          setOptions(Data || []);
        }
      } catch (error) {
        console.error(`Radio ���ݼ���ʧ�� [${sourceId}]`, error);
        if (mounted) {
          setOptions([]);
        }
      } finally {
        if (mounted) {
          setLoading(false);
        }
      }
    };

    fetchLovData();

    return () => {
      mounted = false;
    };
  }, [DataSource, DataIndex]);

  const validationRules = useMemo(
    () => [
      {
        required: Required ?? false,
        message: `请输入${FormTitle ?? "该字段"}!`
      }
    ],
    [Required, FormTitle]
  );

  const handleChange = useCallback(
    (e: RadioChangeEvent) => {
      const value = e.target.value;
      const record = options.filter(item => item.value === value);
      onChange?.(value, e.target, record.length > 0 ? record : null);
    },
    [onChange, options]
  );
  debugger
  return (
    <FormItem name={DataIndex} label={<FieldTitle {...field} />} rules={validationRules} initialValue={DefaultValue ?? undefined}>
      <Radio.Group disabled={isDisabled || loading} options={options} optionType="default" onChange={handleChange} />
    </FormItem>
  );
};

export default React.memo(RadioField);
