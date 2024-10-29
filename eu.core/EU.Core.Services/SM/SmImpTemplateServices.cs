/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmImpTemplate.cs
*
*功 能： N / A
* 类 名： SmImpTemplate
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/22 9:39:18  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 系统导入模板 (服务)
/// </summary>
public class SmImpTemplateServices : BaseServices<SmImpTemplate, SmImpTemplateDto, InsertSmImpTemplateInput, EditSmImpTemplateInput>, ISmImpTemplateServices
{
    private readonly IBaseRepository<SmImpTemplate> _dal;
    public SmImpTemplateServices(IBaseRepository<SmImpTemplate> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    #region 按模块代码获取数据
    public async Task<ServiceResult<SmImpTemplateDto>> QueryByModuleId(Guid moduleId)
    {
        var template1 = new SmImpTemplateDto();

        var template = await QuerySingle(x => x.ModuleId == moduleId);
        if (template != null)
        {
            template1 = Mapper.Map(template).ToANew<SmImpTemplateDto>();
            
            var attachment = await Db.Queryable<FileAttachment>().Where(x => x.MasterId == template.ID).OrderByDescending(x => x.CreatedTime).FirstAsync();
            if (attachment != null)
                template1.FileId = attachment.ID;
        }
        return ServiceResult<SmImpTemplateDto>.OprateSuccess(template1, ResponseText.QUERY_SUCCESS);
    }
    #endregion
}