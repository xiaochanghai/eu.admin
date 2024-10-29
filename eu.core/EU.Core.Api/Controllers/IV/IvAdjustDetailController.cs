/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* IvAdjustDetail.cs
*
*功 能： N / A
* 类 名： IvAdjustDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 11:59:45  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Api.Controllers;

/// <summary>
/// 库存调整单明细(Controller)
/// </summary>
[ApiController, ApiExplorerSettings(GroupName = Grouping.GroupName_IV)]
public class IvAdjustDetailController : BaseController1<IvAdjustDetail>
{
    /// <summary>
    /// 库存调整单明细
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public IvAdjustDetailController(DataContext _context, IBaseCRUDVM<IvAdjustDetail> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(IvAdjustDetail Model)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            #region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", Model.ColorNo, ModifyType.Add, null, "材质编号");
            #endregion
            //Model.OrderDetailNo = Utility.GenerateContinuousSequence("SdOrderDetailNo");

            Model.SerialNumber = Utility.GenerateContinuousSequence("IvAdjustDetail", "SerialNumber", "OrderId", Model.OrderId.ToString());
            Model.Amount = Model.QTY * Model.Price;
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult BatchAdd(List<IvAdjustDetail> data)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            Guid? OrderId = data[0].OrderId;

            for (int i = 0; i < data.Count; i++)
            {
                data[i].ID = Guid.NewGuid();
                DoAddPrepare(data[i]);
                data[i].CreatedBy = new Guid(User.Identity.Name);
                data[i].Amount = data[i].QTY * data[i].Price;
            }

            if (data.Count > 0)
                DBHelper.Instance.AddRange(data);

            BatchUpdateSerialNumber(OrderId.ToString());

            status = "ok";
            message = "添加成功！";
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
    /// 
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
            //#region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", modelModify.ColorNo.Value, ModifyType.Edit, modelModify.ID.Value, "材质编号");
            //#endregion

            decimal QTY = Convert.ToDecimal(modelModify.QTY);
            decimal Price = Convert.ToDecimal(modelModify.Price);
            modelModify.Amount = QTY * Price;
            Update<IvAdjustDetail>(modelModify);
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
    /// <param name="orderId">订单ID</param>
    private void BatchUpdateSerialNumber(string orderId)
    {
        string sql = @"UPDATE A
                        SET A.SerialNumber = C.NUM
                        FROM IvAdjustDetail A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM IvAdjustDetail A
                                          WHERE     1 = 1
                                                AND A.OrderId =
                                                    '{0}'
                                                AND A.IsDeleted = 'false'
                                                AND A.IsActive = 'true') A) B) C
                                ON A.ID = C.ID";
        sql = string.Format(sql, orderId);
        DBHelper.Instance.ExecuteScalar(sql);

    }
    #endregion

    #region 删除重写
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    [HttpGet]
    public override IActionResult Delete(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            _BaseCrud.DoDelete(Id);

            IvAdjustDetail Model = _context.IvAdjustDetail.Where(x => x.ID == Id).SingleOrDefault();
            if (Model != null)
                BatchUpdateSerialNumber(Model.OrderId.ToString());

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entryList"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult BatchDelete(List<IvAdjustDetail> entryList)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {

            for (int i = 0; i < entryList.Count; i++)
            {
                DbUpdate du = new DbUpdate("IvAdjustDetail");
                du.Set("IsDeleted", "true");
                du.Where("ID", "=", entryList[i].ID);
                DBHelper.Instance.ExecuteScalar(du.GetSql());
            }

            IvAdjustDetail Model = _context.IvAdjustDetail.Where(x => x.ID == entryList[0].ID).SingleOrDefault();
            if (Model != null)
                BatchUpdateSerialNumber(Model.OrderId.ToString());

            status = "ok";
            message = "批量删除成功！";
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