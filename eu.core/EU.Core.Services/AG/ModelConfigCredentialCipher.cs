using System.Security.Cryptography;
using System.Text;

namespace EU.Core.Services;

/// <summary>模型密钥专用 AES-GCM 信封，主密钥由部署配置提供，不持久化到业务表。</summary>
public static class ModelConfigCredentialCipher
{
    #region 加密
    /// <summary>以随机 nonce 加密 API Key，并绑定记录主键。</summary>
    /// <param name="base64Key">32 字节主密钥的 Base64 表示。</param>
    /// <param name="id">模型配置主键。</param>
    /// <param name="plaintext">不超过 4096 UTF-8 字节的 API Key。</param>
    /// <returns>包含版本号、nonce、认证标签的密文。</returns>
    public static string Protect(string base64Key, Guid id, string plaintext)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(plaintext) || Encoding.UTF8.GetByteCount(plaintext) > 4096)
            throw new ArgumentException("API Key 不能为空，且不能超过 4096 字节。");
        byte[] key = ReadKey(base64Key);
        byte[] plain = Encoding.UTF8.GetBytes(plaintext);
        try
        {
            byte[] data = new byte[28 + plain.Length];
            RandomNumberGenerator.Fill(data.AsSpan(0, 12));
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(data.AsSpan(0, 12), plain, data.AsSpan(28), data.AsSpan(12, 16), Encoding.UTF8.GetBytes("AgModelConfig:v1:" + id.ToString("D")));
            return "amc:v1:" + Convert.ToBase64String(data);
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain); }
    }
    #endregion

    #region 解密
    /// <summary>验证记录绑定和密文完整性；不提供公开解密 API。</summary>
    /// <param name="base64Key">部署主密钥。</param>
    /// <param name="id">模型配置主键。</param>
    /// <param name="ciphertext">服务端保存的版本化密文。</param>
    /// <returns>验证成功后的 API Key。</returns>
    public static string Unprotect(string base64Key, Guid id, string ciphertext)
    {
        if (id == Guid.Empty || ciphertext is null || !ciphertext.StartsWith("amc:v1:", StringComparison.Ordinal) || ciphertext.Length > 5600)
            throw new CryptographicException("模型密钥密文无效。");
        byte[] key = ReadKey(base64Key);
        byte[] plain = Array.Empty<byte>();
        try
        {
            byte[] data = Convert.FromBase64String(ciphertext[7..]);
            if (data.Length <= 28 || data.Length > 4124) throw new CryptographicException("模型密钥密文无效。");
            plain = new byte[data.Length - 28];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(data.AsSpan(0, 12), data.AsSpan(28), data.AsSpan(12, 16), plain, Encoding.UTF8.GetBytes("AgModelConfig:v1:" + id.ToString("D")));
            return Encoding.UTF8.GetString(plain);
        }
        finally { CryptographicOperations.ZeroMemory(key); CryptographicOperations.ZeroMemory(plain); }
    }
    #endregion

    #region 读取部署主密钥
    /// <summary>校验主密钥格式，失败时不输出配置内容。</summary>
    private static byte[] ReadKey(string value)
    {
        byte[] key;
        try { key = Convert.FromBase64String(value ?? ""); }
        catch (FormatException) { throw new InvalidOperationException("请在 Redis 中配置 ModelConfig 的 EncryptionKey 字段（32 字节主密钥的 Base64 表示）。"); }
        if (key.Length == 32) return key;
        CryptographicOperations.ZeroMemory(key);
        throw new InvalidOperationException("请在 Redis 中配置 ModelConfig 的 EncryptionKey 字段（32 字节主密钥的 Base64 表示）。");
    }
    #endregion
}
