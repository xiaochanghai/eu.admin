using EU.Core.Common.DB;
using EU.Core.Common.Seed;
using EU.Core.IServices;
using EU.Core.Model.Models;
using EU.Core.Repository.UnitOfWorks;
using EU.Core.Services.BASE;
using System.Threading.Tasks;

namespace EU.Core.Services;

public class TenantService : BaseServices<SysTenant>, ITenantService
{
    private readonly IUnitOfWorkManage _uowManager;

    public TenantService(IUnitOfWorkManage uowManage)
    {
        this._uowManager = uowManage;
    }


    public async Task SaveTenant(SysTenant tenant)
    {
        bool initDb = tenant.ID == 0;
        using (var uow = _uowManager.CreateUnitOfWork())
        {

            tenant.DefaultTenantConfig();

            if (tenant.ID == 0)
            {
                await Db.Insertable(tenant).ExecuteReturnSnowflakeIdAsync();
            }
            else
            {
                var oldTenant = await QueryById(tenant.ID);
                if (oldTenant.Connection != tenant.Connection)
                {
                    initDb = true;
                }

                await Db.Updateable(tenant).ExecuteCommandAsync();
            }

            uow.Commit();
        }

        if (initDb)
        {
            await InitTenantDb(tenant);
        }
    }

    public async Task InitTenantDb(SysTenant tenant)
    {
        await DBSeed.InitTenantSeedAsync(Db.AsTenant(), tenant.GetConnectionConfig());
    }
}