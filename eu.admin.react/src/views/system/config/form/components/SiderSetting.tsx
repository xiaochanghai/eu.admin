import { ReactNode, useCallback, useMemo } from "react";
import { Input, Tabs, Form, Select, Switch, Tooltip, InputNumber, Radio } from "antd";
import { Icon } from "@/components/Icon";
import FieldSetting from "./FieldSetting";
import { FormComponents } from "./CompDatas";
import { Mode } from "./dsl/base";
import ComboGrid from "@/components/ComBoGrid/index";

const { TextArea } = Input;
const { TabPane } = Tabs;
const FormItem = Form.Item;
import schemaDef, { deps, SchemaClz, types } from "./fieldSettingSchema";

interface SiderSettingProps {
  mode: Mode; //当前组件场景
  // field: FormFieldVo; //
  field: any; //
  // form: FormVo;
  form: any;
  onDataChange: (field: any) => void;
  // onDataChange: (field: FormFieldVo) => void;
}

const SiderSetting = ({ field, form, onDataChange, mode }: SiderSettingProps) => {
  /**
   *  对字段属性的字典，和列宽,所在容器进行计算和提取在这里完成组装
   */
  const fieldsConf = useMemo((): SchemaClz => {
    //动态加入相关内容
    schemaDef["formTabCode"].items = [];
    if (form.formTabDtos && form.formTabDtos.length > 0) {
      form.formTabDtos.forEach((tab: any) => {
        schemaDef["formTabCode"].items?.push({
          value: tab.code,
          label: tab.name || ""
        });
      });
    }
    form = { ...form, modelSize: 4 };
    if (form.modelSize) {
      const obj: { [key: string]: number } =
        form.modelSize + "" === "4"
          ? { "25": 25, "50": 50, "75": 75, "100": 100 }
          : form.modelSize + "" === "3"
            ? { "33": 1, "66": 2, "100": 3 }
            : form.modelSize + "" === "2"
              ? { "50": 1, "100": 2 }
              : { "100": 1 };

      schemaDef["GridSpan"].items = Object.keys(obj).map((key: string) => {
        return {
          value: obj[key],
          label: key,
          tooltip: `一行${form.modelSize}列占${obj[key]}列`
        };
      });
    }
    return schemaDef;
  }, [JSON.stringify(form)]);

  /** 设置的属性是否能够展示进行检查，依赖的项目是否满足 */
  const check = useCallback(
    (_fieldName: string, dd: deps | deps[] | undefined): boolean => {
      if (dd === undefined) return true;
      let arr: deps[] = [];
      if (dd instanceof Array) {
        arr = dd;
      } else {
        arr.push(dd);
      }
      let back: boolean = false;
      back =
        //大类必须都满足
        arr.filter(a => a.value.includes(field[a.field])).length === arr.length;
      return back;
    },
    [field]
  );

  const render = useCallback(
    (key: string): ReactNode => {
      if (
        //1无依赖值无UI场景，2有依赖有ui都满足 3满足ui场景 4满足字段依赖
        (fieldsConf[key].deps === undefined && fieldsConf[key].mode === undefined) ||
        (fieldsConf[key].mode &&
          fieldsConf[key].mode === mode && //场景满足
          fieldsConf[key].deps &&
          check(key, fieldsConf[key].deps)) ||
        (fieldsConf[key].mode && fieldsConf[key].deps == undefined && fieldsConf[key].mode === mode) ||
        (fieldsConf[key].deps && fieldsConf[key].mode == undefined && check(key, fieldsConf[key].deps))
        // fieldsConf[key].deps?.value.includes(
        //   field[fieldsConf[key].deps?.field || ""]
        // )
      ) {
        if (fieldsConf[key].type === "select") {
          const optionList = fieldsConf[key].items?.filter(item => {
            if (
              //选择项的显示根据依赖值是否满足来完成进一步的展示
              (item.deps === undefined && item.mode === undefined) ||
              (item.deps && check(key, item.deps) && item.mode && mode === item.mode) ||
              (item.deps && item.mode === undefined && check(key, item.deps)) ||
              (item.mode && item.deps === undefined && mode === item.mode)
            ) {
              return true;
            }
            return false;
          });
          if (optionList && optionList?.length > 0) {
            return (
              <Select
                allowClear
                key={"select_" + key}
                // field={key}
                value={field[key]}
                onChange={d => {
                  onDataChange({
                    ...field,
                    [key]: d
                  });
                }}
                style={{ width: "100%" }}
                options={optionList.filter(d => d !== undefined)}
              ></Select>
            );
          }
        }
        if (fieldsConf[key].type === "input") {
          return (
            <Input
              key={"input_" + key}
              value={field[key]}
              style={{ width: "100%" }}
              onChange={value => {
                onDataChange({
                  ...field,
                  [key]: value.target.value
                });
              }}
            ></Input>
          );
        }
        if (fieldsConf[key].type === "inputNumber") {
          return (
            <InputNumber
              key={"inputNumber_" + key}
              value={field[key]}
              style={{ width: "100%" }}
              onChange={value => {
                onDataChange({
                  ...field,
                  [key]: value
                });
              }}
            ></InputNumber>
          );
        }
        if (fieldsConf[key].type === "textArea") {
          return (
            <TextArea
              key={"textArea_" + key}
              value={field[key]}
              style={{ width: "100%" }}
              onChange={value => {
                onDataChange({
                  ...field,
                  [key]: value.target.value
                });
              }}
            ></TextArea>
          );
        }
        if (fieldsConf[key].type === "switch") {
          return (
            <Switch
              checked={field[key]}
              onChange={val => {
                onDataChange({
                  ...field,
                  [key]: val
                });
              }}
              key={"switch" + key}
            ></Switch>
          );
        }
        if (fieldsConf[key].type === "comboGrid") {
          return <ComboGrid value={field[key]} code={fieldsConf[key].comboGridCode} />;
        }
        const items = fieldsConf[key]?.items;
        if (fieldsConf[key].type === "buttonGroup" && items !== undefined && items.length > 0) {
          return (
            <Radio.Group
              value={field[key]}
              buttonStyle="solid"
              onChange={e => {
                onDataChange({
                  ...field,
                  [key]: e.target.value
                });
              }}
            >
              {items?.map((item, index) => {
                return (
                  <Radio.Button
                    key={`subButton_${key + index}`}
                    type={item.value + "" === field[key] + "" ? "primary" : `tertiary`}
                    value={item.value}
                    onClick={() => {
                      onDataChange({
                        ...field,
                        [key]: item.value
                      });
                    }}
                  >
                    {item.label}
                  </Radio.Button>
                );
              })}
            </Radio.Group>
          );
        }
        //类型时表单，默认值使用他，目前存在部分类型组件可以不需要默认值。
        // if (fieldsConf[key].type === "form" && field && field.fieldName !== undefined) {
        //   return (
        //     <Form
        //       modelInfo={{
        //         ...form,
        //         modelSize: 1,
        //         fields: form.fields.map(f => {
        //           return { ...f, title: "" };
        //         })
        //       }}
        //       fieldMode={field.fieldName}
        //       formData={{ [field.fieldName]: field[key] }}
        //       onDataChange={d => {
        //         onDataChange({
        //           ...field,
        //           [key]: d[field.fieldName + ""]
        //         });
        //       }}
        //     />
        //   );
        // }
      }
    },
    [fieldsConf, field]
  );

  return (
    <div>
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
      <Tabs defaultActiveKey={"panel_0"}>
        {types.map((t, index) => {
          return (
            <TabPane key={"panel_" + index} tab={t.title} icon={<t.icon></t.icon>} style={{ padding: "2px" }}>
              {/* 第一个panel设置组件 */}
              {index === 0 && (
                <FieldSetting
                  field={field}
                  compDatas={FormComponents}
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
                  const RenderObj = render(key); //待渲染的数据
                  return (
                    RenderObj && (
                      <div key={key} style={{ marginTop: 10 }}>
                        <FormItem
                          label={
                            <label>
                              {fieldsConf[key].name}{" "}
                              {fieldsConf[key].tooltip && (
                                <Tooltip title={fieldsConf[key].tooltip}>
                                  <Icon name="ExclamationCircleOutlined" />
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
            </TabPane>
          );
        })}
      </Tabs>
    </div>
  );
};

export default SiderSetting;
