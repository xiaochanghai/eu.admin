/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsBOMProcess.cs
*
*功 能： N / A
* 类 名： PsBOMProcess
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:58:22  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

/// <summary>
/// PsBOMProcess(Controller)
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PS)]
public class BOMProcessController : BaseController1<PsBOMProcess>
{
    /// <summary>
    /// BOM工艺路线
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public BOMProcessController(DataContext _context, IBaseCRUDVM<PsBOMProcess> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Add(PsBOMProcess Model)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            #region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "PsBOMProcess", "MaterialId", Model.MaterialId.ToString(), ModifyType.Add, null, "材质编号", "BOMId='" + Model.BOMId + "'");
            #endregion

            Model.SerialNumber = Utility.GenerateContinuousSequence("PsBOMProcess", "SerialNumber", "BOMId", Model.BOMId.ToString());
            PsProcess process = _context.PsProcess.Where(a => a.ID == Model.ProcessId).SingleOrDefault();
            PsBOM bom = _context.PsBOM.Where(a => a.ID == Model.BOMId).Where(a => a.IsDeleted == false).SingleOrDefault();
            if (process != null && bom != null)
            {
                int count = _context.PsBOMProcess.Where(a => a.BOMId == Model.BOMId && a.IsDeleted == false).ToList().Count();
                bom.Process += "→" + (count + 1) + "." + process.ProcessName;
                _context.SaveChanges();
            }

            return base.Add(Model);
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }

    /// <summary>
    /// 批量新增
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult BatchAdd(List<PsBOMProcess> data)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            Guid? BOMId = data[0].BOMId;

            for (int i = 0; i < data.Count; i++)
            {
                data[i].ID = Guid.NewGuid();
                DoAddPrepare(data[i]);
                data[i].CreatedBy = UserId;
                data[i].CreatedTime = Utility.GetSysDate();
            }

            if (data.Count > 0)
                DBHelper.Instance.AddRange(data);

            BatchUpdateSerialNumber(BOMId);

            status = "ok";
            message = "添加成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    #endregion

    #region 更新重写
    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="modelModify"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Update(dynamic modelModify)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        string sql = string.Empty;
        try
        {
            #region 检查是否存在相同的编码
            //Utility.CheckCodeExist("", "BdColor", "ColorNo", modelModify.ColorNo.Value, ModifyType.Edit, modelModify.ID.Value, "材质编号", "BOMId='" + modelModify.BOMId.Value + "'");
            #endregion

            Update<PsBOMProcess>(modelModify);
            _context.SaveChanges();
            BatchUpdateProcess(modelModify.BOMId.Value);

            status = "ok";
            message = "修改成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    #endregion

    #region 批量更新BOM工序流程
    /// <summary>
    /// 批量更新排序号
    /// </summary>
    /// <param name="BOMId">BOMId</param>
    private void BatchUpdateProcess(string BOMId)
    {
        Guid id = Guid.Parse(BOMId);
        PsBOM bom = _context.PsBOM.Where(a => a.ID == id).SingleOrDefault();

        var list = _context.PsBOMProcess
            .Where(a => a.BOMId == id && a.IsDeleted == false)
            .OrderBy(w => w.SerialNumber)
            .Join(_context.Set<PsProcess>(), x => x.ProcessId, y => y.ID, (x, y) => new { x, y })
            .Select(s => new
            {
                Name = s.y.ProcessName,
                SerialNumber = s.x.SerialNumber
            }).ToList();
        string process = string.Empty;
        for (int i = 0; i < list.Count; i++)
        {
            if (i == 0)
                process = "1." + list[i].Name;
            else
                process += "→" + (i + 1) + "." + list[i].Name;
        }
        bom.Process = process;
        _context.SaveChanges();
    }
    #endregion

    #region 批量更新排序号
    /// <summary>
    /// 批量更新排序号
    /// </summary>
    /// <param name="BOMId">BOMId</param>
    private void BatchUpdateSerialNumber(Guid? BOMId)
    {
        string sql = @"UPDATE A
                        SET A.SerialNumber = C.NUM
                        FROM PsBOMProcess A
                             JOIN
                             (SELECT *, ROW_NUMBER () OVER (ORDER BY CreatedTime ASC) NUM
                              FROM (SELECT *
                                    FROM (SELECT A.*
                                          FROM PsBOMProcess A
                                          WHERE     1 = 1
                                                AND A.BOMId =
                                                    '{0}'
                                                AND A.IsDeleted = 'false'
                                                AND A.IsActive = 'true') A) B) C
                                ON A.ID = C.ID";
        sql = string.Format(sql, BOMId);
        DBHelper.Instance.ExecuteScalar(sql);

    }
    #endregion

    #region 删除重写
    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="Id"></param>
    /// <returns></returns>
    [HttpGet]
    public override IActionResult Delete(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            _BaseCrud.DoDelete(Id);

            PsBOMProcess Model = _context.PsBOMProcess.Where(x => x.ID == Id).SingleOrDefault();
            if (Model != null)
                BatchUpdateSerialNumber(Model.BOMId);

            status = "ok";
            message = "删除成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    /// <summary>
    /// 批量删除
    /// </summary>
    /// <param name="entryList"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult BatchDelete(List<PsBOMProcess> entryList)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            if (entryList.Count > 0)
            {
                var ids = entryList.Select(o => o.ID).ToList();
                _context.PsBOMProcess
                    .Where(x => ids.Contains(x.ID))
                    .UpdateFromQuery(x =>
                    new PsBOMProcess
                    {
                        IsDeleted = true,
                        UpdateBy = UserId,
                        UpdateTime = Utility.GetSysDate()
                    });
                _context.SaveChanges();

                PsBOMProcess Model = _context.PsBOMProcess.Where(x => x.ID == entryList[0].ID).SingleOrDefault();
                if (Model != null)
                    BatchUpdateSerialNumber(Model.BOMId);
            }

            status = "ok";
            message = "批量删除成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }
    #endregion
}