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
}