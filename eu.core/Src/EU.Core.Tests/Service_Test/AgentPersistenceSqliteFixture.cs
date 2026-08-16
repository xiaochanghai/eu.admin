using EU.Core.Repository.Base;
using EU.Core.Repository.UnitOfWorks;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSugar;

namespace EU.Core.Tests.Service_Test;

internal sealed class AgentPersistenceSqliteFixture : IDisposable
{
    public AgentPersistenceSqliteFixture(params Type[] entityTypes)
    {
        Db = new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = "Data Source=:memory:",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = false
        });
        Db.Ado.Open();
        Db.CodeFirst.InitTables(entityTypes);
    }

    public SqlSugarScope Db { get; }

    public BaseRepository<TEntity> CreateRepository<TEntity>()
        where TEntity : class, new()
    {
        var unitOfWork = new UnitOfWorkManage(
            Db,
            NullLogger<UnitOfWorkManage>.Instance);
        return new BaseRepository<TEntity>(unitOfWork);
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}
