/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgAgentDefinition.cs
*
* 功 能： N / A
* 类 名： AgAgentDefinition
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2026/8/12 0:58:24  SahHsiao   初版
*
* Copyright(c) 2026 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// Agent 定义表 (服务)
/// </summary>
public class AgAgentDefinitionServices : BaseServices<AgAgentDefinition, AgAgentDefinitionDto, InsertAgAgentDefinitionInput, EditAgAgentDefinitionInput>, IAgAgentDefinitionServices
{
    public AgAgentDefinitionServices(IBaseRepository<AgAgentDefinition> dal)
    {
        BaseDal = dal;
    }
}