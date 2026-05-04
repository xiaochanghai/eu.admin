using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Senparc.Weixin.Entities;

namespace EU.Core.Extensions.Weixin;

public sealed class TenPayV3NotifyService
{
    private readonly SenparcWeixinSetting _setting;
    private readonly string _tenPayPubKey;

    public TenPayV3NotifyService(IOptions<SenparcWeixinSetting> setting, IConfiguration configuration)
    {
        _setting = setting.Value;
        _tenPayPubKey = configuration["SenparcWeixinSetting:TenPayV3_TenPayPubKey"] ?? string.Empty;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_setting.TenPayV3_APIv3Key) &&
        !string.IsNullOrWhiteSpace(_tenPayPubKey);

    public TenPayNotifyProcessResult ProcessNotification(
        string body,
        string timestamp,
        string nonce,
        string signature,
        string serial)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return TenPayNotifyProcessResult.CreateFail("回调 Body 不能为空");
        }

        if (!IsConfigured)
        {
            return TenPayNotifyProcessResult.CreateFail("TenPayV3_TenPayPubKey 或 TenPayV3_APIv3Key 未配置");
        }

        TenPayNotifyEnvelope envelope;
        try
        {
            envelope = JObject.Parse(body).ToObject<TenPayNotifyEnvelope>();
        }
        catch (Exception ex)
        {
            return TenPayNotifyProcessResult.CreateFail($"回调 JSON 解析失败：{ex.Message}");
        }

        if (envelope?.Resource == null)
        {
            return TenPayNotifyProcessResult.CreateFail("回调缺少 resource 节点");
        }

        if (!VerifySignature(body, timestamp, nonce, signature))
        {
            return TenPayNotifyProcessResult.CreateFail("微信支付回调签名校验失败");
        }

        try
        {
            var plaintext = DecryptResource(envelope.Resource);
            var decrypted = string.IsNullOrWhiteSpace(plaintext) ? new JObject() : JObject.Parse(plaintext);

            return TenPayNotifyProcessResult.CreateSuccess(
                envelope.Id,
                envelope.EventType,
                envelope.Summary,
                envelope.Resource.OriginalType,
                serial,
                decrypted);
        }
        catch (Exception ex)
        {
            return TenPayNotifyProcessResult.CreateFail($"微信支付回调解密失败：{ex.Message}");
        }
    }

    private bool VerifySignature(string body, string timestamp, string nonce, string signature)
    {
        if (string.IsNullOrWhiteSpace(timestamp) ||
            string.IsNullOrWhiteSpace(nonce) ||
            string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var message = $"{timestamp}\n{nonce}\n{body}\n";
        var data = Encoding.UTF8.GetBytes(message);
        var signatureBytes = Convert.FromBase64String(signature);

        using var rsa = RSA.Create();
        ImportPublicKey(rsa, _tenPayPubKey);
        return rsa.VerifyData(data, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    private static void ImportPublicKey(RSA rsa, string publicKey)
    {
        var normalized = (publicKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("TenPayV3_TenPayPubKey 未配置");
        }

        if (normalized.Contains("BEGIN PUBLIC KEY", StringComparison.OrdinalIgnoreCase))
        {
            rsa.ImportFromPem(normalized);
            return;
        }

        if (normalized.Contains("BEGIN CERTIFICATE", StringComparison.OrdinalIgnoreCase))
        {
            using var certificate = X509Certificate2.CreateFromPem(normalized);
            using var publicRsa = certificate.GetRSAPublicKey()
                ?? throw new InvalidOperationException("证书中未包含 RSA 公钥");
            rsa.ImportParameters(publicRsa.ExportParameters(false));
            return;
        }

        var pem = new StringBuilder()
            .AppendLine("-----BEGIN PUBLIC KEY-----")
            .AppendLine(normalized)
            .AppendLine("-----END PUBLIC KEY-----")
            .ToString();

        rsa.ImportFromPem(pem);
    }

    private string DecryptResource(TenPayNotifyResource resource)
    {
        var key = Encoding.UTF8.GetBytes(_setting.TenPayV3_APIv3Key);
        var nonce = Encoding.UTF8.GetBytes(resource.Nonce ?? string.Empty);
        var associatedData = Encoding.UTF8.GetBytes(resource.AssociatedData ?? string.Empty);
        var cipherBytes = Convert.FromBase64String(resource.Ciphertext ?? string.Empty);

        const int tagLength = 16;
        if (cipherBytes.Length <= tagLength)
        {
            throw new InvalidOperationException("ciphertext 长度无效");
        }

        var ciphertext = cipherBytes[..^tagLength];
        var tag = cipherBytes[^tagLength..];
        var plaintext = new byte[ciphertext.Length];

        using var aesGcm = new AesGcm(key, tagLength);
        aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        return Encoding.UTF8.GetString(plaintext);
    }
}

public sealed class TenPayNotifyProcessResult
{
    public bool Success { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public string? NotifyId { get; private set; }

    public string? EventType { get; private set; }

    public string? Summary { get; private set; }

    public string? OriginalType { get; private set; }

    public string? Serial { get; private set; }

    public JObject? Resource { get; private set; }

    public static TenPayNotifyProcessResult CreateFail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };

    public static TenPayNotifyProcessResult CreateSuccess(
        string? notifyId,
        string? eventType,
        string? summary,
        string? originalType,
        string? serial,
        JObject resource) =>
        new()
        {
            Success = true,
            Message = "成功",
            NotifyId = notifyId,
            EventType = eventType,
            Summary = summary,
            OriginalType = originalType,
            Serial = serial,
            Resource = resource
        };
}

public sealed class TenPayNotifyEnvelope
{
    public string? Id { get; set; }

    public string? EventType { get; set; }

    public string? Summary { get; set; }

    public string? CreateTime { get; set; }

    public TenPayNotifyResource? Resource { get; set; }
}

public sealed class TenPayNotifyResource
{
    public string? OriginalType { get; set; }

    public string? Ciphertext { get; set; }

    public string? AssociatedData { get; set; }

    public string? Nonce { get; set; }
}
