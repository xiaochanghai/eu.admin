using System.Data;
using Microsoft.AspNetCore.Http;

namespace EU.Core.Model;

public class ImportExcelResult
{
    public List<SmImportError> ErrorList { get; set; }
    public string ImportDataId { get; set; }
    public List<string> ImportColumns { get; set; }
    public List<string> ImportColumnNames { get; set; }
    public DataTable ImportList { get; set; }


}
public class ImportExcelForm
{
    /// <summary>
    /// 文件
    /// </summary>
    public IFormFile file { get; set; }

    /// <summary>
    /// 文件名
    /// </summary>
    public string fileName { get; set; }
}

public class TransferExcelRequest
{
    public string ImportTemplateCode { get; set; }
    public string ImportDataId { get; set; }
    public string Type { get; set; }
    public string MasterId { get; set; }
    public string ModuleCode { get; set; }

}
