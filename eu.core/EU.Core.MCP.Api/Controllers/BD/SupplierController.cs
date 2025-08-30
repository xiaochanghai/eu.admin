using ClaudeMCP.API.Interfaces;
using EU.Core.MCP.Controllers;

namespace ClaudeMCP.API.Controllers;

/// <summary>
/// π©”¶…Ã
/// </summary>
public class SupplierController : BaseController<ISupplierService>
{
    public SupplierController(ISupplierService service, ILogger<SupplierController> logger) : base(service, logger)
    {
    }
}