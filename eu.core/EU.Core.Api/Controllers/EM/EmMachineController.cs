/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EmMachine.cs
*
*功 能： N / A
* 类 名： EmMachine
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 11:26:21  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
using System.Dynamic;
using EU.Core.Common.Enums;
using EU.Core.DataAccess;
using EU.Core.Domain;

namespace EU.Core.Api.Controllers;

/// <summary>
/// 设备基础资料(Controller)
/// </summary>  
[ApiController, ApiExplorerSettings(GroupName = Grouping.GroupName_EM)]
public class MachineController : BaseController1<EmMachine>
{
    /// <summary>
    /// 设备基础资料
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public MachineController(DataContext _context, IBaseCRUDVM<EmMachine> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost, Route("Add")]
    public override IActionResult Add(EmMachine Model)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            //#region 检查是否存在相同的编码
            Utility.CheckCodeExist("", "EmMachine", "MachineNo", Model.MachineNo, ModifyType.Add, null, "设备编号");
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
    [HttpPost, Route("Update")]
    public override IActionResult Update(dynamic modelModify)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {

            #region 检查是否存在相同的编码
            Utility.CheckCodeExist("", "EmMachine", "MachineNo", modelModify.MachineNo.Value, ModifyType.Edit, modelModify.ID.Value, "设备编号");
            #endregion

            Update<EmMachine>(modelModify);
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