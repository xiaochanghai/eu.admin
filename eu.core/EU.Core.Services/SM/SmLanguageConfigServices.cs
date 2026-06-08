/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmLanguageConfig.cs
*
* 功 能： N / A
* 类 名： SmLanguageConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2026/6/7 11:13:05  SahHsiao   初版
*
* Copyright(c) 2026 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using Dm;

namespace EU.Core.Services;

/// <summary>
/// 多语配置 (服务)
/// </summary>
public class SmLanguageConfigServices : BaseServices<SmLanguageConfig, SmLanguageConfigDto, InsertSmLanguageConfigInput, EditSmLanguageConfigInput>, ISmLanguageConfigServices
{
    public SmLanguageConfigServices(IBaseRepository<SmLanguageConfig> dal)
    {
        BaseDal = dal;
    }



    #region 复制模块

    /// <summary>
    /// 复制模块及其配置
    /// </summary>
    /// <param name="moduleId">源模块ID</param>
    /// <param name="module1">新模块信息</param>
    /// <returns>复制结果</returns>
    /// <remarks>
    /// 复制模块的基本信息、SQL配置和列配置
    /// 新模块使用新的ID和代码
    /// </remarks>
    public async Task<ServiceResult<SmLanguageConfig>> ByRefId(Guid refId, string refType, string refField)
    {
        var config = await Db.Queryable<SmLanguageConfig>()
            .Where(x => x.RefId == refId && x.RefType == refType && x.RefField == refField)
            .FirstAsync();

        if (config is null)
        {
            string valueZH = null;
            if (refType == "ModuleColumn")
            {
                var column = await Db.Queryable<SmModuleColumn>()
                    .Where(x => x.ID == refId)
                    .FirstAsync();
                valueZH = column?.GetType().GetProperty(refField)?.GetValue(column)?.ToString();
            }
            else
            {
                var module = await Db.Queryable<SmModules>()
                    .Where(x => x.ID == refId)
                    .Select(x => x.ModuleName)
                    .FirstAsync();
                valueZH = module;
            }

            config = new SmLanguageConfig()
            {
                RefId = refId,
                RefType = refType,
                RefField = refField,
                Value_ZH = valueZH
            };

            await Db.Insertable(config).ExecuteCommandAsync();
        }
        return Success(config);
    }

    #endregion
}