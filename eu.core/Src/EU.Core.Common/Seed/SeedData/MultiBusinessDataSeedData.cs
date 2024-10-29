using EU.Core.Model.Models;
using SqlSugar;

namespace EU.Core.Common.Seed.SeedData;

public class MultiBusinessDataSeedData : IEntitySeedData<MultiBusinessTable>
{
    public IEnumerable<MultiBusinessTable> InitSeedData()
    {
        return new List<MultiBusinessTable>()
        {
            new()
            {
                ID = Guid.NewGuid(),
                Name = "业务数据1",
                Amount = 100,
            },
            new()
            {
                ID = Guid.NewGuid(),
                Name = "业务数据2",
                Amount = 1000,
            },
        };
    }

    public IEnumerable<MultiBusinessTable> SeedData()
    {
        return default;
    }

    public Task CustomizeSeedData(ISqlSugarClient db)
    {
        return Task.CompletedTask;
    }
}