/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EcNews.cs
*
* 功 能： N / A
* 类 名： EcNews
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/12/22 22:22:59  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.IServices;

/// <summary>
/// 新闻(自定义服务接口)
/// </summary>	
public interface IEcNewsServices : IBaseServices<EcNews, EcNewsDto, InsertEcNewsInput, EditEcNewsInput>
{
}