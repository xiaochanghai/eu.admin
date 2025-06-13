/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* RmReume.cs
*
* 功 能： N / A
* 类 名： RmReume
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
* V1.0  2025/6/12 17:43:59  SahHsiao   初版
*
* Copyright(c) 2025 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System.Text.RegularExpressions; 

namespace EU.Core.Services;

/// <summary>
/// 简历 (服务)
/// </summary>
public class RmReumeServices : BaseServices<RmReume, RmReumeDto, InsertRmReumeInput, EditRmReumeInput>, IRmReumeServices
{
    public RmReumeServices(IBaseRepository<RmReume> dal)
    {
        BaseDal = dal;
    }

    public async Task ReadPdfAttachmentsAsync()
    {

        var emails = await Db.Queryable<RmEmail>().ToListAsync();

        for (int m = 0; m < emails.Count; m++)
        {
            var imap = emails[m].ImapHost;
            var port = emails[m].ImapPort;
            var userName = emails[m].UserName;
            var password = emails[m].Password;

            using (var client = new ImapClient())
            {

                //// 连接到邮件服务器
                await client.ConnectAsync(imap, port.Value, true);
                await client.AuthenticateAsync(userName, password);

                if(imap== "imap.163.com")
                    client.Identify(new ImapImplementation { Name = "MailKit", Version = "1.0.0" });
                // 打开收件箱
                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly);

                // 搜索最近30天的邮件
                var startDate = DateTime.Now.AddDays(-30);
                var query = SearchQuery.DeliveredAfter(startDate);
                var uids = await inbox.SearchAsync(query);

                for (int i = 0; i < uids.Count; i++)
                {
                    var uid = uids[i];
                    var message = await inbox.GetMessageAsync(uid);
                    Console.WriteLine($"处理邮件: {message.Subject}");

                    if (await Db.Queryable<RmReume>().Where(x => x.Uid == uid.ObjToString()).AnyAsync())
                    {
                        Common.LogHelper.Logger.WriteLog($"邮件: {message.Subject}，已同步进系统");
                        continue;
                    }

                    if (message.Subject.IndexOf("BOSS直聘") < 0)
                        continue;
                    message.Subject = message.Subject.Replace("转发: ", "");
                    message.Subject = message.Subject.Replace("【BOSS直聘】", "");

                    Common.LogHelper.Logger.WriteLog($"邮件: {message.Subject}，开始同步");


                    var array = message.Subject.Replace("转发: ", "").Split('|');

                    string pattern = @"(?<name>[\u4e00-\u9fa5]+)\s*\|\s*(?<experience>[^，]+)，应聘\s+(?<position>[^|]+)\s*\|\s*(?<location>[\u4e00-\u9fa5]+)(?<salary>\d+-\d+K)";

                    Regex regex = new Regex(pattern);
                    Match match = regex.Match(message.Subject);

                    for (int j = 0; j < message.Attachments.ToList().Count; j++)
                    {
                        var attachment = message.Attachments.ToList()[j];

                        if (attachment is MimePart mimePart &&
                           mimePart.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                // 将附件内容保存到内存流
                                await mimePart.Content.DecodeToAsync(memoryStream);
                                memoryStream.Position = 0;

                                // 读取PDF内容
                                var pdfText = ExtractTextFromPdf(message.Subject, memoryStream);
                                //Console.WriteLine($"PDF内容: {pdfText}");
                                if (pdfText.IsNullOrEmpty())
                                    continue;
                                var info = ExtractResumeInfo(pdfText);

                                //Console.WriteLine("\n提取的信息：");
                                //Console.WriteLine($"姓名: {info.Name ?? array[0]}");
                                //Console.WriteLine($"电话: {info.Phone}");
                                //Console.WriteLine($"邮箱: {info.Email}");
                                //Console.WriteLine($"年龄: {info.Age}");
                                //Console.WriteLine($"学历: {info.Education}");
                                //Console.WriteLine($"工作经历: {info.WorkExperience}");
                                //Console.WriteLine($"教育经历: {info.EducationBackground}");

                                var resume = new RmReume()
                                {
                                    Uid = uid.ObjToString(),
                                    StaffName = array[0],
                                    Phone = info.Phone,
                                    Email = info.Email,
                                    Age = info.Age,
                                    EmailSubject = message.Subject,
                                    FromEmail = userName,
                                    Experience = match.Groups["experience"].Value,
                                    Distinct = match.Groups["location"].Value,
                                    Position = match.Groups["position"].Value,
                                    Salary = match.Groups["salary"].Value
                                };
                                await Db.Insertable(resume).ExecuteCommandAsync();
                            }
                        }
                    }
                    Common.LogHelper.Logger.WriteLog($"邮件: {message.Subject}，完成同步");

                }

                await client.DisconnectAsync(true);
            }
        }
    }

    private string ExtractTextFromPdf(string pdfText, Stream pdfStream)
    {
        try
        {
            var text = new StringBuilder();
            using (var pdfReader = new PdfReader(pdfStream))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var strategy = new LocationTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    text.AppendLine(pageText);
                }
            }
            return text.ToString();
        }
        catch (Exception E)
        {
            return "";
        }

    }

    static ResumeInfo ExtractResumeInfo(string text)
    {
        var info = new ResumeInfo();

        // 姓名（简单匹配中文名）
        var nameMatch = Regex.Match(text, @"[\u4e00-\u9fa5]{2,4}");
        if (nameMatch.Success) info.Name = nameMatch.Value;

        // 手机号
        var text1 = Regex.Replace(text, @"\s+", "");
        var phoneMatch = Regex.Match(text1, @"1[3-9]\d{9}");
        if (phoneMatch.Success) info.Phone = phoneMatch.Value;

        if (info.Phone.IsNullOrEmpty())
        {

        }

        // 邮箱
        var emailMatch = Regex.Match(text, @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase);
        if (emailMatch.Success) info.Email = emailMatch.Value;

        // 年龄
        var ageMatch = Regex.Match(text, @"\b\d{1,2}(?=岁)");
        if (ageMatch.Success) info.Age = ageMatch.Value;

        // 学历
        var educationMatch = Regex.Match(text, @"(本科|硕士|博士|学士|大专|高中)", RegexOptions.IgnoreCase);
        if (educationMatch.Success) info.Education = educationMatch.Value;

        // 工作经历（关键词后的内容，可优化为段落识别）
        var workExpMatch = Regex.Match(text, @"工作经历.*?(?=\n\S|\Z)", RegexOptions.Singleline);
        if (workExpMatch.Success) info.WorkExperience = workExpMatch.Value.Trim();

        // 教育背景
        var eduMatch = Regex.Match(text, @"教育背景.*?(?=\n\S|\Z)", RegexOptions.Singleline);
        if (eduMatch.Success) info.EducationBackground = eduMatch.Value.Trim();

        return info;
    }

    static string ExtractValue(string text, string fieldName, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value.Trim();
        }
        return null;
    }

    static string ExtractSection(string text, string sectionHeader)
    {
        var match = Regex.Match(text, $@"({sectionHeader})[\s\S]*?(?=\n\\S|$)", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.Trim() : null;
    }
}

public class ResumeInfo()
{
    public string Name { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Age { get; set; }
    public string Education { get; set; }
    public string WorkExperience { get; set; }
    public string EducationBackground { get; set; }
}