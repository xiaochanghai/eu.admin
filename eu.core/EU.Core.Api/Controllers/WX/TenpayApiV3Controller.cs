using EU.Core.Extensions.Weixin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EU.Core.Api.Controllers;

[ApiController]
[Route("[controller]")]
[ApiExplorerSettings(GroupName = Grouping.GroupName_WX)]
public class TenpayApiV3Controller : ControllerBase
{
    private readonly TenPayV3NotifyService _notifyService;
    private readonly WeixinPayNotifyPersistenceService _notifyPersistenceService;
    private readonly ILogger<TenpayApiV3Controller> _logger;

    public TenpayApiV3Controller(
        TenPayV3NotifyService notifyService,
        WeixinPayNotifyPersistenceService notifyPersistenceService,
        ILogger<TenpayApiV3Controller> logger)
    {
        _notifyService = notifyService;
        _notifyPersistenceService = notifyPersistenceService;
        _logger = logger;
    }

    [HttpPost("PayNotifyUrl")]
    [AllowAnonymous]
    public async Task<IActionResult> PayNotifyUrl()
    {
        return await HandleNotifyAsync("mp");
    }

    [HttpPost("PayNotifyUrlWxOpen")]
    [AllowAnonymous]
    public async Task<IActionResult> PayNotifyUrlWxOpen()
    {
        return await HandleNotifyAsync("wxopen");
    }

    [HttpGet("NotifyLog")]
    [Authorize]
    public async Task<IActionResult> NotifyLog([FromQuery] string channel, [FromQuery] string outTradeNo, [FromQuery] string transactionId)
    {
        if (string.IsNullOrWhiteSpace(channel) ||
            (string.IsNullOrWhiteSpace(outTradeNo) && string.IsNullOrWhiteSpace(transactionId)))
        {
            return BadRequest(new
            {
                message = "channel 必填，outTradeNo 和 transactionId 至少传一个"
            });
        }

        var log = await _notifyPersistenceService.FindLatestAsync(channel, outTradeNo, transactionId);
        if (log == null)
        {
            return NotFound(new
            {
                message = "未找到支付通知记录"
            });
        }

        return Ok(log);
    }

    private async Task<IActionResult> HandleNotifyAsync(string channel)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        var result = _notifyService.ProcessNotification(
            body,
            Request.Headers["Wechatpay-Timestamp"].ToString(),
            Request.Headers["Wechatpay-Nonce"].ToString(),
            Request.Headers["Wechatpay-Signature"].ToString(),
            Request.Headers["Wechatpay-Serial"].ToString());

        if (!result.Success)
        {
            _logger.LogWarning(
                "TenPayV3 notify failed. channel={Channel}, message={Message}, body={Body}",
                channel,
                result.Message,
                body);

            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                code = "FAIL",
                message = result.Message
            });
        }

        var saved = await _notifyPersistenceService.SaveAsync(channel, result, body);

        _logger.LogInformation(
            "TenPayV3 notify success. channel={Channel}, notifyId={NotifyId}, outTradeNo={OutTradeNo}, transactionId={TransactionId}, tradeState={TradeState}, notifyCount={NotifyCount}, eventType={EventType}, summary={Summary}, originalType={OriginalType}, serial={Serial}",
            channel,
            saved.NotifyId,
            saved.OutTradeNo,
            saved.TransactionId,
            saved.TradeState,
            saved.NotifyCount,
            result.EventType,
            result.Summary,
            result.OriginalType,
            result.Serial);

        return Ok(new
        {
            code = "SUCCESS",
            message = "成功"
        });
    }
}
