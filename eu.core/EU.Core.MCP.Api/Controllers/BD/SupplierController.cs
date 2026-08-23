using EU.Core.MCP.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace EU.Core.Api.MCP.Controllers;

/// <summary>
/// Supplier MCP endpoints.
/// </summary>
[Route("/[controller]")]
public class SupplierController : BaseController<ISupplierService>
{
    public SupplierController(
        ISupplierService service,
        ILogger<SupplierController> logger) : base(service, logger)
    {
    }

    [AllowAnonymous, HttpGet]
    public IActionResult HealthCheck() => Ok("MCP API is running!");

    [HttpPost("mcp")]
    public Task<JsonRpcResponse> HandleMcpRequest(
        [FromBody] JsonRpcRequest request,
        CancellationToken cancellationToken) =>
        HandleMcpRequestAsync(request, cancellationToken);
}
