using EU.Core.Model.Entity;
using Newtonsoft.Json.Linq;
using SqlSugar;

namespace EU.Core.Extensions.Weixin;

public class WeixinPayNotifyPersistenceService
{
    private static readonly object InitLock = new();
    private static bool _tableInitialized;

    private readonly ISqlSugarClient _db;

    public WeixinPayNotifyPersistenceService(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<WxPayNotifyLog> SaveAsync(string channel, TenPayNotifyProcessResult result, string rawBody)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException("支付通知未成功解密，不能落库");
        }

        EnsureTable();

        var resource = result.Resource ?? new JObject();
        var amount = resource["amount"] as JObject;
        var payer = resource["payer"] as JObject;
        var now = DateTime.Now;

        var existing = await _db.Queryable<WxPayNotifyLog>()
            .FirstAsync(x => x.IsDeleted == false && x.Channel == channel && x.NotifyId == result.NotifyId);

        if (existing == null)
        {
            existing = new WxPayNotifyLog
            {
                Channel = channel,
                NotifyId = result.NotifyId,
                EventType = result.EventType,
                OriginalType = result.OriginalType,
                Serial = result.Serial,
                OutTradeNo = resource.Value<string>("out_trade_no"),
                TransactionId = resource.Value<string>("transaction_id"),
                TradeState = resource.Value<string>("trade_state"),
                TradeStateDesc = resource.Value<string>("trade_state_desc"),
                TradeType = resource.Value<string>("trade_type"),
                AppId = resource.Value<string>("appid"),
                MchId = resource.Value<string>("mchid"),
                OpenId = payer?.Value<string>("openid"),
                AmountFen = amount?.Value<int?>("total"),
                PayerAmountFen = amount?.Value<int?>("payer_total"),
                SuccessTime = ParseDateTime(resource.Value<string>("success_time")),
                Attach = resource.Value<string>("attach"),
                NotifyCount = 1,
                FirstNotifyTime = now,
                LastNotifyTime = now,
                RawBody = rawBody,
                ResourceJson = resource.ToString(),
                Remark = result.Summary
            };

            await _db.Insertable(existing).ExecuteCommandAsync();
            return existing;
        }

        existing.EventType = result.EventType;
        existing.OriginalType = result.OriginalType;
        existing.Serial = result.Serial;
        existing.OutTradeNo = resource.Value<string>("out_trade_no");
        existing.TransactionId = resource.Value<string>("transaction_id");
        existing.TradeState = resource.Value<string>("trade_state");
        existing.TradeStateDesc = resource.Value<string>("trade_state_desc");
        existing.TradeType = resource.Value<string>("trade_type");
        existing.AppId = resource.Value<string>("appid");
        existing.MchId = resource.Value<string>("mchid");
        existing.OpenId = payer?.Value<string>("openid");
        existing.AmountFen = amount?.Value<int?>("total");
        existing.PayerAmountFen = amount?.Value<int?>("payer_total");
        existing.SuccessTime = ParseDateTime(resource.Value<string>("success_time"));
        existing.Attach = resource.Value<string>("attach");
        existing.NotifyCount = (existing.NotifyCount ?? 0) + 1;
        existing.FirstNotifyTime ??= now;
        existing.LastNotifyTime = now;
        existing.RawBody = rawBody;
        existing.ResourceJson = resource.ToString();
        existing.Remark = result.Summary;

        await _db.Updateable(existing).UpdateColumns(x => new
        {
            x.EventType,
            x.OriginalType,
            x.Serial,
            x.OutTradeNo,
            x.TransactionId,
            x.TradeState,
            x.TradeStateDesc,
            x.TradeType,
            x.AppId,
            x.MchId,
            x.OpenId,
            x.AmountFen,
            x.PayerAmountFen,
            x.SuccessTime,
            x.Attach,
            x.NotifyCount,
            x.FirstNotifyTime,
            x.LastNotifyTime,
            x.RawBody,
            x.ResourceJson,
            x.Remark
        }).ExecuteCommandAsync();

        return existing;
    }

    public async Task<WxPayNotifyLog> FindLatestAsync(string channel, string outTradeNo = null, string transactionId = null)
    {
        EnsureTable();

        var query = _db.Queryable<WxPayNotifyLog>()
            .Where(x => x.IsDeleted == false && x.Channel == channel);

        if (!string.IsNullOrWhiteSpace(outTradeNo))
        {
            query = query.Where(x => x.OutTradeNo == outTradeNo);
        }

        if (!string.IsNullOrWhiteSpace(transactionId))
        {
            query = query.Where(x => x.TransactionId == transactionId);
        }

        return await query.OrderByDescending(x => x.LastNotifyTime).FirstAsync();
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

            _db.CodeFirst.InitTables<WxPayNotifyLog>();
            _tableInitialized = true;
        }
    }

    private static DateTime? ParseDateTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTime.TryParse(value, out var time) ? time : null;
    }
}
