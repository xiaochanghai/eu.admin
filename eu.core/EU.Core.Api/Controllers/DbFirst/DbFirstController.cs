using EU.Core.Common.DB;
using EU.Core.Common.Seed;
using EU.Core.Model.Systems.DataBase;
using EU.Core.Model.Tenants;
using Mapster;
using SqlSugar;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace EU.Core.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController, ApiExplorerSettings(GroupName = Grouping.GroupName_Assistant)]
    //[Authorize(Permissions.Name)]
    public class DbFirstController : ControllerBase
    {
        private readonly SqlSugarScope _sqlSugarClient;
        private readonly IWebHostEnvironment Env;

        /// <summary>
        /// 构造函数
        /// </summary>
        public DbFirstController(ISqlSugarClient sqlSugarClient, IWebHostEnvironment env)
        {
            _sqlSugarClient = sqlSugarClient as SqlSugarScope;
            Env = env;
        }

        /// <summary>
        /// 获取 整体框架 文件(主库)(一般可用第一次生成)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ServiceResult<string> GetFrameFiles()
        {
            var data = new ServiceResult<string>() { Success = true, Message = "" };
            data.Data += @"file path is:C:\my-file\}";
            var isMuti = BaseDBConfig.IsMulti;
            if (Env.IsDevelopment())
            {
                data.Data += $"Controller层生成：{FrameSeed.CreateControllers(_sqlSugarClient)} || ";

                BaseDBConfig.ValidConfig.ForEach(m =>
                {
                    _sqlSugarClient.ChangeDatabase(m.ConfigId.ToLower());
                    data.Data += $"库{m.ConfigId}-Model层生成：{FrameSeed.CreateModels(_sqlSugarClient, m.ConfigId, isMuti)} || ";
                    data.Data += $"库{m.ConfigId}-IRepositorys层生成：{FrameSeed.CreateIRepositorys(_sqlSugarClient, m.ConfigId, isMuti)} || ";
                    data.Data += $"库{m.ConfigId}-IServices层生成：{FrameSeed.CreateIServices(_sqlSugarClient, m.ConfigId, isMuti)} || ";
                    data.Data += $"库{m.ConfigId}-Repository层生成：{FrameSeed.CreateRepository(_sqlSugarClient, m.ConfigId, isMuti)} || ";
                    data.Data += $"库{m.ConfigId}-Services层生成：{FrameSeed.CreateServices(_sqlSugarClient, m.ConfigId, isMuti)} || ";
                });

                // 切回主库
                _sqlSugarClient.ChangeDatabase(MainDb.CurrentDbConnId.ToLower());
            }
            else
            {
                data.Success = false;
                data.Message = "当前不处于开发模式，代码生成不可用！";
            }

            return data;
        }

        /// <summary>
        /// 获取仓储层和服务层(需指定表名和数据库)
        /// </summary>
        /// <param name="ConnID">数据库链接名称</param>
        /// <param name="tableNames">需要生成的表名</param>
        /// <returns></returns>
        [HttpPost]
        public ServiceResult<string> GetFrameFilesByTableNames([FromBody] string[] tableNames, [FromQuery] string ConnID = null)
        {
            ConnID = ConnID == null ? MainDb.CurrentDbConnId.ToLower() : ConnID;

            var isMuti = BaseDBConfig.IsMulti;
            var data = new ServiceResult<string>() { Success = true, Message = "" };
            if (Env.IsDevelopment())
            {
                data.Data += $"库{ConnID}-IRepositorys层生成：{FrameSeed.CreateIRepositorys(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                data.Data += $"库{ConnID}-IServices层生成：{FrameSeed.CreateIServices(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                data.Data += $"库{ConnID}-Repository层生成：{FrameSeed.CreateRepository(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                data.Data += $"库{ConnID}-Services层生成：{FrameSeed.CreateServices(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
            }
            else
            {
                data.Success = false;
                data.Message = "当前不处于开发模式，代码生成不可用！";
            }

            return data;
        }
        /// <summary>
        /// 获取实体(需指定表名和数据库)
        /// </summary>
        /// <param name="ConnID">数据库链接名称</param>
        /// <param name="tableNames">需要生成的表名</param>
        /// <returns></returns>
        [HttpPost]
        public ServiceResult<string> GetFrameFilesByTableNamesForEntity([FromBody] string[] tableNames, [FromQuery] string ConnID = null)
        {
            ConnID = ConnID == null ? MainDb.CurrentDbConnId.ToLower() : ConnID;

            var isMuti = BaseDBConfig.IsMulti;
            var data = new ServiceResult<string>() { Success = true, Message = "" };
            if (Env.IsDevelopment())
            {
                data.Data += $"库{ConnID}-Models层生成：{FrameSeed.CreateModels(_sqlSugarClient, ConnID, isMuti, tableNames)}";
            }
            else
            {
                data.Success = false;
                data.Message = "当前不处于开发模式，代码生成不可用！";
            }
            return data;
        }
        /// <summary>
        /// 获取控制器(需指定表名和数据库)
        /// </summary>
        /// <param name="ConnID">数据库链接名称</param>
        /// <param name="tableNames">需要生成的表名</param>
        /// <returns></returns>
        [HttpPost]
        public ServiceResult<string> GetFrameFilesByTableNamesForController([FromBody] string[] tableNames, [FromQuery] string ConnID = null)
        {
            ConnID = ConnID == null ? MainDb.CurrentDbConnId.ToLower() : ConnID;

            var isMuti = BaseDBConfig.IsMulti;
            var data = new ServiceResult<string>() { Success = true, Message = "" };
            if (Env.IsDevelopment())
            {
                data.Data += $"库{ConnID}-Controllers层生成：{FrameSeed.CreateControllers(_sqlSugarClient, ConnID, isMuti, tableNames)}";
            }
            else
            {
                data.Success = false;
                data.Message = "当前不处于开发模式，代码生成不可用！";
            }
            return data;
        }

        /// <summary>
        /// DbFrist 根据数据库表名 生成整体框架,包含Model层(一般可用第一次生成)
        /// </summary>
        /// <param name="ConnID">数据库链接名称</param>
        /// <param name="tableName">需要生成的表名</param>
        /// <returns></returns>
        [HttpPost]
        public ServiceResult<string> GetAllFrameFilesByTableNames([FromQuery] string tableName, [FromQuery] string ConnID = null)
        {
            string[] tableNames = new string[1];
            tableNames[0] = tableName;
            ConnID = ConnID == null ? MainDb.CurrentDbConnId.ToLower() : ConnID;

            var isMuti = BaseDBConfig.IsMulti;
            var data = new ServiceResult<string>() { Success = true, Message = "" };
            if (Env.IsDevelopment())
            {
                _sqlSugarClient.ChangeDatabase(ConnID.ToLower());
                data.Data += $"Controller层生成：{FrameSeed.CreateControllers(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                data.Data += $"库{ConnID}-Model层生成：{FrameSeed.CreateModels(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                //data.response += $"库{ConnID}-IRepositorys层生成：{FrameSeed.CreateIRepositorys(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                data.Data += $"库{ConnID}-IServices层生成：{FrameSeed.CreateIServices(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                //data.response += $"库{ConnID}-Repository层生成：{FrameSeed.CreateRepository(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                data.Data += $"库{ConnID}-Services层生成：{FrameSeed.CreateServices(_sqlSugarClient, ConnID, isMuti, tableNames)} || ";
                // 切回主库
                _sqlSugarClient.ChangeDatabase(MainDb.CurrentDbConnId.ToLower());
            }
            else
            {
                data.Success = false;
                data.Message = "当前不处于开发模式，代码生成不可用！";
            }

            return data;
        }
        [return: NotNull]
        private ISqlSugarClient GetTenantDb(string configId)
        {
            if (!_sqlSugarClient.AsTenant().IsAnyConnection(configId))
            {
                var tenant = _sqlSugarClient.Queryable<SysTenant>().WithCache()
                    .Where(s => s.TenantType == TenantTypeEnum.Db)
                    .Where(s => s.ConfigId == configId)
                    .First();
                if (tenant != null)
                {
                    _sqlSugarClient.AsTenant().AddConnection(tenant.GetConnectionConfig());
                }
            }

            var db = _sqlSugarClient.AsTenant().GetConnectionScope(configId);
            if (db is null)
            {
                throw new ApplicationException("无效的数据库配置");
            }

            return db;
        }
        /// <summary>
        /// 获取表信息
        /// </summary>
        /// <param name="configId">配置Id</param>
        /// <param name="readType">读取类型</param>
        /// <returns></returns>
        [HttpGet]
        public ServiceResult<List<DbTableInfo>> GetTableInfoList(string configId, DataBaseReadType readType = DataBaseReadType.Db)
        {
            if (configId.IsNullOrEmpty())
            {
                configId = MainDb.CurrentDbConnId;
            }

            configId = configId.ToLower();

            var provider = GetTenantDb(configId);
            List<DbTableInfo> data = null;
            switch (readType)
            {
                case DataBaseReadType.Db:
                    data = provider.DbMaintenance.GetTableInfoList(false);
                    break;
                case DataBaseReadType.Entity:
                    if (EntityUtility.TenantEntitys.TryGetValue(configId, out var types))
                    {
                        data = types.Select(s => provider.EntityMaintenance.GetEntityInfo(s))
                            .Select(s => new { Name = s.DbTableName, Description = s.TableDescription })
                            .Adapt<List<DbTableInfo>>();
                    }

                    break;
            }


            return new ServiceResult<List<DbTableInfo>>() { Data = data, Success = true, Message = "" };
        }

        /// <summary>
        /// 获取表字段
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="configId">ConfigId</param>
        /// <param name="readType">读取类型</param>
        /// <returns></returns>
        [HttpGet]
        public ServiceResult<List<DbColumnInfoOutput>> GetColumnInfosByTableName(string tableName, string configId = null,
            DataBaseReadType readType = DataBaseReadType.Db)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return ServiceResult<List<DbColumnInfoOutput>>.OprateFailed("表名不能为空");

            if (configId.IsNullOrEmpty())
            {
                configId = MainDb.CurrentDbConnId;
            }

            configId = configId.ToLower();

            List<DbColumnInfoOutput> data = null;
            var provider = GetTenantDb(configId);
            switch (readType)
            {
                case DataBaseReadType.Db:
                    data = provider.DbMaintenance.GetColumnInfosByTableName(tableName, false)
                        .Adapt<List<DbColumnInfoOutput>>();
                    break;
                case DataBaseReadType.Entity:
                    if (EntityUtility.TenantEntitys.TryGetValue(configId, out var types))
                    {
                        var type = types.FirstOrDefault(s => s.Name == tableName);
                        data = provider.EntityMaintenance.GetEntityInfo(type).Columns.Adapt<List<DbColumnInfoOutput>>();
                    }

                    break;
            }

            return ServiceResult<List<DbColumnInfoOutput>>.OprateSuccess(data);
        }
    }
}