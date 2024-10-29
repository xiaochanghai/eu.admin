/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ApInvoiceDetail.cs
*
*功 能： N / A
* 类 名： ApInvoiceDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/26 17:41:02  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 应付发票单明细(Controller)
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_AP)]
public class ApInvoiceDetailController : BaseController1<ApInvoiceDetail>
{
    /// <summary>
    /// 应付发票单明细
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public ApInvoiceDetailController(DataContext _context, IBaseCRUDVM<ApInvoiceDetail> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(ApInvoiceDetail Model)
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

            Model.SerialNumber = Utility.GenerateContinuousSequence("ApInvoiceDetail", "SerialNumber", "OrderId", Model.OrderId.ToString());

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
    public override IActionResult BatchAdd(List<ApInvoiceDetail> data)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            Guid? OrderId = data[0].OrderId;
            var invoiceOrder = _context.ApInvoiceOrder.Where(x => x.ID == OrderId).FirstOrDefault();
            var supper = _context.BdSupplier.Where(x => x.ID == invoiceOrder.SupplierId).FirstOrDefault();

            data?.ForEach(o =>
            {
                o.ID = Guid.NewGuid();
                DoAddPrepare(o);
                o.CreatedBy = UserId;
                o.CreatedTime = Utility.GetSysDate();

                #region 税额计算
                //比如税率13%情况下，客户按未税价计算，含税金额=单价x数量x1.13，未税金额=单价x数量
                //比如税率13 % 情况下，客户按含税价计算，含税金额 = 单价x数量，未税金额 = 单价x数量 / 1.13
                //零税
                if (supper.TaxType == "ZeroTax" || o.TaxRate == 0)
                {
                    o.NoTaxAmount = o.Price * o.QTY;
                    o.TaxAmount = 0;
                    o.TaxIncludedAmount = o.NoTaxAmount;
                }//未税
                else if (supper.TaxType == "ExcludingTax")
                {
                    o.NoTaxAmount = o.Price * o.QTY;
                    o.TaxIncludedAmount = o.NoTaxAmount / ((100 + o.TaxRate) / 100);
                    o.TaxAmount = o.TaxIncludedAmount - o.NoTaxAmount;
                }//含税
                else if (supper.TaxType == "IncludingTax")
                {
                    o.TaxIncludedAmount = o.Price * o.QTY;
                    o.NoTaxAmount = o.TaxIncludedAmount / ((100 + o.TaxRate) / 100);
                    o.TaxAmount = o.TaxIncludedAmount - o.NoTaxAmount;
                }
                #endregion
            });
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

    #region 批量更新排序号
    /// <summary>
    /// 批量更新排序号
    /// </summary>
    /// <param name="orderId">订单ID</param>
    private void BatchUpdateSerialNumber(string orderId)
    {
        string sql = @"UPDATE A
                        SET A.SerialNumber = C.NUM
                        FROM ApInvoiceDetail A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM ApInvoiceDetail A
                                          WHERE     1 = 1
                                                AND A.OrderId =
                                                    '{0}'
                                                AND A.IsDeleted = 'false'
                                                AND A.IsActive = 'true') A) B) C
                                ON A.ID = C.ID";
        sql = string.Format(sql, orderId);
        DBHelper.Instance.ExecuteScalar(sql);

        var details = _context.ApInvoiceDetail
        .Where(o => o.IsDeleted == false && o.OrderId == Guid.Parse(orderId))
        .Select(o => new { o.NoTaxAmount, o.TaxAmount, o.TaxIncludedAmount })
        .ToList();

        var NoTaxAmount = details.Sum(o => o.NoTaxAmount);
        var TaxAmount = details.Sum(o => o.TaxAmount);
        var TaxIncludedAmount = details.Sum(o => o.TaxIncludedAmount);
        var order = _context.ApInvoiceOrder.Where(o => o.ID == Guid.Parse(orderId)).FirstOrDefault();
        order.NoTaxAmount = NoTaxAmount;
        order.TaxAmount = TaxAmount;
        order.TaxIncludedAmount = TaxIncludedAmount;
        _context.SaveChanges();
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

            ApInvoiceDetail Model = _context.ApInvoiceDetail.Where(x => x.ID == Id).SingleOrDefault();
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
    /// 批量删除
    /// </summary>
    /// <param name="entryList">list</param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult BatchDelete(List<ApInvoiceDetail> entryList)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            var ids = entryList.Select(o => o.ID).ToList();
            _context.ApInvoiceDetail
                   .Where(x => ids.Contains(x.ID))
                   .UpdateFromQuery(x =>
                   new ApInvoiceDetail
                   {
                       IsDeleted = true,
                       UpdateBy = UserId,
                       UpdateTime = Utility.GetSysDate()
                   });
            _context.SaveChanges();

            if (entryList.Count > 0)
            {
                var detail = _context.ApInvoiceDetail.Where(x => x.ID == entryList[0].ID).FirstOrDefault();
                BatchUpdateSerialNumber(detail.OrderId.ToString());
            }

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

    #region 获取来源数据
    /// <summary>
    /// 获取来源数据
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public virtual IActionResult GetSourceList(string paramData, string masterId, string Source)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        int current = 1;
        int pageSize = 10000;
        int total = 0;
        string sql = string.Empty;
        List<ApInvoiceDetailExtend> list = null;

        try
        {
            var searchParam = JsonHelper.JsonToObj<Dictionary<string, object>>(paramData);
            var invoiceOrder = _context.ApInvoiceOrder.Where(x => x.ID == Guid.Parse(masterId)).FirstOrDefault();
            var supper = _context.BdSupplier.Where(x => x.ID == invoiceOrder.SupplierId).FirstOrDefault();

            #region 处理查询条件
            foreach (var item in searchParam)
            {
                if (item.Key == "current")
                {
                    current = int.Parse(item.Value.ToString());
                    continue;
                }

                if (item.Key == "pageSize")
                {
                    pageSize = int.Parse(item.Value.ToString());
                    continue;
                }
            }
            #endregion

            int _pageSize = pageSize;
            //计算分页起始索引
            int startIndex = current > 1 ? (current - 1) * _pageSize : 0;

            //计算分页结束索引
            int endIndex = current * _pageSize;
            if (Source == "ApCheckOrder")
            {
                sql = @"SELECT *
                                FROM (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) ROWNUM
                                      FROM (SELECT *
                                            FROM (SELECT NEWID () ID,
                                                         'InOrder' OrderSource,
                                                         B.ID SourceOrderId,
                                                         B.OrderNo SourceOrderNo,
                                                         B.OrderDate SourceOrderDate,
                                                         A.ID SourceOrderDetailId,
                                                         A.SerialNumber SourceOrderDetailSerialNumber,
                                                         A.MaterialId,
                                                         D.MaterialNo,
                                                         D.MaterialName,
                                                         D.Specifications,
                                                         D.Description,
                                                         D.UnitName,
                                                         A.CheckQTY - ISNULL (C.QTY, 0) QTY,
                                                         A.CheckQTY - ISNULL (C.QTY, 0) MaxQTY,
                                                         A.CreatedTime,
                                                         A.Price,
                                                         A.TaxRate,
                                                         A.NoTaxAmount,
                                                         A.TaxAmount,
                                                         A.TaxIncludedAmount
                                                  FROM ApCheckDetail A
                                                       JOIN ApCheckOrder B
                                                          ON     A.OrderId = B.ID
                                                             AND B.IsDeleted = 'false'
                                                             AND B.IsActive = 'true'
                                                             AND B.AuditStatus = 'CompleteAudit'
                                                             AND B.SupplierId = '{2}'
                                                       LEFT JOIN BdMaterial_V D ON A.MaterialId = D.ID
                                                       LEFT JOIN ApInvoiceDetailSum_V C
                                                          ON A.ID = C.SourceOrderDetailId
                                                  WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                            WHERE A.QTY > 0) B) C
                                 WHERE ROWNUM <= {1} AND ROWNUM > {0}";
                sql = string.Format(sql, startIndex, endIndex, invoiceOrder.SupplierId);
                list = DBHelper.Instance.QueryList<ApInvoiceDetailExtend>(sql);

                string countString = @"SELECT COUNT (0)
                                                FROM (SELECT A.CheckQTY - ISNULL (C.QTY, 0) QTY
                                                      FROM ApCheckDetail A
                                                           JOIN ApCheckOrder B
                                                              ON     A.OrderId = B.ID
                                                                 AND B.IsDeleted = 'false'
                                                                 AND B.IsActive = 'true'
                                                                 AND B.AuditStatus = 'CompleteAudit'
                                                                 AND B.SupplierId = '{0}'
                                                           LEFT JOIN BdMaterial_V D ON A.MaterialId = D.ID
                                                           LEFT JOIN ApInvoiceDetailSum_V C ON A.ID = C.SourceOrderDetailId
                                                      WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                                WHERE A.QTY > 0";
                countString = string.Format(countString, invoiceOrder.SupplierId);
                total = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(countString));
            }
            else
            {


                sql = @"SELECT *
                                FROM (SELECT NEWID () ID,
                                             *,
                                             ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) ROWNUM
                                      FROM (SELECT *
                                            FROM (SELECT 'InOrder' OrderSource,
                                                         B.ID SourceOrderId,
                                                         B.OrderNo SourceOrderNo,
                                                         B.OrderDate SourceOrderDate,
                                                         A.ID SourceOrderDetailId,
                                                         A.SerialNumber SourceOrderDetailSerialNumber,
                                                         A.MaterialId,
                                                         D.MaterialNo,
                                                         D.MaterialName,
                                                         D.Specifications,
                                                         D.Description,
                                                         D.UnitName,
                                                         A.InQTY - ISNULL (H.QTY, 0) QTY,
                                                         A.InQTY - ISNULL (H.QTY, 0) MaxQTY,
                                                         ISNULL (ISNULL (E.Price, G.Price), 0) Price,
                                                         ISNULL (I.TaxRate, 0) TaxRate,
                                                         '0' NoTaxAmount,
                                                         '0' TaxAmount,
                                                         '0' TaxIncludedAmount,
                                                         A.CreatedTime
                                                  FROM PoInOrderDetail A
                                                       JOIN PoInOrder B
                                                          ON     A.OrderId = B.ID
                                                             AND B.IsDeleted = 'false'
                                                             AND B.IsActive = 'true'
                                                             AND B.AuditStatus = 'CompleteIn'
                                                       --  AND B.SupplierId = '{0}'
                                                           LEFT JOIN BdMaterial_V D ON A.MaterialId = D.ID
                                                       LEFT JOIN PoOrderDetail E
                                                          ON A.SourceOrderDetailId = E.ID
                                                       LEFT JOIN PoArrivalOrderDetail F
                                                          ON A.SourceOrderDetailId = F.ID
                                                       LEFT JOIN PoOrderDetail G
                                                          ON F.SourceOrderDetailId = G.ID
                                                       LEFT JOIN PoOrder I ON I.ID = G.OrderId
                                                       LEFT JOIN ApInvoiceDetailSum_V H
                                                          ON A.ID = H.SourceOrderDetailId
                                                  WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                            WHERE A.QTY > 0
                                            UNION
                                            SELECT *
                                            FROM (SELECT 'FeeOrder' OrderSource,
                                                         B.ID SourceOrderId,
                                                         B.OrderNo SourceOrderNo,
                                                         B.OrderDate SourceOrderDate,
                                                         A.ID SourceOrderDetailId,
                                                         A.SerialNumber SourceOrderDetailSerialNumber,
                                                         A.MaterialId,
                                                         D.MaterialNo,
                                                         D.MaterialName,
                                                         D.Specifications,
                                                         D.Description,
                                                         D.UnitName,
                                                         A.QTY - ISNULL (C.QTY, 0) QTY,
                                                         A.QTY - ISNULL (C.QTY, 0) MaxQTY,
                                                         A.Price,
                                                         A.TaxRate,
                                                         A.NoTaxAmount,
                                                         A.TaxAmount,
                                                         A.TaxIncludedAmount,
                                                         A.CreatedTime
                                                  FROM PoFeeDetail A
                                                       JOIN PoFeeOrder B
                                                          ON     A.OrderId = B.ID
                                                             AND B.IsDeleted = 'false'
                                                             AND B.IsActive = 'true'
                                                       --  AND B.AuditStatus = 'CompleteReturn' -- AND B.SupplierId = '{0}'
                                                       LEFT JOIN ApInvoiceDetailSum_V C
                                                          ON A.ID = C.SourceOrderDetailId
                                                           LEFT JOIN BdMaterial_V D ON A.MaterialId = D.ID
                                                  WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                            WHERE A.QTY > 0) B) C
                                 WHERE ROWNUM <= {1} AND ROWNUM > {0}";
                sql = string.Format(sql, startIndex, endIndex, invoiceOrder.SupplierId);
                list = DBHelper.Instance.QueryList<ApInvoiceDetailExtend>(sql);

                string countString = @"SELECT COUNT (0)
                                    FROM (SELECT *
                                          FROM (SELECT 'InOrder' OrderSource,
                                                       B.ID SourceOrderId,
                                                       B.OrderNo SourceOrderNo,
                                                       B.OrderDate SourceOrderDate,
                                                       A.ID SourceOrderDetailId,
                                                       A.SerialNumber SourceOrderDetailSerialNumber,
                                                       A.MaterialId,
                                                       A.InQTY QTY,
                                                       A.InQTY - ISNULL (H.QTY, 0) CheckQTY,
                                                       ISNULL (ISNULL (E.Price, G.Price), 0) Price,
                                                       ISNULL (I.TaxRate, 0) TaxRate,
                                                       '0' NoTaxAmount,
                                                       '0' TaxAmount,
                                                       '0' TaxIncludedAmount
                                                FROM PoInOrderDetail A
                                                     JOIN PoInOrder B
                                                        ON     A.OrderId = B.ID
                                                           AND B.IsDeleted = 'false'
                                                           AND B.IsActive = 'true'
                                                           AND B.AuditStatus = 'CompleteIn'
                                                     --  AND B.SupplierId = '{0}'
                                                     LEFT JOIN PoOrderDetail E ON A.SourceOrderDetailId = E.ID
                                                     LEFT JOIN PoArrivalOrderDetail F
                                                        ON A.SourceOrderDetailId = F.ID
                                                     LEFT JOIN PoOrderDetail G ON F.SourceOrderDetailId = G.ID
                                                     LEFT JOIN PoOrder I ON I.ID = G.OrderId
                                                     LEFT JOIN ApInvoiceDetailSum_V H
                                                        ON A.ID = H.SourceOrderDetailId
                                                WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                          WHERE A.CheckQTY > 0
                                          UNION
                                          SELECT *
                                          FROM (SELECT 'FeeOrder' OrderSource,
                                                       B.ID SourceOrderId,
                                                       B.OrderNo SourceOrderNo,
                                                       B.OrderDate SourceOrderDate,
                                                       A.ID SourceOrderDetailId,
                                                       A.SerialNumber SourceOrderDetailSerialNumber,
                                                       A.MaterialId,
                                                       A.QTY,
                                                       A.QTY - ISNULL (C.QTY, 0) CheckQTY,
                                                       A.Price,
                                                       A.TaxRate,
                                                       A.NoTaxAmount,
                                                       A.TaxAmount,
                                                       A.TaxIncludedAmount
                                                FROM PoFeeDetail A
                                                     JOIN PoFeeOrder B
                                                        ON     A.OrderId = B.ID
                                                           AND B.IsDeleted = 'false'
                                                           AND B.IsActive = 'true'
                                                     --  AND B.AuditStatus = 'CompleteReturn' -- AND B.SupplierId = '{0}'
                                                     LEFT JOIN ApInvoiceDetailSum_V C
                                                        ON A.ID = C.SourceOrderDetailId
                                                WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                          WHERE A.CheckQTY > 0) A";
                countString = string.Format(countString, invoiceOrder.SupplierId);
                total = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(countString));
            }

            list?.ForEach(o => { o.TaxRate = supper.TaxRate; });
            status = "ok";
        }
        catch (Exception E)
        {
            message = E.Message;
        }
        obj.data = list;
        obj.current = current;
        obj.pageSize = pageSize;
        obj.total = total;
        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }

    #endregion
}
