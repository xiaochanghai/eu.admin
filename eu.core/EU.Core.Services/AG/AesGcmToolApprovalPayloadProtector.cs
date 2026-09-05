#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EU.Core.IServices.Approvals;

namespace EU.Core.Services;

// 文件职责：AesGcmToolApprovalPayloadProtector 职责实现

/// <summary>
/// 使用 AES-GCM 保护工具审批载荷。
/// </summary>
public sealed partial class AesGcmToolApprovalPayloadProtector :
    IToolApprovalPayloadProtector,
    IDisposable
{
    #region 审批载荷保护

    /// <summary>AES-GCM 密钥要求的字节数。</summary>
    public const int KeySizeBytes = 32;
    /// <summary>允许加密的明文最大 UTF-8 字节数。</summary>
    public const int MaximumPlaintextUtf8Bytes =
        ToolApprovalStateMachine.MaximumResultPlaintextUtf8Bytes;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private const string Prefix = "enc:v1:";
    private readonly byte[] _key;
    private bool _disposed;

    #region 构造（AesGcmToolApprovalPayloadProtector）
    /// <summary>
    /// 构造（AesGcmToolApprovalPayloadProtector）
    /// </summary>
    /// <param name="key">当前操作使用的键值。</param>
    public AesGcmToolApprovalPayloadProtector(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != KeySizeBytes)
        {
            throw new ArgumentException(
                $"The tool approval payload key must be {KeySizeBytes} bytes.",
                nameof(key));
        }

        _key = key.ToArray();
    }
    #endregion

    #region 加密并绑定工具审批恢复载荷（Protect）
    /// <summary>
    /// 加密并绑定工具审批恢复载荷（Protect）。
    /// </summary>
    /// <param name="context">用于绑定密文的审批身份和执行上下文。</param>
    /// <param name="plaintext">需要加密保护的明文。</param>
    /// <returns>与给定审批上下文绑定的加密载荷字符串。</returns>
    public string Protect(ToolApprovalPayloadContext context, string plaintext)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        byte[] additionalData = ValidateAndBuildAdditionalData(context);
        byte[] plaintextBytes = StrictUtf8().GetBytes(plaintext ?? string.Empty);
        if (plaintextBytes.Length > MaximumPlaintextUtf8Bytes)
        {
            throw PayloadInvalid();
        }

        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[TagSizeBytes];
        try
        {
            using var aes = new AesGcm(_key, TagSizeBytes);
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, additionalData);
            byte[] envelope = new byte[nonce.Length + tag.Length + ciphertext.Length];
            nonce.CopyTo(envelope, 0);
            tag.CopyTo(envelope, nonce.Length);
            ciphertext.CopyTo(envelope, nonce.Length + tag.Length);
            return Prefix + Convert.ToBase64String(envelope);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(additionalData);
        }
    }
    #endregion

    #region 解密并验证工具审批恢复载荷（Unprotect）
    /// <summary>
    /// 解密并验证工具审批恢复载荷（Unprotect）。
    /// </summary>
    /// <param name="context">必须与加密时一致的审批身份和执行上下文。</param>
    /// <param name="protectedPayload">已加密保护的载荷。</param>
    /// <returns>通过上下文绑定和完整性校验后恢复的明文。</returns>
    public string Unprotect(ToolApprovalPayloadContext context, string protectedPayload)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        byte[] additionalData = ValidateAndBuildAdditionalData(context);
        byte[] envelope;
        try
        {
            if (string.IsNullOrWhiteSpace(protectedPayload)
                || !protectedPayload.StartsWith(Prefix, StringComparison.Ordinal))
            {
                throw PayloadInvalid();
            }

            envelope = Convert.FromBase64String(protectedPayload[Prefix.Length..]);
        }
        catch (FormatException)
        {
            throw PayloadInvalid();
        }

        if (envelope.Length < NonceSizeBytes + TagSizeBytes
            || envelope.Length > MaximumPlaintextUtf8Bytes + NonceSizeBytes + TagSizeBytes)
        {
            CryptographicOperations.ZeroMemory(envelope);
            CryptographicOperations.ZeroMemory(additionalData);
            throw PayloadInvalid();
        }

        byte[] plaintext = new byte[envelope.Length - NonceSizeBytes - TagSizeBytes];
        try
        {
            ReadOnlySpan<byte> nonce = envelope.AsSpan(0, NonceSizeBytes);
            ReadOnlySpan<byte> tag = envelope.AsSpan(NonceSizeBytes, TagSizeBytes);
            ReadOnlySpan<byte> ciphertext = envelope.AsSpan(
                NonceSizeBytes + TagSizeBytes);
            using var aes = new AesGcm(_key, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, additionalData);
            return StrictUtf8().GetString(plaintext);
        }
        catch (CryptographicException)
        {
            throw PayloadInvalid();
        }
        catch (DecoderFallbackException)
        {
            throw PayloadInvalid();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(envelope);
            CryptographicOperations.ZeroMemory(additionalData);
        }
    }
    #endregion

    #region 释放资源（Dispose）
    /// <summary>
    /// 释放资源（Dispose）
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_key);
        _disposed = true;
    }
    #endregion

    #region 校验（ValidateAndBuildAdditionalData）
    /// <summary>
    /// 校验（ValidateAndBuildAdditionalData）
    /// </summary>
    /// <param name="context">审批载荷认证上下文，包含审批标识、租户和参数摘要。</param>
    /// <returns>绑定审批标识、租户和参数摘要的 UTF-8 附加认证数据；上下文无效时抛出审批异常。</returns>
    private static byte[] ValidateAndBuildAdditionalData(ToolApprovalPayloadContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ApprovalId == Guid.Empty
            || string.IsNullOrWhiteSpace(context.TenantId)
            || context.TenantId.Length > 256
            || !Sha256Pattern().IsMatch(context.ArgumentsSha256))
        {
            throw PayloadInvalid();
        }

        return Encoding.UTF8.GetBytes(string.Join(
            '\n',
            "eu-core-agent-tool-approval-v1",
            context.ApprovalId.ToString("D"),
            context.TenantId,
            context.ArgumentsSha256));
    }
    #endregion

    #region 处理（StrictUtf8）
    /// <summary>
    /// 处理（StrictUtf8）
    /// </summary>
    /// <returns>不带 BOM 且在遇到无效 UTF-8 字节时抛出异常的编码器。</returns>
    private static UTF8Encoding StrictUtf8() =>
        new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);
    #endregion

    #region 处理（PayloadInvalid）
    /// <summary>
    /// 处理（PayloadInvalid）
    /// </summary>
    /// <returns>错误码为 PayloadInvalid 的审批载荷异常。</returns>
    private static ToolApprovalException PayloadInvalid() =>
        new(
            ToolApprovalErrorCodes.PayloadInvalid,
            "The protected tool approval payload is invalid.");
    #endregion

    #region 处理（Sha256Pattern）
    /// <summary>
    /// 处理（Sha256Pattern）
    /// </summary>
    /// <returns>用于匹配 SHA-256 摘要格式的正则表达式。</returns>
    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();
    #endregion

    #endregion
}
