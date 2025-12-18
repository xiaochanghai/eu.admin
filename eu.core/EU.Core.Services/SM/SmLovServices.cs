/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmLov.cs
*
*功 能： N / A
* 类 名： SmLov
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/21 1:10:42  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using static EU.Core.Model.KeyValue;

namespace EU.Core.Services;

/// <summary>
/// SmLov (服务)
/// </summary>
public class SmLovServices : BaseServices<SmLov, SmLovDto, InsertSmLovInput, EditSmLovInput>, ISmLovServices
{
    private readonly IBaseRepository<SmLov> _dal;
    public SmLovServices(IBaseRepository<SmLov> dal)
    {
        this._dal = dal;
        base.BaseDal = dal;
    }

    public async Task<ServiceResult<IEnumerable<KeyValue>>> GetByCode(string code)
    {
        var list = await LovHelper.GetLovListAsync(code);
        var data = list.Select(x => new KeyValue() { key = x.Value, value = x.Text });

        return Success(data);
    }

    public async Task<ServiceResult<IEnumerable<LovData>>> QueryByCode(string code)
    {
        var list = await LovHelper.GetLovListAsync(code);
        var data = list.Select(x => new LovData() { value = code == "ColumnNo" ? x.Value.ObjToInt() : x.Value, label = x.Text });

        return Success(data);
    }
}