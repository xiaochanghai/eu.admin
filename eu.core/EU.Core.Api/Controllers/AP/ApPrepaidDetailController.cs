/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ApPrepaidDetail.cs
*
*功 能： N / A
* 类 名： ApPrepaidDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/26 17:54:43  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 采购预付款明细(Controller)
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_AP)]
public class ApPrepaidDetailController : BaseController1<ApPrepaidDetail>
{
    /// <summary>
    /// 采购预付款明细
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public ApPrepaidDetailController(DataContext _context, IBaseCRUDVM<ApPrepaidDetail> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(ApPrepaidDetail Model)
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

            Model.SerialNumber = Utility.GenerateContinuousSequence("ApPrepaidDetail", "SerialNumber", "OrderId", Model.OrderId.ToString());

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
    public override IActionResult BatchAdd(List<ApPrepaidDetail> data)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            Guid? OrderId = data[0].OrderId;
            Guid? sourceOrderDetailId = data[0].SourceOrderDetailId;

            var CheckOrder = _context.ApPrepaidOrder.Where(x => x.ID == OrderId).FirstOrDefault();
            var supper = _context.BdSupplier.Where(x => x.ID == CheckOrder.SupplierId).FirstOrDefault();
            if (data.Count > 0)
            {
                data?.ForEach(o =>
                {
                    DoAddPrepare(o);
                    o.ID = Guid.NewGuid();
                    o.CreatedBy = UserId;
                    o.CreatedTime = Utility.GetSysDate();
                });

                _context.ApPrepaidDetail.AddRange(data);
                _context.SaveChanges();
            }

            BatchUpdateSerialNumber(OrderId.ToString());

            #region 更新采购单-已付金额
            var prepaidDetails = _context.ApPrepaidDetail.AsQueryable()
                .Where(o => o.SourceOrderDetailId == sourceOrderDetailId && o.IsDeleted == false && o.IsActive == true)
                .Select(o => new { o.PaymentAmount, o.OrderSource })
                .ToList();
            var amount = prepaidDetails.Sum(o => o.PaymentAmount);
            var orderPrepayment = _context.PoOrderPrepayment.Where(o => o.ID == sourceOrderDetailId).FirstOrDefault();
            orderPrepayment.HasAmount = amount;
            _context.SaveChanges();
            #endregion

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
                        FROM ApPrepaidDetail A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM ApPrepaidDetail A
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

            ApPrepaidDetail Model = _context.ApPrepaidDetail.Where(x => x.ID == Id).SingleOrDefault();
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
        int total = 0;
        string sql = string.Empty;
        List<ApPrepaidDetailExtend> list = null;

        Utility.GetPageData(paramData, out int current, out int pageSize);

        try
        {
            var searchParam = JsonHelper.JsonToObj<Dictionary<string, object>>(paramData);
            var CheckOrder = _context.ApPrepaidOrder.Where(x => x.ID == Guid.Parse(masterId)).FirstOrDefault();
            var supper = _context.BdSupplier.Where(x => x.ID == CheckOrder.SupplierId).FirstOrDefault();

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

            Utility.GetPageIndex(paramData, out int startIndex, out int endIndex);

            sql = @"SELECT *
                                FROM (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) ROWNUM
                                      FROM (SELECT *
                                                FROM (SELECT A.ID,
                                                             'POOrder' OrderSource,
                                                             A.OrderId SourceOrderId,
                                                             B.OrderNo SourceOrderNo,
                                                             A.ID SourceOrderDetailId,
                                                             A.Amount,
                                                             A.[Percent],
                                                             A.Amount - ISNULL (C.PaymentAmount, 0) PaymentAmount,
                                                             A.Amount - ISNULL (C.PaymentAmount, 0) MaxPaymentAmount,
                                                             ISNULL (D.TaxIncludedAmount, 0) TaxIncludedAmount,
                                                             A.CreatedTime
                                                      FROM PoOrderPrepayment A
                                                           JOIN PoOrder B
                                                              ON     A.OrderId = B.ID
                                                                 AND B.IsDeleted = 'false'
                                                                 AND B.IsActive = 'true'
                                                                 AND B.AuditStatus ! = 'Add'
                                                                 AND B.SupplierId = '{2}'
                                                           LEFT JOIN ApPrepaidDetailSum_V C ON A.ID = C.SourceOrderDetailId
                                                           LEFT JOIN PdOrderDetailTaxIncludedAmount_V D
                                                              ON A.OrderId = D.OrderId
                                                      WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                                WHERE A.PaymentAmount > 0) B) C
                                 WHERE ROWNUM <= {1} AND ROWNUM > {0}";
            sql = string.Format(sql, startIndex, endIndex, CheckOrder.SupplierId);
            list = DBHelper.Instance.QueryList<ApPrepaidDetailExtend>(sql);

            string countString = @"SELECT COUNT(0)
                                        FROM (SELECT 
                                                     A.Amount - ISNULL (H.PaymentAmount, 0) Amount
                                              FROM PoOrderPrepayment A
                                                   JOIN PoOrder B
                                                      ON     A.OrderId = B.ID
                                                         AND B.IsDeleted = 'false'
                                                         AND B.IsActive = 'true'
                                                         AND B.AuditStatus ! = 'Add'
                                                    AND B.SupplierId = '{0}'
                                                   LEFT JOIN ApPrepaidDetailSum_V H ON A.ID = H.SourceOrderDetailId
                                              WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                        WHERE A.Amount > 0";
            countString = string.Format(countString, CheckOrder.SupplierId);
            total = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(countString));


            //list?.ForEach(o => { o.TaxRate = supper.TaxRate; });
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

    #region 获取详情
    /// <summary>
    /// 获取详情
    /// </summary>
    /// <param name="Id">数据ID</param>
    /// <returns></returns>
    [HttpGet]
    public override async Task<IActionResult> GetById(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        dynamic extend = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        int count = 0;

        try
        {

            ApPrepaidDetail detail = await _context.ApPrepaidDetail.Where(O => O.ID == Id).FirstOrDefaultAsync();
            var detailExtend = AgileObjects.AgileMapper.Mapper.Map(detail).ToANew<ApPrepaidDetailExtend>();
            //var customer = detail.Map().ToANew<ApPrepaidDetailExtend>();
            string sql = $@"SELECT A.Amount - ISNULL (C.PaymentAmount, 0) MaxPaymentAmount
                                    FROM PoOrderPrepayment A
                                         -- AND B.SupplierId = '{2}'
                                         LEFT JOIN
                                         (SELECT SUM (ISNULL (A.PaymentAmount, 0)) PaymentAmount,
                                                 A.SourceOrderDetailId
                                          FROM ApPrepaidDetail A
                                               JOIN ApPrepaidOrder B
                                                  ON     A.OrderId = B.ID
                                                     AND A.IsActive = B.IsActive
                                                     AND A.IsDeleted = B.IsDeleted
                                          WHERE     B.IsDeleted = 'false'
                                                AND B.IsActive = 'true'
                                                AND A.PaymentAmount IS NOT NULL
                                                AND A.PaymentAmount > 0
                                                AND A.ID ! = '{detail.ID}'
                                          GROUP BY A.SourceOrderDetailId) C
                                            ON A.ID = C.SourceOrderDetailId
                                    WHERE A.ID = '{detail.SourceOrderDetailId}'";
            ApPrepaidDetailExtend max = await DBHelper.Instance.QueryFirstAsync<ApPrepaidDetailExtend>(sql);
            detailExtend.MaxPaymentAmount = max.MaxPaymentAmount;
            obj.data = detailExtend;
            status = "ok";
        }
        catch (Exception E)
        {
            message = E.Message;
        }
        obj.count = count;
        obj.extend = extend;
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
            Guid Id = Guid.Parse(modelModify.Id.Value);

            Update<ApPrepaidDetail>(modelModify);
            _context.SaveChanges();

            BatchUpdateSerialNumber(orderId.ToString());

            #region 更新采购单-已付金额
            Guid? sourceOrderDetailId = _context.ApPrepaidDetail.AsQueryable().Where(o => o.ID == Id).FirstOrDefault()?.ID;
            var prepaidDetails = _context.ApPrepaidDetail.AsQueryable()
                .Where(o => o.SourceOrderDetailId == sourceOrderDetailId && o.IsDeleted == false && o.IsActive == true)
                .Select(o => new { o.PaymentAmount, o.OrderSource })
                .ToList();
            var amount = prepaidDetails.Sum(o => o.PaymentAmount);
            var orderPrepayment = _context.PoOrderPrepayment.Where(o => o.ID == sourceOrderDetailId).FirstOrDefault();
            orderPrepayment.HasAmount = amount;
            _context.SaveChanges();
            #endregion

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