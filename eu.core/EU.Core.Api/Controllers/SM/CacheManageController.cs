using EU.Core.Common.Caches;
using EU.Core.Model;
namespace EU.Core.Api.Controllers.Systems;

/// <summary>
/// 缓存管理
/// </summary>
[Route("api/Systems/[controller]")]
[ApiController, ApiExplorerSettings(GroupName = Grouping.GroupName_Assistant)]
[Authorize(Permissions.Name)]
public class CacheManageController : BaseApiController
{
    private readonly ICaching _caching;

    public CacheManageController(ICaching caching)
    {
        _caching = caching;
    }

    /// <summary>
    /// 获取全部缓存
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ServiceResult<List<string>>> Get()
    {
        return Success(await _caching.GetAllCacheKeysAsync());
    }

    /// <summary>
    /// 获取缓存
    /// </summary>
    /// <returns></returns>
    [HttpGet("{key}")]
    public async Task<ServiceResult<string>> Get(string key)
    {
        return Success<string>(await _caching.GetStringAsync(key));
    }

    /// <summary>
    /// 新增
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<ServiceResult> Post([FromQuery] string key, [FromQuery] string value, [FromQuery] int? expire)
    {
        if (expire.HasValue)
            await _caching.SetStringAsync(key, value, TimeSpan.FromMilliseconds(expire.Value));
        else
            await _caching.SetStringAsync(key, value);

        return Success();
    }

    /// <summary>
    /// 删除全部缓存
    /// </summary>
    /// <returns></returns>
    [HttpDelete]
    public async Task<ServiceResult> Delete()
    {
        await _caching.RemoveAllAsync();
        return Success();
    }

    /// <summary>
    /// 删除缓存
    /// </summary>
    /// <returns></returns>
    [Route("{key}")]
    [HttpDelete]
    public async Task<ServiceResult> Delete(string key)
    {
        await _caching.RemoveAsync(key);
        return Success();
    }
}