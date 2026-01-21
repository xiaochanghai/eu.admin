import { ReactNode, useCallback, useMemo } from "react";
import { Input, Tabs, Form, Select, Switch, Tooltip, InputNumber, Radio, ColorPicker } from "antd";
import FieldSetting from "./FieldSetting";
import { FormComponents } from "./CompDatas";
import { Mode, FormFieldVo } from "./dsl/base";
import { ComboGrid, Icon } from "@/components";
import schemaDef, { deps, SchemaClz, formTypes, listTypes } from "./fieldSettingSchema";

const { TextArea } = Input;
const FormItem = Form.Item;

// 类型定义

interface FormVo {
  formTabDtos?: Array<{ code: string; name: string }>;
  modelSize?: number;
  fields?: any[];
  [key: string]: any;
}

interface SiderSettingProps {
  mode: Mode;
  field: FormFieldVo;
  form: FormVo;
  onDataChange: (field: FormFieldVo) => void;
}

const SiderSetting = ({ field, form, onDataChange, mode }: SiderSettingProps) => {
  /**
   *  对字段属性的字典，和列宽,所在容器进行计算和提取在这里完成组装
   */
  const fieldsConf = useMemo((): SchemaClz => {
    // 动态加入相关内容 - 创建副本避免修改原始对象
    const schemaDefCopy = { ...schemaDef };
    schemaDefCopy["formTabCode"] = { ...schemaDefCopy["formTabCode"], items: [] };

    if (form.formTabDtos && form.formTabDtos.length > 0) {
      form.formTabDtos.forEach(tab => {
        schemaDefCopy["formTabCode"].items?.push({
          value: tab.code,
          label: tab.name || ""
        });
      });
    }

    const modelSize = form.modelSize ?? 4;
    const gridSpanMap: Record<string, number> =
      modelSize === 4
        ? { "25": 25, "33": 33, "50": 50, "75": 75, "100": 100 }
        : modelSize === 3
          ? { "33": 1, "66": 2, "100": 3 }
          : modelSize === 2
            ? { "50": 1, "100": 2 }
            : { "100": 1 };

    schemaDefCopy["GridSpan"] = {
      ...schemaDefCopy["GridSpan"],
      items: Object.entries(gridSpanMap).map(([label, value]) => ({
        value,
        label,
        mode: Mode.form,
        tooltip: `一行${modelSize}列占${value}列`
      }))
    };

    return schemaDefCopy;
  }, [form.formTabDtos, form.modelSize]);

  const types = mode === Mode.list ? listTypes : formTypes;

  /** 检查依赖项是否满足 */
  const check = useCallback(
    (_fieldName: string, dd: deps | deps[] | undefined): boolean => {
      if (dd === undefined) return true;
      const arr = Array.isArray(dd) ? dd : [dd];
      return arr.every(a => a.value.includes(field[a.field]));
    },
    [field]
  );

  /** 通用数据更新处理器 */
  const handleFieldChange = useCallback(
    (key: string, value: any) => {
      onDataChange({ ...field, [key]: value });
    },
    [field, onDataChange]
  );

  /** 检查字段配置是否满足显示条件 */
  const shouldShowField = useCallback(
    (fieldConf: SchemaClz[string]): boolean => {
      const { deps: fieldDeps, mode: fieldMode } = fieldConf;

      // 无依赖且无模式限制
      if (!fieldDeps && !fieldMode) return true;

      // 同时满足模式和依赖
      if (fieldMode && fieldDeps) {
        return fieldMode === mode && check("", fieldDeps);
      }

      // 仅满足模式
      if (fieldMode && !fieldDeps) {
        return fieldMode === mode;
      }

      // 仅满足依赖
      if (!fieldMode && fieldDeps) {
        return check("", fieldDeps);
      }

      return false;
    },
    [mode, check]
  );

  /** 检查选项是否满足显示条件 */
  const shouldShowOption = useCallback(
    (item: any): boolean => {
      if (!item.deps && !item.mode) return true;
      if (item.deps && item.mode) return check("", item.deps) && mode === item.mode;
      if (item.deps && !item.mode) return check("", item.deps);
      if (!item.deps && item.mode) return mode === item.mode;
      return false;
    },
    [mode, check]
  );

  const render = useCallback(
    (key: string): ReactNode => {
      const fieldConf = fieldsConf[key];

      if (!shouldShowField(fieldConf)) return null;

      switch (fieldConf.type) {
        case "select": {
          const optionList = fieldConf.items?.filter(shouldShowOption);
          if (optionList && optionList.length > 0) {
            return (
              <Select
                allowClear
                key={`select_${key}`}
                value={field[key]}
                onChange={value => handleFieldChange(key, value)}
                style={{ width: "100%" }}
                options={optionList.filter(Boolean)}
              />
            );
          }
          return null;
        }

        case "input":
          return (
            <Input
              key={`input_${key}`}
              value={field[key]}
              style={{ width: "100%" }}
              onChange={e => handleFieldChange(key, e.target.value)}
            />
          );

        case "inputNumber":
          return (
            <InputNumber
              key={`inputNumber_${key}`}
              value={field[key]}
              style={{ width: "100%" }}
              onChange={value => handleFieldChange(key, value)}
            />
          );

        case "textArea":
          return (
            <TextArea
              key={`textArea_${key}`}
              value={field[key]}
              style={{ width: "100%" }}
              onChange={e => handleFieldChange(key, e.target.value)}
            />
          );

        case "switch":
          return <Switch checked={field[key]} onChange={value => handleFieldChange(key, value)} key={`switch_${key}`} />;

        case "colorPicker":
          return (
            <ColorPicker
              defaultValue={field[key]}
              defaultFormat="hex"
              showText
              onChange={(_val: any, css: string) => handleFieldChange(key, css)}
            />
          );

        case "comboGrid":
          return (
            <ComboGrid value={field[key]} code={fieldConf.comboGridCode} onChange={(val: any) => handleFieldChange(key, val)} />
          );

        case "buttonGroup": {
          const items = fieldConf?.items;
          if (!items || items.length === 0) return null;
          return (
            <Radio.Group value={field[key]} buttonStyle="solid" onChange={e => handleFieldChange(key, e.target.value)}>
              {items?.map((item, index) => (
                <Radio.Button key={`subButton_${key}_${index}`} value={item.value}>
                  {item.label}
                </Radio.Button>
              ))}
            </Radio.Group>
          );
        }

        default:
          return null;
      }
    },
    [fieldsConf, field, shouldShowField, shouldShowOption, handleFieldChange]
  );

  return (
    <div>
      {mode === Mode.form ? (
        <div
          style={{
            fontSize: "14px",
            borderStyle: "dotted solid dashed solid",
            borderColor: "#cccccc",
            marginTop: 10,
            paddingTop: 10,
            paddingBottom: 10
          }}
        >
          {field ? (
            <div>
              <b style={{ font: "14px" }}>
                &nbsp;&nbsp;&nbsp;&nbsp;标识/模型：{field.DataIndex}
                {field.FieldType ? "/" : null}
                {field.FieldType}
              </b>
            </div>
          ) : (
            "请选择一个表单元素"
          )}
        </div>
      ) : null}
      <Tabs
        defaultActiveKey="panel_0"
        items={types.map((t, index) => ({
          key: `panel_${index}`,
          label: (
            <span>
              <t.icon />
              {t.title}
            </span>
          ),
          disabled: mode === Mode.list && t.value === "layout",
          children: (
            <>
              {index === 0 && mode === Mode.form && (
                <FieldSetting
                  field={field}
                  componentData={FormComponents}
                  onDataChange={(data: any) => {
                    onDataChange({
                      ...field,
                      FieldType: data
                    });
                  }}
                />
              )}
              {Object.keys(fieldsConf)
                .filter(key => fieldsConf[key].tag === t.value)
                .map(key => {
                  const RenderObj = render(key);
                  return (
                    RenderObj && (
                      <div key={key} style={{ marginTop: 10 }}>
                        <FormItem
                          label={
                            <label>
                              {fieldsConf[key].name}{" "}
                              {fieldsConf[key].tooltip && (
                                <Tooltip title={fieldsConf[key].tooltip}>
                                  <span>
                                    <Icon name="ExclamationCircleOutlined" />
                                  </span>
                                </Tooltip>
                              )}
                            </label>
                          }
                        >
                          {RenderObj}
                        </FormItem>
                      </div>
                    )
                  );
                })}
            </>
          )
        }))}
      />
    </div>
  );
};

export default SiderSetting;
