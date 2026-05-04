using EU.Core.Common;
using EU.Core.Model.Entity;
using SqlSugar;

namespace EU.Core.Extensions.Weixin;

public class WeixinUserBindingService
{
    private static readonly object InitLock = new();
    private static bool _tableInitialized;

    private readonly ISqlSugarClient _db;

    public WeixinUserBindingService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<WxUserBinding> FindBindingAsync(string channel, string weixinId, string openId, string unionId)
    {
        EnsureTable();

        var query = _db.Queryable<WxUserBinding>()
            .Where(x => x.IsDeleted == false && x.Channel == channel && x.WeixinId == weixinId);

        if (!string.IsNullOrWhiteSpace(unionId))
        {
            var byUnionId = await _db.Queryable<WxUserBinding>()
                .FirstAsync(x => x.IsDeleted == false && x.Channel == channel && x.WeixinId == weixinId && x.UnionId == unionId);
            if (byUnionId != null)
            {
                return byUnionId;
            }
        }

        if (string.IsNullOrWhiteSpace(openId))
        {
            return null;
        }

        return await query.FirstAsync(x => x.OpenId == openId);
    }

    public async Task<WxUserBinding> BindCurrentUserAsync(
        string channel,
        string weixinId,
        string appId,
        string openId,
        string unionId,
        string workUserId = null,
        string remark = null)
    {
        EnsureTable();

        var userId = App.User?.ID;
        if (userId == null || userId == Guid.Empty)
        {
            throw new InvalidOperationException("当前用户未登录，无法完成绑定");
        }

        var binding = await FindBindingAsync(channel, weixinId, openId, unionId);
        if (binding == null)
        {
            binding = new WxUserBinding
            {
                Channel = channel,
                WeixinId = weixinId,
                AppId = appId,
                OpenId = openId,
                UnionId = unionId,
                WorkUserId = workUserId,
                UserId = userId,
                BindTime = DateTime.Now,
                LastLoginTime = DateTime.Now,
                Remark = remark
            };

            await _db.Insertable(binding).ExecuteCommandAsync();
            return binding;
        }

        binding.AppId = appId;
        binding.OpenId = openId;
        binding.UnionId = unionId;
        binding.WorkUserId = workUserId;
        binding.UserId = userId;
        binding.BindTime ??= DateTime.Now;
        binding.LastLoginTime = DateTime.Now;
        if (!string.IsNullOrWhiteSpace(remark))
        {
            binding.Remark = remark;
        }

        await _db.Updateable(binding).UpdateColumns(x => new
        {
            x.AppId,
            x.OpenId,
            x.UnionId,
            x.WorkUserId,
            x.UserId,
            x.BindTime,
            x.LastLoginTime,
            x.Remark
        }).ExecuteCommandAsync();

        return binding;
    }

    public async Task TouchLoginAsync(Guid? bindingId)
    {
        if (bindingId == null || bindingId == Guid.Empty)
        {
            return;
        }

        EnsureTable();
        await _db.Updateable<WxUserBinding>()
            .SetColumns(x => x.LastLoginTime == DateTime.Now)
            .Where(x => x.ID == bindingId)
            .ExecuteCommandAsync();
    }

    private void EnsureTable()
    {
        if (_tableInitialized)
        {
            return;
        }

        lock (InitLock)
        {
            if (_tableInitialized)
            {
                return;
            }

            _db.CodeFirst.InitTables<WxUserBinding>();
            _tableInitialized = true;
        }
    }
}
