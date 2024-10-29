using EU.Core.IServices.BASE;
using EU.Core.Model;

namespace EU.Core.Api.Controllers.Tenant;

/// <summary>
/// 多租户-多表方案 测试
/// </summary>
[Produces("application/json")]
[Route("api/Tenant/ByTable")]
[Authorize, ApiExplorerSettings(GroupName = Grouping.GroupName_Other)]
public class TenantByTableController : BaseApiController
{
    private readonly IBaseServices<MultiBusinessTable> _services;
    private readonly IUser _user;

    public TenantByTableController(IUser user, IBaseServices<MultiBusinessTable> services)
    {
        _user = user;
        _services = services;
    }

    /// <summary>
    /// 获取租户下全部业务数据 <br/>
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ServiceResult<List<MultiBusinessTable>>> GetAll()
    {
        //查询
        // var data = await _services.Query();

        //关联查询
        var data = await _services.Db
            .Queryable<MultiBusinessTable>()
            .Includes(s => s.Child)
            .ToListAsync();
        return Success(data);
    }

    /// <summary>
    /// 新增数据
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<ServiceResult> Post(MultiBusinessTable data)
    {
        await _services.Db.Insertable(data).ExecuteReturnSnowflakeIdAsync();

        return Success();
    }
}