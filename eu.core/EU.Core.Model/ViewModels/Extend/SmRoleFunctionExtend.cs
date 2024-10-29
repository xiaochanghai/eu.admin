using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace EU.Core.Model.ViewModels.Extend;

public class SmRoleFunctionExtend
{
    /// <summary>
    /// SmFunctionId
    /// </summary>
    public Guid? SmFunctionId { get; set; }

    /// <summary>
    /// NoActionCode
    /// </summary>
    [Display(Name = "NoActionCode"), Description("NoActionCode"), MaxLength(-1, ErrorMessage = "NoActionCode 不能超过 -1 个字符")]
    public string NoActionCode { get; set; }

    /// <summary>
    /// 模块ID
    /// </summary>
    public Guid? SmModuleId { get; set; }

    /// <summary>
    /// 操作代码
    /// </summary>
    [Display(Name = "ActionCode"), Description("操作代码"), MaxLength(-1, ErrorMessage = "操作代码 不能超过 32 个字符")]
    public string ActionCode { get; set; }


}