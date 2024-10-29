using Microsoft.AspNetCore.Http;

namespace EU.Core.Common;

public static class HttpUseContext
{
    private static IHttpContextAccessor _accessor;

    public static HttpContext Current => _accessor?.HttpContext;

    internal static void Configure(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }
}
