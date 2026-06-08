import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Form, Radio } from "antd";
import type { RadioChangeEvent } from "antd";
import { getLovData } from "@/api/modules/module";
import FieldTitle from "./FieldTitle";
import { FieldProps, ModifyType, SmLovData } from "@/typings";
import { RootState, useSelector } from "@/redux";
import { useTranslation } from "react-i18next";

const FormItem = Form.Item;

interface RadioFieldProps {
  field: FieldProps;
  disabled?: boolean;
  modifyType?: ModifyType;
  onChange?: (value: string, option?: any, record?: SmLovData[] | null) => void;
}

const RadioField: React.FC<RadioFieldProps> = ({ field, disabled = false, modifyType = ModifyType.Edit, onChange }) => {
  const { DefaultValue, DataIndex, Required, DataSource, Disabled, ModifyDisabled, FormTitle, FormTitle_EN } = field;
  const language = useSelector((state: RootState) => state.global.language);
  const formTitle = language === "en" ? FormTitle_EN || FormTitle : FormTitle;
  const { t } = useTranslation();

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
        if (mounted) {
          setOptions(Data || []);
        }
      } catch (error) {
        console.error(`Radio 获取数据失败 [${sourceId}]`, error);
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
        message: `${t("formOption.inputPlaceholder")}${formTitle ?? null}!`
      }
    ],
    [Required, formTitle]
  );

  const handleChange = useCallback(
    (e: RadioChangeEvent) => {
      const value = e.target.value;
      const record = options.filter(item => item.value === value);
      onChange?.(value, e.target, record.length > 0 ? record : null);
    },
    [onChange, options]
  );
  return (
    <FormItem name={DataIndex} label={<FieldTitle {...field} />} rules={validationRules} initialValue={DefaultValue ?? undefined}>
      <Radio.Group disabled={isDisabled || loading} options={options} optionType="default" onChange={handleChange} />
    </FormItem>
  );
};

export default React.memo(RadioField);
