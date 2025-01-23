using System.ComponentModel.DataAnnotations;

namespace EU.Core.Common;

/// <summary>
/// 动态查询条件
/// </summary>
public class QueryFilter
{
    private int _pageIndex;
    /// <summary>
    /// 起始位置(e.g. 0)
    /// </summary>
    [Required]
    public int PageIndex
    {
        get { return _pageIndex; }
        set
        {
            //前端默认从分页显示默认1开始，所以后端需要-1
            //if (value >= 1)
            //    value -= 1;
            _pageIndex = value;
        }
    }
    /// <summary>
    /// 每页数量(e.g. 10)
    /// </summary>
    [Required]
    public int PageSize { get; set; }
    private string _conditions;
    /// <summary>
    /// 查询条件( 例如:id = 1 and name = 小明)
    /// </summary>
    public string Conditions
    {
        get { return _conditions; }
        set
        {
            //前端默认从分页显示默认1开始，所以后端需要-1
            if (value == "1=1")
                value = null;
            _conditions = value;
        }
    }

    /// <summary>
    /// 排序条件表达式(e.g. LoginName ASC,Name DESC)
    /// </summary>
    public string Sorting { get; set; }

    /// <summary>
    /// 参数 
    /// </summary>
    public Dictionary<string, string> sorter { get; set; }

    private Dictionary<string, object> _params;

    /// <summary>
    /// 参数 
    /// </summary>
    public Dictionary<string, object> @params
    {
        get { return _params; }
        set
        {
            if (value.Count > 0)
                value = value.Where(pair => pair.Key != "current" && pair.Key != "pageSize").ToDictionary(pair => pair.Key, pair => pair.Value); ;
            _params = value;
        }
    }
    /// <summary>
    /// 缺省值
    /// </summary>
    public static QueryFilter Default => new QueryFilter
    {
        PageIndex = 1,
        PageSize = 100000,
        Sorting = string.Empty,
        Conditions = string.Empty
    };
}