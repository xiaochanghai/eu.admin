/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmQuartzJob.cs
*
*功 能： N / A
* 类 名： SmQuartzJob
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/24 23:35:32  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

using EU.Core.Tasks;
using Quartz;

namespace EU.Core.Services;

/// <summary>
/// 任务调度 (服务)
/// </summary>
public class SmQuartzJobServices : BaseServices<SmQuartzJob, SmQuartzJobDto, InsertSmQuartzJobInput, EditSmQuartzJobInput>, ISmQuartzJobServices
{
    private readonly IBaseRepository<SmQuartzJob> _dal;
    public SmQuartzJobServices(IBaseRepository<SmQuartzJob> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 远程操作
    /// <summary>
    /// 远程操作
    /// </summary>
    /// <param name="id">任务清单标识</param>
    /// <param name="operate">操作值 字典Code为`DIC.TASK.OPERATE`</param>
    /// <param name="args">操作参数，当操作为修改参数是必填</param>
    /// <returns></returns>
    public async Task<ServiceResult> Operate(Guid id, string operate, SmQuartzJob args)
    {
        //消息内容
        var task = await QueryDto(id);
        var taskMsg = new TaskMsg
        {
            MsgId = Utility.GuidId,
            TaskType = JobConsts.TASK_TYPE_JOB,
            TaskId = id,
            TaskCode = task.JobCode,
            Oprate = operate,
            Args = args.ScheduleRule
        };
        //发送消息至对应的任务
        var result = Success();
        switch (operate)
        {
            //获取配置文件
            case "CONF":
            //当前日志
            case "LOG.CURRENT":
            //历史日志
            case "LOG.HISTORY":
                {
                    //发送消息并等待接收返回值
                    (bool suc, object o) = TaskHelper.SendMsg(taskMsg);
                    if (suc)
                        result.Data = o;
                    else
                        result = Failed(o.ToString());
                    break;
                }
            default:
                {
                    if (operate == "ARGS")
                    {
                        try
                        {
                            var expression = new CronExpression(taskMsg.Args);
                            if (expression == null)
                                return Failed($"表达式格式不正确!");
                        }
                        catch (Exception ex)
                        {
                            return Failed($"表达式格式不正确:{ex.Message}");
                        }

                    }
                    //发送消息并直接返回操作成功
                    TaskHelper.PostMsg(taskMsg);
                    result.Message = $"操作成功";
                    break;
                }
        }
        return result;
    }
    #endregion
}