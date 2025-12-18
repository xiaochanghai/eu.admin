/**
 * UploadExcel 组件常量定义
 */

// Excel文件MIME类型
export const EXCEL_MIME_TYPES = [
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  "application/vnd.ms-excel"
] as const;

// 文件大小限制 (50MB)
export const MAX_FILE_SIZE = 50 * 1024 * 1024;

// 步骤定义
export const UPLOAD_STEPS = {
  UPLOAD: 0,
  PREVIEW: 1,
  SUCCESS: 2
} as const;

export const CHAT_STEPS = {
  PREVIEW: 0,
  SUCCESS: 1
} as const;

// 表格配置
export const TABLE_CONFIG = {
  size: "small" as const,
  scroll: { x: "max-content", y: 400 },
  pagination: {
    showSizeChanger: true,
    showQuickJumper: true,
    showTotal: (total: number) => `共 ${total} 条记录`,
    pageSize: 50,
    showLessItems: true
  }
};

// 错误表格列配置
export const ERROR_TABLE_COLUMNS = [
  {
    title: "序号",
    dataIndex: "Key",
    key: "Key",
    width: 80,
    fixed: "left" as const
  },
  {
    title: "Sheet名",
    dataIndex: "SheetName",
    key: "SheetName",
    width: 150,
    ellipsis: true
  },
  {
    title: "错误信息",
    dataIndex: "ErrorName",
    key: "ErrorName",
    ellipsis: true
  }
];

// 导入步骤说明
export const UPLOAD_INSTRUCTIONS = ["根据模板中的格式填写内容，不可以调整列的先后顺序。", '点击"选择Excel文件"执行上传操作。'];

// 注意事项
export const NOTICE_ITEMS = [
  "后缀名必须为xlsx或xls。",
  "数据请勿放在合并的单元格中。",
  "第一行红色字体的为必填栏位，同时注意特殊字段的格式是否正确，例如：日期类型，数字类型等。",
  "不可以调整Excel模板中列的顺序。",
  "不可以修改导入模板中的工作簿(Sheet)名称。",
  "导入数据时，系统会将第一行的内容作为标题行，因此导入的内容请从第2行开始填写。"
];

// 表单布局配置
export const FORM_LAYOUTS = {
  default: {
    labelCol: { span: 8 },
    wrapperCol: { span: 18 }
  },
  responsive: {
    labelCol: {
      xs: { span: 6 },
      sm: { span: 6 },
      md: { span: 6 }
    },
    wrapperCol: {
      xs: { span: 16 },
      sm: { span: 16 },
      md: { span: 16 }
    }
  }
};
