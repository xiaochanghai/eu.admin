using EU.Core.Common.DB.Extension;
using EU.Core.Model.Models.RootTkey;
using SqlSugar;
using EU.Core.Model;

namespace EU.Core.Api.Controllers.Systems;

/// <summary>
/// 动态建表 CURD
/// </summary>
[Route("api/Systems/[controller]/[action]")]
[ApiController, ApiExplorerSettings(GroupName = Grouping.GroupName_Assistant)]
[Authorize(Permissions.Name)]
public class DynamicCodeFirstController : BaseApiController
{
    private readonly ISqlSugarClient _db;

    public DynamicCodeFirstController(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 动态type
    /// </summary>
    /// <returns></returns>
    private Type GetDynamicType()
    {
        return _db.DynamicBuilder().CreateClass("DynamicTestTable")
            //{table} 占位符会自动替换成表名
            .CreateIndex(new SugarIndexAttribute("idx_{table}_Code", "Code", OrderByType.Desc))
            .CreateProperty("Id", typeof(int), new SugarColumn() {IsPrimaryKey = true, IsIdentity = true})
            .CreateProperty("Code", typeof(string), new SugarColumn() {Length = 50})
            .CreateProperty("Name", typeof(string), new SugarColumn() {Length = 50})
            .WithCache()
            .BuilderType();
    }

    /// <summary>
    /// 动态type 继承BaseEntity
    /// </summary>
    /// <returns></returns>
    private Type GetDynamicType2()
    {
        return _db.DynamicBuilder().CreateClass("DynamicTestTable2", null, typeof(BaseEntity))
            //{table} 占位符会自动替换成表名
            .CreateIndex(new SugarIndexAttribute("idx_{table}_Code", "Code", OrderByType.Desc))
            .CreateProperty("Code", typeof(string), new SugarColumn() {Length = 50})
            .CreateProperty("Name", typeof(string), new SugarColumn() {Length = 50})
            .WithCache()
            .BuilderType();
    }

    /// <summary>
    /// 测试建表
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public ServiceResult TestCreateTable()
    {
        var type = GetDynamicType();
        _db.CodeFirst.InitTables(type);
        return Success();
    }

    /// <summary>
    /// 测试查询
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public ServiceResult<object> TestQuery()
    {
        var type = GetDynamicType();
        return Success(_db.QueryableByObject(type).ToList());
    }

    /// <summary>
    /// 测试写入
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public ServiceResult TestInsert(string code, string name)
    {
        var type = GetDynamicType();
        var entity = Activator.CreateInstance(type);
        type.GetProperty("Code")!.SetValue(entity, code);
        type.GetProperty("Name")!.SetValue(entity, name);
        _db.InsertableByObject(entity).ExecuteCommand();
        return Success();
    }
}