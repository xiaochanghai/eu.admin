namespace EU.Core.Model;

public class RoleTree
{
    public string title { get; set; }

    public string key { get; set; }

    public bool isLeaf { get; set; }

    public List<RoleTree> children { get; set; }
}
