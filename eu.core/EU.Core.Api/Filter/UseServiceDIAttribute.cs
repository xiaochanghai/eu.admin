using Microsoft.AspNetCore.Mvc.Filters;

namespace EU.Core.Filter;

public class UseServiceDIAttribute : ActionFilterAttribute
{

    protected readonly ILogger<UseServiceDIAttribute> _logger;
    private readonly string _name;

    public UseServiceDIAttribute(ILogger<UseServiceDIAttribute> logger, string Name = "")
    {
        _logger = logger;
        _name = Name;
    }


    public override void OnActionExecuted(ActionExecutedContext context)
    { 
        _logger.LogInformation("测试自定义服务特性");
        Console.WriteLine(_name);
        base.OnActionExecuted(context);
        DeleteSubscriptionFiles();
    }

    private void DeleteSubscriptionFiles()
    {

    }
}
