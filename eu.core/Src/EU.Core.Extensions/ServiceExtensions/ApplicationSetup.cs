using EU.Core.Common;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace EU.Core.Extensions;

public static class ApplicationSetup
{
    public static void UseApplicationSetup(this WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            App.IsRun = true;
        });

        app.Lifetime.ApplicationStopped.Register(() =>
        {
            App.IsRun = false;

            //清除日志
            Log.CloseAndFlush();
        });
    }
}