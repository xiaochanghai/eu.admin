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

using EU.Core.IServices.Agents;
using System.Security.Cryptography;

namespace EU.Core.Services;

/// <summary>
/// 平台共享模型配置，API 密钥仅以密文持久化。 (服务)
/// </summary>
public class AgModelConfigServices : BaseServices<AgModelConfig, AgModelConfigDto, InsertAgModelConfigInput, EditAgModelConfigInput>, IAgModelConfigServices
{
    private readonly RedisCacheService Redis;

    public AgModelConfigServices(IBaseRepository<AgModelConfig> dal, IRedisCacheServiceFactory redisFactory)
    {
        BaseDal = dal;
        Redis = redisFactory.Create(9);
    }

    #region 获取可用模型（ListAvailableProfilesAsync）
    /// <summary>查询可供 Agent 选择的公开模型标识，不读取密钥。</summary>
    /// <param name="cancellationToken">取消当前查询的令牌。</param>
    /// <returns>合法、启用且未删除的模型标识，按标识排序。</returns>
    public async Task<IReadOnlyList<string>> ListAvailableProfilesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var codes = await Query(x => x.ProfileCode, x => x.Enabled == true && x.IsDeleted == false, "ProfileCode asc");
        cancellationToken.ThrowIfCancellationRequested();
        return codes.Where(code => PublicModelProfileCatalog.AreValid(new[] { code }) && code == code.Trim())
            .Distinct(StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal).ToArray();
    }
    #endregion

    #region 解析模型调用配置（ResolveRuntimeProfileAsync）
    /// <summary>按公开标识读取当前配置；不回退到环境变量或其他模型。</summary>
    /// <param name="profileCode">Agent 使用的模型配置标识。</param>
    /// <param name="cancellationToken">取消当前查询的令牌。</param>
    /// <returns>仅供本次模型调用使用的配置，密钥不参与序列化。</returns>
    public async Task<AgentModelRuntimeProfile> ResolveRuntimeProfileAsync(string profileCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!PublicModelProfileCatalog.AreValid(new[] { profileCode }))
            throw new InvalidOperationException("模型配置标识无效。");
        var rows = await Query(x => x.ProfileCode == profileCode && x.Enabled == true && x.IsDeleted == false);
        cancellationToken.ThrowIfCancellationRequested();
        // 数据库排序规则可能忽略大小写；最终在内存中按公开引用契约精确匹配。
        var matches = rows.Where(x => string.Equals(x.ProfileCode, profileCode, StringComparison.Ordinal)).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("模型配置不存在、已禁用或标识重复。");
        var model = matches[0];
        var endpoint = ValidateConfiguration(model.ProfileCode, model.Provider, model.ModelName, model.Endpoint, model.TimeoutSeconds);
        string apiKey;
        try
        {
            var key = await Redis.GetAsync("ModelConfig", "EncryptionKey");
            apiKey = ModelConfigCredentialCipher.Unprotect(key, model.ID, model.ApiKeyCiphertext);
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException or InvalidOperationException)
        {
            throw new InvalidOperationException("模型凭据无法解密，请检查 Redis 中 ModelConfig 的 EncryptionKey 字段是否与保存密钥时一致。");
        }
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("模型凭据为空，请在模型配置中重新保存 API Key。");
        return new AgentModelRuntimeProfile
        {
            Endpoint = endpoint,
            ModelName = model.ModelName,
            ApiKey = apiKey,
            Timeout = TimeSpan.FromSeconds(model.TimeoutSeconds.Value),
            EnableThinking = model.EnableThinking
        };
    }
    #endregion


    #region 新增
    /// <summary>新增模型配置，先完成凭据加密，再一次性写入配置与密文。</summary>
    /// <param name="entity">标准动态表单输入，ApiKey 为待加密的明文凭据。</param>
    /// <returns>成功写入的模型配置主键。</returns>
    public override async Task<Guid> Add(object entity)
    {
        var model = ConvertToEntity(entity);

        var insert = JsonHelper.JsonToObj<InsertAgModelConfigInput>(entity.ObjToString());

        #region 检查是否存在相同值
        await CheckOnly(model);
        #endregion
        if (string.IsNullOrWhiteSpace(model.Provider)) model.Provider = "OpenAICompatible";
        ValidateConfiguration(model.ProfileCode, model.Provider, model.ModelName, model.Endpoint, model.TimeoutSeconds);
        model.CredentialRevision = 1;
        model.LogicalRevision = 1;
        var id = Guid.NewGuid();
        model.ID = id;
        var key = await Redis.GetAsync("ModelConfig", "EncryptionKey");
        model.ApiKeyCiphertext = ModelConfigCredentialCipher.Protect(key, id, insert.ApiKey!);
        return await base.Add(model, id);
    }
    #endregion

    #region 更新
    /// <summary>更新模型配置，以路由主键为准；未填写 API Key 时保留原凭据。</summary>
    /// <param name="Id">路由中指定的目标配置主键。</param>
    /// <param name="entity">标准动态表单输入，仅更新提交的业务字段。</param>
    /// <returns>目标存在且更新成功时返回 true，否则返回 false。</returns>
    public override async Task<bool> Update(Guid Id, object entity)
    {
        if (Id == Guid.Empty) return false;
        var current = await QuerySingle(x => x.ID == Id && x.IsDeleted == false);
        if (current is null) return false;
        var insert = JsonHelper.JsonToObj<InsertAgModelConfigInput>(entity.ObjToString());
        var model = ConvertToEntity(entity);
        model.ID = Id;
        var dic = ConvertToDic(entity);
        // 明文并非数据库列；密文和修订号必须由服务端生成，不能由请求覆盖。
        var protectedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ID", "ApiKey", nameof(AgModelConfig.ApiKeyCiphertext),
            nameof(AgModelConfig.CredentialRevision), nameof(AgModelConfig.LogicalRevision)
        };
        var lstColumns = dic.Keys.Where(x => !protectedColumns.Contains(x)).ToList();
        // 部分更新按提交字段覆盖当前值后验证，不能把未提交字段当成空值。
        bool Submitted(string field) => lstColumns.Contains(field, StringComparer.OrdinalIgnoreCase);
        // 仅停用允许隔离历史无效配置；不能借停用同时修改配置或轮换密钥。
        bool disableOnly = lstColumns.Count == 1 && Submitted(nameof(model.Enabled))
            && model.Enabled == false && string.IsNullOrWhiteSpace(insert.ApiKey);
        if (!disableOnly)
            ValidateConfiguration(
                Submitted(nameof(model.ProfileCode)) ? model.ProfileCode : current.ProfileCode,
                Submitted(nameof(model.Provider)) ? model.Provider : current.Provider,
                Submitted(nameof(model.ModelName)) ? model.ModelName : current.ModelName,
                Submitted(nameof(model.Endpoint)) ? model.Endpoint : current.Endpoint,
                Submitted(nameof(model.TimeoutSeconds)) ? model.TimeoutSeconds : current.TimeoutSeconds);
        if (!string.IsNullOrWhiteSpace(insert.ApiKey))
        {
            var key = await Redis.GetAsync("ModelConfig", "EncryptionKey");
            model.ApiKeyCiphertext = ModelConfigCredentialCipher.Protect(key, Id, insert.ApiKey!);
            model.CredentialRevision = checked((current.CredentialRevision ?? 0) + 1);
            lstColumns.Add(nameof(AgModelConfig.ApiKeyCiphertext));
            lstColumns.Add(nameof(AgModelConfig.CredentialRevision));
        }
        if (lstColumns.Count == 0) return false;
        model.LogicalRevision = checked((current.LogicalRevision ?? 0) + 1);
        lstColumns.Add(nameof(AgModelConfig.LogicalRevision));
        await CheckOnly(model, Id);
        var result = await Update(model, lstColumns);

        return result;
    }
    #endregion

    #region 校验模型配置（ValidateConfiguration）
    /// <summary>保存与运行共用配置校验，不包含密钥，不回显输入值。</summary>
    /// <param name="profileCode">公开模型配置标识。</param>
    /// <param name="provider">协议提供商。</param>
    /// <param name="modelName">供应商模型名。</param>
    /// <param name="endpoint">模型服务地址。</param>
    /// <param name="timeoutSeconds">请求超时秒数。</param>
    /// <returns>通过校验的 HTTPS 地址。</returns>
    private static Uri ValidateConfiguration(string profileCode, string provider, string modelName, string endpoint, int? timeoutSeconds)
    {
        if (!PublicModelProfileCatalog.AreValid(new[] { profileCode }) || profileCode != profileCode.Trim())
            throw new InvalidOperationException("模型配置标识无效。");
        if (provider != "OpenAICompatible") throw new InvalidOperationException("模型提供商必须是 OpenAICompatible。");
        if (string.IsNullOrWhiteSpace(modelName) || modelName.Length > 200 || modelName.Any(char.IsControl))
            throw new InvalidOperationException("模型名称不能为空、不能含控制字符，且长度不能超过 200。");
        if (timeoutSeconds is not (>= 5 and <= 600))
            throw new InvalidOperationException("模型超时必须为 5 至 600 秒。");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || uri.IsLoopback)
            throw new InvalidOperationException("模型地址必须是无凭据、无查询参数的 HTTPS 地址。");
        return uri;
    }
    #endregion
}
