namespace EU.Core.Common.Const;


/// <summary>
/// MQ消费类型
/// </summary>
public enum ConsumeAction
{
    /// <summary>
    /// 消费成功
    /// </summary>
    Accept = 1,
    /// <summary>
    /// 消费失败，可以放回队列重新消费
    /// </summary>
    Retry = 2,
    /// <summary>
    /// 消费失败，直接丢弃
    /// </summary>
    Rehect = 3,
    /// <summary>
    /// 其他
    /// </summary>
    Other = 4
}
/// <summary>
/// MQ消息队列名称
/// </summary>
public class RabbitMQConsts
{
    /// <summary>
    /// IOT -- 联机盒接收测血压指令
    /// </summary>
    public const string CLIENT_ID_JOB_IOT_BP = "IOT-BP";
    /// <summary>
    /// IOT -- 自动透中检测
    /// </summary>
    public const string CLIENT_ID_JOB_IOT_AutoMonitor = "IOT-AutoMonitor";
    /// <summary>
    /// 任务处理中心 -- 班次
    /// </summary>
    public const string CLIENT_ID_JOB_Shift = "JobProcessingCenter-Shift";
    /// <summary>
    /// 任务处理中心 -- IOT -- 联机盒数据上报
    /// </summary>
    public const string CLIENT_ID_JOB_IOT_BOX_REPORT = "JobProcessingCenter-IOT-BoxReport";
    /// <summary>
    /// 任务处理中心 -- IOT -- 联机盒控制设备
    /// </summary>
    public const string CLIENT_ID_JOB_IOT_BOX_CONTROL = "JobProcessingCenter-IOT-BoxControl";
    /// <summary>
    /// 任务处理中心 -- JOB -- JOB相关系统参数变更
    /// </summary>
    public const string CLIENT_ID_JOB_SYS_CHANGE = "JobProcessingCenter-Job-SysChange";
    /// <summary>
    /// 任务处理中心 -- JOB -- 患者透析充分性任务
    /// </summary>
    public const string CLIENT_ID_JOB_PATIENT_HA = "JobProcessingCenter-Job-HemodialysisAdequacy";
    /// <summary>
    /// 任务处理中心 -- FSC
    /// </summary>
    public const string CLIENT_ID_FSC = "FSC";
    /// <summary>
    /// 任务处理中心 -- FSC回传
    /// </summary>
    public const string CLIENT_ID_FSC_CALLBACK = "FSC-CALLBACK";
    /// <summary>
    /// 任务处理中心 -- TASK -- JOB
    /// </summary>
    public const string CLIENT_ID_TASK_JOB = "TASK-JOB";
    /// <summary>
    /// 任务处理中心 -- TASK -- ADAPTER
    /// </summary>
    public const string CLIENT_ID_TASK_ADAPTER = "TASK-ADAPTER";
    /// <summary>
    /// 任务处理中心 -- TASK回传 
    /// </summary>
    public const string CLIENT_ID_TASK_CALLBACK = "TASK-CALLBACK";
    /// <summary>
    /// 任务处理中心 -- PenSign -- ADAPTER
    /// </summary>
    public const string CLIENT_ID_PENSIGN_ADAPTER = "PENSIGN-ADAPTER";
    /// <summary>
    /// 任务处理中心 -- PenSign回传 
    /// </summary>
    public const string CLIENT_ID_PENSIGN_CALLBACK = "PENSIGN-CALLBACK";
    /// <summary>
    /// 任务处理中心 -- TASK -- JOB
    /// </summary>
    public const string CLIENT_ID_TASK_MONITOR = "TASK-MONITOR";
}