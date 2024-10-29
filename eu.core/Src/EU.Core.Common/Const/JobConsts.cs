namespace EU.Core.Common.Const;

/// <summary>
/// 任务常量
/// </summary>
public class JobConsts
{
    #region 任务管理

    #region 任务类型
    /// <summary>
    /// JOB
    /// </summary>
    public const string TASK_TYPE_JOB = "JOB";
    #endregion

    #region 运行状态
    /// <summary>
    /// 执行中
    /// </summary>
    public const string TASK_RUN_STATE_RUNNING = "RUNNING";
    /// <summary>
    /// 停止
    /// </summary>
    public const string TASK_RUN_STATE_STOP = "STOP";
    /// <summary>
    /// 等待中
    /// </summary>
    public const string TASK_RUN_STATE_READY = "READY";
    /// <summary>
    /// 禁用
    /// </summary>
    public const string TASK_RUN_STATE_DISABLED = "DISABLED";
    #endregion

    #region 执行结果
    /// <summary>
    /// 成功
    /// </summary>
    public const string TASK_EXEC_RESULT_SUCCESS = "SUCCESS";
    /// <summary>
    /// 失败
    /// </summary>
    public const string TASK_EXEC_RESULT_FAIL = "FAIL";
    #endregion

    #region 操作类型
    /// <summary>
    /// 修改参数
    /// </summary>
    public const string TASK_OPERATE_ARGS = "ARGS";
    /// <summary>
    /// 修改配置
    /// </summary>
    public const string TASK_OPERATE_CONF = "CONF";
    /// <summary>
    /// 修改参数 -  上传文件
    /// </summary>
    public const string TASK_OPERATE_CONF_FILE_UPLOAD = "CONF_FILE_UPLOAD";
    /// <summary>
    /// 修改参数 -  保存文件
    /// </summary>
    public const string TASK_OPERATE_CONF_FILE_SAVE = "CONF_FILE_SAVE";
    /// <summary>
    /// 禁用任务
    /// </summary>
    public const string TASK_OPERATE_DISABLED = "DISABLED";
    /// <summary>
    /// 启用任务
    /// </summary>
    public const string TASK_OPERATE_ENABLE = "ENABLE";
    /// <summary>
    /// 立即执行
    /// </summary>
    public const string TASK_OPERATE_START = "START";
    /// <summary>
    /// 立即停止
    /// </summary>
    public const string TASK_OPERATE_STOP = "STOP";
    /// <summary>
    /// 当前日志
    /// </summary>
    public const string TASK_OPERATE_LOG_CURRENT = "LOG.CURRENT";
    /// <summary>
    /// 历史日志
    /// </summary>
    public const string TASK_OPERATE_LOG_HISTORY = "LOG.HISTORY";
    /// <summary>
    /// 下载历史日志
    /// </summary>
    public const string TASK_OPERATE_LOG_HISTORY_FILE = "LOG.HISTORY.FILE";

    /// <summary>
    /// 初始化租户任务
    /// </summary>
    public const string TASK_OPERATE_INIT_TENANR_TASK = "INIT.TENANR.TASK";
    #endregion

    #endregion
}