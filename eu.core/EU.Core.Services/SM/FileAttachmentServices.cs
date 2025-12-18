/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* FileAttachment.cs
*
*功 能： N / A
* 类 名： FileAttachment
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/24 22:53:42  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace EU.Core.Services;

/// <summary>
/// FileAttachment (服务)
/// </summary>
public class FileAttachmentServices : BaseServices<FileAttachment, FileAttachmentDto, InsertFileAttachmentInput, EditFileAttachmentInput>, IFileAttachmentServices
{
    private readonly IBaseRepository<FileAttachment> _dal;
    /// <summary>
    /// 配置信息
    /// </summary>
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _hostingEnvironment;
    public FileAttachmentServices(IBaseRepository<FileAttachment> dal, IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
    {
        this._dal = dal;
        base.BaseDal = dal;
        _configuration = configuration;
        _hostingEnvironment = hostingEnvironment;
    }

    public async Task<ServiceResult<Guid>> UploadAsync(UploadForm upload)
    {
        var file = upload.file;
        var filePath = upload.filePath;
        filePath = !string.IsNullOrEmpty(filePath) ? filePath : _configuration["FileUploadOptions:UploadDir"];
        string ImageType = filePath;

        var ext = string.Empty;
        if (file.FileName.IsNotEmptyOrNull())
        {
            var dotPos = file.FileName.LastIndexOf('.');
            ext = file.FileName.Substring(dotPos + 1);
        }
        filePath = $"/{filePath}/";

        FileHelper.CreateRootDirectory(filePath);

        string fileName = $"{Utility.SnowID()}.{ext}";
        var filepath = Path.Combine(filePath, fileName);
        //var filepath = Path.Combine(pathHeader, file.FileName);
        using (var stream = File.Create(FileHelper.GetPhysicsPath() + filepath))
        {
            await file.CopyToAsync(stream);
        }

        InsertFileAttachmentInput fileAttachment = new();
        fileAttachment.OriginalFileName = file.FileName;
        fileAttachment.FileName = fileName;
        fileAttachment.FileExt = ext;
        fileAttachment.MasterId = upload.masterId;
        fileAttachment.Length = file.Length;
        fileAttachment.Path = filePath;
        fileAttachment.ImageType = ImageType;
        var id = await base.Add(fileAttachment);

        return Success(id);
    }

    public async Task<ServiceResult<Guid>> UploadImageAsync(UploadForm upload)
    {
        string filePath = upload.filePath;
        filePath = !string.IsNullOrEmpty(filePath) ? filePath : _configuration["FileUploadOptions:UploadDir"];
        var ext = string.Empty;
        var file = upload.file;
        if (file.FileName.IsNotEmptyOrNull())
        {
            var dotPos = file.FileName.LastIndexOf('.');
            ext = file.FileName.Substring(dotPos + 1);
        }
        string pathHeader = "wwwroot/" + filePath;
        FileHelper.CreateDirectory(pathHeader);

        var fileName = $"{Utility.SnowID()}.{ext}";
        var filepath = Path.Combine(pathHeader, fileName);
        //var filepath = Path.Combine(pathHeader, file.FileName);
        using (var stream = File.Create(filepath))
        {
            await file.CopyToAsync(stream);
        }

        if (upload.isUnique)
            await Db.Updateable<FileAttachment>()
                .SetColumns(it => new FileAttachment() { IsDeleted = true }, true)
                .Where(x => x.MasterId == upload.masterId && x.ImageType == upload.imageType)
                .ExecuteCommandAsync();

        InsertFileAttachmentInput fileAttachment = new();
        fileAttachment.OriginalFileName = file.FileName;
        fileAttachment.FileName = fileName;
        fileAttachment.FileExt = ext;
        fileAttachment.MasterId = upload.masterId;
        fileAttachment.Length = file.Length;
        fileAttachment.Path = filePath;
        fileAttachment.ImageType = upload.imageType ?? filePath;
        var id = await base.Add(fileAttachment);

        if (upload.masterTable.IsNotEmptyOrNull() && upload.masterColumn.IsNotEmptyOrNull())
        {
            var dt = new Dictionary<string, object>
            {
                { "ID", upload.masterId },
                { "UpdateBy", Utility.GetUserId() },
                { "UpdateTime", Utility.GetSysDate() }
            };
            if (upload.masterColumn == "ImageUrl")
                dt.Add(upload.masterColumn, fileName);
            else
                dt.Add(upload.masterColumn, id);
            await Db.Updateable(dt).AS(upload.masterTable)
                .WhereColumns("ID").ExecuteCommandAsync();
        }

        return Success(id, "上传成功！");
    }

    public async Task<ServiceResult<string>> UploadVideoAsync(ChunkUpload upload)
    {
        var path = $"{$"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}files{Path.DirectorySeparatorChar}upload{Path.DirectorySeparatorChar}{upload.id}{Path.DirectorySeparatorChar}"}";

        FileHelper.CreateDirectory(path);

        using (var stream = File.Create(path + $"{upload.chunkIndex}"))
        {
            await upload.file.CopyToAsync(stream);
        }

        if (upload.chunkIndex == upload.totalChunks - 1)
        {
            var ext = string.Empty;
            var file = upload.file;
            if (file.FileName.IsNotEmptyOrNull())
            {
                var dotPos = upload.fileName.LastIndexOf('.');
                ext = upload.fileName.Substring(dotPos + 1);
            }
            string id = Utility.GetSysID();
            await VideoHelper.FileMerge(upload.id, "." + ext, id);

            InsertFileAttachmentInput fileAttachment = new();
            fileAttachment.OriginalFileName = file.FileName;
            fileAttachment.FileName = upload.fileName;
            fileAttachment.FileExt = ext;
            //fileAttachment.MasterId = upload.masterId;
            fileAttachment.Length = file.Length;
            fileAttachment.Path = $"/files/upload/{id}.{ext}";
            await base.Add(fileAttachment);

            return Success<string>(null, "上传成功！");
        }
        return Success<string>(null, "上传成功！");
    }

    public async Task<ServiceResult<List<FileAttachment>>> GetFileListAsync(Guid masterId, string imageType = null)
    {
        var data = await Db.Queryable<FileAttachment>()
            .WhereIF(!string.IsNullOrEmpty(imageType), o => o.ImageType == imageType)
            .Where(x => x.MasterId == masterId).OrderByDescending(x => x.CreatedTime)
            .ToListAsync();
        return Success(data);
    }

    /// <summary>
    /// 通过文件地址导入数据
    /// </summary>
    /// <param name="fileUrl"></param>
    /// <returns></returns>
    public async Task<ServiceResult<FileAttachment>> AddByFileUrl(string fileUrl)
    {
        InsertFileAttachmentInput fileAttachment = new();

        FileInfo fileInfo = new FileInfo(fileUrl);
        fileAttachment.OriginalFileName = fileInfo.Name;
        fileAttachment.FileName = fileInfo.Name;
        fileAttachment.FileExt = fileInfo.Extension.Replace(".", null);
        fileAttachment.Length = fileInfo.Length;
        fileAttachment.Path = fileUrl.Replace("wwwroot", null).Replace(fileInfo.Name, null);
        var id = await base.Add(fileAttachment);

        var entity1 = Mapper.Map(fileAttachment).ToANew<FileAttachment>();

        entity1.ID = id;
        return Success(entity1);
    }

    public async Task<ServiceResult<AnalysisUploadResult>> AnalysisUploadAsync(UploadForm upload)
    {
        var result = new AnalysisUploadResult();
        var file = upload.file;

        if (file is null)
            return Failed<AnalysisUploadResult>("无效的文件！");

        var filePath = upload.filePath;
        filePath = !string.IsNullOrEmpty(filePath) ? filePath : _configuration["FileUploadOptions:UploadDir"];
        string ImageType = filePath;

        var ext = string.Empty;
        if (string.IsNullOrEmpty(file.FileName) == false)
        {
            var dotPos = file.FileName.LastIndexOf('.');
            ext = file.FileName.Substring(dotPos + 1);
        }
        filePath += "/" + Utility.SnowID() + "/";

        string pathHeader = "wwwroot/" + filePath;
        filePath = "/" + filePath;
        FileHelper.CreateRootDirectory(filePath);

        string fileName = file.FileName;
        var filepath = Path.Combine(pathHeader, fileName);
        //var filepath = Path.Combine(pathHeader, file.FileName);
        using (var stream = File.Create(filepath))
        {
            await file.CopyToAsync(stream);
        }
        InsertFileAttachmentInput fileAttachment = new();
        fileAttachment.OriginalFileName = fileName;
        fileAttachment.FileName = fileName;
        fileAttachment.FileExt = ext;
        fileAttachment.MasterId = upload.masterId;
        fileAttachment.Length = file.Length;
        fileAttachment.Path = filePath;
        fileAttachment.ImageType = ImageType;
        var id = await base.Add(fileAttachment);
        result.FileId = id;

        var info = NPOIHelper.GetTemplateInfo(filePath + fileName);

        if (info.TemplateId.IsNotEmptyOrNull())
        {
            var template = await Db.Queryable<SmImpTemplate>().Where(x => x.ID == info.TemplateId).FirstAsync();
            result.TemplateId = info.TemplateId;

            if (template != null)
            {
                result.IsTemplate = true;

                var importDataId = Utility.GuidId;

                try
                {
                    await ImportHelper.ImportData(Db, importDataId, template, filePath + fileName);
                }
                catch (Exception E)
                {
                    result.Message = E.Message;
                }
                result.ImportDataId = importDataId;
            }
        }

        return Success(result);
    }
}