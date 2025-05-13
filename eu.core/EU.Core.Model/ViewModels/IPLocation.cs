using static System.Runtime.InteropServices.JavaScript.JSType;

namespace EU.Core.Model.ViewModels;

/// <summary>
/// IP位置
/// </summary>
public class IPLocation
{
    public string ret { get; set; } 
    public IPLocationData data { get; set; }

}

public class IPLocationData
{
    public string country { get; set; }
    public string prov { get; set; }
    public string continent { get; set; }
    public string zipcode { get; set; }
    public string owner { get; set; }
    public string isp { get; set; }
    public string adcode { get; set; }
    public string city { get; set; }
    public string district { get; set; }
}
