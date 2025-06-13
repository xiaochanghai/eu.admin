/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* BdCustomerDeliveryAddress.cs
*
*功 能： N / A
* 类 名： BdCustomerDeliveryAddress
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/25 17:23:45  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 客户地址 (服务)
/// </summary>
public class BdCustomerDeliveryAddressServices : BaseServices<BdCustomerDeliveryAddress, BdCustomerDeliveryAddressDto, InsertBdCustomerDeliveryAddressInput, EditBdCustomerDeliveryAddressInput>, IBdCustomerDeliveryAddressServices
{
    private readonly IBaseRepository<BdCustomerDeliveryAddress> _dal;
    public BdCustomerDeliveryAddressServices(IBaseRepository<BdCustomerDeliveryAddress> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);
        var result = await base.Add(entity);
        if (model.IsDefault == true)
            await Db.Updateable<BdCustomerDeliveryAddress>()
                .SetColumns(it => new BdCustomerDeliveryAddress()
                {
                    IsDefault = false,
                    UpdateBy = UserId,
                    UpdateTime = Utility.GetSysDate()
                })
                .Where(x => x.ID != result && x.IsDefault == true)
                .ExecuteCommandAsync();

        return result;
    }
    #endregion

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var model = ConvertToEntity(entity);
        if (model.IsDefault == true)
            await Db.Updateable<BdCustomerDeliveryAddress>()
                .SetColumns(it => new BdCustomerDeliveryAddress()
                {
                    IsDefault = false
                })
                .Where(x => x.ID != Id && x.IsDefault == true)
                .ExecuteCommandAsync();
        return await base.Update(Id, entity);
    }
    #endregion

    public async Task<ServiceResult<BdCustomerDeliveryAddress>> GetDefaultData(Guid masterId)
    {
        var src = Db.Queryable<BdCustomerDeliveryAddress>();

        var address = await src
            .Where(x => x.CustomerId == masterId && x.IsDefault == true && x.IsActive == true && x.IsDeleted == false)
            .FirstAsync();
        if (address == null)
            address = await src.Where(x => x.CustomerId == masterId && x.IsActive == true && x.IsDeleted == false).FirstAsync();

        return ServiceResult<BdCustomerDeliveryAddress>.OprateSuccess(address);

    }
}