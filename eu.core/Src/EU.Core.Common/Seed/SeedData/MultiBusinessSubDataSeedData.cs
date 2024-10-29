using EU.Core.Model.Models;
using SqlSugar;

namespace EU.Core.Common.Seed.SeedData;

public class MultiBusinessSubDataSeedData : IEntitySeedData<MultiBusinessSubTable>
{
    public IEnumerable<MultiBusinessSubTable> InitSeedData()
    {
        return new List<MultiBusinessSubTable>()
        {
            new()
            {
                ID = Guid.NewGuid(),
                MainId = 1001,
                Memo = "子数据",
            },
            new()
            {
                ID = Guid.NewGuid(),
                MainId = 1001,
                Memo = "子数据2",
            },
        };
    }

    public IEnumerable<MultiBusinessSubTable> SeedData()
    {
        return default;
    }

    public Task CustomizeSeedData(ISqlSugarClient db)
    {
        return Task.CompletedTask;
    }
}