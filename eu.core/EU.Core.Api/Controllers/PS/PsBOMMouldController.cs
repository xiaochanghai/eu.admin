/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsBOMMould.cs
*
*功 能： N / A
* 类 名： PsBOMMould
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:58:21  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PS)]
public class BOMMouldController : BaseController1<PsBOMMould>
{

    public BOMMouldController(DataContext _context, IBaseCRUDVM<PsBOMMould> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    [HttpPost]
    public override IActionResult Add(PsBOMMould Model)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            #region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "PsBOMMould", "MaterialId", Model.MaterialId.ToString(), ModifyType.Add, null, "材质编号", "BOMId='" + Model.BOMId + "'");
            #endregion

            Model.SerialNumber = Utility.GenerateContinuousSequence("PsBOMMould", "SerialNumber", "BOMId", Model.BOMId.ToString());

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
            #region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", modelModify.ColorNo.Value, ModifyType.Edit, modelModify.ID.Value, "材质编号", "BOMId='" + modelModify.BOMId.Value + "'");
            #endregion

            Update<PsBOMMould>(modelModify);
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
    /// <param name="BOMId">BOMId</param>
    private void BatchUpdateSerialNumber(string BOMId)
    {
        string sql = @"UPDATE A
                        SET A.SerialNumber = C.NUM
                        FROM PsBOMMould A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM PsBOMMould A
                                          WHERE     1 = 1
                                                AND A.BOMId =
                                                    '{0}'
                                                AND A.IsDeleted = 'false'
                                                AND A.IsActive = 'true') A) B) C
                                ON A.ID = C.ID";
        sql = string.Format(sql, BOMId);
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

            PsBOMMould Model = _context.PsBOMMould.Where(x => x.ID == Id).SingleOrDefault();
            if (Model != null)
                BatchUpdateSerialNumber(Model.BOMId.ToString());

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