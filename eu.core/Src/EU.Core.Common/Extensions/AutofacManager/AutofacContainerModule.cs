namespace EU.Core.Common.Extensions;

public class AutofacContainerModule
{
    public static TService GetService<TService>() where TService : class
    {
        return typeof(TService).GetService() as TService;
    }
}