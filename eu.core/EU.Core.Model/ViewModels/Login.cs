using EU.Core.Model.Models;

namespace EU.Core.Model;


public class LoginRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    public string UserAccount { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    [Required]
    public string PassWord { get; set; }
}
public class LoginReturn
{
    /// <summary>
    /// Token
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 模板列表
    /// </summary>
    public List<SmModules> Modules { get; set; }

    public CurrentUser UserInfo { get; set; } = new CurrentUser();
}

/// <summary>
/// 用户信息
/// </summary>
public class CurrentUser
{
    /// <summary>
    /// 角色
    /// </summary>
    public string Role { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// 当前用户ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 头像ID
    /// </summary>
    public Guid? AvatarFileId { get; set; }

    /// <summary>
    /// 当前周几
    /// </summary>
    public string WeekName { get; set; }
}
public class RestPassword
{
    /// <summary>
    /// 
    /// </summary>
    public string oldPassword { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string newPassword { get; set; }
}



