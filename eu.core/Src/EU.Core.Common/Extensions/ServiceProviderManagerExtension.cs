namespace EU.Core.Common.Extensions;

public static class ServiceProviderManagerExtension
{
    public static object GetService(this Type serviceType)
    {
        // HttpContext.Current.RequestServices.GetRequiredService<T>(serviceType);
        return HttpUseContext.Current.RequestServices.GetService(serviceType);
    }

}
