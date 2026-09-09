/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* AgModelConfig.cs
*
* 功 能： N / A
* 类 名： AgModelConfig
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2026/9/9 23:28:44  SahHsiao   初版
*
* Copyright(c) 2026 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using Mysqlx.Crud;

namespace EU.Core.Services;

/// <summary>
/// 平台共享模型配置，API 密钥仅以密文持久化。 (服务)
/// </summary>
public class AgModelConfigServices : BaseServices<AgModelConfig, AgModelConfigDto, InsertAgModelConfigInput, EditAgModelConfigInput>, IAgModelConfigServices
{
    public AgModelConfigServices(IBaseRepository<AgModelConfig> dal)
    {
        BaseDal = dal;
    }


    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);

        var insert = JsonHelper.JsonToObj<InsertAgModelConfigInput>(entity.ObjToString());

        #region 检查是否存在相同值
        await CheckOnly(model);
        #endregion
        model.Provider = "OpenAICompatible";
        model.ApiKeyCiphertext = "";
        model.CredentialRevision = 1;
        model.LogicalRevision = 1;
        var id = await base.Add(model);
        var key = AppSettings.app(["ModelConfig", "EncryptionKey"]).ObjToString();
        model.ApiKeyCiphertext = ModelConfigCredentialCipher.Protect(key, id, insert.ApiKey!);
        await Update(model, new List<string> { "ApiKeyCiphertext" });
        return id;
    }
    #endregion

    #region 更新
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var insert = JsonHelper.JsonToObj<InsertAgModelConfigInput>(entity.ObjToString());
        var model = ConvertToEntity(entity);
        if (insert.ApiKey.IsNotEmptyOrNull())
        {
            var key = AppSettings.app(["ModelConfig", "EncryptionKey"]).ObjToString();
            model.ApiKeyCiphertext = ModelConfigCredentialCipher.Protect(key, Id, insert.ApiKey!);
        }

        var dic = ConvertToDic(entity);
        var lstColumns = dic.Keys.Where(x => x != "ID" && x != "Id").ToList();

        var order = await QueryDto(Id);
        var result = await Update(model, lstColumns);

        return result;
    }
    #endregion
}