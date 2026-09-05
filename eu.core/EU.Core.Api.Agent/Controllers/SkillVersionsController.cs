using EU.Core.IServices.Skills;
using EU.Core.Api.Agent.Security;
using EU.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;


/// <summary>
/// 提供已发布技能版本查询的 HTTP 接口。
/// </summary>
/// <param name="catalog">用于查询已发布技能版本的目录。</param>
[Route("api/skill-versions")]
[Authorize(Policy = AgentAuthorizationPolicies.Admin)]
public sealed class SkillVersionsController(
    IPublishedSkillVersionCatalog catalog) : Base.ControllerBase
{
    #region 查询列表（List）
    /// <summary>
    /// 查询列表（List）
    /// </summary>
    /// <param name="cancellationToken">用于取消当前异步操作的令牌。</param>
    /// <returns>服务结果，成功时包含已发布技能版本引用集合，失败时包含错误状态和提示。</returns>
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<PublishedSkillReference>>> List(CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedSkillReference> values = await catalog.ListAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<PublishedSkillReference>>.QuerySuccess(values);
    }
    #endregion
}
