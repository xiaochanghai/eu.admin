using MailKit.Security;
using MimeKit;

namespace EU.Core.Common.Helper;

/// <summary>
/// SMTP 邮件发送帮助类。
/// </summary>
public static class MailHelper
{
    private const int DefaultPort = 25;
    private const int DefaultTimeoutMilliseconds = 300000;

    public static void SendMail(string host, MailContent mailContent)
    {
        SendMail(host, DefaultPort, false, mailContent, null, null);
    }

    public static void SendMail(string host, int port, MailContent mailContent)
    {
        SendMail(host, port, false, mailContent, null, null);
    }

    public static void SendMail(
        string host,
        MailContent mailContent,
        string sender,
        string senderPassword)
    {
        SendMail(host, DefaultPort, false, mailContent, sender, senderPassword);
    }

    public static void SendMail(
        string host,
        int port,
        MailContent mailContent,
        string sender,
        string senderPassword)
    {
        SendMail(host, port, false, mailContent, sender, senderPassword);
    }

    public static void SendMail(
        string host,
        int port,
        bool enableSsl,
        MailContent mailContent,
        string sender,
        string senderPassword)
    {
        ValidateHost(host);
        using var message = CreateMimeMessage(mailContent);
        using var smtpClient = new MailKit.Net.Smtp.SmtpClient
        {
            Timeout = DefaultTimeoutMilliseconds
        };

        try
        {
            smtpClient.Connect(host, port, GetSocketOptions(port, enableSsl));
            if (!string.IsNullOrWhiteSpace(sender))
            {
                smtpClient.Authenticate(
                    sender,
                    (senderPassword ?? string.Empty).Replace("\0", string.Empty));
            }

            smtpClient.Send(message);
        }
        finally
        {
            if (smtpClient.IsConnected)
                smtpClient.Disconnect(true);
        }
    }

    public static async Task SendMailAsync(
        string host,
        int port,
        bool enableSsl,
        MailContent mailContent,
        string sender,
        string senderPassword,
        CancellationToken cancellationToken = default)
    {
        ValidateHost(host);
        using var message = CreateMimeMessage(mailContent);
        using var smtpClient = new MailKit.Net.Smtp.SmtpClient
        {
            Timeout = DefaultTimeoutMilliseconds
        };

        try
        {
            await smtpClient.ConnectAsync(
                host,
                port,
                GetSocketOptions(port, enableSsl),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(sender))
            {
                await smtpClient.AuthenticateAsync(
                    sender,
                    (senderPassword ?? string.Empty).Replace("\0", string.Empty),
                    cancellationToken);
            }

            await smtpClient.SendAsync(message, cancellationToken);
        }
        finally
        {
            if (smtpClient.IsConnected)
                await smtpClient.DisconnectAsync(true, cancellationToken);
        }
    }

    private static SecureSocketOptions GetSocketOptions(int port, bool enableSsl)
    {
        if (!enableSsl)
            return SecureSocketOptions.None;

        return port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }

    private static void ValidateHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("SMTP 服务器不能为空。", nameof(host));
    }

    private static MimeMessage CreateMimeMessage(MailContent mailContent)
    {
        ArgumentNullException.ThrowIfNull(mailContent);

        var message = new MimeMessage
        {
            Subject = mailContent.Subject
        };
        message.From.Add(ToMailboxAddress(mailContent.From));
        AddAddresses(message.To, mailContent.To);
        AddAddresses(message.Cc, mailContent.Cc);
        AddAddresses(message.Bcc, mailContent.Bcc);
        if (message.To.Count == 0 && message.Cc.Count == 0 && message.Bcc.Count == 0)
        {
            message.Dispose();
            throw new InvalidOperationException("邮件至少需要一个收件人、抄送人或密送人。");
        }

        var bodyBuilder = new BodyBuilder();
        if (mailContent.IsBodyHtml)
            bodyBuilder.HtmlBody = mailContent.Body ?? string.Empty;
        else
            bodyBuilder.TextBody = mailContent.Body ?? string.Empty;

        foreach (var mailAttachment in mailContent.MailAttachments ?? [])
        {
            if (mailAttachment.AttachmentType == MailAttachmentType.Path)
            {
                if (File.Exists(mailAttachment.FilePathAndName))
                    bodyBuilder.Attachments.Add(mailAttachment.FilePathAndName);
                continue;
            }

            if (!mailAttachment.IsInline)
            {
                bodyBuilder.Attachments.Add(
                    mailAttachment.FileName,
                    mailAttachment.ContentStream);
                continue;
            }

            if (string.IsNullOrWhiteSpace(mailAttachment.ContentId))
                throw new InvalidOperationException("内嵌附件必须设置 ContentId。");

            var linkedResource = bodyBuilder.LinkedResources.Add(
                mailAttachment.FileName,
                mailAttachment.ContentStream);
            linkedResource.ContentId = mailAttachment.ContentId;
        }

        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }

    private static MailboxAddress ToMailboxAddress(System.Net.Mail.MailAddress address)
    {
        return new MailboxAddress(address.DisplayName ?? string.Empty, address.Address);
    }

    private static void AddAddresses(
        InternetAddressList destination,
        IEnumerable<System.Net.Mail.MailAddress> addresses)
    {
        foreach (var address in addresses)
            destination.Add(ToMailboxAddress(address));
    }

#if false
    // 以下旧接口依赖 ChuangCai.DB.Sql、AM_MAIL_SENDER、SM_USER 和 PassWord，
    // 在 EU.Core 中没有对应依赖，因此按迁移要求保留但禁用。
    using ChuangCai.DB.Sql;

    public static void Send(string sender, string email, string title, string content)
    {
        // 原实现从 AM_MAIL_SENDER 查询 SMTP 配置后调用 SendMail。
    }

    public static void SendByUserId(string sender, string userId, string title, string content)
    {
        // 原实现通过 SM_USER 查询用户邮箱后调用 Send。
    }
#endif
}
