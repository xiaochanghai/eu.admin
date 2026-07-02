namespace EU.Core.Common.Extensions;

public static class ServiceProviderManagerExtension
{
    public static object GetService(this Type serviceType)
    {
        // HttpContext.Current.RequestServices.GetRequiredService<T>(serviceType);
        var serviceProvider = App.HttpContext?.RequestServices ?? App.RootServices;
        return serviceProvider?.GetService(serviceType);
    }

}
