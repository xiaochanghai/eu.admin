/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ApCheckDetail.cs
*
*功 能： N / A
* 类 名： ApCheckDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/26 14:04:44  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
using EU.Core.Common.Const;

namespace EU.Core.Api.Controllers;

/// <summary>
/// 应付对账单明细(Controller)
/// </summary>

[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_AP)]
public class ApCheckDetailController : BaseController1<ApCheckDetail>
{
    /// <summary>
    /// 应付对账单明细
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public ApCheckDetailController(DataContext _context, IBaseCRUDVM<ApCheckDetail> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(ApCheckDetail Model)
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

            Model.SerialNumber = Utility.GenerateContinuousSequence("ApCheckDetail", "SerialNumber", "OrderId", Model.OrderId.ToString());

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
    public override IActionResult BatchAdd(List<ApCheckDetail> data)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            Guid? OrderId = data[0].OrderId;

            var CheckOrder = _context.ApCheckOrder.Where(x => x.ID == OrderId).FirstOrDefault();
            var supper = _context.BdSupplier.Where(x => x.ID == CheckOrder.SupplierId).FirstOrDefault();

            for (int i = 0; i < data.Count; i++)
            {
                data[i].ID = Guid.NewGuid();
                DoAddPrepare(data[i]);
                data[i].CreatedBy = UserId;
                data[i].CreatedTime = Utility.GetSysDate();

                #region 税额计算
                //比如税率13%情况下，客户按未税价计算，含税金额=单价x数量x1.13，未税金额=单价x数量
                //比如税率13 % 情况下，客户按含税价计算，含税金额 = 单价x数量，未税金额 = 单价x数量 / 1.13
                //零税
                if (supper.TaxType == "ZeroTax" || supper.TaxRate == 0)
                {
                    data[i].NoTaxAmount = data[i].Price * data[i].CheckQTY;
                    data[i].TaxAmount = 0;
                    data[i].TaxIncludedAmount = data[i].NoTaxAmount;
                }//未税
                else if (supper.TaxType == "ExcludingTax")
                {
                    data[i].NoTaxAmount = data[i].Price * data[i].CheckQTY;
                    data[i].TaxIncludedAmount = data[i].NoTaxAmount / ((100 + supper.TaxRate) / 100);
                    data[i].TaxAmount = data[i].TaxIncludedAmount - data[i].NoTaxAmount;
                }//含税
                else if (supper.TaxType == "IncludingTax")
                {
                    data[i].TaxIncludedAmount = data[i].Price * data[i].CheckQTY;
                    data[i].NoTaxAmount = data[i].TaxIncludedAmount / ((100 + supper.TaxRate) / 100);
                    data[i].TaxAmount = data[i].TaxIncludedAmount - data[i].NoTaxAmount;
                }
                #endregion
            }

            if (data.Count > 0)
                DBHelper.AddRange(data);

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
    private async Task BatchUpdateSerialNumber(string orderId)
    {
        string sql = @"UPDATE A
                        SET A.SerialNumber = C.NUM
                        FROM ApCheckDetail A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM ApCheckDetail A
                                          WHERE     1 = 1
                                                AND A.OrderId =
                                                    '{0}'
                                                AND A.IsDeleted = 'false'
                                                AND A.IsActive = 'true') A) B) C
                                ON A.ID = C.ID";
        sql = string.Format(sql, orderId);
        await DBHelper.ExecuteScalarAsync(sql);

        var details = await _context.ApCheckDetail
            .AsNoTracking()
            .Where(o => o.IsDeleted == false && o.OrderId == Guid.Parse(orderId))
            .Select(o => new { o.NoTaxAmount, o.TaxAmount, o.TaxIncludedAmount })
            .ToListAsync();

        var NoTaxAmount = details.Sum(o => o.NoTaxAmount);
        var TaxAmount = details.Sum(o => o.TaxAmount);
        var TaxIncludedAmount = details.Sum(o => o.TaxIncludedAmount);
        var order = await _context.ApCheckOrder.Where(o => o.ID == Guid.Parse(orderId)).FirstOrDefaultAsync();
        order.NoTaxAmount = NoTaxAmount;
        order.TaxAmount = TaxAmount;
        order.TaxIncludedAmount = TaxIncludedAmount;
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

            ApCheckDetail Model = _context.ApCheckDetail.Where(x => x.ID == Id).SingleOrDefault();
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
        int current = 1;
        int pageSize = 10000;
        int total = 0;
        string sql = string.Empty;
        List<ApCheckDetailExtend> list = null;

        try
        {
            var searchParam = JsonHelper.JsonToObj<Dictionary<string, object>>(paramData);
            var CheckOrder = _context.ApCheckOrder.Where(x => x.ID == Guid.Parse(masterId)).FirstOrDefault();
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

            int _pageSize = pageSize;
            //计算分页起始索引
            int startIndex = current > 1 ? (current - 1) * _pageSize : 0;

            //计算分页结束索引
            int endIndex = current * _pageSize;
            if (Source == "InOrder")
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
                                                         A.InQTY QTY,
                                                         NULL HasCheckQTY,
                                                         NULL NoCheckQTY,
                                                         A.InQTY - ISNULL (H.CheckQTY, 0) CheckQTY,
                                                         A.InQTY - ISNULL (H.CheckQTY, 0) MaxCheckQTY,
                                                         A.CreatedTime,
                                                         ISNULL (ISNULL (E.Price, G.Price), 0) Price
                                                  FROM PoInOrderDetail A
                                                       JOIN PoInOrder B
                                                          ON     A.OrderId = B.ID
                                                             AND B.IsDeleted = 'false'
                                                             AND B.IsActive = 'true'
                                                             AND B.AuditStatus = 'CompleteIn'
                                                             AND B.SupplierId = '{2}'
                                                       LEFT JOIN BdMaterial_V D ON A.MaterialId = D.ID
                                                       LEFT JOIN PoOrderDetail E ON A.SourceOrderDetailId = E.ID
                                                       LEFT JOIN PoArrivalOrderDetail F ON A.SourceOrderDetailId = F.ID
                                                       LEFT JOIN PoOrderDetail G ON F.SourceOrderDetailId = G.ID
                                                       LEFT JOIN ApCheckDetailSum_V H ON A.ID = H.SourceOrderDetailId
                                                  WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                            WHERE A.CheckQTY > 0) B) C
                                 WHERE ROWNUM <= {1} AND ROWNUM > {0}";
                sql = string.Format(sql, startIndex, endIndex, CheckOrder.SupplierId);
                list = DBHelper.Instance.QueryList<ApCheckDetailExtend>(sql);

                string countString = @"SELECT COUNT (0)
                                                FROM (SELECT A.InQTY - ISNULL (H.CheckQTY, 0) CheckQTY
                                                      FROM PoInOrderDetail A
                                                           JOIN PoInOrder B
                                                              ON     A.OrderId = B.ID
                                                                 AND B.IsDeleted = 'false'
                                                                 AND B.IsActive = 'true'
                                                                 AND B.AuditStatus = 'CompleteIn'
                                                                 AND B.SupplierId = '{0}'
                                                           LEFT JOIN ApCheckDetailSum_V H ON A.ID = H.SourceOrderDetailId
                                                      WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                                WHERE A.CheckQTY > 0";
                countString = string.Format(countString, CheckOrder.SupplierId);
                total = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(countString));
            }
            else if (Source == "ReturnOrder")
            {
                sql = @"SELECT *
                                FROM (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) ROWNUM
                                      FROM (SELECT *
                                                FROM (SELECT NEWID () ID,
                                                             'ReturnOrder' OrderSource,
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
                                                             A.ReturnQTY QTY,
                                                             NULL HasCheckQTY,
                                                             NULL NoCheckQTY,
                                                             A.ReturnQTY - ISNULL (I.CheckQTY, 0) CheckQTY,
                                                             A.ReturnQTY - ISNULL (I.CheckQTY, 0) MaxCheckQTY,
                                                             A.CreatedTime,
                                                             ISNULL (ISNULL (F.Price, H.Price), 0) Price
                                                      FROM PoReturnOrderDetail A
                                                           JOIN PoReturnOrder B
                                                              ON     A.OrderId = B.ID
                                                                 AND B.IsDeleted = 'false'
                                                                 AND B.IsActive = 'true'
                                                                 AND B.AuditStatus = 'CompleteReturn'
                                                                 AND B.SupplierId = '{2}'
                                                           LEFT JOIN BdMaterial_V D ON A.MaterialId = D.ID
                                                           LEFT JOIN PoInOrderDetail E ON A.SourceOrderDetailId = E.ID
                                                           LEFT JOIN PoOrderDetail F ON E.SourceOrderDetailId = F.ID
                                                           LEFT JOIN PoArrivalOrderDetail G ON A.SourceOrderDetailId = G.ID
                                                           LEFT JOIN PoOrderDetail H ON G.SourceOrderDetailId = H.ID
                                                           LEFT JOIN ApCheckDetailSum_V I ON A.ID = I.SourceOrderDetailId
                                                      WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                                WHERE A.CheckQTY > 0) B) C
                                 WHERE ROWNUM <= {1} AND ROWNUM > {0}";
                sql = string.Format(sql, startIndex, endIndex, CheckOrder.SupplierId);
                list = DBHelper.Instance.QueryList<ApCheckDetailExtend>(sql);

                string countString = @"SELECT COUNT (0)
                                                FROM (SELECT A.ReturnQTY - ISNULL (I.CheckQTY, 0) CheckQTY
                                                      FROM PoReturnOrderDetail A
                                                           JOIN PoReturnOrder B
                                                              ON     A.OrderId = B.ID
                                                                 AND B.IsDeleted = 'false'
                                                                 AND B.IsActive = 'true'
                                                                 AND B.AuditStatus = 'CompleteReturn'
                                                                AND B.SupplierId = '{0}'
                                                           LEFT JOIN ApCheckDetailSum_V I ON A.ID = I.SourceOrderDetailId
                                                      WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                                WHERE A.CheckQTY > 0";
                countString = string.Format(countString, CheckOrder.SupplierId);
                total = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(countString));
            }
            else if (Source == "FeeOrder")
            {
                sql = @"SELECT *
                                FROM (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) ROWNUM
                                      FROM (SELECT *
                                                FROM (SELECT NEWID () ID,
                                                             'FeeOrder' OrderSource,
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
                                                             A.QTY,
                                                             NULL HasCheckQTY,
                                                             NULL NoCheckQTY,
                                                             A.QTY - ISNULL (C.CheckQTY, 0) CheckQTY,
                                                             A.QTY - ISNULL (C.CheckQTY, 0) MaxCheckQTY,
                                                             A.CreatedTime,
                                                             A.Price
                                                      FROM PoFeeDetail A
                                                           JOIN PoFeeOrder B
                                                              ON     A.OrderId = B.ID
                                                                 AND B.IsDeleted = 'false'
                                                                 AND B.IsActive = 'true'
                                                           --  AND B.AuditStatus = 'CompleteReturn'
                                                                 AND B.SupplierId = '{2}'
                                                           LEFT JOIN BdMaterial_V D ON A.MaterialId = D.ID
                                                           LEFT JOIN ApCheckDetailSum_V C ON A.ID = C.SourceOrderDetailId
                                                      WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                                WHERE A.CheckQTY > 0) B) C
                                 WHERE ROWNUM <= {1} AND ROWNUM > {0}";
                sql = string.Format(sql, startIndex, endIndex, CheckOrder.SupplierId);
                list = DBHelper.Instance.QueryList<ApCheckDetailExtend>(sql);

                string countString = @"SELECT COUNT (0)
                                                FROM (SELECT A.QTY - ISNULL (C.CheckQTY, 0) CheckQTY
                                                      FROM PoFeeDetail A
                                                           JOIN PoFeeOrder B
                                                              ON     A.OrderId = B.ID
                                                                 AND B.IsDeleted = 'false'
                                                                 AND B.IsActive = 'true'
                                                                 --  AND B.AuditStatus = 'CompleteReturn'
                                                                 AND B.SupplierId = '{0}'
                                                           LEFT JOIN ApCheckDetailSum_V C ON A.ID = C.SourceOrderDetailId
                                                      WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                                WHERE A.CheckQTY > 0";
                countString = string.Format(countString, CheckOrder.SupplierId);
                total = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(countString));
            }
            else
            {
                sql = @"SELECT *
                                FROM (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) ROWNUM
                                      FROM (SELECT *
                                                FROM (SELECT NEWID () ID,
                                                             'InitAccount' OrderSource,
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
                                                             A.QTY,
                                                             NULL HasCheckQTY,
                                                             NULL NoCheckQTY,
                                                             A.QTY - ISNULL (C.CheckQTY, 0) CheckQTY,
                                                             A.QTY - ISNULL (C.CheckQTY, 0) MaxCheckQTY,
                                                             A.CreatedTime
                                                      FROM ApInitAccountDetail A
                                                           JOIN ApInitAccountOrder B
                                                              ON     A.OrderId = B.ID
                                                                 AND B.IsDeleted = 'false'
                                                                 AND B.IsActive = 'true'
                                                                 --  AND B.AuditStatus = 'CompleteReturn'
                                                                 AND B.SupplierId = '{2}'
                                                           LEFT JOIN BdMaterial_V D ON A.MaterialId = D.ID
                                                           LEFT JOIN ApCheckDetailSum_V C ON A.ID = C.SourceOrderDetailId
                                                      WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                                WHERE A.CheckQTY > 0) B) C
                                 WHERE ROWNUM <= {1} AND ROWNUM > {0}";
                sql = string.Format(sql, startIndex, endIndex, CheckOrder.SupplierId);
                list = DBHelper.Instance.QueryList<ApCheckDetailExtend>(sql);

                string countString = @"SELECT COUNT (0)
                                                FROM (SELECT A.QTY - ISNULL (C.CheckQTY, 0) CheckQTY
                                                      FROM ApInitAccountDetail A
                                                           JOIN ApInitAccountOrder B
                                                              ON     A.OrderId = B.ID
                                                                 AND B.IsDeleted = 'false'
                                                                 AND B.IsActive = 'true'
                                                                 --  AND B.AuditStatus = 'CompleteReturn'
                                                                 AND B.SupplierId = '{0}'
                                                           LEFT JOIN ApCheckDetailSum_V C ON A.ID = C.SourceOrderDetailId
                                                      WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                                WHERE A.CheckQTY > 0";
                countString = string.Format(countString, CheckOrder.SupplierId);
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

    #region 获取详情
    /// <summary>
    /// 获取详情
    /// </summary>
    /// <param name="Id">数据ID</param>
    /// <returns></returns>
    [HttpGet("{Id}")]
    public async Task<ServiceResult<dynamic>> GetById(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        dynamic extend = new ExpandoObject();

        try
        {
            ApCheckDetail detail = await _context.ApCheckDetail.Where(O => O.ID == Id).FirstOrDefaultAsync();
            obj.data = detail;

            string sql = "SELECT CheckQTY FROM ApCheckDetailSum_V";
            //sql = string.Format(sql, detail.SourceOrderDetailId);
            ApCheckDetailExtend detailExtend = (await DBHelper.QueryListAsync<ApCheckDetailExtend>(sql, null)).FirstOrDefault();
            if (detailExtend != null)
                extend.MaxCheckQTY = (detail.QTY - detailExtend.CheckQTY) + detail.CheckQTY;
            else
                extend.MaxCheckQTY = 0;
            obj.extend = extend;
            return ServiceResult<dynamic>.OprateSuccess(obj, ResponseText.QUERY_SUCCESS);

        }
        catch (Exception)
        {
            throw;
        }
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

            var CheckOrder = _context.ApCheckOrder.Where(x => x.ID == orderId).FirstOrDefault();
            var supper = _context.BdSupplier.Where(x => x.ID == CheckOrder.SupplierId).FirstOrDefault();

            decimal? TaxRate = supper.TaxRate;
            decimal Price = Convert.ToDecimal(modelModify.Price.Value);
            decimal QTY = Convert.ToDecimal(modelModify.CheckQTY.Value);

            #region 税额计算
            ApCheckOrder order = _context.ApCheckOrder.Where(O => O.ID == orderId).SingleOrDefault();
            if (order != null)
            {
                BdSupplier supplier = _context.BdSupplier.Where(O => O.ID == order.SupplierId).SingleOrDefault();
                if (supplier != null)
                {
                    ApCheckDetail Model = new ApCheckDetail();
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

            Update<ApCheckDetail>(modelModify);
            _context.SaveChanges();

            BatchUpdateSerialNumber(orderId.ToString());

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

    #region 生成明细数据
    /// <summary>
    /// 生成明细数据
    /// </summary>
    /// <param name="orderId">订单ID</param>
    /// <returns></returns>
    [HttpPost, Route("{orderId}")]
    public async Task<ServiceResult> GenerateDetail(Guid orderId)
    {

        try
        {

            var checkOrder = await _context.ApCheckOrder.FindAsync(orderId);
            var supper = await _context.BdSupplier.FindAsync(checkOrder.SupplierId);

            string sql = @"SELECT NEWID () ID, GETDATE () CreatedTime, *
                                FROM(SELECT *
                                      FROM(SELECT 'InOrder' OrderSource,
                                                   B.ID SourceOrderId,
                                                   B.OrderNo SourceOrderNo,
                                                   B.OrderDate SourceOrderDate,
                                                   A.ID SourceOrderDetailId,
                                                   A.SerialNumber SourceOrderDetailSerialNumber,
                                                   A.MaterialId,
                                                   A.InQTY QTY,
                                                   NULL HasCheckQTY,
                                                   NULL NoCheckQTY,
                                                   A.InQTY - ISNULL(H.CheckQTY, 0) CheckQTY,
                                                   ISNULL(ISNULL(E.Price, G.Price), 0) Price
                                            FROM PoInOrderDetail A
                                                 JOIN PoInOrder B
                                                    ON     A.OrderId = B.ID
                                                       AND B.IsDeleted = 'false'
                                                       AND B.IsActive = 'true'
                                                       AND B.AuditStatus = 'CompleteIn'
                                                       AND B.SupplierId = '{0}'
                                                 LEFT JOIN PoOrderDetail E ON A.SourceOrderDetailId = E.ID
                                                 LEFT JOIN PoArrivalOrderDetail F
                                                    ON A.SourceOrderDetailId = F.ID
                                                 LEFT JOIN PoOrderDetail G ON F.SourceOrderDetailId = G.ID
                                                 LEFT JOIN ApCheckDetailSum_V H
                                                    ON A.ID = H.SourceOrderDetailId
                                            WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                      WHERE A.CheckQTY > 0
                                      UNION
                                      SELECT *
                                      FROM(SELECT 'ReturnOrder' OrderSource,
                                                   B.ID SourceOrderId,
                                                   B.OrderNo SourceOrderNo,
                                                   B.OrderDate SourceOrderDate,
                                                   A.ID SourceOrderDetailId,
                                                   A.SerialNumber SourceOrderDetailSerialNumber,
                                                   A.MaterialId,
                                                   A.ReturnQTY QTY,
                                                   NULL HasCheckQTY,
                                                   NULL NoCheckQTY,
                                                   A.ReturnQTY - ISNULL(I.CheckQTY, 0) CheckQTY,
                                                   ISNULL(ISNULL(F.Price, H.Price), 0) Price
                                            FROM PoReturnOrderDetail A
                                                 JOIN PoReturnOrder B
                                                    ON     A.OrderId = B.ID
                                                       AND B.IsDeleted = 'false'
                                                       AND B.IsActive = 'true'
                                                       AND B.AuditStatus = 'CompleteReturn'
                                                       AND B.SupplierId = '{0}'
                                                 LEFT JOIN PoInOrderDetail E ON A.SourceOrderDetailId = E.ID
                                                 LEFT JOIN PoOrderDetail F ON E.SourceOrderDetailId = F.ID
                                                 LEFT JOIN PoArrivalOrderDetail G
                                                    ON A.SourceOrderDetailId = G.ID
                                                 LEFT JOIN PoOrderDetail H ON G.SourceOrderDetailId = H.ID
                                                 LEFT JOIN ApCheckDetailSum_V I
                                                    ON A.ID = I.SourceOrderDetailId
                                            WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                      WHERE A.CheckQTY > 0
                                      UNION
                                      SELECT *
                                      FROM(SELECT 'FeeOrder' OrderSource,
                                                   B.ID SourceOrderId,
                                                   B.OrderNo SourceOrderNo,
                                                   B.OrderDate SourceOrderDate,
                                                   A.ID SourceOrderDetailId,
                                                   A.SerialNumber SourceOrderDetailSerialNumber,
                                                   A.MaterialId,
                                                   A.QTY,
                                                   NULL HasCheckQTY,
                                                   NULL NoCheckQTY,
                                                   A.QTY - ISNULL(C.CheckQTY, 0) CheckQTY,
                                                   A.Price
                                            FROM PoFeeDetail A
                                                 JOIN PoFeeOrder B
                                                    ON     A.OrderId = B.ID
                                                       AND B.IsDeleted = 'false'
                                                       AND B.IsActive = 'true'
                                                       --  AND B.AuditStatus = 'CompleteReturn'
                                                       AND B.SupplierId = '{0}'
                                                 LEFT JOIN ApCheckDetailSum_V C
                                                    ON A.ID = C.SourceOrderDetailId
                                            WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                      WHERE A.CheckQTY > 0
                                      UNION
                                      SELECT *
                                      FROM(SELECT 'InitAccount' OrderSource,
                                                   B.ID SourceOrderId,
                                                   B.OrderNo SourceOrderNo,
                                                   B.OrderDate SourceOrderDate,
                                                   A.ID SourceOrderDetailId,
                                                   A.SerialNumber SourceOrderDetailSerialNumber,
                                                   A.MaterialId,
                                                   A.QTY,
                                                   NULL HasCheckQTY,
                                                   NULL NoCheckQTY,
                                                   A.QTY - ISNULL(C.CheckQTY, 0) CheckQTY,
                                                   0  Price
                                            FROM ApInitAccountDetail A
                                                 JOIN ApInitAccountOrder B
                                                    ON     A.OrderId = B.ID
                                                       AND B.IsDeleted = 'false'
                                                       AND B.IsActive = 'true'
                                                       --  AND B.AuditStatus = 'CompleteReturn'
                                                       AND B.SupplierId = '{0}'
                                                 LEFT JOIN ApCheckDetailSum_V C
                                                    ON A.ID = C.SourceOrderDetailId
                                            WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                      WHERE A.CheckQTY > 0) A";
            sql = string.Format(sql, checkOrder.SupplierId);
            var list = await Common.Helper.DBHelper.QueryListAsync<ApCheckDetail>(sql);
            list?.ForEach(o =>
            {
                DoAddPrepare(o);
                o.OrderId = orderId;

                #region 税额计算
                //比如税率13%情况下，客户按未税价计算，含税金额=单价x数量x1.13，未税金额=单价x数量
                //比如税率13 % 情况下，客户按含税价计算，含税金额 = 单价x数量，未税金额 = 单价x数量 / 1.13
                //零税
                if (supper.TaxType == "ZeroTax" || supper.TaxRate == 0)
                {
                    o.NoTaxAmount = o.Price * o.CheckQTY;
                    o.TaxAmount = 0;
                    o.TaxIncludedAmount = o.NoTaxAmount;
                }//未税
                else if (supper.TaxType == "ExcludingTax")
                {
                    o.NoTaxAmount = o.Price * o.CheckQTY;
                    o.TaxIncludedAmount = o.NoTaxAmount / ((100 + supper.TaxRate) / 100);
                    o.TaxAmount = o.TaxIncludedAmount - o.NoTaxAmount;
                }//含税
                else if (supper.TaxType == "IncludingTax")
                {
                    o.TaxIncludedAmount = o.Price * o.CheckQTY;
                    o.NoTaxAmount = o.TaxIncludedAmount / ((100 + supper.TaxRate) / 100);
                    o.TaxAmount = o.TaxIncludedAmount - o.NoTaxAmount;
                }
                #endregion
            });
            if (list.Count > 0)
            {
                await _context.AddRangeAsync(list);
                await BatchUpdateSerialNumber(orderId.ToString());
            }

            await _context.SaveChangesAsync();

            return ServiceResult.OprateSuccess("生成成功！");

        }
        catch (Exception)
        {
            throw;
        }

    }
    #endregion
}