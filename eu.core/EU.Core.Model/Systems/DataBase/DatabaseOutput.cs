using SqlSugar;

namespace EU.Core.Model.Systems.DataBase;

public class DatabaseOutput
{
	public string ConfigId { get; set; }

	public DbType DbType { get; set; }
}