using EU.Core.Api.Agent.Security;
using EU.Core.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Agent.Controllers;

[ApiController]
[Route("api/test/sqlsugar")]
[Authorize(Policy = AgentAuthorizationPolicies.Debug)]
public sealed class SqlSugarTestController(
    IBdSupplierServices supplierServices) : ControllerBase
{
    private const int MaximumTake = 100;

    /// <summary>
    /// Verifies that Agent can resolve EU.Core services and query suppliers via SqlSugar.
    /// </summary>
    [HttpGet("suppliers")]
    [ProducesResponseType<SupplierQueryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SupplierQueryResponse>> GetSuppliers(
        [FromQuery] int take = 10)
    {
        if (take is < 1 or > MaximumTake)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = $"take must be between 1 and {MaximumTake}.",
            });
        }

        var suppliers = await supplierServices.Query(
            supplier => !supplier.IsDeleted
                && supplier.FullName != null
                && supplier.FullName != string.Empty,
            take,
            "SupplierNo asc");
        var items = suppliers
            .Select(supplier => new SupplierSummary(
                supplier.ID,
                supplier.SupplierNo,
                supplier.FullName,
                supplier.ShortName,
                supplier.IsActive))
            .ToArray();

        return Ok(new SupplierQueryResponse(items.Length, items));
    }
}

public sealed record SupplierQueryResponse(int Count, IReadOnlyList<SupplierSummary> Items);

public sealed record SupplierSummary(
    Guid Id, string? SupplierNo, string? FullName, string? ShortName, bool? IsActive);
