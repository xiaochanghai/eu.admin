namespace EU.Core.Model;

public class UserAccessModel
{
    public string User { get; set; }
    public string IP { get; set; }
    public string API { get; set; }
    public string BeginTime { get; set; }
    public string OPTime { get; set; }
    public string RequestMethod { get; set; }
    public string RequestData { get; set; }
    public string Agent { get; set; }
    public string Filter { get; set; }
}