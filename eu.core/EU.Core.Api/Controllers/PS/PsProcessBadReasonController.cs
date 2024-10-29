/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsProcessBadReason.cs
*
*功 能： N / A
* 类 名： PsProcessBadReason
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:58:24  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

/// <summary>
/// PsProcessBadReason(Controller)
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PS)]
public class ProcessBadReasonController : BaseController1<PsProcessBadReason>
{

    public ProcessBadReasonController(DataContext _context, IBaseCRUDVM<PsProcessBadReason> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    [HttpPost]
    public override IActionResult Add(PsProcessBadReason Model)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            //#region 检查是否存在相同的编码
            Utility.CheckCodeExist("", "PsProcessBadReason", "BadCode", Model.BadCode, ModifyType.Add, null, "不良原因代码");
            //#endregion 

            return base.Add(Model);
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    #endregion

    #region 更新重写
    /// <summary>
    /// 更新重写
    /// </summary>
    /// <param name="modelModify"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Update(dynamic modelModify)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {

            #region 检查是否存在相同的编码
            Utility.CheckCodeExist("", "PsProcessBadReason", "BadCode", modelModify.BadCode.Value, ModifyType.Edit, modelModify.ID.Value, "不良原因代码");
            #endregion

            Update<PsProcessBadReason>(modelModify);
            _context.SaveChanges();

            status = "ok";
            message = "修改成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    #endregion

}