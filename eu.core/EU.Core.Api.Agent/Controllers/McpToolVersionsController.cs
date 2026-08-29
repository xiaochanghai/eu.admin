using EU.Core.IServices.Mcp;
using Microsoft.AspNetCore.Mvc;
using EU.Core.Model;

namespace EU.Core.Api.Agent.Controllers;

[Route("api/mcp/tool-versions")]
public sealed class McpToolVersionsController(
    IPublishedMcpToolCatalog catalog) : Base.ControllerBase
{
    [HttpGet]
    public async Task<ServiceResult<IReadOnlyList<PublishedMcpToolReference>>> List(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PublishedMcpToolReference> values =
            await catalog.ListAsync(cancellationToken);
        return ServiceResult<IReadOnlyList<PublishedMcpToolReference>>.QuerySuccess(values);
    }
}
