/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* EcGoods.cs
*
* 功 能： N / A
* 类 名： EcGoods
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V0.01  2025/12/21 21:24:01  SahHsiao   初版
*
* Copyright(c) 2025 EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　作者：SahHsiao                                                  │
*└──────────────────────────────────┘
*/ 

namespace EU.Core.Model.Models;

/// <summary>
/// 商品管理(Dto.View)
/// </summary>
public class EcGoodsDto : EcGoods
{
    /// <summary>产品配件，仅返回展示字段，不暴露服务器存储路径。</summary>
    [SqlSugar.SugarColumn(IsIgnore = true)]
    public List<EcGoodsAccessoryDto> Accessories { get; set; } = [];
}

/// <summary>商品详情配件，原图通过 api/File/Img/{ID} 获取。</summary>
public class EcGoodsAccessoryDto
{
    public Guid ID { get; set; }
    public string FileName { get; set; }
    public string OriginalFileName { get; set; }
}
