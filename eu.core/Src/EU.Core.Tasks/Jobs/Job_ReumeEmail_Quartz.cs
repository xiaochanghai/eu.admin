/// <summary>
/// 这里要注意下，命名空间和程序集是一样的，不然反射不到
/// </summary>
namespace EU.Core.Tasks;

public class Job_ReumeEmail_Quartz : JobBase, IJob
{
    private readonly IRmReumeServices _resumeServices;

    public Job_ReumeEmail_Quartz(ISmQuartzJobServices tasksQzServices,
        ISmQuartzJobLogServices tasksQzLogServices,
        IRmReumeServices resumeServices) : base(tasksQzServices, tasksQzLogServices)
    {
        _resumeServices = resumeServices;
    }
    public async Task Execute(IJobExecutionContext context)
    {
        await ExecuteJob(context, async () => await Run(context));
    }
    public async Task Run(IJobExecutionContext context)
    {
        await _resumeServices.ReadPdfAttachmentsAsync();
    }
}
