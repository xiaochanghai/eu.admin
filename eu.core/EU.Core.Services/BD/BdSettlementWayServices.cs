/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdSettlementWay.cs
*
*功 能： N / A
* 类 名： BdSettlementWay
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/25 19:29:37  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 结算方式 (服务)
/// </summary>
public class BdSettlementWayServices : BaseServices<BdSettlementWay, BdSettlementWayDto, InsertBdSettlementWayInput, EditBdSettlementWayInput>, IBdSettlementWayServices
{
    private readonly IBaseRepository<BdSettlementWay> _dal;
    public BdSettlementWayServices(IBaseRepository<BdSettlementWay> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }
    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);

        var dic = ConvertToDic(entity);
        var lstColumns = dic.Keys.Where(x => x != "ID" && x != "Id").ToList();

        #region 检查是否存在相同值
        CheckOnly(model);
        #endregion

        var enumData = LovHelper.GetLovList("SettlementAccountType").ToList();

        if (enumData.Any())
        {
            string SettlementName = string.Empty;
            var info = enumData.Where(x => x.Value == model.SettlementAccountType).SingleOrDefault();
            if (info != null)
                SettlementName = info.Text;
            if (model.Days > 0)
                SettlementName += ",付款天数为" + model.Days + "天";
            model.SettlementName = SettlementName;
            lstColumns.Add("SettlementName");
        }

        return await BaseDal.Add(model, lstColumns);

    }
    #endregion

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);

        #region 检查是否存在相同值
        CheckOnly(model, Id);
        #endregion

        var enumData = LovHelper.GetLovList("SettlementAccountType").ToList();

        if (enumData.Any())
        {
            string SettlementName = string.Empty;
            var info = enumData.Where(x => x.Value == model.SettlementAccountType).SingleOrDefault();
            if (info != null)
                SettlementName = info.Text;
            if (model.Days > 0)
                SettlementName += ",付款天数为" + model.Days + "天";
            model.SettlementName = SettlementName;
        }

        var dic = ConvertToDic(entity);
        var lstColumns = dic.Keys.ToList();
        lstColumns.Add("SettlementName");
        return await Update(model, lstColumns);
    }
    #endregion

}