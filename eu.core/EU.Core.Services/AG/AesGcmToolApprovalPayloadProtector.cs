#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EU.Core.IServices.Approvals;

namespace EU.Core.Services;

#region 文件职责：AesGcmToolApprovalPayloadProtector 职责实现

public sealed partial class AesGcmToolApprovalPayloadProtector :
    IToolApprovalPayloadProtector,
    IDisposable
{
    #region 审批载荷保护

    public const int KeySizeBytes = 32;
    public const int MaximumPlaintextUtf8Bytes =
        ToolApprovalStateMachine.MaximumResultPlaintextUtf8Bytes;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private const string Prefix = "enc:v1:";
    private readonly byte[] _key;
    private bool _disposed;

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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CryptographicOperations.ZeroMemory(_key);
        _disposed = true;
    }

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

    private static UTF8Encoding StrictUtf8() =>
        new(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    private static ToolApprovalException PayloadInvalid() =>
        new(
            ToolApprovalErrorCodes.PayloadInvalid,
            "The protected tool approval payload is invalid.");

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();

    #endregion
}

#endregion
