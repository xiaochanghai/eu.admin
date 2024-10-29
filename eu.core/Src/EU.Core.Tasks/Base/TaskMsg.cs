namespace EU.Core.Tasks;

/// <summary>
/// TaskMsg
/// </summary>
public class TaskMsg
{
    /// <summary>
    /// 消息ID，用于回传消息
    /// </summary>
    public Guid MsgId { get; set; }

    /// <summary>
    /// 任务ID
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// 任务类型 *DIC.TASK.TYPE*
    /// </summary>
    public string TaskType { get; set; }
    /// <summary>
    /// 任务编码 *DIC.TASK.CODE*
    /// </summary>
    public string TaskCode { get; set; }
    /// <summary>
    /// 操作指令 *DIC.TASK.OPERATE*
    /// </summary>
    public string Oprate { get; set; }
    /// <summary>
    /// 操作参数
    /// </summary>
    public string Args { get; set; }
    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; }


    public DateTime Time { get; set; }

    /// <summary>
    /// 重写ToString()
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"\r\n消息ID {MsgId}\r\n任务ID {TaskId}\r\n任务类型 {TaskType}\r\n任务编码 {TaskCode}\r\n操作类型 {Oprate}\r\n操作参数 {Args}\r\n内容 {Content}";
    }
}

/// <summary>
/// TaskCallbackMsg
/// </summary>
public class TaskCallbackMsg
{
    /// <summary>
    /// 消息ID，用于回传消息
    /// </summary>
    public Guid MsgId { get; set; }
    /// <summary>
    /// 消息内容
    /// </summary>
    public object Content { get; set; }
}
