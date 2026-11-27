using System.Reflection;
using EU.Core.IRepository.Base;
using EU.Core.Model.Entity;
using EU.Core.Services;
using Xunit;

namespace EU.Core.Tests;

/// <summary>纯单元测试：不创建数据库连接，不加载宿主或生产配置。</summary>
public class EcGoodsDetail_Should
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Reject_invalid_id_without_accessing_repository(string id)
    {
        Assert.Null(await new EcGoodsServices(null).QueryById(id));
    }

    [Fact]
    public async Task Return_null_for_missing_product_without_querying_accessories_or_updating_views()
    {
        var repository = DispatchProxy.Create<IBaseRepository<EcGoods>, GoodsRepositoryStub>();
        Assert.Null(await new EcGoodsServices(repository).QueryById(Guid.NewGuid()));
    }

    [Fact]
    public async Task Return_null_for_deleted_product_without_querying_accessories_or_updating_views()
    {
        var id = Guid.NewGuid();
        var repository = DispatchProxy.Create<IBaseRepository<EcGoods>, GoodsRepositoryStub>();
        ((GoodsRepositoryStub)(object)repository).Goods = new EcGoods { ID = id, IsDeleted = true };
        Assert.Null(await new EcGoodsServices(repository).QueryById(id));
    }

    public class GoodsRepositoryStub : DispatchProxy
    {
        public EcGoods Goods { get; set; }
        protected override object Invoke(MethodInfo method, object[] args)
        {
            if (method.Name == "QueryById") return Task.FromResult(Goods);
            throw new InvalidOperationException("Unexpected repository access: " + method.Name);
        }
    }
}
