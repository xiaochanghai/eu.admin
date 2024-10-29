using Microsoft.AspNetCore.Http;

namespace EU.Core.Model;

public class UploadForm
{
    public IFormFile file { get; set; }

    public Guid? masterId { get; set; }
    public bool isUnique { get; set; } = false;
    public string filePath { get; set; } = null;
    public string masterTable { get; set; } = null;
    public string masterColumn { get; set; } = null;
    public string imageType { get; set; } = null;
}

public class ChunkUpload
{
    public IFormFile file { get; set; }

    public string fileName { get; set; } = null;
    public int chunkIndex { get; set; }
    public int totalChunks { get; set; }
    public string id { get; set; } = null;
}