using EU.Core.Model.Models;
using SqlSugar;

namespace EU.Core.Common.Seed.SeedData;

public class SubBusinessDataSeedData : IEntitySeedData<SubLibraryBusinessTable>
{
    public IEnumerable<SubLibraryBusinessTable> InitSeedData()
    {
        return default;
    }

    public IEnumerable<SubLibraryBusinessTable> SeedData()
    {
        return default;
    }

    public async Task CustomizeSeedData(ISqlSugarClient db)
    {
        //初始化分库数据
        //只是用于测试
        if (db.CurrentConnectionConfig.ConfigId == "Tenant_3")
        {
            if (!await db.Queryable<SubLibraryBusinessTable>().AnyAsync())
            {
                await db.Insertable<SubLibraryBusinessTable>(new List<SubLibraryBusinessTable>()
                {
                    new()
                    {
                          ID = Guid.NewGuid(),
                        Name = "王五业务数据1",
                        Amount = 100,
                    },
                    new()
                    {
                          ID= Guid.NewGuid(),
                        Name = "王五业务数据2",
                        Amount = 1000,
                    },
                }).ExecuteReturnSnowflakeIdListAsync();
            }
        }
        else if (db.CurrentConnectionConfig.ConfigId == "Tenant_4")
        {
            if (!await db.Queryable<SubLibraryBusinessTable>().AnyAsync())
            {
                await db.Insertable<SubLibraryBusinessTable>(new List<SubLibraryBusinessTable>()
                {
                    new()
                    {
                        ID = Guid.NewGuid(),
                        Name = "赵六业务数据1",
                        Amount = 50,
                    },
                    new()
                    {
                        ID = Guid.NewGuid(),
                        Name = "赵六业务数据2",
                        Amount = 60,
                    },
                }).ExecuteReturnSnowflakeIdListAsync();
            }
        }


        await Task.Delay(1);
    }
}