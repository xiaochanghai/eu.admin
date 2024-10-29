using System.Data;
using EU.Core.Model.Models;

namespace EU.Core.Model;

public class TreeMenuData
{
    public string id { get; set; }
    public string path { get; set; }
    public string name { get; set; }
    public string icon { get; set; }

    public string redirect { get; set; } = null;
    public string component { get; set; }
    public List<TreeMenuData> children { get; set; }
    public string moduleCode { get; set; }
}

public class TreeAuthMenu
{
    public string id { get; set; }
    public string path { get; set; }
    public string element { get; set; } = null;
    public string redirect { get; set; } = "";

    public List<TreeAuthMenu> children { get; set; } = default;
    public TreeAuthMenuMeta meta { get; set; }
}

public class TreeAuthMenuMeta
{
    /// <summary>
    /// 模块代码
    /// </summary>
    public string key { get; set; }
    public string icon { get; set; }
    public string title { get; set; }
    public string isLink { get; set; } = "";
    public bool? isHide { get; set; } = false;
    public bool? isFull { get; set; } = false;

    public bool isAffix { get; set; } = false;
}

/// <summary>
/// 
/// </summary>
public class SmModuleColumnExtend : SmModuleColumn
{

    /// <summary>
    /// 模块代码
    /// </summary>
    public override string ModuleCode { get; set; }
}
/// <summary>
/// 系统模块扩展类
/// </summary>
public class SmModuleSqlExtend : SmModuleSql
{
    /// <summary>
    /// 模块代码
    /// </summary>
    public override string ModuleCode { get; set; }

}

//操作栏按钮
public class ActionActions
{
    public string id { get; set; }
    public int? taxisNo { get; set; }
}
public class SmModuleForm
{
    public Guid ID { get; set; }

    public string DataIndex { get; set; }

    /// <summary>
    /// 表单排序号
    /// </summary>
    public int? FromTaxisNo { get; set; }

    /// <summary>
    /// 表单项标题
    /// </summary>
    public string FormTitle { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string DefaultValue { get; set; }

    /// <summary>
    /// 表单隐藏
    /// </summary>
    public bool? HideInForm { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool? Required { get; set; }

    /// <summary>
    /// 只读
    /// </summary>
    public bool? Disabled { get; set; }

    /// <summary>
    /// 限定输入格式
    /// </summary>
    public string Validator { get; set; }

    /// <summary>
    /// 正则表达式
    /// </summary>
    public string ValidPattern { get; set; }

    /// <summary>
    /// 是否唯一
    /// </summary>
    public bool? Unique { get; set; }

    /// <summary>
    /// 最大长度
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// 最小长度
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public decimal? Maximum { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    public decimal? Minimum { get; set; }

    /// <summary>
    /// 新增时隐藏
    /// </summary>
    public bool? CreateHide { get; set; }

    /// <summary>
    /// 修改时只读
    /// </summary>
    public bool? ModifyDisabled { get; set; }

    /// <summary>
    /// 字段占比
    /// </summary>
    public int? GridSpan { get; set; }

    /// <summary>
    /// 字段控件类型
    /// </summary>
    public string FieldType { get; set; }

    /// <summary>
    /// 占位符
    /// </summary>
    public string Placeholder { get; set; }

    /// <summary>
    /// 数据来源方式
    /// </summary>
    public string DataSourceType { get; set; }

    /// <summary>
    /// 数据来源
    /// </summary>
    public string DataSource { get; set; }

    /// <summary>
    /// 数据格式
    /// </summary>
    public string DataFormate { get; set; }

    /// <summary>
    /// 标签布局
    /// </summary>
    public int? LabelCol { get; set; }

    /// <summary>
    /// 控件布局
    /// </summary>
    public int? WrapperCol { get; set; }

    /// <summary>
    /// TextArea最小行数
    /// </summary>
    public int? MinRows { get; set; }

    /// <summary>
    /// 表单字段组别
    /// </summary>
    public int? FromFieldGroup { get; set; }
}

public class SmModuleFormOption : SmModuleColumn
{
    public string ComboBoxDataSource { get; set; }
    public string ComboGridDataSource { get; set; }
}

/// <summary>
/// 服务层分页响应实体(泛型)
/// </summary>
public class GridListReturn
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int status { get; set; } = 200;
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool success { get; set; } = false;
    /// <summary>
    /// 返回信息
    /// </summary>
    public string message { get; set; } = null;
    /// <summary>
    /// 当前页标
    /// </summary>
    public int current { get; set; } = 1;
    /// <summary>
    /// 总页数
    /// </summary>
    public int pageCount => (int)Math.Ceiling((decimal)total / pageSize);
    /// <summary>
    /// 数据总数
    /// </summary>
    public int total { get; set; } = 0;
    /// <summary>
    /// 每页大小
    /// </summary>
    public int pageSize { set; get; } = 20;
    /// <summary>
    /// 返回数据
    /// </summary>
    public DataTable data { get; set; }

    public GridListReturn(int pageSize, int page, int totalCount, DataTable data, string message)
    {
        this.success = true;
        this.current = page;
        this.total = totalCount;
        this.pageSize = pageSize;
        this.data = data;
        this.message = message;
    }
}
