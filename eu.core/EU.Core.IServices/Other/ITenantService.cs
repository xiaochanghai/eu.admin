using System.Threading.Tasks;
using EU.Core.IServices.BASE;
using EU.Core.Model.Models;

namespace EU.Core.IServices;

public interface ITenantService : IBaseServices<SysTenant>
{
    public Task SaveTenant(SysTenant tenant);

    public Task InitTenantDb(SysTenant tenant);
}