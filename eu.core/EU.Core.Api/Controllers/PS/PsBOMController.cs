/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* PsBOM.cs
*
*功 能： N / A
* 类 名： PsBOM
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/5/6 14:58:19  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/ 
namespace EU.Core.Api.Controllers;

/// <summary>
/// PsBOM(Controller)
/// </summary>
[GlobalActionFilter, ApiExplorerSettings(GroupName = Grouping.GroupName_PS)]
public class BOMController : BaseController1<PsBOM>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="_context"></param>
    /// <param name="BaseCrud"></param>
    public BOMController(DataContext _context, BaseCRUDVM<PsBOM> BaseCrud) : base(_context, BaseCrud)
    {
    }

    #region 新增重写
    [HttpPost]
    public override IActionResult Add(PsBOM Model)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            //#region 检查是否存在相同的编码
            Utility.CheckCodeExist("", "PsBOM", "Version", Model.Version.ToString(), ModifyType.Add, null, "版本", "MaterialId='" + Model.MaterialId + "'");
            //#endregion 

            string sql = @"UPDATE A
                                SET A.IsLatest = 'false'
                                FROM PsBOM A
                                WHERE A.MaterialId = '{0}' AND A.IsLatest = 'true'";
            sql = string.Format(sql, Model.MaterialId);
            DBHelper.Instance.ExecuteScalar(sql);

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
    #endregion

    #region 更新重写
    /// <summary>
    /// 更新重写
    /// </summary>
    /// <param name="modelModify"></param>
    /// <returns></returns>
    [HttpPost]
    public override IActionResult Update(dynamic modelModify)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {

            #region 检查是否存在相同的编码
            Utility.CheckCodeExist("", "PsBOM", "Version", modelModify.Version.Value, ModifyType.Edit, modelModify.ID.Value, "版本", "MaterialId='" + modelModify.MaterialId.Value + "'");
            #endregion

            Update<PsBOM>(modelModify);
            _context.SaveChanges();

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

    #region 获取BOM树
    /// <summary>
    /// 获取BOM树
    /// </summary>
    /// <param name="bomId">获取bomId</param>
    /// <returns></returns>
    [HttpGet]
    public IActionResult GetBOMTree(Guid? bomId)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        try
        {
            var materialList = _context.PsBOMMaterial
                   .Join(_context.BdMaterial, x => x.MaterialId, y => y.ID, (x, y) => new { x, y })
                   .Select(m => new TreeBOMMaterial
                   {
                       ID = m.x.ID,
                       MaterialId = m.x.MaterialId,
                       MaterialName = m.y.MaterialNames,
                       title = m.y.MaterialNames,
                       key = m.x.BOMId,
                       BOMId = m.x.BOMId
                   }).ToList();

            var BOMList = _context.PsBOM.ToList();

            List<TreeBOMMaterial> tree = new List<TreeBOMMaterial>();
            tree = materialList.Where(a => a.BOMId == bomId).Select(y => new TreeBOMMaterial
            {
                ID = y.ID,
                MaterialId = y.MaterialId,
                MaterialName = y.MaterialName,
                title = y.MaterialName,
                key = y.ID,
                BOMId = y.BOMId
            }).ToList();

            if (tree.Any())
                LoopToAppendChildren(materialList, BOMList, tree);


            var bom = _context.PsBOM.Where(a => a.ID == bomId)
                  .Join(_context.BdMaterial, x => x.MaterialId, y => y.ID, (x, y) => new { x, y })
                     .Select(m => new
                     {
                         ID = m.x.ID,
                         MaterialNo = m.y.MaterialNo,
                         m.x.Version,
                         MaterialName = m.y.MaterialNames
                     }).SingleOrDefault();
            if (bom != null)
            {
                tree = new List<TreeBOMMaterial>
                    {
                        new TreeBOMMaterial
                        {
                            key=bom.ID,
                            BOMId = bom.ID,
                            title=bom.MaterialName,
                            children = tree
                        }
                    };
                obj.title = bom.MaterialNo + "-" + bom.Version;
            }
            obj.data = tree;
            status = "ok";
            message = "查询成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }

    [NonAction]
    public static void LoopToAppendChildren(List<TreeBOMMaterial> materialList, List<PsBOM> bomList, List<TreeBOMMaterial> tree)
    {
        tree.ForEach(item =>
        {
            List<TreeBOMMaterial> subItems = new List<TreeBOMMaterial>();

            List<PsBOM> bomList1 = bomList.Where(x => x.MaterialId == item.MaterialId).OrderByDescending(x => x.CreatedTime).ToList();
            if (bomList1.Any())
            {
                item.BOMId = bomList1[0].ID;
                subItems = materialList.Where(x => x.BOMId == bomList1[0].ID).OrderBy(x => x.CreatedTime).Select(y => new TreeBOMMaterial
                {
                    ID = y.ID,
                    MaterialId = y.MaterialId,
                    MaterialName = y.MaterialName,
                    title = y.MaterialName,
                    key = y.ID,
                    BOMId = y.BOMId

                }).ToList();
                item.children = subItems;
            }
            if (subItems.Any())
                LoopToAppendChildren(materialList, bomList, subItems);
        });
        string aa = string.Empty;
    }
    public class TreeBOMMaterial : PsBOMMaterial
    {

        public string MaterialName { get; set; }
        public string title { get; set; }
        public Guid? key { get; set; }

        public List<TreeBOMMaterial> children { get; set; }
    }
    #endregion

    #region 拷贝
    /// <summary>
    /// 拷贝
    /// </summary>
    /// <param name="Model">数据</param>
    /// <returns></returns>
    [HttpPost]
    public IActionResult Copy(PsBOM Model)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            PsBOM bom = _context.PsBOM.Where(a => a.ID == Model.ID).SingleOrDefault();
            Guid? MaterialId = bom.MaterialId;

            #region 检查是否存在相同的编码
            Utility.CheckCodeExist("", "PsBOM", "Version", Model.Version, ModifyType.Add, null, "版本", "MaterialId='" + MaterialId + "'");
            #endregion



            bom.ID = Guid.NewGuid();
            bom.Version = Model.Version;
            bom.CreatedTime = Utility.GetSysDate();
            DBHelper.Instance.Add(bom);

            #region bom材料
            List<PsBOMMaterial> materialList = _context.PsBOMMaterial.Where(a => a.BOMId == Model.ID).ToList();
            if (materialList.Any())
            {
                materialList.ForEach(item =>
                {
                    item.ID = Guid.NewGuid();
                    item.BOMId = bom.ID;
                    item.CreatedTime = Utility.GetSysDate();
                });
                DBHelper.Instance.AddRange(materialList);
            }

            #endregion

            #region bom工序
            List<PsBOMProcess> processList = _context.PsBOMProcess.Where(a => a.BOMId == Model.ID).ToList();
            if (processList.Any())
            {
                processList.ForEach(item =>
                {
                    item.ID = Guid.NewGuid();
                    item.BOMId = bom.ID;
                    item.CreatedTime = Utility.GetSysDate();
                });
                DBHelper.Instance.AddRange(processList);
            }
            #endregion

            #region bom工模治具
            List<PsBOMMould> mouldList = _context.PsBOMMould.Where(a => a.BOMId == Model.ID).ToList();
            if (mouldList.Any())
            {
                mouldList.ForEach(item =>
                {
                    item.ID = Guid.NewGuid();
                    item.BOMId = bom.ID;
                    item.CreatedTime = Utility.GetSysDate();
                });
                DBHelper.Instance.AddRange(mouldList);
            }
            #endregion

            status = "ok";
            message = "复制成功！";
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