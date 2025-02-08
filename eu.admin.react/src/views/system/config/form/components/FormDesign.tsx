import React, { useState, useEffect } from "react";
// import FieldSelect from "./FieldSelect";
import SiderSetting from "./SiderSetting";
import FormPage from "./FormPage";
import { Mode } from "./dsl/base";
import { FormSetDiv } from "./style";
import { Button, Card, Row, Col, Space, Skeleton, Descriptions } from "antd";
import http from "@/api";
import { Icon } from "@/components";
import { message } from "@/hooks/useMessage";
import { getModuleSqlInfo } from "@/api/modules/module";

const Index: React.FC<any> = props => {
  const { ModuleId, changePage } = props;
  let [currentField, setCurrentField] = useState<any>(null);
  let [moduleCode, setModuleCode] = useState<any>(null);
  let [moduleName, setModuleName] = useState<any>(null);
  let [columns, setColumns] = useState<any[]>([]);
  let [mode, setMode] = useState<Mode>(Mode.list);

  useEffect(() => {
    if (ModuleId) {
      const querySingleData = async () => {
        let { Data, Success } = await getModuleSqlInfo(ModuleId);
        if (Success) {
          if (Data.module) {
            setModuleCode(Data.module.ModuleCode);
          }
          queryFormColumn(Data.module.ModuleCode);
          setModuleName(Data.module.ModuleName);
        }
      };
      querySingleData();
    }
    const queryFormColumn = async (moduleCode1: string) => {
      let { Data } = await http.get<any>(`/api/SmModule/FormColumn/${moduleCode1}`);
      setColumns(Data);
    };
  }, []);

  let currModel: any = {};

  const saveFormColumn = async () => {
    let { Success, Message } = await http.put<any>(`/api/SmModule/UpdateColumn/${moduleCode}/${mode}`, currentField);
    if (Success) message.success(Message);
  };

  return (
    <>
      {/* <div style={{ height: 10 }}></div> */}
      {moduleCode == null ? (
        <Card>
          <Skeleton active />
          <Skeleton active />
          <Skeleton active />
          <Skeleton active />
        </Card>
      ) : (
        <>
          <Space style={{ display: "flex", justifyContent: "flex-end" }}>
            <Button type="default" onClick={() => changePage("FormIndex")} icon={<Icon name="RollbackOutlined" />}></Button>
          </Space>
          <div style={{ height: 10 }}></div>
          <Card>
            <Descriptions title="表单配置">
              <Descriptions.Item label="模块代码">{moduleCode}</Descriptions.Item>
              <Descriptions.Item label="模块名称">{moduleName}</Descriptions.Item>
            </Descriptions>
            <FormSetDiv className={"bg-white"}>
              <div className="fieldSet-main">
                <Row className={"bg-white"}>
                  <Col span={3}> </Col>
                  <Col span={15}> </Col>
                  <Col span={6}>
                    {currentField && (
                      <Space style={{ display: "flex", justifyContent: "flex-end" }}>
                        <Button onClick={saveFormColumn} icon={<Icon name="SaveOutlined" />}>
                          保存
                        </Button>
                      </Space>
                    )}
                  </Col>
                </Row>
                <Row className="fieldSet-main-content">
                  {/* 模块选择 */}
                  {/* <Col span={3} className="fieldSet-main-content-left">
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
          </Col> */}
                  {/* 表单预览 */}
                  <Col span={18} className="fieldSet-main-content-center">
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
                      onSetMode={(mode: Mode) => {
                        setMode(mode);
                        setCurrentField(null);
                      }}
                    />
                  </Col>
                  {/* form表单设置 */}
                  <Col span={6} className="fieldSet-main-content-right">
                    {currentField && (
                      <>
                        <SiderSetting
                          mode={mode}
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
          </Card>
        </>
      )}
    </>
  );
};

export default Index;
