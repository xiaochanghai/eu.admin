/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ArPrepaidDetail.cs
*
*功 能： N / A
* 类 名： ArPrepaidDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 11:06:14  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 销售预收款明细(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_AR)]
public class ArPrepaidDetailController : BaseController1<ArPrepaidDetail>
{
    /// <summary>
    /// 销售预收款明细
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public ArPrepaidDetailController(DataContext _context, IBaseCRUDVM<ArPrepaidDetail> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(ArPrepaidDetail Model)
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

            Model.SerialNumber = Utility.GenerateContinuousSequence("ArPrepaidDetail", "SerialNumber", "OrderId", Model.OrderId.ToString());

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
    public override IActionResult BatchAdd(List<ArPrepaidDetail> data)
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

    #region 批量更新排序号
    /// <summary>
    /// 批量更新排序号
    /// </summary>
    /// <param name="orderId">订单ID</param>
    private void BatchUpdateSerialNumber(string orderId)
    {
        string sql = @"UPDATE A
                        SET A.SerialNumber = C.NUM
                        FROM ArPrepaidDetail A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM ArPrepaidDetail A
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

            ArPrepaidDetail Model = _context.ArPrepaidDetail.Where(x => x.ID == Id).SingleOrDefault();
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
        List<ArPrepaidDetailExtend> list = null;

        Utility.GetPageData(paramData, out int current, out int pageSize);

        try
        {
            var order = _context.ArPrepaidOrder.Where(x => x.ID == Guid.Parse(masterId)).FirstOrDefault();
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
                                                             A.Amount - ISNULL (C.CollectionAmount, 0) CollectionAmount,
                                                             A.Amount - ISNULL (C.CollectionAmount, 0) MaxCollectionAmount,
                                                             ISNULL (D.TaxIncludedAmount, 0) TaxIncludedAmount,
                                                             A.CreatedTime
                                                      FROM SdOrderPrepayment A
                                                           JOIN SdOrder B
                                                              ON     A.OrderId = B.ID
                                                                 AND B.IsDeleted = 'false'
                                                                 AND B.IsActive = 'true'
                                                                 AND B.AuditStatus ! = 'Add'
                                                                 AND B.CustomerId = '{2}'
                                                           LEFT JOIN ArPrepaidDetailSum_V C ON A.ID = C.SourceOrderDetailId
                                                           LEFT JOIN SdOrderDetailTaxIncludedAmount_V D
                                                              ON A.OrderId = D.OrderId
                                                      WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                                WHERE A.CollectionAmount > 0) B) C
                                 WHERE ROWNUM <= {1} AND ROWNUM > {0}";
            sql = string.Format(sql, startIndex, endIndex, order.CustomerId);
            list = DBHelper.Instance.QueryList<ArPrepaidDetailExtend>(sql);

            string countString = @"SELECT COUNT (0)
                                            FROM (SELECT A.Amount - ISNULL (C.CollectionAmount, 0) CollectionAmount
                                                  FROM SdOrderPrepayment A
                                                       JOIN SdOrder B
                                                          ON     A.OrderId = B.ID
                                                             AND B.IsDeleted = 'false'
                                                             AND B.IsActive = 'true'
                                                             AND B.AuditStatus ! = 'Add'
                                                             AND B.CustomerId = '{0}'
                                                       LEFT JOIN ArPrepaidDetailSum_V C ON A.ID = C.SourceOrderDetailId
                                                  WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                            WHERE A.CollectionAmount > 0";
            countString = string.Format(countString, order.CustomerId);
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

            ArPrepaidDetail detail = _context.ArPrepaidDetail.Where(O => O.ID == Id).SingleOrDefault();
            var detailExtend = AgileObjects.AgileMapper.Mapper.Map(detail).ToANew<ArPrepaidDetailExtend>();
            //var customer = detail.Map().ToANew<ApPrepaidDetailExtend>();
            string sql = $@"SELECT A.Amount - ISNULL (C.CollectionAmount, 0) MaxCollectionAmount
                                    FROM SdOrderPrepayment A
                                         -- AND B.SupplierId = '{2}'
                                         LEFT JOIN
                                         (SELECT SUM (ISNULL (A.CollectionAmount, 0)) CollectionAmount,
                                                 A.SourceOrderDetailId
                                          FROM ArPrepaidDetail A
                                               JOIN ArPrepaidOrder B
                                                  ON     A.OrderId = B.ID
                                                     AND A.IsActive = B.IsActive
                                                     AND A.IsDeleted = B.IsDeleted
                                          WHERE     B.IsDeleted = 'false'
                                                AND B.IsActive = 'true'
                                                AND A.CollectionAmount IS NOT NULL
                                                AND A.CollectionAmount > 0
                                                AND A.ID ! = '{detail.ID}'
                                          GROUP BY A.SourceOrderDetailId) C
                                            ON A.ID = C.SourceOrderDetailId
                                    WHERE A.ID = '{detail.SourceOrderDetailId}'";
            ArPrepaidDetailExtend max = DBHelper.Instance.QueryFirst<ArPrepaidDetailExtend>(sql);
            detailExtend.MaxCollectionAmount = max.MaxCollectionAmount;
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
}