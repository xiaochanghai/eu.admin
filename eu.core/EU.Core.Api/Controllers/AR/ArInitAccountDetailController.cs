/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ArInitAccountDetail.cs
*
*功 能： N / A
* 类 名： ArInitAccountDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/30 18:14:17  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 应收期初建账明细(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_AR)]
public class ArInitAccountDetailController : BaseController1<ArInitAccountDetail>
{
    /// <summary>
    /// 应收期初建账明细
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public ArInitAccountDetailController(DataContext _context, IBaseCRUDVM<ArInitAccountDetail> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(ArInitAccountDetail Model)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            #region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", Model.ColorNo, ModifyType.Add, null, "材质编号");
            #endregion

            Model.SerialNumber = Utility.GenerateContinuousSequence("ArInitAccountDetail", "SerialNumber", "OrderId", Model.OrderId.ToString());
            if (Model.OrderDate is null)
                Model.OrderDate = Utility.GetSysDate();

            #region 税额计算
            ArInitAccountOrder order = _context.ArInitAccountOrder.Where(O => O.ID == Model.OrderId).SingleOrDefault();
            if (order != null)
            {
                BdSupplier supplier = _context.BdSupplier.Where(O => O.ID == order.SupplierId).SingleOrDefault();
                if (supplier != null)
                {
                    //比如税率13%情况下，客户按未税价计算，含税金额=单价x数量x1.13，未税金额=单价x数量
                    //比如税率13 % 情况下，客户按含税价计算，含税金额 = 单价x数量，未税金额 = 单价x数量 / 1.13
                    //零税
                    if (supplier.TaxType == "ZeroTax" || Model.TaxRate == 0)
                    {
                        Model.NoTaxAmount = Model.Price * Model.QTY;
                        Model.TaxAmount = 0;
                        Model.TaxIncludedAmount = Model.NoTaxAmount;
                    }//未税
                    else if (supplier.TaxType == "ExcludingTax")
                    {
                        Model.NoTaxAmount = Model.Price * Model.QTY;
                        Model.TaxIncludedAmount = Model.NoTaxAmount / ((100 + Model.TaxRate) / 100);
                        Model.TaxAmount = Model.TaxIncludedAmount - Model.NoTaxAmount;
                    }//含税
                    else if (supplier.TaxType == "IncludingTax")
                    {
                        Model.TaxIncludedAmount = Model.Price * Model.QTY;
                        Model.NoTaxAmount = Model.TaxIncludedAmount / ((100 + Model.TaxRate) / 100);
                        Model.TaxAmount = Model.TaxIncludedAmount - Model.NoTaxAmount;
                    }
                }
            }

            #endregion

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
    /// 批量新增
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult BatchAdd(List<ArInitAccountDetail> data)
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
                data[i].CreatedBy = UserId;
                data[i].CreatedTime = Utility.GetSysDate();
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
    /// 更新
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

            Guid orderId = Guid.Parse(modelModify.OrderId.Value);

            decimal TaxRate = Convert.ToDecimal(modelModify.TaxRate.Value);
            decimal Price = Convert.ToDecimal(modelModify.Price.Value);
            decimal QTY = Convert.ToDecimal(modelModify.QTY.Value);

            #region 税额计算
            ArInitAccountOrder order = _context.ArInitAccountOrder.Where(O => O.ID == orderId).SingleOrDefault();
            if (order != null)
            {
                BdSupplier supplier = _context.BdSupplier.Where(O => O.ID == order.SupplierId).SingleOrDefault();
                if (supplier != null)
                {
                    ArInitAccountDetail Model = new ArInitAccountDetail();
                    //比如税率13%情况下，客户按未税价计算，含税金额=单价x数量x1.13，未税金额=单价x数量
                    //比如税率13 % 情况下，客户按含税价计算，含税金额 = 单价x数量，未税金额 = 单价x数量 / 1.13
                    //零税
                    if (supplier.TaxType == "ZeroTax" || TaxRate == 0)
                    {
                        Model.NoTaxAmount = Price * QTY;
                        Model.TaxAmount = 0;
                        Model.TaxIncludedAmount = Model.NoTaxAmount;
                    }//未税
                    else if (supplier.TaxType == "ExcludingTax")
                    {
                        Model.NoTaxAmount = Price * QTY;
                        Model.TaxIncludedAmount = Model.NoTaxAmount / ((100 + TaxRate) / 100);
                        Model.TaxAmount = Model.TaxIncludedAmount - Model.NoTaxAmount;
                    }//含税
                    else if (supplier.TaxType == "IncludingTax")
                    {
                        Model.TaxIncludedAmount = Price * QTY;
                        Model.NoTaxAmount = Model.TaxIncludedAmount / ((100 + TaxRate) / 100);
                        Model.TaxAmount = Model.TaxIncludedAmount - Model.NoTaxAmount;
                    }
                    modelModify.TaxIncludedAmount = Model.TaxIncludedAmount;
                    modelModify.NoTaxAmount = Model.NoTaxAmount;
                    modelModify.TaxAmount = Model.TaxAmount;
                }
            }

            #endregion

            Update<ApInitAccountDetail>(modelModify);
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
                        FROM ArInitAccountDetail A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM ArInitAccountDetail A
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
    /// 删除
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

            ArInitAccountDetail Model = _context.ArInitAccountDetail.Where(x => x.ID == Id).SingleOrDefault();
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
    #endregion
}