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
    private readonly IWebHostEnvironment _hostingEnvironment;
    public FileController(IFileAttachmentServices service, IConfiguration configuration, IWebHostEnvironment hostingEnvironment) : base(service)
    {
        _hostingEnvironment = hostingEnvironment;
    }

    #region 获取文件列表
    /// <summary>
    /// 获取文件列表
    /// </summary>
    /// <param name="masterId">masterId</param>
    /// <param name="imageType">类型</param>
    /// <returns></returns>
    [HttpGet("GetFileList")]
    public async Task<ServiceResult<List<FileAttachment>>> GetFileList(Guid masterId, string imageType = null) => await _service.GetFileListAsync(masterId, imageType);
    #endregion

    #region 上传附件接口
    /// <summary>
    /// 上传附件接口
    /// </summary>
    /// <param name="upload"></param>
    /// <returns></returns>
    [HttpPost("Upload")]
    public async Task<ServiceResult<Guid>> UploadAsync([FromForm] UploadForm upload) => await _service.UploadAsync(upload);
    #endregion

    #region 上传图片
    /// <summary>
    /// 上传图片
    /// </summary>
    /// <param name="upload"></param>
    /// <returns></returns>
    [HttpPost("UploadImage")]
    public async Task<ServiceResult<Guid>> UploadImageAsync([FromForm] UploadForm upload) => await _service.UploadImageAsync(upload);
    #endregion

    #region 上传视频
    /// <summary>
    /// 上传视频
    /// </summary>
    /// <param name="upload"></param> 
    /// <returns></returns>
    [HttpPost("UploadVideo")]
    public async Task<ServiceResult<string>> UploadVideoAsync([FromForm] ChunkUpload upload) => await _service.UploadVideoAsync(upload);
    #endregion

    #region 获取图片
    /// <summary>
    /// 获取图片
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    [AllowAnonymous, HttpGet("GetByUrl")]
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
    [AllowAnonymous, HttpGet("Download/{id}")]
    public IActionResult Download(Guid id)
    {
        string sql = $"SELECT * FROM FileAttachment where ID='{id}' and IsDeleted='false'";
        var attachment = DBHelper.QueryFirst<FileAttachment>(sql);

        if (attachment != null)
        {
            var fileName = attachment.FileName;
            string path = "wwwroot/" + attachment.Path + fileName;

            if (!Directory.Exists(path))
                return Ok("文件不存在！");

            FileStream fs = new FileStream(path, FileMode.OpenOrCreate);
            fs.Close();
            return File(new FileStream(path, FileMode.Open), "application/octet-stream", fileName);
        }
        else
            return Ok("无效ID");
    }
    #endregion
}