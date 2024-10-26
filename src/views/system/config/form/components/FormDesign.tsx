import React, { useState, useEffect } from "react";
import FieldSelect from "./FieldSelect";
import SiderSetting from "./SiderSetting";
import FormPage from "./FormPage";
import { Mode } from "./dsl/base";
import { FormSetDiv } from "./style";
import { Col, Row, Button } from "antd";
import http from "@/api";

const Index: React.FC = () => {
  // let [fieldList, setFieldList] = useState([]);
  let [currentField, setCurrentField] = useState<any>(null);
  let [columns, setColumns] = useState<any[]>([]);
  useEffect(() => {
    const getModuleInfo1 = async () => {
      let { Data } = await http.get<any>(`/api/SmModule/FormColumn/SD_SALES_ORDER_MNG`);
      setColumns(Data);
    };
    getModuleInfo1();
  }, []);

  let currModel: any = {
    id: "4028b8818cec22b7018cec22cdc1004b",
    title: "产品",
    fields: [
      {
        fieldKey: "Input",
        id: "4028b8818d19bc25018d19bc407f0000",
        title: "主键id",
        fieldName: "id",
        x_hidden: true,
        sort: 0
      },
      {
        id: "4028b8818cec22b7018cec22cdc1004d",
        fieldKey: "Input",
        title: "产品名称",
        fieldName: "name",
        x_hidden: false,
        sort: 1,
        x_component: "Input"
      },
      {
        fieldKey: "Input",
        id: "4028b8818cec22b7018cec22cdc1004c",
        title: "产品编号",
        fieldName: "productNo",
        x_hidden: false,
        sort: 2
      },
      {
        id: "4028b8818cec22b7018cec22cdc1004f",
        title: "规格型号",
        fieldKey: "Input",

        fieldName: "xh",
        x_hidden: false,
        sort: 3
      },
      {
        id: "4028b8818cec22b7018cec22cdc20050",
        title: "产品品牌",
        fieldKey: "Input",
        fieldName: "brand",
        x_hidden: false,
        sort: 4
      },
      {
        id: "4028b8818cec22b7018cec22cdc20053",
        fieldKey: "Input",
        title: "计量单位",
        fieldName: "unit",
        x_hidden: false,
        sort: 5
      },
      {
        id: "4028b8818cec22b7018cec22cdc20055",
        fieldKey: "Input",
        title: "产品分类",
        fieldName: "type",
        x_hidden: false,
        sort: 6
      },
      {
        id: "4028b8818cec22b7018cec22cdc20054",
        fieldKey: "Input",
        title: "备注",
        x_hidden: false,
        fieldName: "remark",
        sort: 7
      },
      {
        id: "4028b8818cec22b7018cec22cdc1004e",
        fieldKey: "Input",
        title: "数据标题",
        // "javaTitle": "数据标题",
        // "hideLabel": false,
        // "dataType": "basic",
        // "fieldType": "string",
        // "formId": "4028b8818cec22b7018cec22cdc1004b",
        // "formGroupCode": null,
        // "formTabCode": null,
        // "entityType": "product",
        // "entityFieldName": "title",
        // "componentType": null,
        fieldName: "title",
        // "pathName": "title",
        // "dictCode": null,
        // "dataIndex": null,
        // "initialValues": null,
        // "required": false,
        x_hidden: false,
        // "description": null,
        // "x_component": "Input",
        // "x_read_pretty": false,
        // "disabled": null,
        // "readOnly": null,
        sort: 8
        // "x_decorator": null,
        // "vlife_pattern": null,
        // "vlife_message": null,
        // "x_decorator_props$gridSpan": null,
        // "x_decorator_props$layout": null,
        // "x_decorator_props$labelAlign": null,
        // "x_component_props$placeholder": "",
        // "apiKey": null,
        // "x_validator": null,
        // "componentSettingJson": null,
        // "minLength": null,
        // "maxLength": null,
        // "minimum": null,
        // "maximum": null,
        // "validate_unique": false,
        // "pageComponentPropDtos": null,
        // "create_hide": false,
        // "divider": false,
        // "dividerLabel": null,
        // "modify_read": false,
        // "form_type": "product",
        // "events": null,
        // "listSort": 2,
        // "listHide": null,
        // "listWidth": null,
        // "listSearch": true,
        // "money": null,
        // "listAlign": null,
        // "safeStr": false
      }
    ]
  };
  // fieldList = currModel.fields;

  const saveFormColumn = () => {
    // let { currentField } = me.state;
    // let result = await UpdateFormColumn(currentField);
    // if (result.Success)
    //   message.success(result.Message);
    // else
    //   message.error(response.Message);
  };

  return (
    <FormSetDiv className={"bg-white"}>
      <div className="fieldSet-main">
        <Row className={"bg-white"}>
          <Col span={18}> </Col>
          <Col span={6}>
            <Button onClick={saveFormColumn}>保存</Button>
          </Col>
        </Row>
        <Row className="fieldSet-main-content">
          {/* 字段选择排序 */}
          <Col span={3} className="fieldSet-main-content-left">
            <FieldSelect
              key={currModel.type + "_fieldSelect"}
              fields={columns}
              mode={Mode.form}
              outSelectedField={currentField}
              onDataChange={fields => {
                setColumns(fields);
              }}
              onSelect={field => {
                setCurrentField(field);
              }}
            />
          </Col>
          {/* 表单预览 */}
          <Col span={15} className="fieldSet-main-content-center">
            <FormPage
              fieldList={columns}
              currentField={currentField}
              mode={Mode.form}
              onDataChange={fields => {
                setColumns(fields);
                // saveFormColumnTaxisNo(fields);
              }}
              onSelect={field => {
                setCurrentField(field);
              }}
              // onFieldChange={(list: any) => setFieldList(list)}
              handleChooseField={(field: any) => setCurrentField(field)}
            />
          </Col>
          {/* form表单设置 */}
          <Col span={6} className="fieldSet-main-content-right">
            {currentField && (
              <>
                <SiderSetting
                  // fieldList={columns}
                  mode={Mode.form}
                  form={currModel}
                  field={currentField}
                  onDataChange={data => {
                    currentField = data;
                    columns = columns.map(f => {
                      if (f.ID === currentField.ID) return data;
                      return f;
                    });
                    setCurrentField(currentField);
                    setColumns(columns);
                  }}
                />
              </>
            )}
          </Col>
        </Row>
      </div>
    </FormSetDiv>
  );
};

export default Index;
