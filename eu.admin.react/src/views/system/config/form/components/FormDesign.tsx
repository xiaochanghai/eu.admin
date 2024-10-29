import React, { useState, useEffect } from "react";
import FieldSelect from "./FieldSelect";
import SiderSetting from "./SiderSetting";
import FormPage from "./FormPage";
import { Mode } from "./dsl/base";
import { FormSetDiv } from "./style";
import { Col, Row, Button, Flex } from "antd";
import http from "@/api";
import { Icon } from "@/components/Icon";
import { message } from "@/hooks/useMessage";

const Index: React.FC<any> = props => {
  let { moduleCode } = props;
  let [currentField, setCurrentField] = useState<any>(null);
  let [columns, setColumns] = useState<any[]>([]);
  useEffect(() => {
    const queryFormColumn = async () => {
      let { Data } = await http.get<any>(`/api/SmModule/FormColumn/${moduleCode}`);
      setColumns(Data);
    };
    queryFormColumn();
  }, []);

  let currModel: any = {};

  const saveFormColumn = async () => {
    let { Success, Message } = await http.put<any>(`/api/SmModule/UpdateFormColumn/${moduleCode}`, currentField);
    if (Success) message.success(Message);
  };

  return (
    <FormSetDiv className={"bg-white"}>
      <div className="fieldSet-main">
        <Row className={"bg-white"}>
          <Col span={3}> </Col>
          <Col span={15}> </Col>
          <Col span={6}>
            {currentField && (
              <Flex justify={"flex-end"} align={"flex-start"}>
                <Button onClick={saveFormColumn} style={{ marginRight: 10 }} icon={<Icon name="SaveOutlined" />}>
                  保存
                </Button>
              </Flex>
            )}
          </Col>
        </Row>
        <Row className="fieldSet-main-content">
          {/* 模块选择 */}
          <Col span={3} className="fieldSet-main-content-left">
            <FieldSelect
              key={currModel.type + "_fieldSelect"}
              fields={columns}
              currentField={currentField}
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
              moduleCode={moduleCode}
              fieldList={columns}
              currentField={currentField}
              // mode={Mode.form}
              onDataChange={fields => {
                setColumns(fields);
              }}
              onPlus={field => {
                columns = columns.map(f => {
                  if (f.ID === field.ID) return field;
                  return f;
                });
                setColumns(columns);
                setCurrentField(field);
              }}
              onSelect={field => {
                setCurrentField(field);
              }}
            />
          </Col>
          {/* form表单设置 */}
          <Col span={6} className="fieldSet-main-content-right">
            {currentField && (
              <>
                <SiderSetting
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
