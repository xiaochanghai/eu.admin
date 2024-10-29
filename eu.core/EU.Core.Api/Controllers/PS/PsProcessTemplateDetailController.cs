/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsProcessTemplateDetail.cs
*
*功 能： N / A
* 类 名： PsProcessTemplateDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:58:31  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

/// <summary>
/// PsProcessTemplateDetail(Controller)
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PS)]
public class ProcessTemplateDetailController : BaseController1<PsProcessTemplateDetail>
{

    public ProcessTemplateDetailController(DataContext _context, IBaseCRUDVM<PsProcessTemplateDetail> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    [HttpPost]
    public override IActionResult Add(PsProcessTemplateDetail Model)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            #region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", Model.ColorNo, ModifyType.Add, null, "材质编号");
            #endregion

            Model.SerialNumber = Utility.GenerateContinuousSequence("PsProcessTemplateDetail", "SerialNumber", "TemplateId", Model.TemplateId.ToString());

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
    [HttpPost]
    public override IActionResult Update(dynamic modelModify)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        string sql = string.Empty;
        try
        {
            //#region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", modelModify.ColorNo.Value, ModifyType.Edit, modelModify.ID.Value, "材质编号");
            //#endregion

            Update<PsProcessTemplateDetail>(modelModify);
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

    #region 批量更新排序号
    /// <summary>
    /// 批量更新排序号
    /// </summary>
    /// <param name="TemplateId">模板ID</param>
    private void BatchUpdateSerialNumber(string TemplateId)
    {
        string sql = @"UPDATE A
                        SET A.SerialNumber = C.NUM
                        FROM PsProcessTemplateDetail A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM PsProcessTemplateDetail A
                                          WHERE     1 = 1
                                                AND A.TemplateId =
                                                    '{0}'
                                                AND A.IsDeleted = 'false'
                                                AND A.IsActive = 'true') A) B) C
                                ON A.ID = C.ID";
        sql = string.Format(sql, TemplateId);
        DBHelper.Instance.ExecuteScalar(sql);

    }
    #endregion

    #region 删除重写

    [HttpGet]
    public override IActionResult Delete(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            _BaseCrud.DoDelete(Id);

            PsProcessTemplateDetail Model = _context.PsProcessTemplateDetail.Where(x => x.ID == Id).SingleOrDefault();
            if (Model != null)
                BatchUpdateSerialNumber(Model.TemplateId.ToString());

            status = "ok";
            message = "删除成功！";
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