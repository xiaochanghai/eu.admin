namespace EU.Core.Model;

public class functionData
{
    public string label { get; set; }
    public string value { get; set; }
}



#region 基础按钮


public class RoleFuncVM
{
    public List<string> RoleFuncData { get; set; }
    public List<string> AddAction { get; set; }
    public Guid RoleId { get; set; }
    public Guid SmModuleId { get; set; }
}
#endregion