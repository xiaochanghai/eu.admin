namespace EU.Core.Model;

public class ModuleTree
{
    public string title { get; set; }

    public string key { get; set; }
    public string value { get; set; }

    public bool? isLeaf { get; set; }

    public List<ModuleTree> children { get; set; }
}

public class ModuleTree1
{
    public string title { get; set; }

    public string value { get; set; }

    public bool? isLeaf { get; set; }

    public List<ModuleTree1> children { get; set; }
}

public class MaterialTypeTree
{
    public string title { get; set; }

    public string key { get; set; }
    public string value { get; set; }

    public bool? isLeaf { get; set; }

    public List<MaterialTypeTree> children { get; set; }

    public bool? selectable = true;
}
