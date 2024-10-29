/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* ArSalesCollectionWriteOff.cs
*
*功 能： N / A
* 类 名： ArSalesCollectionWriteOff
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 11:18:05  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 销售收款核销明细(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_AR)]
public class ArSalesCollectionWriteOffController : BaseController1<ArSalesCollectionWriteOff>
{
    /// <summary>
    /// 销售收款核销明细
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public ArSalesCollectionWriteOffController(DataContext _context, IBaseCRUDVM<ArSalesCollectionWriteOff> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(ArSalesCollectionWriteOff Model)
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

            Model.SerialNumber = Utility.GenerateContinuousSequence("ArSalesCollectionWriteOff", "SerialNumber", "OrderId", Model.OrderId.ToString());

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
    public override IActionResult BatchAdd(List<ArSalesCollectionWriteOff> data)
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
                        FROM ArSalesCollectionWriteOff A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM ArSalesCollectionWriteOff A
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

            ArSalesCollectionWriteOff Model = _context.ArSalesCollectionWriteOff.Where(x => x.ID == Id).SingleOrDefault();
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
    /// <param name="paramData">参数</param>
    /// <param name="masterId">masterId</param>
    /// <param name="Source">来源</param>
    /// <returns></returns>
    [HttpGet]
    public virtual IActionResult GetSourceList(string paramData, string masterId, string Source)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        int total = 0;
        string sql = string.Empty;
        List<ArSalesCollectionWriteOffExtend> list = null;
        Utility.GetPageData(paramData, out int current, out int pageSize);

        try
        {
            var order = _context.ArSalesCollectionOrder.Where(x => x.ID == Guid.Parse(masterId)).FirstOrDefault();

            Utility.GetPageIndex(paramData, out int startIndex, out int endIndex);

            if (Source == "ArInvoiceOrder")
            {

            }
            else
            {

                sql = @"SELECT *
                                FROM (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) ROWNUM
                                      FROM (SELECT *
                                        FROM (SELECT A.ID,
                                                     'ArPrepaidOrder'
                                                        OrderSource,
                                                     A.OrderId
                                                        SourceOrderId,
                                                     B.OrderNo
                                                        SourceOrderNo,
                                                     A.ID
                                                        SourceOrderDetailId,
                                                     A.CollectionAmount,
                                                     A.CollectionAmount - ISNULL (C.ReceivableAmount, 0)
                                                        ReceivableAmount,
                                                     A.CollectionAmount - ISNULL (C.ReceivableAmount, 0)
                                                        MaxReceivableAmount,
                                                     A.CreatedTime
                                              FROM ArPrepaidDetail A
                                                   JOIN ArPrepaidOrder B
                                                      ON     A.OrderId = B.ID
                                                         AND B.IsDeleted = 'false'
                                                         AND B.IsActive = 'true'
                                                         AND B.AuditStatus ! = 'Add'
                                                    AND B.CustomerId = '{2}'
                                                   LEFT JOIN ArSalesCollectionWriteOffSum_V C
                                                      ON A.ID = C.SourceOrderDetailId
                                              WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                        WHERE A.ReceivableAmount > 0) B) C
                                 WHERE ROWNUM <= {1} AND ROWNUM > {0}";
                sql = string.Format(sql, startIndex, endIndex, order.CustomerId);
                list = DBHelper.Instance.QueryList<ArSalesCollectionWriteOffExtend>(sql);

                string countString = @"SELECT COUNT (0)
                                            FROM (SELECT A.CollectionAmount - ISNULL (C.ReceivableAmount, 0)
                                                            ReceivableAmount
                                                  FROM ArPrepaidDetail A
                                                       JOIN ArPrepaidOrder B
                                                          ON     A.OrderId = B.ID
                                                             AND B.IsDeleted = 'false'
                                                             AND B.IsActive = 'true'
                                                             AND B.AuditStatus ! = 'Add'
                                                         AND B.CustomerId = '{0}'
                                                       LEFT JOIN ArSalesCollectionWriteOffSum_V C
                                                          ON A.ID = C.SourceOrderDetailId
                                                  WHERE A.IsDeleted = 'false' AND A.IsActive = 'true') A
                                            WHERE A.ReceivableAmount > 0";
                countString = string.Format(countString, order.CustomerId);
                total = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(countString));
            }
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