namespace EU.Core.Services;

/// <summary>
/// sysUserInfoServices
/// </summary>	
public class SplitDemoServices : BaseServices<SplitDemo>, ISplitDemoServices
{
    private readonly IBaseRepository<SplitDemo> _splitDemoRepository;
    public SplitDemoServices(IBaseRepository<SplitDemo> splitDemoRepository)
    {
        _splitDemoRepository = splitDemoRepository;
    }


}
