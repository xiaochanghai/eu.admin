using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Model;
using EU.Core.Model.Entity;
using SqlSugar;
using System.Data;

namespace EU.Core.Common.Module;

public class ModuleSqlColumn
{
    /// <summary>
    /// 模块代码
    /// </summary>
    private string moduleCode;
    private static RedisCacheService _redisInstance;
    private static RedisCacheService Redis => _redisInstance ??= RedisCacheService.Create(2);
    private static string code = CacheKeys.SmModuleColumn.ToString();
    private static ISqlSugarClient Db => App.GetService<ISqlSugarClient>(false);

    public ModuleSqlColumn(string moduleCode = null)
    {
        this.moduleCode = moduleCode;
    }
    public List<SmModuleColumnExtend> GetModuleSqlColumn()
    {
        var cache = Redis.Get<List<SmModuleColumnExtend>>(code, moduleCode);
        if (cache == null)
        {
            var moduleList = ModuleInfo.GetModuleList();

            cache = Db.Queryable<SmModuleColumn, SmModules>((a, b) =>
            new object[]
            {
                JoinType.Inner, a.SmModuleId == b.ID
            })
                .OrderBy((a, b) => a.TaxisNo, OrderByType.Asc)
                .Select((a, b) => new { a, b.ModuleCode })
                .Select<SmModuleColumnExtend>()
                .ToList();
            Redis.Remove(code);
            foreach (var item in moduleList)
            {
                var columns = cache.Where(x => x.ModuleCode == item.ModuleCode).ToList();
                if (columns.Any())
                    Redis.AddObject(code, item.ModuleCode, columns);
            }
            cache = cache.Where(x => x.ModuleCode == moduleCode).ToList();
        }
        return cache;
    }

    public List<SmModuleColumnExtend> GetModuleSqlFormColumn()
    {
        var cache = GetModuleSqlColumn();
        cache = cache.Where(x => x.HideInForm == false || x.IsMasterId == true).ToList();
        return cache;
    }
    /// <summary>
    /// 获取可表格编辑栏位列表
    /// </summary>
    /// <returns></returns>
    public List<string> GetModuleTableEditableColumns()
    {
        var cache = GetModuleSqlColumn();
        return cache.Where(x => x.IsTableEditable == true).Select(x => x.DataIndex).ToList();
    }

    public string GetExportExcelColumns()
    {
        string columns = string.Empty;
        string name = string.Empty;
        string id = string.Empty;

        var moduleSqlColumn = GetModuleSqlColumn();
        moduleSqlColumn = moduleSqlColumn.Where(x => x.IsExport == true).ToList();
        if (moduleSqlColumn != null)
        {
            for (int i = 0; i < moduleSqlColumn.Count; i++)
            {
                name = Convert.ToString(moduleSqlColumn[i].Title);
                id = Convert.ToString(moduleSqlColumn[i].DataIndex);
                if (i < moduleSqlColumn.Count - 1)
                    if (string.IsNullOrEmpty(name))
                        columns += id + ",";
                    else
                        columns += id + " '" + name + "',";
                else
                {
                    if (string.IsNullOrEmpty(name))
                        columns += id;

                    else
                        columns += id + " '" + name + "'";
                }
            }
        }
        return columns;
    }

    public string GetExportExcelColumnRenderer(string columnName)
    {
        string renderer = string.Empty;
        //List<Sm_Module_Sql_Column> moduleSqlColumnList = GetModuleSqlAllColumn();
        //Sm_Module_Sql_Column moduleSqlColumn = moduleSqlColumnList.Where(x => x.Description?.ToLower() == columnName?.ToLower()).FirstOrDefault();
        //if (moduleSqlColumn != null)
        //{
        //    renderer = moduleSqlColumn.Renderer;
        //}
        return renderer;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public static void Init()
    {
        var moduleColumnInfo = new ModuleSqlColumn("");
        moduleColumnInfo.GetModuleSqlColumn();
    }

    public static async Task Reload(string moduleCode)
    {
        var cache = await Db.Queryable<SmModuleColumn, SmModules>((a, b) =>
        new JoinQueryInfos(JoinType.Inner, a.SmModuleId == b.ID))
            .Where((a, b) => b.ModuleCode == moduleCode)
            .OrderBy(a => a.TaxisNo)
            .Select<SmModuleColumnExtend>()
            .ToListAsync();

        Redis.AddObject(code, moduleCode, cache);
    }
}
