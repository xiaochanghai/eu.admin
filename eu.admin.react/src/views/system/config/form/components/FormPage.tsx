import { Tabs, Card, Form, Flex, Tag, Button, Modal, Input } from "antd";
import { useState, useCallback, useRef, useEffect } from "react";
import { useDrag, useDrop, DropTargetMonitor } from "react-dnd";
import { HTML5Backend } from "react-dnd-html5-backend";
import { DndProvider } from "react-dnd";
import http from "@/api";
import { Mode } from "./dsl/base";
const { TabPane } = Tabs;
import { Element, Icon } from "@/components";
import { message } from "@/hooks/useMessage";

type FieldType = {
  Title?: string;
  DataIndex?: string;
};
interface FieldSetCenterProps {
  moduleCode: any;
  fieldList: any[];
  currentField: any;
  onDataChange: (ang: any[]) => void; //数据返回出去
  onSelect: (field: string) => void; //当前选中字段
  onPlus: (field: any) => void; //当前选中字段
  onSetMode: (mode: Mode) => void; //设置表单配置还是列表配置
  onReload: () => void; //重新加载
}
const FieldSetCenter = ({
  fieldList,
  currentField,
  onSelect,
  onDataChange,
  onPlus,
  moduleCode,
  onSetMode,
  onReload
}: FieldSetCenterProps) => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [mode, setMode] = useState<Mode>(Mode.list);
  const [form] = Form.useForm(); // 表单实例
  const fieldListRef = useRef(fieldList);

  // 同步最新的 fieldList 到 ref
  useEffect(() => {
    fieldListRef.current = fieldList;
  }, [fieldList]);

  // 保存函数
  const saveFormColumnTaxisNo = useCallback(async (columns: any[]) => {
    const items1 = columns.map(x => {
      return { ID: x.ID, FormTaxisNo: x.FormTaxisNo };
    });
    await http.put<any>(`/api/SmModule/UpdateTaxisNo/${moduleCode}/form`, items1);
  }, [moduleCode]);

  const saveListColumnTaxisNo = useCallback(async (items: any[]) => {
    const items1 = items.map(x => {
      return { ID: x.ID, TaxisNo: x.TaxisNo };
    });
    await http.put<any>(`/api/SmModule/UpdateTaxisNo/${moduleCode}/list`, items1);
  }, [moduleCode]);

  // 使用 useCallback 优化拖动函数
  const moveFormHideItem = useCallback(
    (fromIndex: number, toIndex: number) => {
      // 先排序再拖拽
      let updatedItems = [...fieldList.filter(f => f.HideInForm != false && f.ColumnMode != Mode.list)].sort((a, b) => (a.FormTaxisNo || 0) - (b.FormTaxisNo || 0));
      const [movedItem] = updatedItems.splice(fromIndex, 1);
      updatedItems.splice(toIndex, 0, movedItem);

      // 更新序号
      updatedItems.forEach((x, i) => {
        x.FormTaxisNo = 100 * (i + 1);
      });

      // 合并所有字段
      const visibleItems = fieldList.filter(f => f.HideInForm === false && f.ColumnMode != Mode.list);
      const otherItems = fieldList.filter(f => !(f.HideInForm === false && f.ColumnMode != Mode.list) && !(f.HideInForm != false && f.ColumnMode != Mode.list));
      const allItems = [...visibleItems, ...updatedItems, ...otherItems];

      fieldListRef.current = allItems;
      onDataChange(allItems);
    },
    [fieldList, onDataChange]
  );

  const moveFormItem = useCallback(
    (fromIndex: number, toIndex: number) => {
      // 先排序再拖拽
      let updatedItems = [...fieldList.filter(f => f.HideInForm === false && f.ColumnMode != Mode.list)].sort((a, b) => (a.FormTaxisNo || 0) - (b.FormTaxisNo || 0));
      const [movedItem] = updatedItems.splice(fromIndex, 1);
      updatedItems.splice(toIndex, 0, movedItem);

      // 更新序号
      updatedItems.forEach((x, i) => {
        x.FormTaxisNo = 100 * (i + 1);
      });

      // 合并所有字段
      const hiddenItems = fieldList.filter(f => f.HideInForm != false && f.ColumnMode != Mode.list);
      const otherItems = fieldList.filter(f => f.ColumnMode == Mode.list);
      const allItems = [...updatedItems, ...hiddenItems, ...otherItems];

      fieldListRef.current = allItems;
      onDataChange(allItems);
    },
    [fieldList, onDataChange]
  );

  const moveListHideItem = useCallback(
    (fromIndex: number, toIndex: number) => {
      // 先排序再拖拽
      let updatedItems = [...fieldList.filter(f => f.HideInTable != false && f.ColumnMode != Mode.form)].sort((a, b) => (a.TaxisNo || 0) - (b.TaxisNo || 0));
      const [movedItem] = updatedItems.splice(fromIndex, 1);
      updatedItems.splice(toIndex, 0, movedItem);

      // 更新序号
      updatedItems.forEach((x, i) => {
        x.TaxisNo = 100 * (i + 1);
      });

      // 合并所有字段
      const visibleItems = fieldList.filter(f => f.HideInTable === false && f.ColumnMode != Mode.form);
      const otherItems = fieldList.filter(f => f.ColumnMode == Mode.form);
      const allItems = [...visibleItems, ...updatedItems, ...otherItems];

      fieldListRef.current = allItems;
      onDataChange(allItems);
    },
    [fieldList, onDataChange]
  );

  const moveListItem = useCallback(
    (fromIndex: number, toIndex: number) => {
      // 先排序再拖拽
      let updatedItems = [...fieldList.filter(f => f.HideInTable === false && f.ColumnMode != Mode.form)].sort((a, b) => (a.TaxisNo || 0) - (b.TaxisNo || 0));
      const [movedItem] = updatedItems.splice(fromIndex, 1);
      updatedItems.splice(toIndex, 0, movedItem);

      // 更新序号
      updatedItems.forEach((x, i) => {
        x.TaxisNo = 100 * (i + 1);
      });

      // 合并所有字段
      const hiddenItems = fieldList.filter(f => f.HideInTable != false && f.ColumnMode != Mode.form);
      const otherItems = fieldList.filter(f => f.ColumnMode == Mode.form);
      const allItems = [...updatedItems, ...hiddenItems, ...otherItems];

      fieldListRef.current = allItems;
      onDataChange(allItems);
    },
    [fieldList, onDataChange]
  );

  const DragFormHideItem = ({ id, text, index, moveItem, field }: any) => {
    const ref = useRef<HTMLDivElement>(null);

    const [{ isDragging }, drag] = useDrag({
      type: "form-hide-item",
      item: () => ({ id, index }),
      end: () => {
        // 拖拽结束后保存数据
        saveFormColumnTaxisNo(fieldListRef.current);
      },
      collect: monitor => ({
        isDragging: monitor.isDragging()
      })
    });

    const [{ isOver }, drop] = useDrop({
      accept: "form-hide-item",
      hover: (draggedItem: any, monitor: DropTargetMonitor) => {
        if (!ref.current) {
          return;
        }
        const dragIndex = draggedItem.index;
        const hoverIndex = index;

        if (dragIndex === hoverIndex) {
          return;
        }

        const hoverBoundingRect = ref.current.getBoundingClientRect();
        const hoverMiddleY = (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
        const clientOffset = monitor.getClientOffset();

        if (!clientOffset) {
          return;
        }

        const hoverClientY = clientOffset.y - hoverBoundingRect.top;

        // 向下拖动
        if (dragIndex < hoverIndex && hoverClientY < hoverMiddleY) {
          return;
        }

        // 向上拖动
        if (dragIndex > hoverIndex && hoverClientY > hoverMiddleY) {
          return;
        }

        moveItem(dragIndex, hoverIndex);
        draggedItem.index = hoverIndex;
      },
      collect: monitor => ({
        isOver: monitor.isOver()
      })
    });

    drag(drop(ref));

    return (
      <div
        ref={ref}
        style={{
          opacity: isDragging ? 0.4 : 1,
          cursor: "move",
          padding: 10,
          transform: isOver ? "scale(1.05)" : "scale(1)",
          transition: "transform 0.2s ease"
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

  const DragFormItem = ({ id, index, moveItem, field }: any) => {
    const ref = useRef<HTMLDivElement>(null);

    const [{ isDragging }, drag] = useDrag({
      type: "form-item",
      item: () => ({ id, index }),
      end: () => {
        // 拖拽结束后保存数据
        saveFormColumnTaxisNo(fieldListRef.current);
      },
      collect: monitor => ({
        isDragging: monitor.isDragging()
      })
    });

    const [{ isOver }, drop] = useDrop({
      accept: "form-item",
      hover: (draggedItem: any, monitor: DropTargetMonitor) => {
        if (!ref.current) {
          return;
        }
        const dragIndex = draggedItem.index;
        const hoverIndex = index;

        if (dragIndex === hoverIndex) {
          return;
        }

        const hoverBoundingRect = ref.current.getBoundingClientRect();
        const hoverMiddleY = (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
        const clientOffset = monitor.getClientOffset();

        if (!clientOffset) {
          return;
        }

        const hoverClientY = clientOffset.y - hoverBoundingRect.top;

        // 向下拖动
        if (dragIndex < hoverIndex && hoverClientY < hoverMiddleY) {
          return;
        }

        // 向上拖动
        if (dragIndex > hoverIndex && hoverClientY > hoverMiddleY) {
          return;
        }

        moveItem(dragIndex, hoverIndex);
        draggedItem.index = hoverIndex;
      },
      collect: monitor => ({
        isOver: monitor.isOver()
      })
    });

    drag(drop(ref));

    return (
      <div
        ref={ref}
        className="main-field"
        style={{
          border: currentField?.ID === field?.ID ? "1px solid #3b82f680" : "",
          background: currentField?.ID === field?.ID ? "#F8FBFF" : "",
          width: (field.GridSpan != null ? field?.GridSpan : 50) + "%",
          opacity: isDragging ? 0.4 : 1,
          cursor: "move",
          transform: isOver ? "scale(1.02)" : "scale(1)",
          transition: "all 0.2s ease"
        }}
        key={index}
        onClick={() => onSelect(field)}
      >
        <Element field={field} />
      </div>
    );
  };

  const DragListItem = ({ id, index, moveItem, field }: any) => {
    const ref = useRef<HTMLDivElement>(null);

    const [{ isDragging }, drag] = useDrag({
      type: "list-item",
      item: () => ({ id, index }),
      end: () => {
        // 拖拽结束后保存数据
        saveListColumnTaxisNo(fieldListRef.current);
      },
      collect: monitor => ({
        isDragging: monitor.isDragging()
      })
    });

    const [{ isOver }, drop] = useDrop({
      accept: "list-item",
      hover: (draggedItem: any, monitor: DropTargetMonitor) => {
        if (!ref.current) {
          return;
        }
        const dragIndex = draggedItem.index;
        const hoverIndex = index;

        if (dragIndex === hoverIndex) {
          return;
        }

        const hoverBoundingRect = ref.current.getBoundingClientRect();
        const hoverMiddleY = (hoverBoundingRect.bottom - hoverBoundingRect.top) / 2;
        const clientOffset = monitor.getClientOffset();

        if (!clientOffset) {
          return;
        }

        const hoverClientY = clientOffset.y - hoverBoundingRect.top;

        // 向下拖动
        if (dragIndex < hoverIndex && hoverClientY < hoverMiddleY) {
          return;
        }

        // 向上拖动
        if (dragIndex > hoverIndex && hoverClientY > hoverMiddleY) {
          return;
        }

        moveItem(dragIndex, hoverIndex);
        draggedItem.index = hoverIndex;
      },
      collect: monitor => ({
        isOver: monitor.isOver()
      })
    });

    drag(drop(ref));

    return (
      <div
        ref={ref}
        style={{
          opacity: isDragging ? 0.4 : 1,
          cursor: "move",
          padding: 10,
          transform: isOver ? "scale(1.05)" : "scale(1)",
          transition: "transform 0.2s ease"
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
  const add = async (data: any) => {
    let { Success, Message } = await http.post<any>(`/api/SmModule/AddColumn/${moduleCode}`, {
      ...data,
      HideInTable: mode == Mode.list && false,
      HideInForm: mode == Mode.form && false
    });
    if (Success) {
      onReload();
      form.resetFields();
      setIsModalOpen(false);

      message.success(Message);
    }
  };
  const handleOk = async () => {
    form.validateFields().then(add);
  };
  const onTabsChange = (key: string) => {
    console.log(key);
    onSetMode(key == "panel_list" ? Mode.list : Mode.form);
    setMode(key == "panel_list" ? Mode.list : Mode.form);
  };
  return (
    <DndProvider backend={HTML5Backend}>
      <div className="bg-white">
        <Tabs
          defaultActiveKey={"panel_list"}
          onChange={onTabsChange}
          tabBarExtraContent={{
            right: (
              <Button type="primary" icon={<Icon name="PlusOutlined" />} onClick={() => setIsModalOpen(true)}>
                新增
              </Button>
            )
          }}
        >
          <TabPane key="panel_list" tab="列表栏位" icon={<Icon name="TableOutlined" />}>
            <Card size="small" title="显示栏位">
              <Flex wrap>
                {fieldList
                  .filter(f => f.HideInTable === false && f.ColumnMode != Mode.form)
                  .sort((a, b) => (a.TaxisNo || 0) - (b.TaxisNo || 0))
                  .map((item, index) => (
                    <DragListItem key={item.ID} id={item.ID} index={index} field={item} moveItem={moveListItem} />
                  ))}
              </Flex>
            </Card>
            <div style={{ height: 20 }}></div>
            <Card size="small" title="隐藏栏位">
              <Flex wrap gap="small">
                {fieldList
                  .filter(f => f.HideInTable != false && f.ColumnMode != Mode.form)
                  .sort((a, b) => (a.TaxisNo || 0) - (b.TaxisNo || 0))
                  .map((item, index) => (
                    <DragListItem key={item.ID} id={item.ID} index={index} field={item} moveItem={moveListHideItem} />
                  ))}
                {fieldList.filter(f => f.HideInTable != false && f.ColumnMode != Mode.form).length == 0 ? "暂无隐藏栏位" : null}
              </Flex>
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
                <Flex wrap>
                  {fieldList
                    .filter(f => f.HideInForm === false && f.ColumnMode != Mode.list)
                    .sort((a, b) => (a.FormTaxisNo || 0) - (b.FormTaxisNo || 0))
                    .map((item, index) => (
                      <DragFormItem key={item.ID} id={item.ID} index={index} field={item} moveItem={moveFormItem} />
                    ))}
                </Flex>
              </Card>
              <div style={{ height: 20 }}></div>
              <Card size="small" title="隐藏栏位">
                <Flex wrap gap="small">
                  {fieldList
                    .filter(f => f.HideInForm != false && f.ColumnMode != Mode.list)
                    .sort((a, b) => (a.FormTaxisNo || 0) - (b.FormTaxisNo || 0))
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
              </Card>
            </Form>
          </TabPane>
        </Tabs>

        <Modal
          title={mode === Mode.form ? "新增模块栏位" : "新增列表栏位"}
          closable={{ "aria-label": "Custom Close Button" }}
          open={isModalOpen}
          onOk={handleOk}
          destroyOnHidden={true}
          onCancel={() => setIsModalOpen(false)}
        >
          <Form
            name="basic"
            labelCol={{
              span: 6
            }}
            wrapperCol={{
              span: 16
            }}
            form={form}
            // onFinish={onFinish}
            // onFinishFailed={onFinishFailed}
            autoComplete="off"
          >
            <Form.Item<FieldType> label="列名" name="Title" rules={[{ required: true, message: "请输入列名!" }]}>
              <Input />
            </Form.Item>
            <Form.Item<FieldType> label="数据列" name="DataIndex" rules={[{ required: true, message: "请输入数据列!" }]}>
              <Input />
            </Form.Item>
          </Form>
        </Modal>
      </div>
    </DndProvider>
  );
};

export default FieldSetCenter;
