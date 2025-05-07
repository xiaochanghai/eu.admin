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
*│　版权所有：苏州一优信息技术有限公司                                │
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
        if (string.IsNullOrEmpty(file.FileName) == false)
        {
            var dotPos = file.FileName.LastIndexOf('.');
            ext = file.FileName.Substring(dotPos + 1);
        }
        filePath += "/" + Utility.GetLongID() + "/";

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

        if (upload.isUnique)
            await Db.Updateable<FileAttachment>()
                    .SetColumns(it => new FileAttachment() { IsDeleted = true })
                    .Where(it => it.MasterId == upload.masterId && it.ID != id && it.ImageType == upload.imageType)
                    .ExecuteCommandAsync();
        return ServiceResult<Guid>.OprateSuccess(id);
    }

    public async Task<ServiceResult<Guid>> UploadImageAsync(UploadForm upload)
    {
        using var _context = ContextFactory.CreateContext();
        string filePath = upload.filePath;
        filePath = !string.IsNullOrEmpty(filePath) ? filePath : _configuration["FileUploadOptions:UploadDir"];
        var ext = string.Empty;
        var file = upload.file;
        if (string.IsNullOrEmpty(file.FileName) == false)
        {
            var dotPos = file.FileName.LastIndexOf('.');
            ext = file.FileName.Substring(dotPos + 1);
        }

        string pathHeader = "wwwroot/" + filePath;
        if (!Directory.Exists(pathHeader))
            Directory.CreateDirectory(pathHeader);

        string fileName = Utility.GetSysID();
        var filepath = Path.Combine(pathHeader, $"{fileName}.{ext}");
        //var filepath = Path.Combine(pathHeader, file.FileName);
        using (var stream = File.Create(filepath))
        {
            await file.CopyToAsync(stream);
        }

        if (upload.isUnique)
        {
            string sql = @"UPDATE FileAttachment
                                SET IsDeleted = 'true'
                                WHERE MasterId = '{0}'
                                      AND IsDeleted = 'false'
                                      AND ImageType = '{1}'";
            sql = string.Format(sql, upload.masterId, upload.imageType);
            DBHelper.ExcuteNonQuery(sql);
        }

        FileAttachment fileAttachment = new();
        fileAttachment.OriginalFileName = file.FileName;
        fileAttachment.CreatedBy = App.User.ID;
        fileAttachment.CreatedTime = Utility.GetSysDate();
        fileAttachment.FileName = fileName;
        fileAttachment.FileExt = ext;
        fileAttachment.MasterId = upload.masterId;
        fileAttachment.Length = file.Length;
        fileAttachment.Path = filePath;
        fileAttachment.ImageType = upload.imageType ?? filePath;
        //url = fileName + "." + ext;
        await _context.AddAsync(fileAttachment);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(upload.masterTable) && !string.IsNullOrEmpty(upload.masterColumn))
        {
            string sql = "UPDATE {2} SET {3}='{1}' WHERE ID='{0}'";
            sql = string.Format(sql, upload.masterId, fileName + "." + ext, upload.masterTable, "ImageUrl");
            DBHelper.ExcuteNonQuery(sql);
        }

        return ServiceResult<Guid>.OprateSuccess(fileAttachment.ID, "上传成功！");
    }

    public async Task<ServiceResult<string>> UploadVideoAsync(ChunkUpload upload)
    {
        var path = $"{$"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}files{Path.DirectorySeparatorChar}upload{Path.DirectorySeparatorChar}{upload.id}{Path.DirectorySeparatorChar}"}";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        using (var stream = File.Create(path + $"{upload.chunkIndex}"))
        {
            await upload.file.CopyToAsync(stream);
        }

        if (upload.chunkIndex == upload.totalChunks - 1)
        {
            var ext = string.Empty;
            var file = upload.file;
            if (string.IsNullOrEmpty(file.FileName) == false)
            {
                var dotPos = upload.fileName.LastIndexOf('.');
                ext = upload.fileName.Substring(dotPos + 1);
            }
            string id = Utility.GetSysID();
            await VideoHelper.FileMerge(upload.id, "." + ext, id);


            FileAttachment fileAttachment = new();
            fileAttachment.OriginalFileName = file.FileName;
            fileAttachment.CreatedBy = App.User.ID;
            fileAttachment.CreatedTime = Utility.GetSysDate();
            fileAttachment.FileName = upload.fileName;
            fileAttachment.FileExt = ext;
            //fileAttachment.MasterId = upload.masterId;
            fileAttachment.Length = file.Length;
            fileAttachment.Path = $"/files/upload/{id}.{ext}";
            //fileAttachment.ImageType = upload.imageType ?? filePath;
            //url = fileName + "." + ext;
            await _context.AddAsync(fileAttachment);
            await _context.SaveChangesAsync();

            return Success<string>(null, "上传成功！");
        }
        return Success<string>(null, "上传成功！");
    }

    public async Task<ServiceResult<List<FileAttachment>>> GetFileListAsync(Guid masterId, string imageType = null)
    {
        var src = Db.Queryable<FileAttachment>();
        if (!string.IsNullOrEmpty(imageType))
            src = src.Where(o => o.ImageType == imageType);

        var data = await src.Where(x => x.MasterId == masterId).OrderByDescending(x => x.CreatedTime).ToListAsync();
        return ServiceResult<List<FileAttachment>>.OprateSuccess(data);
    }
}