using System.Text;
using System.Text.RegularExpressions;
using EU.Core.Common.Helper;
using EU.Core.Model;
using SqlSugar;

namespace EU.Core.Common.Seed;

public class FrameSeed
{

    public static string path = AppDomain.CurrentDomain.BaseDirectory.Replace("EU.Core.Api\\bin\\Debug\\net8.0\\", null).Replace("Src\\EU.CodeGenerator\\bin\\Debug\\net8.0\\", null);
    /// <summary>
    /// 生成Controller层
    /// </summary>
    /// <param name="sqlSugarClient">sqlsugar实例</param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="tableNames">数据库表名数组，默认空，生成所有表</param>
    /// <param name="isMuti"></param>
    /// <returns></returns>
    public static bool CreateControllers(SqlSugarScope sqlSugarClient, string ConnId = null, bool isMuti = false, string[] tableNames = null)
    {
        Create_Controller_ClassFileByDBTalbe(sqlSugarClient, ConnId, path + $@"EU.Core.Api\Controllers", "EU.Core.Api.Controllers", tableNames, "", isMuti);
        return true;
    }

    /// <summary>
    /// 生成Model层
    /// </summary>
    /// <param name="sqlSugarClient">sqlsugar实例</param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="tableNames">数据库表名数组，默认空，生成所有表</param>
    /// <param name="isMuti"></param>
    /// <returns></returns>
    public static bool CreateModels(SqlSugarScope sqlSugarClient, string ConnId, bool isMuti = false, string[] tableNames = null)
    {
        Create_Model_ClassFileByDBTalbe(sqlSugarClient, ConnId, path + $@"EU.Core.Model", "EU.Core.Model.Entity", tableNames, "", isMuti);
        return true;
    }

    /// <summary>
    /// 生成IRepository层
    /// </summary>
    /// <param name="sqlSugarClient">sqlsugar实例</param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="isMuti"></param>
    /// <param name="tableNames">数据库表名数组，默认空，生成所有表</param>
    /// <returns></returns>
    public static bool CreateIRepositorys(SqlSugarScope sqlSugarClient, string ConnId, bool isMuti = false, string[] tableNames = null)
    {
        Create_IRepository_ClassFileByDBTalbe(sqlSugarClient, ConnId, path + $@"EU.Core.IRepository", "EU.Core.IRepository", tableNames, "", isMuti);
        return true;
    }



    /// <summary>
    /// 生成 IService 层
    /// </summary>
    /// <param name="sqlSugarClient">sqlsugar实例</param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="isMuti"></param>
    /// <param name="tableNames">数据库表名数组，默认空，生成所有表</param>
    /// <returns></returns>
    public static bool CreateIServices(SqlSugarScope sqlSugarClient, string ConnId, bool isMuti = false, string[] tableNames = null)
    {
        Create_IServices_ClassFileByDBTalbe(sqlSugarClient, ConnId, path + $@"EU.Core.IServices", "EU.Core.IServices", tableNames, "", isMuti);
        return true;
    }



    /// <summary>
    /// 生成 Repository 层
    /// </summary>
    /// <param name="sqlSugarClient">sqlsugar实例</param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="isMuti"></param>
    /// <param name="tableNames">数据库表名数组，默认空，生成所有表</param>
    /// <returns></returns>
    public static bool CreateRepository(SqlSugarScope sqlSugarClient, string ConnId, bool isMuti = false, string[] tableNames = null)
    {
        Create_Repository_ClassFileByDBTalbe(sqlSugarClient, ConnId, path + $@"EU.Core.Repository", "EU.Core.Repository", tableNames, "", isMuti);
        return true;
    }



    /// <summary>
    /// 生成 Service 层
    /// </summary>
    /// <param name="sqlSugarClient">sqlsugar实例</param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="isMuti"></param>
    /// <param name="tableNames">数据库表名数组，默认空，生成所有表</param>
    /// <returns></returns>
    public static bool CreateServices(SqlSugarScope sqlSugarClient, string ConnId, bool isMuti = false, string[] tableNames = null)
    {
        Create_Services_ClassFileByDBTalbe(sqlSugarClient, ConnId, path + $@"EU.Core.Services", "EU.Core.Services", tableNames, "", isMuti);
        return true;
    }
    public static string GetGroupName(string input)
    {
        var groupName = Regex.Matches(input, "([A-Z][a-z]+)")
                    .Cast<Match>()
                    .Select(m => m.Value).First();
        return groupName.ToUpper();
    }

    public static string GetCopyRight(string tableName)
    {
        var build = new StringBuilder();
        build.Append("/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。\r\n");
        build.Append("* " + tableName + ".cs\r\n");
        build.Append("*\r\n");
        build.Append("* 功 能： N / A\r\n");
        build.Append("* 类 名： " + tableName + "\r\n");
        build.Append("*\r\n");
        build.Append("* Ver    变更日期 负责人  变更内容\r\n");
        build.Append("* ───────────────────────────────────\r\n");
        build.Append("* V1.0  " + DateTime.Now.ToString() + "  SimonHsiao   初版\r\n");
        build.Append("*\r\n");
        build.Append("* Copyright(c) " + DateTime.Now.Year + " SUZHOU EU Corporation. All Rights Reserved.\r\n");
        build.Append("*┌──────────────────────────────────┐\r\n");
        build.Append("*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│\r\n");
        build.Append("*│　版权所有：苏州一优信息技术有限公司                                │\r\n");
        build.Append("*└──────────────────────────────────┘\r\n");
        build.Append("*/\r\n");
        return build.ToString();
    }
    #region 根据数据库表生产Controller层

    /// <summary>
    /// 根据数据库表生产Controller层
    /// </summary>
    /// <param name="sqlSugarClient"></param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="strPath">实体类存放路径</param>
    /// <param name="strNameSpace">命名空间</param>
    /// <param name="lstTableNames">生产指定的表</param>
    /// <param name="strInterface">实现接口</param>
    /// <param name="isMuti"></param>
    /// <param name="blnSerializable">是否序列化</param>
    private static void Create_Controller_ClassFileByDBTalbe(SqlSugarScope sqlSugarClient, string ConnId, string strPath, string strNameSpace, string[] lstTableNames, string strInterface, bool isMuti = false, bool blnSerializable = false)
    {
        var IDbFirst = sqlSugarClient.DbFirst;
        if (lstTableNames != null && lstTableNames.Length > 0)
        {
            IDbFirst = IDbFirst.Where(lstTableNames);
        }

        var tableName = lstTableNames[0];

        var groupName = GetGroupName(tableName);
        //            var ls = IDbFirst.IsCreateDefaultValue().IsCreateAttribute()

        //                 .SettingClassTemplate(p => p =
        //@"namespace " + strNameSpace + @"
        //{
        //    /// <summary>
        //    /// {ClassName}
        //    /// </summary>
        //	[Route(""api/[controller]"")]
        //    [ApiController, GlobalActionFilter]
        //    [Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_" + groupName + @")]
        //    public class {ClassName}Controller : ControllerBase
        //    {
        //        #region 初始化
        //        /// <summary>
        //        /// 服务器接口，因为是模板生成，所以首字母是大写的，自己可以重构下
        //        /// </summary>
        //        private readonly I{ClassName}Services _{ClassName}Services;

        //        public {ClassName}Controller(I{ClassName}Services {ClassName}Services)
        //        {
        //            _{ClassName}Services = {ClassName}Services;
        //        }
        //        #endregion

        //        #region 基础接口

        //        #region 查询
        //        /// <summary>
        //        /// {ClassName} -- 根据条件查询数据
        //        /// </summary>
        //        /// <param name=""filter"">条件</param>
        //        /// <returns></returns>
        //        [HttpGet]
        //        public async Task<ServiceResult<PageModel<{ClassName}Dto>>> Get([FromFilter] QueryFilter filter)
        //        {
        //            var response = await _{ClassName}Services.QueryFilterPage(filter);
        //            return new ServiceResult<PageModel<{ClassName}Dto>>() { msg = ""获取成功"", success = true, response = response };
        //        }

        //        /// <summary>
        //        ///  {ClassName} -- 根据Id查询数据
        //        /// </summary>
        //        /// <param name=""Id"">主键ID</param>
        //        /// <returns></returns>
        //        [HttpGet(""{Id}"")]
        //        public async Task<ServiceResult<{ClassName}Dto>> Get(string Id)
        //        {
        //            var entity = await _{ClassName}Services.QueryById(Id);
        //            if (entity == null)
        //                return ServiceResult<{ClassName}Dto>.Fail(""获取失败"");
        //            else
        //                return new ServiceResult<{ClassName}Dto>() { msg = ""获取成功"", success = true, response = entity };
        //        }
        //        #endregion

        //        #region 新增
        //        /// <summary>
        //        /// {ClassName} -- 新增数据
        //        /// </summary>
        //        /// <param name=""insertModel""></param>
        //        /// <returns></returns>
        //        [HttpPost]
        //        public async Task<ServiceResult<string>> Post([FromBody] Insert{ClassName}Input insertModel)
        //        {
        //            var data = ServiceResult<string>.Success(""获取成功"", null);

        //            var id = await _{ClassName}Services.Add(insertModel);
        //            data.success = id > 0;
        //            if (data.success)
        //                data.response = id.ObjToString();

        //            return data;
        //        }
        //        #endregion

        //        #region 更新
        //        /// <summary>
        //        /// {ClassName} -- 更新数据
        //        /// </summary>
        //        /// <param name=""Id""></param>
        //        /// <param name=""editModel""></param>
        //        /// <returns></returns>
        //        [HttpPut(""{Id}"")]
        //        public async Task<ServiceResult> Put(long Id, [FromBody] Edit{ClassName}Input editModel)
        //        {
        //            var data = MessageModel.Success(""更新成功"");
        //            data.success = await _{ClassName}Services.Update(Id, editModel);
        //            if (!data.success)
        //                data.msg = ""更新失败"";

        //            return data;
        //        }
        //        #endregion

        //        #region 删除
        //        /// <summary>
        //        /// {ClassName} -- 删除数据
        //        /// </summary>
        //        /// <param name=""Id""></param>
        //        /// <returns></returns>
        //        [HttpDelete(""{Id}"")]
        //        public async Task<ServiceResult> Delete(long Id)
        //        {
        //            var data = MessageModel.Success(""删除成功"");
        //            var entity = await _{ClassName}Services.QueryById(Id);
        //            if (entity == null)
        //                return MessageModel.Fail(""删除失败"");

        //            entity.IsEnable = 0;
        //            data.success = await _{ClassName}Services.Update(entity);
        //            if (!data.success)
        //                data.msg = ""删除失败"";
        //            return data;
        //        }
        //        #endregion

        //        #endregion
        //    }
        //}")

        //                  .ToClassStringList(strNameSpace);

        #region 获取表中文名
        string sql = @"SELECT f.value TableName
                            FROM sysobjects d
                                 LEFT JOIN sys.extended_properties f
                                    ON d.id = f.major_id AND f.minor_id = 0
                            WHERE d.name = '{0}'";
        sql = string.Format(sql, tableName);
        string TableCnName = Convert.ToString(DBHelper.ExecuteScalar(sql, null));
        if (string.IsNullOrWhiteSpace(TableCnName)) TableCnName = tableName;
        #endregion

        var ls = IDbFirst.IsCreateDefaultValue().IsCreateAttribute()

                       .SettingClassTemplate(p => p =
       GetCopyRight(tableName) + @"namespace " + strNameSpace + @";

/// <summary>
/// " + TableCnName + @"(Controller)
/// </summary>
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_" + groupName + @")]
public class {ClassName}Controller : BaseController<I{ClassName}Services, {ClassName}, {ClassName}Dto, Insert{ClassName}Input, Edit{ClassName}Input>
{
    public {ClassName}Controller(I{ClassName}Services service) : base(service)
    {
    }
}")

                        .ToClassStringList(strNameSpace);

        Dictionary<string, string> newdic = new Dictionary<string, string>();
        //循环处理 首字母小写 并插入新的 Dictionary
        foreach (KeyValuePair<string, string> item in ls)
        {
            string newkey = "_" + item.Key.First().ToString().ToLower() + item.Key.Substring(1);
            string newvalue = item.Value.Replace("_" + item.Key, newkey);
            strPath = strPath + @"\" + groupName;
            var fileName = $"{string.Format("{0}Controller", item.Key)}.cs";
            var fileFullPath = Path.Combine(strPath, fileName);

            if (!File.Exists(fileFullPath))
                newdic.Add(item.Key, newvalue);
        }
        CreateFilesByClassStringList(newdic, strPath, "{0}Controller");
    }
    #endregion


    #region 根据数据库表生产Model层

    /// <summary>
    /// 根据数据库表生产Model层
    /// </summary>
    /// <param name="sqlSugarClient"></param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="strPath">实体类存放路径</param>
    /// <param name="strNameSpace">命名空间</param>
    /// <param name="lstTableNames">生产指定的表</param>
    /// <param name="strInterface">实现接口</param>
    /// <param name="isMuti"></param>
    /// <param name="blnSerializable">是否序列化</param>
    private static void Create_Model_ClassFileByDBTalbe(SqlSugarScope sqlSugarClient, string ConnId, string strPath, string strNameSpace, string[] lstTableNames, string strInterface, bool isMuti = false, bool blnSerializable = false)
    {
        //多库文件分离
        if (isMuti)
        {
            strPath = strPath + @"\Models\" + ConnId;
            strNameSpace = strNameSpace + "." + ConnId;
        }

        var IDbFirst = sqlSugarClient.DbFirst;
        if (lstTableNames != null && lstTableNames.Length > 0)
        {
            IDbFirst = IDbFirst.Where(lstTableNames);
        }
        var ls = IDbFirst.IsCreateDefaultValue().IsCreateAttribute()

              .SettingClassTemplate(p => p =
@"{using}

namespace " + strNameSpace + @"
{
{ClassDescription}
    [SugarTable( ""{ClassName}"", """ + ConnId + @"""), Entity(TableCnName = ""{ClassName}"", TableName = ""{ClassName}"")]" + (blnSerializable ? "\n    [Serializable]" : "") + @"
    public class {ClassName}" + (string.IsNullOrEmpty(strInterface) ? "" : (" : " + strInterface)) + @"
    {
           public {ClassName}()
           {
           }
{PropertyName}
    }
}")
              //.SettingPropertyDescriptionTemplate(p => p = string.Empty)
              .SettingPropertyTemplate(p => p =
@"{SugarColumn}
           public {PropertyType} {PropertyName} { get; set; }")

               //.SettingConstructorTemplate(p => p = "              this._{PropertyName} ={DefaultValue};")

               .ToClassStringList(strNameSpace);

        StringBuilder build = new StringBuilder();
        var tableName = lstTableNames[0];
        var groupName = GetGroupName(tableName);
        string sql = @"SELECT A.name AS table_name,
                                       B.name AS column_name,
                                       D.data_type,
                                       C.value AS column_description,
                                       D.NUMERIC_PRECISION, D.NUMERIC_SCALE,D.CHARACTER_MAXIMUM_LENGTH
                                FROM sys.tables A
                                     INNER JOIN sys.columns B ON B.object_id = A.object_id
                                     LEFT JOIN sys.extended_properties C
                                        ON C.major_id = B.object_id AND C.minor_id = B.column_id
                                     LEFT JOIN information_schema.columns D
                                        ON D.column_name = B.name AND D.TABLE_NAME = '{0}'
                                WHERE A.name = '{0}' ORDER BY B.column_id ASC";
        sql = string.Format(sql, tableName);
        var dtColumn = sqlSugarClient.Ado.GetDataTable(sql);

        #region 获取表中文名
        sql = @"SELECT f.value TableName
                            FROM sysobjects d
                                 LEFT JOIN sys.extended_properties f
                                    ON d.id = f.major_id AND f.minor_id = 0
                            WHERE d.name = '{0}'";
        sql = string.Format(sql, tableName);
        string TableCnName = Convert.ToString(DBHelper.ExecuteScalar(sql, null));
        if (string.IsNullOrWhiteSpace(TableCnName)) TableCnName = tableName;
        #endregion

        build.Append("/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。\r\n");
        build.Append("* " + tableName + ".cs\r\n");
        build.Append("*\r\n");
        build.Append("* 功 能： N / A\r\n");
        build.Append("* 类 名： " + tableName + "\r\n");
        build.Append("*\r\n");
        build.Append("* Ver    变更日期 负责人  变更内容\r\n");
        build.Append("* ───────────────────────────────────\r\n");
        build.Append("* V0.01  " + DateTime.Now.ToString() + "  SimonHsiao   初版\r\n");
        build.Append("*\r\n");
        build.Append("* Copyright(c) " + DateTime.Now.Year + " EU Corporation. All Rights Reserved.\r\n");
        build.Append("*┌──────────────────────────────────┐\r\n");
        build.Append("*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│\r\n");
        build.Append("*│　作者：SimonHsiao                                                  │\r\n");
        build.Append("*└──────────────────────────────────┘\r\n");
        build.Append("*/\r\n");
        build.Append("\r\n");
        build.Append($"namespace {strNameSpace};\r\n");
        //build.Append("{\r\n");
        build.Append("\r\n");
        build.Append("/// <summary>\r\n");
        build.Append("/// " + TableCnName + " (Model)\r\n");
        build.Append("/// </summary>\r\n");
        build.Append(@"[SugarTable(" + "\"" + tableName + "\"" + ", " + "\"" + TableCnName + "\"" + "), Entity(TableCnName = \"" + TableCnName + "\", TableName = \"" + tableName + "\")]\r\n");
        build.Append("public class " + tableName + " : BasePoco\r\n");
        build.Append("{\r\n");

        #region 属性
        //build.Append("\r\n");

        #region 处理表字段
        string columnCode = string.Empty;
        string dataType = string.Empty;
        string column_description = string.Empty;
        string NUMERIC_PRECISION = string.Empty;
        string NUMERIC_SCALE = string.Empty;
        string CHARACTER_MAXIMUM_LENGTH = string.Empty;

        string[] a = {
                    "ID", "CreatedBy", "CreatedTime", "UpdateBy", "UpdateTime", "ImportDataId", "ModificationNum",
                "Tag", "GroupId", "CompanyId", "CurrentNode", "IsActive","AuditStatus","IsDeleted"

        };

        for (int i = 0; i < dtColumn.Rows.Count; i++)
        {
            columnCode = dtColumn.Rows[i]["column_name"].ToString();
            dataType = dtColumn.Rows[i]["data_type"].ToString();
            column_description = dtColumn.Rows[i]["column_description"].ToString();
            NUMERIC_PRECISION = dtColumn.Rows[i]["NUMERIC_PRECISION"].ToString();
            NUMERIC_SCALE = dtColumn.Rows[i]["NUMERIC_SCALE"].ToString();
            CHARACTER_MAXIMUM_LENGTH = dtColumn.Rows[i]["CHARACTER_MAXIMUM_LENGTH"].ToString();

            if (string.IsNullOrWhiteSpace(column_description))
                column_description = columnCode;

            if (a.Contains(columnCode))
                continue;

            build.Append("\r\n");
            build.Append("    /// <summary>\r\n");
            build.Append("    /// " + column_description + "\r\n");
            build.Append("    /// </summary>\r\n");
            if (dataType == "decimal")
                build.Append($"    [Display(Name = \"" + columnCode + "\"), Description(\"" + column_description + "\"), Column(TypeName = \"decimal(" + NUMERIC_PRECISION + "," + NUMERIC_SCALE + ")\"), SugarColumn(IsNullable = true)]\r\n");
            else if (dataType == "varchar" || dataType == "nvarchar" || dataType == "char" || dataType == "text")
                build.Append("    [Display(Name = \"" + columnCode + "\"), Description(\"" + column_description + "\"), MaxLength(" + CHARACTER_MAXIMUM_LENGTH + ", ErrorMessage = \"" + column_description + " 不能超过 " + CHARACTER_MAXIMUM_LENGTH + " 个字符\"), SugarColumn(IsNullable = true)]\r\n");
            else
                build.Append("    [Display(Name = \"" + columnCode + "\"), Description(\"" + column_description + "\"), SugarColumn(IsNullable = true)]\r\n");

            switch (dataType)
            {
                #region 字符串
                case "varchar":
                case "nvarchar":
                case "char":
                case "text":
                    {
                        build.Append("    public string " + columnCode + " { get; set; }\r\n");
                        break;
                    }
                #endregion

                #region 日期
                case "datetime":
                case "date":
                    {
                        build.Append("    public DateTime? " + columnCode + " { get; set; }\r\n");
                        break;
                    }
                #endregion

                #region 数字
                case "decimal":
                    {

                        build.Append("    public decimal? " + columnCode + " { get; set; }\r\n");
                    }
                    break;
                case "int":
                    {

                        build.Append("    public int? " + columnCode + " { get; set; }\r\n");

                        break;
                    }
                case "uniqueidentifier":
                    {

                        build.Append("    public Guid? " + columnCode + " { get; set; }\r\n");

                        break;
                    }
                case "bit":
                    {

                        build.Append("    public bool? " + columnCode + " { get; set; }\r\n");

                        break;
                    }
                case "bigint":
                    {

                        build.Append("    public long? " + columnCode + " { get; set; }\r\n");

                        break;
                    }
                    #endregion
            }
        }
        #endregion
        #endregion


        //build.Append("    }\r\n");
        build.Append("}\r\n");

        ls[tableName] = build.ToString();
        CreateFilesByClassStringList(ls, strPath + @"\Entity\" + groupName, "{0}");

        #region Base
        build = new StringBuilder();
        ls = new Dictionary<string, string>();
        build.Append("/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。\r\n");
        build.Append("* " + tableName + ".cs\r\n");
        build.Append("*\r\n");
        build.Append("* 功 能： N / A\r\n");
        build.Append("* 类 名： " + tableName + "\r\n");
        build.Append("*\r\n");
        build.Append("* Ver    变更日期 负责人  变更内容\r\n");
        build.Append("* ───────────────────────────────────\r\n");
        build.Append("* V0.01  " + DateTime.Now.ToString() + "  SimonHsiao   初版\r\n");
        build.Append("*\r\n");
        build.Append("* Copyright(c) " + DateTime.Now.Year + " EU Corporation. All Rights Reserved.\r\n");
        build.Append("*┌──────────────────────────────────┐\r\n");
        build.Append("*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│\r\n");
        build.Append("*│　作者：SimonHsiao                                                  │\r\n");
        build.Append("*└──────────────────────────────────┘\r\n");
        build.Append("*/\r\n");
        build.Append("\r\n");
        build.Append("namespace EU.Core.Model.Models;\r\n");
        //build.Append("{\r\n");
        build.Append("\r\n");
        build.Append("/// <summary>\r\n");
        build.Append("/// " + TableCnName + " (Dto.Base)\r\n");
        build.Append("/// </summary>\r\n");
        build.Append("public class " + tableName + "Base : BasePoco\r\n");
        build.Append("{\r\n");

        #region 属性
        //build.Append("\r\n");

        #region 处理表字段 

        for (int i = 0; i < dtColumn.Rows.Count; i++)
        {
            columnCode = dtColumn.Rows[i]["column_name"].ToString();
            dataType = dtColumn.Rows[i]["data_type"].ToString();
            column_description = dtColumn.Rows[i]["column_description"].ToString();
            NUMERIC_PRECISION = dtColumn.Rows[i]["NUMERIC_PRECISION"].ToString();
            NUMERIC_SCALE = dtColumn.Rows[i]["NUMERIC_SCALE"].ToString();
            CHARACTER_MAXIMUM_LENGTH = dtColumn.Rows[i]["CHARACTER_MAXIMUM_LENGTH"].ToString();

            if (string.IsNullOrWhiteSpace(column_description))
                column_description = columnCode;

            if (a.Contains(columnCode))
                continue;

            build.Append("\r\n");
            build.Append("    /// <summary>\r\n");
            build.Append("    /// " + column_description + "\r\n");
            build.Append("    /// </summary>\r\n");
            if (dataType == "decimal")
                build.Append($"    [Display(Name = \"" + columnCode + "\"), Description(\"" + column_description + "\"), Column(TypeName = \"decimal(" + NUMERIC_PRECISION + "," + NUMERIC_SCALE + ")\"), SugarColumn(IsNullable = true)]\r\n");
            else if (dataType == "varchar" || dataType == "nvarchar" || dataType == "char" || dataType == "text")
                build.Append("    [Display(Name = \"" + columnCode + "\"), Description(\"" + column_description + "\"), MaxLength(" + CHARACTER_MAXIMUM_LENGTH + ", ErrorMessage = \"" + column_description + " 不能超过 " + CHARACTER_MAXIMUM_LENGTH + " 个字符\"), SugarColumn(IsNullable = true)]\r\n");
            else
                build.Append("    [Display(Name = \"" + columnCode + "\"), Description(\"" + column_description + "\"), SugarColumn(IsNullable = true)]\r\n");
            switch (dataType)
            {
                #region 字符串
                case "varchar":
                case "nvarchar":
                case "char":
                case "text":
                    {
                        build.Append("    public string " + columnCode + " { get; set; }\r\n");
                        break;
                    }
                #endregion

                #region 日期
                case "datetime":
                case "date":
                    {
                        build.Append("    public DateTime? " + columnCode + " { get; set; }\r\n");
                        break;
                    }
                #endregion

                #region 数字
                case "decimal":
                    {

                        build.Append("    public decimal? " + columnCode + " { get; set; }\r\n");
                    }
                    break;
                case "int":
                    {

                        build.Append("    public int? " + columnCode + " { get; set; }\r\n");

                        break;
                    }
                case "uniqueidentifier":
                    {

                        build.Append("    public Guid? " + columnCode + " { get; set; }\r\n");

                        break;
                    }
                case "bit":
                    {

                        build.Append("    public bool? " + columnCode + " { get; set; }\r\n");

                        break;
                    }
                    #endregion
            }
        }
        #endregion 

        #endregion


        //build.Append("    }\r\n");
        build.Append("}\r\n");

        ls.Add(tableName + ".Dto.Base", build.ToString());
        CreateFilesByClassStringList(ls, strPath + @"\Base\" + groupName, "{0}");
        #endregion

        #region Insert
        build = new StringBuilder();
        ls = new Dictionary<string, string>();

        build.Append("/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。\r\n");
        build.Append("* " + tableName + ".cs\r\n");
        build.Append("*\r\n");
        build.Append("* 功 能： N / A\r\n");
        build.Append("* 类 名： " + tableName + "\r\n");
        build.Append("*\r\n");
        build.Append("* Ver    变更日期 负责人  变更内容\r\n");
        build.Append("* ───────────────────────────────────\r\n");
        build.Append("* V0.01  " + DateTime.Now.ToString() + "  SimonHsiao   初版\r\n");
        build.Append("*\r\n");
        build.Append("* Copyright(c) " + DateTime.Now.Year + " EU Corporation. All Rights Reserved.\r\n");
        build.Append("*┌──────────────────────────────────┐\r\n");
        build.Append("*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│\r\n");
        build.Append("*│　作者：SimonHsiao                                                  │\r\n");
        build.Append("*└──────────────────────────────────┘\r\n");
        build.Append("*/ \r\n");
        build.Append("\r\n");
        build.Append("namespace EU.Core.Model.Models;\r\n");
        //build.Append("{\r\n");
        build.Append("\r\n");
        build.Append("/// <summary>\r\n");
        build.Append("/// " + TableCnName + " (Dto.InsertInput)\r\n");
        build.Append("/// </summary>\r\n");
        build.Append("public class Insert" + tableName + "Input : " + tableName + "Base\r\n");
        build.Append("{\r\n");

        build.Append("}\r\n");
        //build.Append("}\r\n");

        ls.Add(tableName + ".Dto.InsertInput", build.ToString());

        string strPath1 = strPath + @"\Insert\" + groupName;
        var fileName = $"{string.Format("{0}", tableName + ".Dto.InsertInput")}.cs";
        var fileFullPath = Path.Combine(strPath1, fileName);

        if (!File.Exists(fileFullPath))
            CreateFilesByClassStringList(ls, strPath1, "{0}");
        #endregion

        #region Edit
        build = new StringBuilder();
        ls = new Dictionary<string, string>();

        build.Append("/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。\r\n");
        build.Append("* " + tableName + ".cs\r\n");
        build.Append("*\r\n");
        build.Append("* 功 能： N / A\r\n");
        build.Append("* 类 名： " + tableName + "\r\n");
        build.Append("*\r\n");
        build.Append("* Ver    变更日期 负责人  变更内容\r\n");
        build.Append("* ───────────────────────────────────\r\n");
        build.Append("* V0.01  " + DateTime.Now.ToString() + "  SimonHsiao   初版\r\n");
        build.Append("*\r\n");
        build.Append("* Copyright(c) " + DateTime.Now.Year + " EU Corporation. All Rights Reserved.\r\n");
        build.Append("*┌──────────────────────────────────┐\r\n");
        build.Append("*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│\r\n");
        build.Append("*│　作者：SimonHsiao                                                  │\r\n");
        build.Append("*└──────────────────────────────────┘\r\n");
        build.Append("*/ \r\n");
        build.Append("\r\n");
        build.Append("namespace EU.Core.Model.Models;\r\n");
        //build.Append("{\r\n");
        build.Append("\r\n");
        build.Append("/// <summary>\r\n");
        build.Append("/// " + TableCnName + " (Dto.EditInput)\r\n");
        build.Append("/// </summary>\r\n");
        build.Append("public class Edit" + tableName + "Input : " + tableName + "Base\r\n");
        build.Append("{\r\n");

        build.Append("}\r\n");
        //build.Append("}\r\n");

        ls.Add(tableName + ".Dto.EditInput", build.ToString());

        strPath1 = strPath + @"\Edit\" + groupName;
        fileName = $"{string.Format("{0}", tableName + ".Dto.EditInput")}.cs";
        fileFullPath = Path.Combine(strPath1, fileName);

        if (!File.Exists(fileFullPath))
            CreateFilesByClassStringList(ls, strPath1, "{0}");
        #endregion

        #region View
        build = new StringBuilder();
        ls = new Dictionary<string, string>();

        build.Append("/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。\r\n");
        build.Append("* " + tableName + ".cs\r\n");
        build.Append("*\r\n");
        build.Append("* 功 能： N / A\r\n");
        build.Append("* 类 名： " + tableName + "\r\n");
        build.Append("*\r\n");
        build.Append("* Ver    变更日期 负责人  变更内容\r\n");
        build.Append("* ───────────────────────────────────\r\n");
        build.Append("* V0.01  " + DateTime.Now.ToString() + "  SimonHsiao   初版\r\n");
        build.Append("*\r\n");
        build.Append("* Copyright(c) " + DateTime.Now.Year + " EU Corporation. All Rights Reserved.\r\n");
        build.Append("*┌──────────────────────────────────┐\r\n");
        build.Append("*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│\r\n");
        build.Append("*│　作者：SimonHsiao                                                  │\r\n");
        build.Append("*└──────────────────────────────────┘\r\n");
        build.Append("*/ \r\n");
        build.Append("\r\n");
        build.Append("namespace EU.Core.Model.Models;\r\n");
        //build.Append("{\r\n");k
        build.Append("\r\n");
        build.Append("/// <summary>\r\n");
        build.Append("/// " + TableCnName + "(Dto.View)\r\n");
        build.Append("/// </summary>\r\n");
        build.Append("public class " + tableName + "Dto : " + tableName + "\r\n");
        build.Append("{\r\n");

        build.Append("}\r\n");
        //build.Append("}\r\n");

        ls.Add(tableName + ".Dto.View", build.ToString());

        strPath1 = strPath + @"\View\" + groupName;
        fileName = $"{string.Format("{0}", tableName + ".Dto.View")}.cs";
        fileFullPath = Path.Combine(strPath1, fileName);

        if (!File.Exists(fileFullPath))
            CreateFilesByClassStringList(ls, strPath1, "{0}");
        #endregion

    }
    #endregion


    #region 根据数据库表生产IRepository层

    /// <summary>
    /// 根据数据库表生产IRepository层
    /// </summary>
    /// <param name="sqlSugarClient"></param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="strPath">实体类存放路径</param>
    /// <param name="strNameSpace">命名空间</param>
    /// <param name="lstTableNames">生产指定的表</param>
    /// <param name="strInterface">实现接口</param>
    /// <param name="isMuti"></param>
    private static void Create_IRepository_ClassFileByDBTalbe(
      SqlSugarScope sqlSugarClient,
      string ConnId,
      string strPath,
      string strNameSpace,
      string[] lstTableNames,
      string strInterface,
      bool isMuti = false
        )
    {
        //多库文件分离
        if (isMuti)
        {
            strPath = strPath + @"\" + ConnId;
            strNameSpace = strNameSpace + "." + ConnId;
        }

        var IDbFirst = sqlSugarClient.DbFirst;
        if (lstTableNames != null && lstTableNames.Length > 0)
        {
            IDbFirst = IDbFirst.Where(lstTableNames);
        }
        var ls = IDbFirst.IsCreateDefaultValue().IsCreateAttribute()

             .SettingClassTemplate(p => p =

        @"namespace " + strNameSpace + @";

	/// <summary>
	/// I{ClassName}Repository
	/// </summary>	
    public interface I{ClassName}Repository : IBaseRepository<{ClassName}>" + (string.IsNullOrEmpty(strInterface) ? "" : (" , " + strInterface)) + @"
    {
    }")

              .ToClassStringList(strNameSpace);
        CreateFilesByClassStringList(ls, strPath, "I{0}Repository");
    }
    #endregion


    #region 根据数据库表生产IServices层

    /// <summary>
    /// 根据数据库表生产IServices层
    /// </summary>
    /// <param name="sqlSugarClient"></param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="strPath">实体类存放路径</param>
    /// <param name="strNameSpace">命名空间</param>
    /// <param name="lstTableNames">生产指定的表</param>
    /// <param name="strInterface">实现接口</param>
    /// <param name="isMuti"></param>
    private static void Create_IServices_ClassFileByDBTalbe(
      SqlSugarScope sqlSugarClient,
      string ConnId,
      string strPath,
      string strNameSpace,
      string[] lstTableNames,
      string strInterface,
      bool isMuti = false)
    {
        //多库文件分离
        if (isMuti)
        {
            strPath = strPath + @"\" + ConnId;
            strNameSpace = strNameSpace + "." + ConnId;
        }

        var IDbFirst = sqlSugarClient.DbFirst;
        if (lstTableNames != null && lstTableNames.Length > 0)
        {
            IDbFirst = IDbFirst.Where(lstTableNames);
        }

        var groupName = GetGroupName(lstTableNames[0]);

        #region 获取表中文名
        string sql = @"SELECT f.value TableName
                            FROM sysobjects d
                                 LEFT JOIN sys.extended_properties f
                                    ON d.id = f.major_id AND f.minor_id = 0
                            WHERE d.name = '{0}'";
        sql = string.Format(sql, lstTableNames[0]);
        string TableCnName = Convert.ToString(DBHelper.ExecuteScalar(sql, null));
        if (string.IsNullOrWhiteSpace(TableCnName)) TableCnName = lstTableNames[0];
        #endregion
        var ls = IDbFirst.IsCreateDefaultValue().IsCreateAttribute()

              .SettingClassTemplate(p => p =
GetCopyRight(lstTableNames[0]) +

@"namespace " + strNameSpace + @";

/// <summary>
/// " + TableCnName + @"(自定义服务接口)
/// </summary>	
public interface I{ClassName}Services : IBaseServices<{ClassName}, {ClassName}Dto, Insert{ClassName}Input, Edit{ClassName}Input>" + (string.IsNullOrEmpty(strInterface) ? "" : (" , " + strInterface)) + @"
{
}")

               .ToClassStringList(strNameSpace);

        strPath = strPath + @"\" + groupName;
        var fileName = $"{string.Format("I{0}Services", lstTableNames[0])}.cs";
        var fileFullPath = Path.Combine(strPath, fileName);

        if (!File.Exists(fileFullPath))
            CreateFilesByClassStringList(ls, strPath, "I{0}Services");
    }
    #endregion



    #region 根据数据库表生产 Repository 层

    /// <summary>
    /// 根据数据库表生产 Repository 层
    /// </summary>
    /// <param name="sqlSugarClient"></param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="strPath">实体类存放路径</param>
    /// <param name="strNameSpace">命名空间</param>
    /// <param name="lstTableNames">生产指定的表</param>
    /// <param name="strInterface">实现接口</param>
    /// <param name="isMuti"></param>
    private static void Create_Repository_ClassFileByDBTalbe(
      SqlSugarScope sqlSugarClient,
      string ConnId,
      string strPath,
      string strNameSpace,
      string[] lstTableNames,
      string strInterface,
      bool isMuti = false)
    {
        //多库文件分离
        if (isMuti)
        {
            strPath = strPath + @"\" + ConnId;
            strNameSpace = strNameSpace + "." + ConnId;
        }

        var IDbFirst = sqlSugarClient.DbFirst;
        if (lstTableNames != null && lstTableNames.Length > 0)
        {
            IDbFirst = IDbFirst.Where(lstTableNames);
        }

        #region 获取表中文名
        string sql = @"SELECT f.value TableName
                            FROM sysobjects d
                                 LEFT JOIN sys.extended_properties f
                                    ON d.id = f.major_id AND f.minor_id = 0
                            WHERE d.name = '{0}'";
        sql = string.Format(sql, lstTableNames[0]);
        string TableCnName = Convert.ToString(DBHelper.ExecuteScalar(sql, null));
        if (string.IsNullOrWhiteSpace(TableCnName)) TableCnName = lstTableNames[0];
        #endregion

        var ls = IDbFirst.IsCreateDefaultValue().IsCreateAttribute()

              .SettingClassTemplate(p => p =
@"using EU.Core.IRepository" + (isMuti ? "." + ConnId + "" : "") + @";
using EU.Core.IRepository.UnitOfWork;
using EU.Core.Model.Models" + (isMuti ? "." + ConnId + "" : "") + @";
using EU.Core.Repository.Base;

namespace " + strNameSpace + @"
{
	/// <summary>
	/// " + TableCnName + @"Repository
	/// </summary>
    public class {ClassName}Repository : BaseRepository<{ClassName}>, I{ClassName}Repository" + (string.IsNullOrEmpty(strInterface) ? "" : (" , " + strInterface)) + @"
    {
        public {ClassName}Repository(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}")
              .ToClassStringList(strNameSpace);


        CreateFilesByClassStringList(ls, strPath, "{0}Repository");
    }
    #endregion


    #region 根据数据库表生产 Services 层

    /// <summary>
    /// 根据数据库表生产 Services 层
    /// </summary>
    /// <param name="sqlSugarClient"></param>
    /// <param name="ConnId">数据库链接ID</param>
    /// <param name="strPath">实体类存放路径</param>
    /// <param name="strNameSpace">命名空间</param>
    /// <param name="lstTableNames">生产指定的表</param>
    /// <param name="strInterface">实现接口</param>
    /// <param name="isMuti"></param>
    private static void Create_Services_ClassFileByDBTalbe(
      SqlSugarScope sqlSugarClient,
      string ConnId,
      string strPath,
      string strNameSpace,
      string[] lstTableNames,
      string strInterface,
      bool isMuti = false)
    {
        //多库文件分离
        if (isMuti)
        {
            strPath = strPath + @"\" + ConnId;
            strNameSpace = strNameSpace + "." + ConnId;
        }

        var IDbFirst = sqlSugarClient.DbFirst;
        if (lstTableNames != null && lstTableNames.Length > 0)
        {
            IDbFirst = IDbFirst.Where(lstTableNames);
        }

        #region 获取表中文名
        string sql = @"SELECT f.value TableName
                            FROM sysobjects d
                                 LEFT JOIN sys.extended_properties f
                                    ON d.id = f.major_id AND f.minor_id = 0
                            WHERE d.name = '{0}'";
        sql = string.Format(sql, lstTableNames[0]);
        string TableCnName = Convert.ToString(DBHelper.ExecuteScalar(sql, null));
        if (string.IsNullOrWhiteSpace(TableCnName)) TableCnName = lstTableNames[0];
        #endregion
        var groupName = GetGroupName(lstTableNames[0]);

        var ls = IDbFirst.IsCreateDefaultValue().IsCreateAttribute()

              .SettingClassTemplate(p => p =
GetCopyRight(lstTableNames[0]) + @"
namespace " + strNameSpace + @";

/// <summary>
/// " + TableCnName + @" (服务)
/// </summary>
public class {ClassName}Services : BaseServices<{ClassName}, {ClassName}Dto, Insert{ClassName}Input, Edit{ClassName}Input>, I{ClassName}Services" + (string.IsNullOrEmpty(strInterface) ? "" : (" , " + strInterface)) + @"
{
    public {ClassName}Services(IBaseRepository<{ClassName}> dal)
    {
        BaseDal = dal;
    }
}")
              .ToClassStringList(strNameSpace);

        strPath = strPath + @"\" + groupName;
        var fileName = $"{string.Format("{0}Services", lstTableNames[0])}.cs";
        var fileFullPath = Path.Combine(strPath, fileName);

        if (!File.Exists(fileFullPath))
            CreateFilesByClassStringList(ls, strPath, "{0}Services");
    }
    #endregion


    #region 根据模板内容批量生成文件
    /// <summary>
    /// 根据模板内容批量生成文件
    /// </summary>
    /// <param name="ls">类文件字符串list</param>
    /// <param name="strPath">生成路径</param>
    /// <param name="fileNameTp">文件名格式模板</param>
    private static void CreateFilesByClassStringList(Dictionary<string, string> ls, string strPath, string fileNameTp)
    {

        foreach (var item in ls)
        {
            var fileName = $"{string.Format(fileNameTp, item.Key)}.cs";
            var fileFullPath = Path.Combine(strPath, fileName);
            if (!Directory.Exists(strPath))
            {
                Directory.CreateDirectory(strPath);
            }
            File.WriteAllText(fileFullPath, item.Value, Encoding.UTF8);
        }
    }
    #endregion
}
