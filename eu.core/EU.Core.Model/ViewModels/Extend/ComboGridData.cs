namespace EU.Core.Model;

public class ComboGridData
{
    public object value { get; set; }
    public string label { get; set; }
}

public class ComboGridDataBody
{
    public object value { get; set; }
    public string label { get; set; }

    public string parentColumn;
    public string parentId;
    public int? current;
    public int? pageSize;
    public string code;
    public string[] items;
    public string key;
}