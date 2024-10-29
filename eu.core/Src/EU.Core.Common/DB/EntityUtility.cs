using EU.Core.Common.Extensions;
using EU.Core.Model;
using SqlSugar;
using System.Diagnostics;
using System.Reflection;

namespace EU.Core.Common.DB;

public class EntityUtility
{
    private static readonly Lazy<Dictionary<string, List<Type>>> _tenantEntitys = new(() =>
    {
        Dictionary<string, List<Type>> dic = new();
        var assembly = Assembly.Load("EU.Core.Model");
        //扫描 实体
        foreach (var type in assembly.GetTypes().Where(s => s.IsClass && !s.IsAbstract))
        {
            var tenant = type.GetCustomAttribute<TenantAttribute>();
            if (tenant != null)
            {
                dic.TryAdd(tenant.configId.ToString(), type);
                continue;
            }

            if (type.IsSubclassOf(typeof(RootEntityTkey<>)))
            {
                dic.TryAdd(MainDb.CurrentDbConnId, type);
                continue;
            }

            var table = type.GetCustomAttribute<SugarTable>();
            if (table != null)
            {
                dic.TryAdd(MainDb.CurrentDbConnId, type);
                continue;
            }

            Debug.Assert(type.Namespace != null, "type.Namespace != null");
            if (type.Namespace.StartsWith("EU.Core.Model.Models"))
            {
                dic.TryAdd(MainDb.CurrentDbConnId, type);
                continue;
            }
        }

        return dic;
    });

    public static Dictionary<string, List<Type>> TenantEntitys => _tenantEntitys.Value;
}