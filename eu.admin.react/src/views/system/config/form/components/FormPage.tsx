import { Tabs, Card, Form, Flex, Tag, Button } from "antd";
import React from "react";
import { useDrag, useDrop } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { DndProvider } from "react-dnd";
import http from "@/api";
import { Mode } from "./dsl/base";
const { TabPane } = Tabs;
import { Element, Icon } from "@/components";

interface FieldSetCenterProps {
  moduleCode: any;
  fieldList: any[];
  currentField: any;
  onDataChange: (ang: any[]) => void; //数据返回出去
  onSelect: (field: string) => void; //当前选中字段
  onPlus: (field: any) => void; //当前选中字段
  onSetMode: (mode: Mode) => void; //设置表单配置还是列表配置
}
const FieldSetCenter = ({
  fieldList,
  currentField,
  onSelect,
  onDataChange,
  onPlus,
  moduleCode,
  onSetMode
}: FieldSetCenterProps) => {
  const DragFormHideItem = ({ id, text, index, moveItem, field }: any) => {
    const ref = React.useRef(null);

    const [{ isDragging }, drag] = useDrag(() => ({
      type: "item",
      item: { id, index },
      collect: monitor => ({
        isDragging: !!monitor.isDragging()
      })
    }));

    const [, drop] = useDrop(() => ({
      accept: "item",
      hover: (item: any) => {
        if (item.index !== index) {
          moveItem(item.index, index);
          item.index = index;
        }
      }
    }));

    drag(drop(ref));

    return (
      <div
        ref={ref}
        style={{
          // width: 120,
          opacity: isDragging ? 0.5 : 1,
          cursor: "move",
          padding: 10
          // border: "1px solid #ccc",
          // margin: "5px"
        }}
      >
        {/* {text} */}
        <Tag
          className="main-hide-field-tag"
          style={{
            width: 100,
            textAlign: "center",
            border: currentField?.ID === field?.ID ? "1px solid #3b82f680" : "",
            background: currentField?.ID === field?.ID ? "#F8FBFF" : ""
            // borderRadius: currentField?.ID === item?.ID ? 10 : 0,
            // width: (item.GridSpan != null ? item?.GridSpan : 50) + "%"
          }}
          onClick={event => {
            event.stopPropagation();
            // 处理按钮的点击事件
            onSelect(field);
          }}
        >
          {text}
          <span
            className="plus"
            onClick={event => {
              event.stopPropagation();
              onPlus({
                ...field,
                HideInForm: false
              });
            }}
          >
            <Icon className={currentField?.ID === field?.ID ? "icon active" : "icon"} name="PlusCircleFilled" />
          </span>
        </Tag>
      </div>
    );
  };

  const moveFormHideItem = (fromIndex: number, toIndex: number) => {
    let updatedItems = [...fieldList.filter(f => f.HideInForm != false)];
    const [movedItem] = updatedItems.splice(fromIndex, 1);
    updatedItems.splice(toIndex, 0, movedItem);
    // debugger;
    let items = [...fieldList.filter(f => f.HideInForm == false), ...updatedItems];
    saveFormColumnTaxisNo(items);
    onDataChange(items);
  };

  const DragFormItem = ({ id, index, moveItem, field }: any) => {
    const ref = React.useRef(null);

    const [{ isDragging }, drag] = useDrag(() => ({
      type: "item",
      item: { id, index },
      collect: monitor => ({
        isDragging: !!monitor.isDragging()
      })
    }));

    const [, drop] = useDrop(() => ({
      accept: "item",
      hover: (item: any) => {
        if (item.index !== index) {
          moveItem(item.index, index);
          item.index = index;
        }
      }
    }));

    drag(drop(ref));

    return (
      <div
        ref={ref}
        className="main-field"
        style={{
          border: currentField?.ID === field?.ID ? "1px solid #3b82f680" : "",
          background: currentField?.ID === field?.ID ? "#F8FBFF" : "",
          // borderRadius: currentField?.ID === item?.ID ? 10 : 0,
          width: (field.GridSpan != null ? field?.GridSpan : 50) + "%",
          opacity: isDragging ? 0.5 : 1,
          cursor: "move"
        }}
        key={index}
        onClick={() => onSelect(field)}
      >
        <Element field={field} />
      </div>
    );
  };

  const moveFormItem = (fromIndex: number, toIndex: number) => {
    let updatedItems = [...fieldList.filter(f => f.HideInForm === false)];
    const [movedItem] = updatedItems.splice(fromIndex, 1);
    updatedItems.splice(toIndex, 0, movedItem);

    let items = [...fieldList.filter(f => f.HideInForm != false), ...updatedItems];
    saveFormColumnTaxisNo(items);
    onDataChange(items);
  };
  const saveFormColumnTaxisNo = async (columns: any[]) => {
    let items1 = columns.map(x => {
      return { ID: x.ID, FromTaxisNo: x.FromTaxisNo };
    });
    await http.put<any>(`/api/SmModule/UpdateTaxisNo/${moduleCode}/form`, items1);
  };

  const DragListItem = ({ id, index, moveItem, field }: any) => {
    const ref = React.useRef(null);

    const [{ isDragging }, drag] = useDrag(() => ({
      type: "item",
      item: { id, index },
      collect: monitor => ({
        isDragging: !!monitor.isDragging()
      })
    }));

    const [, drop] = useDrop(() => ({
      accept: "item",
      hover: (item: any) => {
        if (item.index !== index) {
          moveItem(item.index, index);
          item.index = index;
        }
      }
    }));

    drag(drop(ref));

    return (
      <div
        ref={ref}
        style={{
          // width: 120,
          opacity: isDragging ? 0.5 : 1,
          cursor: "move",
          padding: 10
          // border: "1px solid #ccc",
          // margin: "5px"
        }}
      >
        {/* {text} */}
        <Tag
          className="main-hide-field-tag"
          style={{
            // width: 100,
            textAlign: "center",
            border: currentField?.ID === field?.ID ? "1px solid #3b82f680" : "",
            background: currentField?.ID === field?.ID ? "#F8FBFF" : ""
          }}
          onClick={event => {
            event.stopPropagation();
            // 处理按钮的点击事件
            onSelect(field);
          }}
        >
          {field.Title || field.DataIndex}

          <span
            className="plus"
            onClick={event => {
              event.stopPropagation();
              onPlus({
                ...field,
                HideInTable: !field.HideInTable
              });
            }}
          >
            <Icon
              className={currentField?.ID === field?.ID ? "icon active" : "icon"}
              name={field.HideInTable != false ? "PlusCircleFilled" : "MinusCircleFilled"}
            />
          </span>
        </Tag>
      </div>
    );
  };

  const moveListHideItem = (fromIndex: number, toIndex: number) => {
    let updatedItems = [...fieldList.filter(f => f.HideInTable != false)];
    const [movedItem] = updatedItems.splice(fromIndex, 1);
    updatedItems.splice(toIndex, 0, movedItem);
    let items = [...fieldList.filter(f => f.HideInTable == false), ...updatedItems];
    saveListColumnTaxisNo(items);
  };

  const moveListItem = (fromIndex: number, toIndex: number) => {
    let updatedItems = [...fieldList.filter(f => f.HideInTable === false)];
    const [movedItem] = updatedItems.splice(fromIndex, 1);
    updatedItems.splice(toIndex, 0, movedItem);

    let items = [...fieldList.filter(f => f.HideInTable != false), ...updatedItems];
    saveListColumnTaxisNo(items);
  };

  const saveListColumnTaxisNo = async (items: any[]) => {
    let i = 1;
    items.map(x => {
      x.TaxisNo = 100 * i;
      i++;
    });
    onDataChange(items);
    let items1 = items.map(x => {
      return { ID: x.ID, TaxisNo: x.TaxisNo };
    });
    await http.put<any>(`/api/SmModule/UpdateTaxisNo/${moduleCode}/list`, items1);
  };

  const onTabsChange = (key: string) => {
    console.log(key);
    onSetMode(key == "panel_list" ? Mode.list : Mode.form);
  };
  return (
    <div style={{ backgroundColor: "#fff" }}>
      <Tabs
        defaultActiveKey={"panel_table"}
        onChange={onTabsChange}
        tabBarExtraContent={{
          right: (
            <Button type="primary" icon={<Icon name="PlusOutlined" />}>
              新增
            </Button>
          )
        }}
      >
        <TabPane key="panel_list" tab="表格栏位" icon={<Icon name="TableOutlined" />}>
          <Card size="small" title="显示栏位">
            <DndProvider backend={HTML5Backend}>
              <Flex wrap>
                {fieldList
                  .filter(f => f.HideInTable === false && f.ColumnMode != Mode.form)
                  .sort((a, b) => a.TaxisNo - b.TaxisNo)
                  .map((item, index) => (
                    <DragListItem key={item.ID} id={item.ID} index={index} field={item} moveItem={moveListItem} />
                  ))}
              </Flex>
            </DndProvider>
          </Card>
          <div style={{ height: 20 }}></div>
          <Card size="small" title="隐藏栏位">
            <DndProvider backend={HTML5Backend}>
              <Flex wrap gap="small">
                {fieldList
                  .filter(f => f.HideInTable != false && f.ColumnMode != Mode.form)
                  .sort((a, b) => a.TaxisNo - b.TaxisNo)
                  .map((item, index) => (
                    <DragListItem key={item.ID} id={item.ID} index={index} field={item} moveItem={moveListHideItem} />
                  ))}
                {fieldList.filter(f => f.HideInTable != false && f.ColumnMode != Mode.form).length == 0 ? "暂无隐藏栏位" : null}
              </Flex>
            </DndProvider>
          </Card>
        </TabPane>
        <TabPane key="panel_form" tab="表单栏位" icon={<Icon name="MenuOutlined" />}>
          <Form
            labelCol={{
              xs: { span: 8 },
              sm: { span: 8 },
              md: { span: 8 }
            }}
            wrapperCol={{
              xs: { span: 16 },
              sm: { span: 16 },
              md: { span: 16 }
            }}
            labelWrap
          >
            <Card size="small" title="可编辑栏位">
              <DndProvider backend={HTML5Backend}>
                <Flex wrap>
                  {fieldList
                    .filter(f => f.HideInForm === false && f.ColumnMode != Mode.list)
                    .map((item, index) => (
                      <DragFormItem key={item.ID} id={item.ID} index={index} field={item} moveItem={moveFormItem} />
                    ))}
                </Flex>
              </DndProvider>
            </Card>
            <div style={{ height: 20 }}></div>
            <Card size="small" title="隐藏栏位">
              <DndProvider backend={HTML5Backend}>
                <Flex wrap gap="small">
                  {fieldList
                    .filter(f => f.HideInForm != false && f.ColumnMode != Mode.list)
                    .map((item, index) => (
                      <DragFormHideItem
                        key={item.ID}
                        id={item.ID}
                        text={item.FormTitle || item.DataIndex}
                        index={index}
                        field={item}
                        moveItem={moveFormHideItem}
                      />
                    ))}
                  {fieldList.filter(f => f.HideInForm != false && f.ColumnMode != Mode.list).length == 0 ? "暂无隐藏栏位" : null}
                </Flex>
              </DndProvider>
            </Card>
          </Form>
        </TabPane>
      </Tabs>
    </div>
  );
};

export default FieldSetCenter;
