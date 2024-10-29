/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* FileAttachment.cs
*
*功 能： N / A
* 类 名： FileAttachment
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/24 22:53:39  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Api.Controllers;

/// <summary>
/// 文件服务(Controller)
/// </summary>
[Route("api/[controller]")]
[ApiController, GlobalActionFilter]
[Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_SM)]
public class FileController : BaseController<IFileAttachmentServices, FileAttachment, FileAttachmentDto, InsertFileAttachmentInput, EditFileAttachmentInput>
{
    /// <summary>
    /// 配置信息
    /// </summary>
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _hostingEnvironment;
    public FileController(IFileAttachmentServices service, IConfiguration configuration, IWebHostEnvironment hostingEnvironment) : base(service)
    {
        _configuration = configuration;
        _hostingEnvironment = hostingEnvironment;
    }

    [HttpGet("GetFileList")]
    public async Task<ServiceResult<List<FileAttachment>>> GetFileList(Guid masterId, string imageType = null) => await _service.GetFileListAsync(masterId, imageType);

    #region 上传附件接口
    /// <summary>
    /// 上传附件接口
    /// </summary>
    /// <param name="upload"></param>
    /// <returns></returns>
    [HttpPost, Route("Upload")]
    public async Task<ServiceResult<Guid>> UploadAsync([FromForm] UploadForm upload)
    {
        return await _service.UploadAsync(upload);
    }
    #endregion

    #region 上传图片
    /// <summary>
    /// 上传图片
    /// </summary>
    /// <param name="upload"></param>
    /// <returns></returns>
    [HttpPost, Route("UploadImage")]
    public async Task<ServiceResult<Guid>> UploadImageAsync([FromForm] UploadForm upload)
    {
        return await _service.UploadImageAsync(upload);
    }
    #endregion

    #region 上传视频
    /// <summary>
    /// 上传视频
    /// </summary>
    /// <param name="upload"></param> 
    /// <returns></returns>
    [HttpPost, Route("UploadVideo")]
    public async Task<ServiceResult<string>> UploadVideoAsync([FromForm] ChunkUpload upload)
    {
        return await _service.UploadVideoAsync(upload);
    }
    #endregion

    #region 上传Excel导入模板
    [HttpPost, Route("UploadImport")]
    public async Task<IActionResult> UploadImportAsync(IFormFileCollection fileList, Guid? masterId, bool isUnique = false, string filePath = null)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        string pathHeader = string.Empty;
        string url = string.Empty;

        try
        {
            using var _context = ContextFactory.CreateContext();
            filePath = !string.IsNullOrEmpty(filePath) ? filePath : _configuration["FileUploadOptions:UploadDir"];
            if (fileList.Count > 0)
            {
                foreach (var file in fileList)
                {

                    var ext = string.Empty;
                    if (string.IsNullOrEmpty(file.FileName) == false)
                    {
                        var dotPos = file.FileName.LastIndexOf('.');
                        ext = file.FileName.Substring(dotPos + 1);
                    }

                    pathHeader = "wwwroot/" + filePath;
                    if (!Directory.Exists(pathHeader))
                    {
                        Directory.CreateDirectory(pathHeader);
                    }

                    string fileName = Guid.NewGuid().ToString();
                    var filepath = Path.Combine(pathHeader, $"{fileName}.{ext}");
                    //var filepath = Path.Combine(pathHeader, file.FileName);
                    using (var stream = global::System.IO.File.Create(filepath))
                    {
                        await file.CopyToAsync(stream);
                    }
                    FileAttachment fileAttachment = new FileAttachment();
                    fileAttachment.OriginalFileName = file.FileName;
                    fileAttachment.CreatedBy = App.User.ID;
                    fileAttachment.CreatedTime = Utility.GetSysDate();
                    fileAttachment.FileName = fileName;
                    fileAttachment.FileExt = ext;
                    fileAttachment.MasterId = masterId;
                    fileAttachment.Length = file.Length;
                    fileAttachment.Path = filePath;
                    url = fileName + "." + ext;
                    _context.Add(fileAttachment);
                }

                _context.SaveChanges();

                if (isUnique)
                {
                    string sql = @"UPDATE FileAttachment
                                SET IsDeleted = 'true'
                                WHERE MasterId = '57ec49b5-1a49-4538-9278-4fa7ad86cbc5'
                                      AND ID NOT IN(SELECT TOP (1) ID
                                                    FROM FileAttachment
                                                    WHERE MasterId = '57ec49b5-1a49-4538-9278-4fa7ad86cbc5'
                                                     ORDER BY CreatedTime DESC)
                                      AND IsDeleted = 'false'";
                    sql = string.Format(sql, masterId);
                    DBHelper.ExcuteNonQuery(sql, null);
                }
            }

            status = "ok";
        }
        catch (Exception E)
        {
            message = E.Message;
        }
        obj.url = url;
        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    #endregion

    #region 获取图片
    /// <summary>
    /// 
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    [HttpGet, AllowAnonymous, Route("GetByUrl")]
    public async Task<IActionResult> GetByUrl(string url)
    {
        try
        {
            var webRootPath = _hostingEnvironment.WebRootPath;

            using var _context = ContextFactory.CreateContext();
            var dotPos = url.LastIndexOf('.');
            var ext = url.Substring(dotPos + 1);
            var file = await _context.FileAttachment.Where(o => o.FileName == url.Replace("." + ext, null)).FirstOrDefaultAsync();

            if (file == null)
                return Ok("找不到文件");

            var filePath = $"{webRootPath}{"\\" + file.Path + "\\" + url}";
            var fileName = !string.IsNullOrEmpty(file.FileName) ? file.FileName : Path.GetFileName(filePath);

            var contentTypDict = new Dictionary<string, string> {
                {"jpg","image/jpeg"},
                {"jpeg","image/jpeg"},
                {"jpe","image/jpeg"},
                {"png","image/png"},
                {"gif","image/gif"},
                {"ico","image/x-ico"},
                {"tif","image/tiff"},
                {"tiff","image/tiff"},
                {"fax","image/fax"},
                {"wbmp","image//vnd.wap.wbmp"},
                {"rp","image/vnd.rn-realpix"}
            };
            var contentTypeStr = "image/jpeg";
            //未知的图片类型
            contentTypeStr = contentTypDict[ext];

            using (var sw = new FileStream(filePath, global::System.IO.FileMode.Open))
            {
                var bytes = new byte[sw.Length];
                sw.Read(bytes, 0, bytes.Length);
                sw.Close();
                return new FileContentResult(bytes, contentTypeStr);
            }

        }
        catch (Exception ex)
        {
            return Ok($"下载异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取图片
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("Img/{id}"), AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var webRootPath = _hostingEnvironment.WebRootPath;

        using var _context = ContextFactory.CreateContext();

        var file = await _service.Query(id);
        if (file == null)
            return Ok($"无效的图片ID");

        var dotPos = file.FileName.LastIndexOf('.');
        var ext = file.FileExt;

        if (file == null)
            return Ok("找不到文件");

        var filePath = $"{webRootPath}{"\\" + file.Path + "\\" + file.FileName + "." + ext}";
        //var fileName = !string.IsNullOrEmpty(file.FileName) ? file.FileName : Path.GetFileName(filePath);

        var contentTypDict = new Dictionary<string, string> {
                {"jpg","image/jpeg"},
                {"jpeg","image/jpeg"},
                {"jpe","image/jpeg"},
                {"png","image/png"},
                {"gif","image/gif"},
                {"ico","image/x-ico"},
                {"tif","image/tiff"},
                {"tiff","image/tiff"},
                {"fax","image/fax"},
                {"wbmp","image//vnd.wap.wbmp"},
                {"rp","image/vnd.rn-realpix"}
            };
        var contentTypeStr = "image/jpeg";
        //未知的图片类型
        contentTypeStr = contentTypDict[ext];

        using (var sw = new FileStream(filePath, global::System.IO.FileMode.Open))
        {
            var bytes = new byte[sw.Length];
            sw.Read(bytes, 0, bytes.Length);
            sw.Close();
            return new FileContentResult(bytes, contentTypeStr);
        }
    }
    #endregion

    #region 下载文件
    /// <summary>
    /// 下载文件
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet, AllowAnonymous, Route("Download/{id}")]
    public IActionResult Download(Guid id)
    {
        string sql = "SELECT * FROM FileAttachment where ID='{0}' and IsDeleted='false'";
        sql = string.Format(sql, id);
        FileAttachment attachment = DBHelper.Instance.QueryFirst<FileAttachment>(sql);

        if (attachment != null)
        {
            var fileName = attachment.FileName;
            string path = "wwwroot/" + attachment.Path + fileName;
            FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
            fs.Close();
            return File(new FileStream(path, FileMode.Open), "application/octet-stream", fileName);
        }
        else
        {
            return Ok("无效ID");
        }

    }
    #endregion
}