using EU.Core.Api.MCP.Interfaces;
using EU.Core.Api.MCP.Models.Mcp;
using EU.Core.Api.MCP.Services.BusinessQuery.Health;
using EU.Core.MCP.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace EU.Core.Api.MCP.Controllers;

/// <summary>
/// Business Query MCP protocol and health endpoints.
/// </summary>
[AllowAnonymous]
[Route("mcp/business-query")]
public sealed class BusinessQueryController : BaseController<IBusinessQueryService>
{
    private readonly IConfiguration _configuration;
    private readonly BusinessQueryReadiness? _readiness;

    public BusinessQueryController(
        IConfiguration configuration,
        ILogger<BusinessQueryController> logger,
        IBusinessQueryService? service = null,
        BusinessQueryReadiness? readiness = null) : base(service, logger)
    {
        _configuration = configuration;
        _readiness = readiness;
    }

    [HttpPost("controller")]
    public async Task<IActionResult> HandleAsync(
        [FromBody] JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enabled || _service is null)
        {
            return NotFound();
        }

        if (request.Method == "notifications/initialized")
        {
            return Accepted();
        }

        return Ok(await HandleMcpRequestAsync(request, cancellationToken));
    }

    [HttpGet("/health/business-query/live")]
    public IActionResult Live() => Enabled
        ? Ok(new { status = "live" })
        : NotFound();

    [HttpGet("/health/business-query/ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        if (!Enabled || _readiness is null)
        {
            return NotFound();
        }

        bool ready = await _readiness.IsReadyAsync(cancellationToken);
        return StatusCode(
            ready ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable,
            new { status = ready ? "ready" : "unavailable" });
    }

    private bool Enabled => _configuration.GetValue<bool>("BusinessQuery:Enabled");
}
