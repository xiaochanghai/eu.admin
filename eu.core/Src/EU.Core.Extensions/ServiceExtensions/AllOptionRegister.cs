using EU.Core.Common;
using EU.Core.Common.Option.Core;
using Microsoft.Extensions.DependencyInjection;

namespace EU.Core.Extensions;

public static class AllOptionRegister
{
    public static void AddAllOptionRegister(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        foreach (var optionType in App.EffectiveTypes.Where(s =>
                     !s.IsInterface && typeof(IConfigurableOptions).IsAssignableFrom(s)))
        {
            services.AddConfigurableOptions(optionType);
        }
    }
}