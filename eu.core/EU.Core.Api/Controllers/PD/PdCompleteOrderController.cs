/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PdCompleteOrder.cs
*
*功 能： N / A
* 类 名： PdCompleteOrder
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:39:47  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 产品完工入库
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PD)]
public class PdCompleteOrderController : BaseController1<PdCompleteOrder>
{
    /// <summary>
    /// 产品完工入库
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public PdCompleteOrderController(DataContext _context, IBaseCRUDVM<PdCompleteOrder> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(PdCompleteOrder Model)
    {


        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            //#region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", Model.ColorNo, ModifyType.Add, null, "材质编号");
            //#endregion 

            Model.OrderNo = Utility.GenerateContinuousSequence("PdCompleteOrderNo");
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
    ///// <summary>
    ///// 更新重写
    ///// </summary>
    ///// <param name="modelModify"></param>
    ///// <returns></returns>
    //[HttpPost]
    //public override IActionResult Update(dynamic modelModify)
    //{

    //    dynamic obj = new ExpandoObject();
    //    string status = "error";
    //    string message = string.Empty;

    //    try
    //    {

    //        //#region 检查是否存在相同的编码
    //        //Utility.CheckCodeExist("", "BdColor", "ColorNo", modelModify.ColorNo.Value, ModifyType.Edit, modelModify.ID.Value, "材质编号");
    //        //#endregion

    //        Update<PdCompleteOrder>(modelModify);
    //        _context.SaveChanges();

    //        status = "ok";
    //        message = "修改成功！";
    //    }
    //    catch (Exception E)
    //    {
    //        message = E.Message;
    //    }

    //    obj.status = status;
    //    obj.message = message;
    //    return Ok(obj);
    //}
    #endregion

    #region 审核
    /// <summary>
    /// 审核
    /// </summary>
    /// <param name="modelModify"></param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult AuditOrder(dynamic modelModify)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        string orderId = modelModify.orderId;
        string auditStatus = modelModify.auditStatus;
        string sql = string.Empty;
        try
        {


            #region 修改订单审核状态
            if (auditStatus == "Add")
                auditStatus = "CompleteAudit";
            else if (auditStatus == "CompleteAudit")
            {

                //#region 检查单据是否被引用
                //sql = @"SELECT A.ID
                //            FROM PoInOrderDetail A
                //            WHERE     A.SourceOrderId = '{0}'
                //                  AND A.IsDeleted = 'false'
                //                  AND A.IsActive = 'true'
                //                  AND A.OrderSource = 'PdCompleteOrder'
                //            UNION
                //            SELECT A.ID
                //            FROM PoReturnOrderDetail A
                //            WHERE     A.SourceOrderId = '{0}'
                //                  AND A.IsDeleted = 'false'
                //                  AND A.OrderSource = 'PdCompleteOrder'
                //                  AND A.IsActive = 'true'";
                //sql = string.Format(sql, orderId);
                //DataTable dt = DBHelper.Instance.GetDataTable(sql);
                //#endregion

                //if (dt.Rows.Count == 0)
                auditStatus = "Add";
                //else throw new Exception("该单据已被引用，不可撤销！");
            }

            #endregion

            DbUpdate du = new DbUpdate("PdCompleteOrder");
            du.Set("AuditStatus", auditStatus);
            du.Where("ID", "=", orderId);
            DBHelper.Instance.ExecuteScalar(du.GetSql());

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "PD_COMPLETE_ORDER_MNG", "PdCompleteOrder", orderId, OperateType.Update, "Audit", "修改订单审核状态为：" + auditStatus);
            #endregion


            status = "ok";
            message = "提交成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        obj.auditStatus = auditStatus;
        return Ok(obj);
    }
    #endregion

    #region 获取详情
    //[HttpGet]
    //public override async Task<IActionResult> GetById(Guid Id)
    //{
    //    dynamic obj = new ExpandoObject();
    //    string status = "error";
    //    string message = string.Empty;
    //    int count = 0;

    //    try
    //    {
    //        string sql = @"SELECT COUNT (0)
    //                    FROM PdCompleteOrderDetail A
    //                    WHERE     A.IsDeleted = 'false'
    //                          AND A.IsActive = 'true'
    //                          AND A.OrderId = '{0}'";
    //        sql = string.Format(sql, Id);
    //        count = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(sql));

    //        obj.data = _BaseCrud.GetById(Id);

    //    }
    //    catch (Exception E)
    //    {
    //        message = E.Message;
    //    }
    //    obj.count = count;
    //    obj.status = status;
    //    obj.message = message;
    //    return Ok(obj);
    //}
    #endregion

    #region 删除
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
            var Order = _context.PdCompleteOrder.Where(x => x.ID == Id).SingleOrDefault();

            if (Order == null)
                throw new Exception("无效的数据ID！");

            if (Order.AuditStatus != "Add")
                throw new Exception("该单据已审核通过，暂不可进行删除操作！");

            _BaseCrud.DoDelete(Id);

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

    #region 获取来源列表
    /// <summary>
    /// 获取来源列表
    /// </summary>
    /// <param name="paramData">参数</param>
    /// <returns></returns>
    [HttpGet]
    public virtual IActionResult GetSourceList(string paramData)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        int current = 1;
        int pageSize = 10000;
        int total = 0;

        var searchParam = JsonHelper.JsonToObj<Dictionary<string, object>>(paramData);

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

        string defaultSql = @"SELECT A.ID,
                                       A.CreatedTime,
                                       A.ID SourceOrderId,
                                       A.OrderNo,
                                       B.MaterialNo,
                                       B.MaterialName,
                                       B.Specifications,
                                       B.TextureName,
                                       B.UnitName,
                                       A.QTY PdQTY,
                                       0 InQTY,
                                       NULL StockId,
                                       NULL GoodsLocationId,
                                       A.SourceOrderNo LinkOrderNo
                                FROM PdOrder A LEFT JOIN BdMaterial_V B ON A.MaterialId = B.ID
                                WHERE     A.IsDeleted = 'false'
                                      AND A.IsActive = 'true'
                                      AND A.AuditStatus = 'CompleteAudit'";

        string sql = @"SELECT *
                                FROM(SELECT *, ROW_NUMBER() OVER(ORDER BY CreatedTime DESC) ROWNUM
                                      FROM(SELECT *
                                            FROM (" + defaultSql + ") A) B) C WHERE ROWNUM <= {1} AND ROWNUM > {0}";
        sql = string.Format(sql, startIndex, endIndex);
        obj.data = DBHelper.Instance.QueryList<CompleteSourceList>(sql);

        string countSql = @"SELECT COUNT (0) FROM (" + defaultSql + ") A";
        total = Convert.ToInt32(DBHelper.Instance.ExecuteScalar(countSql));
        status = "ok";

        obj.current = current;
        obj.pageSize = pageSize;
        obj.total = total;
        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }

    #endregion

    #region 确认入库
    /// <summary>
    /// 确认入库
    /// </summary>
    /// <param name="Id">入库单ID</param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult ConfirmIn(Guid Id)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        string sql = string.Empty;

        IDbTransaction trans = DBHelper.Instance.GetNewTransaction();

        try
        {

            Utility.IsNullOrEmpty(Id, "入库单ID不能为空！");

            #region 更新订单状态
            DbUpdate du = new DbUpdate("PdCompleteOrder");
            du.Set("AuditStatus", "CompleteIn");
            du.Where("ID", "=", Id);
            DBHelper.Instance.ExecuteDML(du.GetSql(), null, null, trans);
            #endregion

            DBHelper.Instance.CommitTransaction(trans);

            #region 导入订单操作历史
            Utility.RecordOperateLog(User.Identity.Name, "PD_COMPLETE_ORDER_MNG", "PdCompleteOrder", Id.ToString(), OperateType.Update, "CompleteIn", "修改订单状态为：CompleteIn");
            #endregion

            status = "ok";
            message = "入库成功！";
        }
        catch (Exception E)
        {
            DBHelper.Instance.RollbackTransaction(trans);
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    #endregion
}