import { ReactNode, useCallback, useMemo, useState } from "react";
import { Input, Tabs, Form, Select, Switch, Tooltip, InputNumber, Radio, ColorPicker, Modal, Button, Space, Skeleton, Row, Col } from "antd";
import FieldSetting from "./FieldSetting";
import { FormComponents } from "./CompDatas";
import { Mode, FormFieldVo } from "./dsl/base";
import { ComboGrid, Icon } from "@/components";
import schemaDef, { deps, SchemaClz, formTypes, listTypes } from "./fieldSettingSchema";
import { getColumnLanguageConfig, addLanguageConfig, updateLanguageConfig } from "@/api/modules/smLanguageConfig";
import { message } from "@/hooks/useMessage";

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

  /** 检查依赖项是否满足（AND逻辑：数组需全部满足） */
  const check = useCallback(
    (_fieldName: string, dd: deps | deps[] | undefined): boolean => {
      if (dd === undefined) return true;
      const arr = Array.isArray(dd) ? dd : [dd];
      return arr.every(a => {
        if (a.notEmpty) {
          const v = field[a.field];
          return v !== null && v !== undefined && v !== "";
        }
        return a.value!.includes(field[a.field]);
      });
    },
    [field]
  );

  /** 检查依赖项是否满足（OR逻辑：满足数组中任意一项即可） */
  const checkAnyOf = useCallback(
    (_fieldName: string, dd: deps[] | undefined): boolean => {
      if (dd === undefined || dd.length === 0) return true;
      return dd.some(a => {
        if (a.notEmpty) {
          const v = field[a.field];
          return v !== null && v !== undefined && v !== "";
        }
        return a.value!.includes(field[a.field]);
      });
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

  // 多语配置状态
  const [langForm] = Form.useForm();
  const [langModalOpen, setLangModalOpen] = useState(false);
  const [langLoading, setLangLoading] = useState(false);
  const [currentRefField, setCurrentRefField] = useState<string>("");

  /** 打开多语弹框 */
  const openLangModal = useCallback(async (refField: string) => {
    if (!field?.ID) return;
    setCurrentRefField(refField);
    setLangModalOpen(true);
    setLangLoading(true);
    try {
      const { Data, Success } = await getColumnLanguageConfig(field.ID, refField);
      if (Success && Data) {
        const langValues: Record<string, any> = {};
        langValues.value_EN = Data.Value_EN || "";
        langValues.value_ZH = Data.Value_ZH || "";
        langValues.remark = Data.Remark || "";
        langValues.id = Data.Id || Data.ID || "";
        langForm.setFieldsValue(langValues);
      }
    } catch (error) {
      console.error("获取栏位多语配置失败:", error);
    }
    setLangLoading(false);
  }, [field, langForm]);

  /** 保存多语配置 */
  const saveLangConfig = useCallback(async () => {
    if (!field?.ID || !currentRefField) return;
    try {
      const values = langForm.getFieldsValue();
      const valueZH = field[currentRefField] || "";
      const config: Record<string, any> = {
        Id: values.id || undefined,
        RefId: field.ID,
        RefType: "ModuleColumn",
        RefField: currentRefField,
        Value_ZH: valueZH,
        Value_EN: values.value_EN || "",
        Remark: values.remark || undefined,
      };
      const { Success, Message } = config.Id
        ? await updateLanguageConfig({ ...config, Id: config.Id })
        : await addLanguageConfig(config);
      if (Success) {
        message.success("多语配置保存成功");
        setLangModalOpen(false);
      } else {
        message.error(Message || "保存失败");
      }
    } catch (error) {
      console.error("保存多语配置失败:", error);
      message.error("保存多语配置失败");
    }
  }, [field, currentRefField, langForm]);

  /** 检查字段配置是否满足显示条件 */
  const shouldShowField = useCallback(
    (fieldConf: SchemaClz[string]): boolean => {
      const { deps: fieldDeps, anyOfDeps: fieldAnyOfDeps, mode: fieldMode } = fieldConf;

      // 检查模式
      let modeOk = true;
      if (fieldMode) {
        const modes = Array.isArray(fieldMode) ? fieldMode : [fieldMode];
        modeOk = modes.includes(mode);
      }
      // 检查 deps（AND逻辑：数组需全部满足）
      const depsOk = !fieldDeps || check("", fieldDeps);
      // 检查 anyOfDeps（OR逻辑：满足任意一项即可）
      const anyOfOk = !fieldAnyOfDeps || fieldAnyOfDeps.length === 0 || checkAnyOf("", fieldAnyOfDeps);

      return modeOk && depsOk && anyOfOk;
    },
    [mode, check, checkAnyOf]
  );

  /** 检查选项是否满足显示条件 */
  const shouldShowOption = useCallback(
    (item: any): boolean => {
      const { deps, anyOfDeps, mode: itemMode } = item;

      // 检查模式
      const modeOk = !itemMode || mode === itemMode;
      // 检查 deps（AND逻辑）
      const depsOk = !deps || check("", deps);
      // 检查 anyOfDeps（OR逻辑）
      const anyOfOk = !anyOfDeps || anyOfDeps.length === 0 || checkAnyOf("", anyOfDeps);

      return modeOk && depsOk && anyOfOk;
    },
    [mode, check, checkAnyOf]
  );

  const render = useCallback(
    (key: string): ReactNode => {
      const fieldConf = fieldsConf[key];

      if (!shouldShowField(fieldConf)) return null;

      switch (fieldConf.type) {
        case "select": {
          const optionList = fieldConf.items;
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

        case "langConfig":
          return (
            <Button type="default" block icon={<Icon name="GlobalOutlined" />} onClick={() => openLangModal(fieldConf.comboGridCode ?? "")} style={{ width: "100%" }}>
              多语设置
            </Button>
          );

        default:
          return null;
      }
    },
    [fieldsConf, field, shouldShowField, shouldShowOption, handleFieldChange, openLangModal]
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

      {/* 多语配置弹框 */}
      <Modal
        title={`多语设置 - ${field[currentRefField] || currentRefField}`}
        open={langModalOpen}
        onCancel={() => setLangModalOpen(false)}
        destroyOnHidden
        width={600}
        footer={null}
      >
        {langLoading ? (
          <Skeleton active />
        ) : (
          <Form form={langForm} labelCol={{ span: 6 }} wrapperCol={{ span: 16 }} style={{ marginTop: 16 }}>
            <Form.Item label="id" name="id" hidden>
              <Input />
            </Form.Item>
            <Row gutter={24}>
              <Col span={24}>
                <Form.Item label="中文值" name="value_ZH">
                  <Input disabled />
                </Form.Item>
              </Col>
            </Row>
            <Row gutter={24}>
              <Col span={24}>
                <Form.Item label="English" name="value_EN">
                  <Input placeholder="请输入英文翻译" />
                </Form.Item>
              </Col>
            </Row>
            <Row gutter={24}>
              <Col span={24}>
                <Form.Item label="备注" name="remark">
                  <Input.TextArea placeholder="请输入备注" autoSize={{ minRows: 1 }} />
                </Form.Item>
              </Col>
            </Row>

            {/* 底部按钮 */}
            <Row>
              <Col span={24}>
                <Space style={{ display: "flex", justifyContent: "center", marginTop: 16 }}>
                  <Button onClick={() => setLangModalOpen(false)}>取消</Button>
                  <Button type="primary" icon={<Icon name="SaveOutlined" />} onClick={saveLangConfig}>
                    确认
                  </Button>
                </Space>
              </Col>
            </Row>
          </Form>
        )}
      </Modal>
    </div>
  );
};

export default SiderSetting;
