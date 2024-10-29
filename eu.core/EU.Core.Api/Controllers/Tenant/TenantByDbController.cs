using EU.Core.IServices.BASE;
using EU.Core.Model;

namespace EU.Core.Api.Controllers.Tenant;

/// <summary>
/// 多租户-多库方案 测试
/// </summary>
[Produces("application/json")]
[Route("api/Tenant/ByDb")]
[Authorize, ApiExplorerSettings(GroupName = Grouping.GroupName_Other)]
public class TenantByDbController : BaseApiController
{
    private readonly IBaseServices<SubLibraryBusinessTable> _services;
    private readonly IUser _user;

    public TenantByDbController(IUser user, IBaseServices<SubLibraryBusinessTable> services)
    {
        _user = user;
        _services = services;
    }

    /// <summary>
    /// 获取租户下全部业务数据 <br/>
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ServiceResult<List<SubLibraryBusinessTable>>> GetAll()
    {
        var data = await _services.Query();
        return Success(data);
    }

    /// <summary>
    /// 新增数据
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<ServiceResult> Post(SubLibraryBusinessTable data)
    {
        await _services.Db.Insertable(data).ExecuteReturnSnowflakeIdAsync();

        return Success();
    }
}