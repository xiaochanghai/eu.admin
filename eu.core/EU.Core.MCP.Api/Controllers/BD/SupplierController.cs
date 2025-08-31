using EU.Core.Api.MCP.Interfaces;
using EU.Core.MCP.Controllers;

namespace EU.Core.Api.MCP.Controllers;

/// <summary>
/// π©”¶…Ã
/// </summary>
public class SupplierController : BaseController<ISupplierService>
{
    public SupplierController(ISupplierService service, ILogger<SupplierController> logger) : base(service, logger)
    {
    }
}