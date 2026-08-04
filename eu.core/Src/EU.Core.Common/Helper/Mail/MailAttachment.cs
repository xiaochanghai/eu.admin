namespace EU.Core.Common.Helper;

public class MailAttachment
{
    public MailAttachment(Stream contentStream, string fileName)
    {
        ContentStream = contentStream ?? throw new ArgumentNullException(nameof(contentStream));
        FileName = fileName;
        AttachmentType = MailAttachmentType.Stream;
    }

    public MailAttachment(string filePathAndName)
    {
        FilePathAndName = filePathAndName;
        FileName = Path.GetFileName(filePathAndName);
        FilePath = Path.GetDirectoryName(filePathAndName);
        AttachmentType = MailAttachmentType.Path;
    }

    public MailAttachment(string filePath, string fileName)
    {
        FilePath = filePath;
        FileName = fileName;
        FilePathAndName = Path.Combine(filePath, fileName);
        AttachmentType = MailAttachmentType.Path;
    }

    public string FilePath { get; }

    public string FileName { get; }

    public string FilePathAndName { get; }

    public Stream ContentStream { get; }

    public MailAttachmentType AttachmentType { get; }

    /// <summary>
    /// 是否为内嵌资源。流附件设为 true 时，需要同时设置 ContentId。
    /// </summary>
    public bool IsInline { get; set; }

    public string ContentId { get; set; }
}
