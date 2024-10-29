namespace EU.Core.Model;

#region 自定义功能

public class DataTree
{
    public string title { get; set; }

    public string key { get; set; }

    public bool isLeaf { get; set; }

    public List<DataTree> children { get; set; }
}

public class RoleFuncPric
{
    public List<string> FunctionList { get; set; }
    public Guid RoleId { get; set; }
}
#endregion