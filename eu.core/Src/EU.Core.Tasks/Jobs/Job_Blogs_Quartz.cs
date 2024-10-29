using EU.Core.IServices;
using Quartz;

/// <summary>
/// 这里要注意下，命名空间和程序集是一样的，不然反射不到
/// </summary>
namespace EU.Core.Tasks;

public class Job_Blogs_Quartz : JobBase, IJob
{

    public Job_Blogs_Quartz(ISmQuartzJobServices tasksQzServices, ISmQuartzJobLogServices tasksQzLogServices) : base(tasksQzServices, tasksQzLogServices)
    {
    }
    public async Task Execute(IJobExecutionContext context)
    {
        await ExecuteJob(context, async () => await Run(context));
    }
    public async Task Run(IJobExecutionContext context)
    {
        //Logger.WriteLog($"Job_Blogs_Quartz 执行！");
        var list = await _taskServices.Query();
        //// 也可以通过数据库配置，获取传递过来的参数
        //JobDataMap data = context.JobDetail.JobDataMap;
        //int jobId = data.GetInt("JobParam");
    }
}
