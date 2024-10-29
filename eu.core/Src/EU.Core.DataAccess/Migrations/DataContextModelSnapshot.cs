using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EU.Core.DataAccess.Migrations;

[DbContext(typeof(DataContext))]
partial class DataContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
    }
}
