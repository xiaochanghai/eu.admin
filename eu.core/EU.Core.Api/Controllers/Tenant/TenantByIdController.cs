using EU.Core.IServices.BASE;

namespace EU.Core.Api.Controllers.Tenant;

/// <summary>
/// 多租户-Id方案 测试
/// </summary>
[Produces("application/json")]
[Route("api/Tenant/ById")]
[Authorize, ApiExplorerSettings(GroupName = Grouping.GroupName_Other)]
public class TenantByIdController : BaseApiController
{
    private readonly IBaseServices<BusinessTable> _services;
    private readonly IUser _user;

    public TenantByIdController(IUser user, IBaseServices<BusinessTable> services)
    {
        _user = user;
        _services = services;
    }

    /// <summary>
    /// 获取租户下全部业务数据 <br/>
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ServiceResult<List<BusinessTable>>> GetAll()
    {
        var data = await _services.Query();
        return Success(data);
    }

    /// <summary>
    /// 新增业务数据
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<ServiceResult> Post([FromBody] BusinessTable data)
    {
        await _services.Db.Insertable(data).ExecuteReturnSnowflakeIdAsync();
        return Success();
    }
}