using System.Net.Mail;
using System.Text.Json;

namespace EU.Core.Common.Helper;

public class MailContent
{
    private readonly string _from;
    private readonly string _to;
    private readonly string _cc;
    private readonly string _bcc;

    public MailContent(string from, string to, string subject, string body)
        : this(from, to, string.Empty, string.Empty, subject, body, null)
    {
    }

    public MailContent(string from, string to, string cc, string bcc, string subject, string body)
        : this(from, to, cc, bcc, subject, body, null)
    {
    }

    public MailContent(
        string from,
        string to,
        string subject,
        string body,
        MailAttachment[] mailAttachments)
        : this(from, to, string.Empty, string.Empty, subject, body, mailAttachments)
    {
    }

    public MailContent(
        string from,
        string to,
        string cc,
        string bcc,
        string subject,
        string body,
        MailAttachment[] mailAttachments)
    {
        _from = from;
        _to = to;
        _cc = cc;
        _bcc = bcc;
        Subject = string.IsNullOrEmpty(subject) ? "No Subject" : subject;
        Body = body;
        MailAttachments = mailAttachments;
    }

    public MailAddress From => new(_from);

    public MailAddress[] To => ParseAddresses(_to);

    public MailAddress[] Cc => ParseAddresses(_cc);

    public MailAddress[] Bcc => ParseAddresses(_bcc);

    public string Subject { get; }

    public string Body { get; }

    public MailAttachment[] MailAttachments { get; }

    public bool IsBodyHtml { get; set; }

    private static MailAddress[] ParseAddresses(string addresses)
    {
        if (string.IsNullOrWhiteSpace(addresses))
            return [];

        var values = ParseAddressValues(addresses);
        return values
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(address => new MailAddress(address))
            .ToArray();
    }

    private static string ParseAddressValues(string addresses)
    {
        var value = addresses.Trim();
        if (!value.StartsWith('['))
            return value;

        try
        {
            var jsonAddresses = JsonSerializer.Deserialize<string[]>(value) ?? [];
            return string.Join(';', jsonAddresses.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        catch (JsonException ex)
        {
            throw new FormatException("邮件收件人 JSON 格式不正确。", ex);
        }
    }
}
