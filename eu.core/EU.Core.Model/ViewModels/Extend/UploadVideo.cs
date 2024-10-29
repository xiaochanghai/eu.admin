using Microsoft.AspNetCore.Http;

namespace EU.Core.Model.ViewModels.Extend
{
    public class UploadVideo
    {
        public IFormFile file { get; set; }
        public string videoName { get; set; }
        public int chunkIndex { get; set; }
        public int totalChunks { get; set; }
        public long id { get; set; }
    }
}
