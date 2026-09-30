namespace EU.Core.Api.MCP.Interfaces;

public interface ISupplierService : IBaseService
{
    /// <summary>匿名返回全部公司的有效供应商分页数据，仅暴露最小业务字段。</summary>
    /// <param name="input">供应商查询条件。</param>
    /// <param name="cancellationToken">调用取消令牌。</param>
    /// <returns>包含供应商数据的 MCP 文本结果。</returns>
    Task<McpToolResult> QuerySuppliersAsync(EU.Core.Model.ViewModels.Extend.SupplierQueryInput input, CancellationToken cancellationToken = default);
}
