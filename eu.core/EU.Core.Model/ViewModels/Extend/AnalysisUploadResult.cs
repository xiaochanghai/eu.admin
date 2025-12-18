using System.Data;

namespace EU.Core.Model.ViewModels.Extend;

public class AnalysisUploadResult
{
    public bool IsTemplate { get; set; } = false;
    public Guid? FileId { get; set; }
    public Guid? TemplateId { get; set; }
    public Guid? ImportDataId { get; set; }
    public string Message { get; set; }
    public List<SmImportError> ErrorList { get; set; }

}
