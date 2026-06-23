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
using Microsoft.AspNetCore.Http;

namespace EU.Core.Services;

/// <summary>
/// FileAttachment (服务)
/// </summary>
public class FileAttachmentServices : BaseServices<FileAttachment, FileAttachmentDto, InsertFileAttachmentInput, EditFileAttachmentInput>, IFileAttachmentServices
{
    #region 常量定义
    private const string DEFAULT_UPLOAD_DIR_CONFIG_KEY = "FileUploadOptions:UploadDir";
    private const string PATH_SEPARATOR = "/";
    private const string WWWROOT_PREFIX = "wwwroot/";
    #endregion

    private readonly IBaseRepository<FileAttachment> _dal;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _hostingEnvironment;

    public FileAttachmentServices(IBaseRepository<FileAttachment> dal, IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
    {
        _dal = dal;
        BaseDal = dal;
        _configuration = configuration;
        _hostingEnvironment = hostingEnvironment;
    }

    public async Task<ServiceResult<Guid>> UploadAsync(UploadForm upload)
    {
        // 参数验证
        var validationResult = ValidateUploadFile(upload?.file);
        if (!validationResult.Success)
            return Failed<Guid>(validationResult.Message);

        try
        {
            var file = upload.file;

            // 获取上传路径
            var imageType = upload.imageType ?? upload.filePath;
            var uploadPath = GetUploadPath(upload.filePath);

            // 提取文件扩展名
            var ext = GetFileExtension(file.FileName);

            // 构建完整路径
            var fullPath = $"{PATH_SEPARATOR}{uploadPath}{PATH_SEPARATOR}";
            FileHelper.CreateRootDirectory(fullPath);

            // 生成唯一文件名
            var fileName = $"{Utility.SnowID()}.{ext}";
            var filePath = Path.Combine(fullPath, fileName);

            // 保存文件
            using (var stream = File.Create(FileHelper.GetPhysicsPath() + filePath))
            {
                await file.CopyToAsync(stream);
            }

            // 创建文件附件记录
            var fileAttachment = new InsertFileAttachmentInput
            {
                OriginalFileName = file.FileName,
                FileName = fileName,
                FileExt = ext,
                MasterId = upload.masterId,
                Length = file.Length,
                Path = fullPath,
                ImageType = imageType
            };

            var id = await base.Add(fileAttachment);

            return Success(id, "上传成功！");
        }
        catch (Exception ex)
        {
            return Failed<Guid>($"文件上传失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Guid>> UploadImageAsync(UploadForm upload)
    {
        // 参数验证
        var validationResult = ValidateUploadFile(upload?.file);
        if (!validationResult.Success)
            return Failed<Guid>(validationResult.Message);

        try
        {
            var file = upload.file;

            // 获取上传路径
            var imageType = upload.imageType ?? upload.filePath;
            var uploadPath = GetUploadPath(upload.filePath);

            // 提取文件扩展名
            var ext = GetFileExtension(file.FileName);

            // 构建物理路径
            var physicalPath = WWWROOT_PREFIX + uploadPath;
            FileHelper.CreateDirectory(physicalPath);

            // 生成唯一文件名
            var fileName = $"{Utility.SnowID()}.{ext}";
            var fullFilePath = Path.Combine(physicalPath, fileName);

            // 保存文件
            using (var stream = File.Create(fullFilePath))
            {
                await file.CopyToAsync(stream);
            }

            // 如果需要唯一性,标记同类型旧文件为已删除
            if (upload.isUnique)
            {
                await Db.Updateable<FileAttachment>()
                    .SetColumns(it => new FileAttachment { IsDeleted = true })
                    .Where(x => x.MasterId == upload.masterId && x.ImageType == imageType)
                    .ExecuteCommandAsync();
            }

            // 创建文件附件记录
            var fileAttachment = new InsertFileAttachmentInput
            {
                OriginalFileName = file.FileName,
                FileName = fileName,
                FileExt = ext,
                MasterId = upload.masterId,
                Length = file.Length,
                Path = uploadPath,
                ImageType = imageType
            };

            var id = await base.Add(fileAttachment);

            // 如果指定了主表和主列,更新主表记录
            if (upload.masterTable.IsNotEmptyOrNull() && upload.masterColumn.IsNotEmptyOrNull())
            {
                var updateData = new Dictionary<string, object>
                {
                    { "ID", upload.masterId },
                    { "UpdateBy", Utility.GetUserId() },
                    { "UpdateTime", Utility.GetSysDate() },
                    { upload.masterColumn, upload.masterColumn == "ImageUrl" ? fileName : id }
                };

                await Db.Updateable(updateData)
                    .AS(upload.masterTable)
                    .WhereColumns("ID")
                    .ExecuteCommandAsync();
            }

            return Success(id, "上传成功！");
        }
        catch (Exception ex)
        {
            return Failed<Guid>($"图片上传失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult<Guid?>> UploadVideoAsync(ChunkUpload upload)
    {
        upload.filePath ??= "upload";
        return await UploadChunkInternalAsync(upload);
    }

    public async Task<ServiceResult<Guid?>> UploadChunkAsync(ChunkUpload upload)
    {
        return await UploadChunkInternalAsync(upload);
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

    #region 私有辅助方法

    /// <summary>
    /// 验证上传文件
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <returns>验证结果</returns>
    private ServiceResult ValidateUploadFile(IFormFile file)
    {
        if (file == null)
            return Failed("文件不能为空");

        if (file.Length == 0)
            return Failed("文件大小不能为0");

        if (string.IsNullOrWhiteSpace(file.FileName))
            return Failed("文件名不能为空");

        return Success();
    }

    private ServiceResult ValidateChunkUpload(ChunkUpload upload)
    {
        var fileValidation = ValidateUploadFile(upload?.file);
        if (!fileValidation.Success)
            return fileValidation;

        if (string.IsNullOrWhiteSpace(upload.fileName))
            return Failed("文件名不能为空");

        if (string.IsNullOrWhiteSpace(upload.id))
            return Failed("分片上传ID不能为空");

        if (upload.id.Contains("..") || upload.id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return Failed("分片上传ID格式不正确");

        if (upload.totalChunks <= 0)
            return Failed("分片总数不正确");

        if (upload.chunkIndex < 0 || upload.chunkIndex >= upload.totalChunks)
            return Failed("分片序号不正确");

        return Success();
    }

    private async Task<ServiceResult<Guid?>> UploadChunkInternalAsync(ChunkUpload upload)
    {
        var validationResult = ValidateChunkUpload(upload);
        if (!validationResult.Success)
            return Failed<Guid?>(validationResult.Message);

        try
        {
            var originalFileName = Path.GetFileName(upload.fileName);
            var ext = GetFileExtension(originalFileName);
            var imageType = upload.imageType ?? upload.filePath;
            var uploadPath = GetUploadPath(upload.filePath);
            var fullPath = $"{PATH_SEPARATOR}{uploadPath.Trim(PATH_SEPARATOR.ToCharArray())}{PATH_SEPARATOR}";

            FileHelper.CreateRootDirectory(fullPath);

            var physicalRoot = FileHelper.GetPhysicsPath();
            var relativeUploadPath = uploadPath.Replace("/", Path.DirectorySeparatorChar.ToString()).Trim(Path.DirectorySeparatorChar);
            var tempPath = Path.Combine(physicalRoot, relativeUploadPath, upload.id);

            FileHelper.CreateDirectory(tempPath);

            var chunkPath = Path.Combine(tempPath, upload.chunkIndex.ToString());
            using (var stream = File.Create(chunkPath))
            {
                await upload.file.CopyToAsync(stream);
            }

            if (Directory.GetFiles(tempPath).Length < upload.totalChunks)
                return Success<Guid?>(null, "上传成功！");

            var fileId = Utility.SnowID().ObjToString();
            var fileName = ext.IsNotEmptyOrNull() ? $"{fileId}.{ext}" : fileId;
            var finalPath = Path.Combine(physicalRoot, relativeUploadPath, fileName);

            await MergeChunkFilesAsync(tempPath, finalPath, upload.totalChunks);

            var fileInfo = new FileInfo(finalPath);
            var fileAttachment = new InsertFileAttachmentInput
            {
                OriginalFileName = originalFileName,
                FileName = fileName,
                FileExt = ext,
                MasterId = upload.masterId,
                Length = fileInfo.Length,
                Path = fullPath,
                ImageType = imageType
            };

            Guid? id = await base.Add(fileAttachment);

            return Success(id, "上传成功！");
        }
        catch (Exception ex)
        {
            return Failed<Guid?>($"文件分片上传失败: {ex.Message}");
        }
    }

    private static async Task MergeChunkFilesAsync(string tempPath, string finalPath, int totalChunks)
    {
        using (var finalStream = new FileStream(finalPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            for (var index = 0; index < totalChunks; index++)
            {
                var chunkPath = Path.Combine(tempPath, index.ToString());
                if (!File.Exists(chunkPath))
                    throw new FileNotFoundException($"分片 {index} 不存在", chunkPath);

                using (var chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await chunkStream.CopyToAsync(finalStream);
                }
            }
        }

        Directory.Delete(tempPath, true);
    }

    /// <summary>
    /// 获取上传路径（如果未指定则使用配置中的默认路径）
    /// </summary>
    /// <param name="filePath">指定的文件路径</param>
    /// <returns>上传路径</returns>
    private string GetUploadPath(string filePath)
    {
        return !string.IsNullOrEmpty(filePath)
            ? "files/" + filePath
            : _configuration[DEFAULT_UPLOAD_DIR_CONFIG_KEY] ?? "upload";
    }

    /// <summary>
    /// 提取文件扩展名（不包含点号）
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>文件扩展名</returns>
    private string GetFileExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var dotPos = fileName.LastIndexOf('.');
        if (dotPos < 0 || dotPos == fileName.Length - 1)
            return string.Empty;

        return fileName.Substring(dotPos + 1);
    }

    #endregion
}