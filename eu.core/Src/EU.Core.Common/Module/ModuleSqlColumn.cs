using System.Data;
using EU.Core.Common.Caches;
using EU.Core.Common.Enums;
using EU.Core.Common.Helper;
using EU.Core.Model;

namespace EU.Core.Common.Module;

public class ModuleSqlColumn
{
    /// <summary>
    /// 模块代码
    /// </summary>
    private string moduleCode;
    private static RedisCacheService Redis = new(2);
    private static string code = CacheKeys.SmModuleColumn.ToString();

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
            string sql = @"SELECT A.*, B.ModuleCode
                                FROM SmModuleColumn A
                                     JOIN SmModules B ON A.SmModuleId = B.ID AND A.IsDeleted = B.IsDeleted
                                WHERE A.IsDeleted = 'false'
                                ORDER BY A.TAXISNO ASC";
            cache = DBHelper.QueryList<SmModuleColumnExtend>(sql);
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
        string sql = @"SELECT A.*, B.ModuleCode
                                FROM SmModuleColumn A
                                     JOIN SmModules B
                                        ON     A.SmModuleId = B.ID
                                           AND A.IsDeleted = B.IsDeleted
                                           AND B.ModuleCode = '{0}'
                                WHERE A.IsExport = 'true' AND A.IsDeleted = 'false'
                                ORDER BY A.TAXISNO ASC";
        sql = string.Format(sql, moduleCode);
        var moduleSqlColumn = DBHelper.QueryList<SmModuleColumnExtend>(sql);
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

    public static void Reload(string moduleCode)
    {

        string sql = $@"SELECT A.*, B.ModuleCode
                                FROM SmModuleColumn A
                                     JOIN SmModules B ON A.SmModuleId = B.ID AND A.IsDeleted = B.IsDeleted
                                WHERE A.IsDeleted = 'false' AND B.ModuleCode='{moduleCode}'
                                ORDER BY A.TAXISNO ASC";
        var cache = DBHelper.QueryList<SmModuleColumnExtend>(sql);
        Redis.AddObject(code, moduleCode, cache);
    }
}
