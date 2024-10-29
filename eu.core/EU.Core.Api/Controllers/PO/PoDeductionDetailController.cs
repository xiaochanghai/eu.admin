/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PoDeductionDetail.cs
*
*功 能： N / A
* 类 名： PoDeductionDetail
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 15:55:56  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

/// <summary>
/// 采购扣款单明细
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PO)]
public class PoDeductionDetailController : BaseController1<PoDeductionDetail>
{
    /// <summary>
    /// 采购扣款单明细
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public PoDeductionDetailController(DataContext _context, IBaseCRUDVM<PoDeductionDetail> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 新增
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(PoDeductionDetail Model)
    {


        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            #region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", Model.ColorNo, ModifyType.Add, null, "材质编号");
            #endregion
            //Model.PoDeductionDetailNo = Utility.GenerateContinuousSequence("SdPoDeductionDetailNo");
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
    /// <param name="list"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult BatchAdd(List<PoDeductionDetail> list)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        string OrderId = string.Empty;

        try
        {
            if (list.Count > 0)
            {
                OrderId = list[0].OrderId.ToString();
                PoOrder order = _context.PoOrder.Where(x => x.ID == Guid.Parse(OrderId)).SingleOrDefault();

                list?.ForEach(o =>
                {
                    o.ID = Guid.NewGuid();
                    o.CreatedBy = UserId;
                    o.CreatedTime = Utility.GetSysDate();
                    if (string.IsNullOrEmpty(o.OrderSource) && o.MaterialId != null)
                        o.OrderSource = "Material";
                    DoAddPrepare(o);
                });

                //for (int i = 0; i < list.Count; i++)
                //{
                //    list[i].ID = Guid.NewGuid();
                //    list[i].CreatedBy = UserId;
                //    if(string.IsNullOrEmpty())
                //    DoAddPrepare(list[i]);
                //}
                DBHelper.Instance.AddRange(list);
                BatchUpdateSerialNumber(OrderId);
            }

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
                        FROM PoDeductionDetail A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM PoDeductionDetail A
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

            PoDeductionDetail Model = _context.PoDeductionDetail.Where(x => x.ID == Id).SingleOrDefault();
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