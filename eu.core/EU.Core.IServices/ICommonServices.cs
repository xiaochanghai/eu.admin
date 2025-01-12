using Microsoft.AspNetCore.Mvc;

namespace EU.Core.IServices;

/// <summary>
/// IPayServices
/// </summary>	
public interface ICommonServices : IBaseServices<SmModules, SmModulesDto, InsertSmModulesInput, EditSmModulesInput>
{
    /// <summary>
    /// 自定义列模块数据返回
    /// </summary>
    /// <returns></returns>
    Task<GridListReturn> GetGridList(string paramData, string moduleCode, string sorter = "{}", string filter = "{}", string parentColumn = null, string parentId = null);

    /// <summary>
    /// 清空缓存
    /// </summary>
    ServiceResult ClearCache();

    /// <summary>
    /// Excel导出
    /// </summary>
    /// <param name="moduleCode"></param>
    /// <param name="paramData"></param>
    /// <param name="sorter"></param>
    /// <param name="exportExcelColumns"></param>
    /// <returns></returns>

    ServiceResult<string> ExportExcel(string moduleCode, string paramData = "{}", string sorter = "{}", string exportExcelColumns = "");


    Task<ServiceResult<ImportExcelResult>> ImportExcelAsync(ImportExcelForm import);


    ServiceResult TransferExcelData(TransferExcelRequest request);

    Task<ServiceResult<List<ComboGridData>>> ComboGridData(string parentColumn, string parentId, int? current, int? pageSize, string code, string[] items, string key);
    Task<ServiceResult<List<ComboGridData>>> GetComboGridData([FromBody] ComboGridDataBody body);

    Task<ServiceResult<Guid>> Add(string moduleCode, object insertModel);

    Task<ServiceResult<Guid>> Update(string moduleCode, Guid id, object entity);

    Task<ServiceResult> Delete(string moduleCode, Guid id);
    Task<ServiceResult> Delete(string moduleCode, List<Guid> ids);

    Task<ServiceResult<object>> Query(string moduleCode, Guid id);

}
