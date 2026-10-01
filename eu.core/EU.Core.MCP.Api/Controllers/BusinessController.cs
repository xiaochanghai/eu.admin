using EU.Core.MCP.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace EU.Core.Api.MCP.Controllers;

/// <summary>统一业务 MCP 入口；按当前要求允许匿名，仅限受控环境。</summary>
[Route("/[controller]")]
public sealed class BusinessController : BaseController<IBusinessMcpService>
{
    public BusinessController(IBusinessMcpService service, ILogger<BusinessController> logger) : base(service, logger)
    {
    }

    /// <summary>检查统一业务 MCP 入口是否可访问。</summary>
    [AllowAnonymous, HttpGet]
    public IActionResult HealthCheck() => Ok("MCP API is running!");

    /// <summary>处理初始化、工具发现与调用，保留初始化通知的 202 响应。</summary>
    [HttpPost("mcp")]
    public async Task<IActionResult> HandleAsync([FromBody] JsonRpcRequest request, CancellationToken cancellationToken)
    {
        if (request.Method == "notifications/initialized") return Accepted();
        return Ok(await HandleMcpRequestAsync(request, cancellationToken));
    }
}
