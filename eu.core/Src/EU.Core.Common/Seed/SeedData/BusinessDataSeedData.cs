using EU.Core.Model.Models;
using SqlSugar;

namespace EU.Core.Common.Seed.SeedData;

/// <summary>
/// 初始化 业务数据
/// </summary>
public class BusinessDataSeedData : IEntitySeedData<BusinessTable>
{
    public IEnumerable<BusinessTable> InitSeedData()
    {
        return new[]
        {
            new BusinessTable()
            {
                ID = Guid.NewGuid(),
                TenantId = 1000001,
                Name = "张三的数据01",
                Amount = 150,
                IsDeleted = true,
            },
            new BusinessTable()
            {
                ID = Guid.NewGuid(),
                TenantId = 1000001,
                Name = "张三的数据02",
                Amount = 200,
            },
            new BusinessTable()
            {
                ID = Guid.NewGuid(),
                TenantId = 1000001,
                Name = "张三的数据03",
                Amount = 250,
            },
            new BusinessTable()
            {
                ID = Guid.NewGuid(),
                TenantId = 1000002,
                Name = "李四的数据01",
                Amount = 300,
            },
            new BusinessTable()
            {
                ID = Guid.NewGuid(),
                TenantId = 1000002,
                Name = "李四的数据02",
                Amount = 500,
            },
            new BusinessTable()
            {
                ID = Guid.NewGuid(),
                TenantId = 0,
                Name = "公共数据01",
                Amount = 16600,
            },
            new BusinessTable()
            {
                ID = Guid.NewGuid(),
                TenantId = 0,
                Name = "公共数据02",
                Amount = 19800,
            },
        };
    }

    public IEnumerable<BusinessTable> SeedData()
    {
        return default;
    }

    public Task CustomizeSeedData(ISqlSugarClient db)
    {
        return Task.CompletedTask;
    }
}