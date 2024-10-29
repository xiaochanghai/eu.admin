using System.ComponentModel.DataAnnotations;
using System.Reflection;
using EU.Core.Common.Const;
using EU.Core.Common.Extensions;
using EU.Core.Common.UserManager;
using ExpressionType = EU.Core.DataAccess.ExpressionType;

namespace EU.Core.Controllers;

/// <summary>
/// 基础Controllers类
/// </summary>
/// <typeparam name="T"></typeparam>
[Route("api/[controller]/[action]")]
[GlobalActionFilter, ApiController]
[Authorize(Permissions.Name)]
//[AuthorizeJwt]
public class BaseController1<T> : ControllerBase where T : class
{
    /// <summary>
    /// _context
    /// </summary>
    public readonly DataContext _context;

    /// <summary>
    /// _BaseCrud
    /// </summary>
    public readonly IBaseCRUDVM<T> _BaseCrud;

    /// <summary>
    /// 用户信息
    /// </summary>
    public SmUsers UserInfo = UserContext.Current.UserInfo;

    /// <summary>
    /// 用户ID
    /// </summary>
    public Guid? UserId = UserContext.Current.User_Id;

    /// <summary>
    /// 公司ID
    /// </summary>
    public Guid? CompanyId = UserContext.Current.CompanyId;

    /// <summary>
    /// 集团ID
    /// </summary>
    public Guid? GroupId = UserContext.Current.GroupId;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="BaseCrud"></param>
    public BaseController1(DataContext context, IBaseCRUDVM<T> BaseCrud)
    {
        _context = context;
        _BaseCrud = BaseCrud;
    }

    #region 增删查改
    /// <summary>
    /// 增删查改
    /// </summary>
    /// <param name="Entity"></param>
    [NonAction]
    public void DoAddPrepare(T Entity)
    {
        //自动设定添加日期和添加人
        if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
        {
            var ent = Entity as BasePoco;
            if (string.IsNullOrEmpty(ent.CreatedBy.ToString()))
                ent.CreatedBy = !string.IsNullOrEmpty(User.Identity.Name) ? new Guid(User.Identity.Name) : Guid.Empty;
            if (ent.CreatedTime == null)
                ent.CreatedTime = Utility.GetSysDate();
            if (ent.GroupId == null)
                ent.GroupId = Guid.Parse(GetGroupId());
            if (ent.CompanyId == null)
                ent.CompanyId = Guid.Parse(GetCompanyId());

        }
        //if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(Model.Base.BasePoco<Guid>)))
        //{
        //    Model.Base.BasePoco<Guid> ent = Entity as Model.Base.BasePoco<Guid>;
        //    if (string.IsNullOrEmpty(ent.CreatedBy.ToString()))
        //        ent.CreatedBy = !string.IsNullOrEmpty(User.Identity.Name) ? new Guid(User.Identity.Name) : Guid.Empty;
        //    if (ent.CreatedTime == null)
        //        ent.CreatedTime = Utility.GetSysDate();
        //    if (ent.GroupId == null)
        //        ent.GroupId = Guid.Parse(GetGroupId());
        //    if (ent.CompanyId == null)
        //        ent.CompanyId = Guid.Parse(GetCompanyId());
        //    ent.ModificationNum = 0;
        //    ent.Tag = 0;
        //    ent.IsActive = true;
        //}
    }

    /// <summary>
    /// 增删查改
    /// </summary>
    /// <param name="Entity"></param>
    [NonAction]
    public void DoEditPrepare(T Entity)
    {
        //自动设定修改日期和添加人
        if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
        {
            BasePoco ent = Entity as BasePoco;
            if (string.IsNullOrEmpty(ent.UpdateBy.ToString()))
                ent.UpdateBy = new Guid(User.Identity.Name);
            if (ent.UpdateTime == null)
                ent.UpdateTime = Utility.GetSysDate();

        }
        //if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(Model.Base.BasePoco<Guid>)))
        //{
        //    Model.Base.BasePoco<Guid> ent = Entity as Model.Base.BasePoco<Guid>;
        //    if (string.IsNullOrEmpty(ent.UpdateBy.ToString()))
        //        ent.UpdateBy = new Guid(User.Identity.Name);
        //    if (ent.UpdateTime == null)
        //        ent.UpdateTime = Utility.GetSysDate();
        //    ent.ModificationNum += 1;
        //}
    }

    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="paramData"></param>
    /// <param name="sorter"></param>
    /// <param name="filter"></param>
    /// <param name="parentColumn"></param>
    /// <param name="parentId"></param>
    /// <param name="moduleCode"></param>
    /// <returns></returns>
    [HttpGet]
    public virtual async Task<IActionResult> GetPageList(string paramData, string sorter = "{}", string filter = "{}", string parentColumn = null, string parentId = null, string moduleCode = null)
    {
        dynamic obj = new ExpandoObject();
        var success = true;
        string message = string.Empty;
        int current = 1;
        int pageSize = 20;
        long total = 0;

        string defaultSorter = "TaxisNo";
        string sortType = string.Empty;

        try
        {
            IQueryable<T> query = null;
            query = _context.Set<T>();

            var searchParam = JsonHelper.JsonToObj<Dictionary<string, object>>(paramData);
            var filterParam = JsonHelper.JsonToObj<Dictionary<string, object>>(filter);
            var lamadaExtention = new LamadaExtention<T>();

            foreach (var item in searchParam)
            {
                if (item.Key == "current")
                {
                    searchParam.Remove(item.Key);
                    current = int.Parse(item.Value.ToString());
                    continue;
                }

                if (item.Key == "pageSize")
                {
                    searchParam.Remove(item.Key);
                    pageSize = int.Parse(item.Value.ToString());
                    continue;
                }

                if (item.Key == "_timestamp")
                {
                    searchParam.Remove(item.Key);
                    continue;
                }

                if (item.Value.ToString() == "{}")
                    searchParam.Remove(item.Key);
                //string type = typeof(T).GetProperties().Where(x => x.Name.ToLower() == item.Key.ToLower())
                //                  .FirstOrDefault()
                //                  ?.PropertyType.GetGenericArguments().FirstOrDefault()
                //                  ?.Name ?? typeof(T).GetProperties().Where(x => x.Name.ToLower() == item.Key.ToLower())
                //                  .FirstOrDefault()
                //                  ?.PropertyType.Name;
                //if (item.Value.GetType().Name == "JArray")
                //{
                //    var jArray = JArray.Parse(item.Value.ToString());
                //    if (type == "DateTime" && jArray.Count == 2)
                //    {
                //        lamadaExtention.GetExpression(item.Key, DateTime.Parse(jArray.First.ToString()), ExpressionType.GreaterThanOrEqual);
                //        lamadaExtention.GetExpression(item.Key, DateTime.Parse(jArray.Last.ToString()), ExpressionType.LessThanOrEqual);
                //    }
                //}
                //else
                //    lamadaExtention.GetExpression(item.Key, item.Value.ToString().Trim(), GetExpressionType(type));
            }

            if (searchParam.Count > 0)
                lamadaExtention.GetExpression(searchParam);

            foreach (var item in filterParam)
            {
                if (!string.IsNullOrEmpty(item.Value.ToString()))
                    lamadaExtention.GetExpression(item.Key, item.Value.ToString().Trim(), ExpressionType.Equal);
            }

            lamadaExtention.GetExpression("IsDeleted", "false", ExpressionType.Equal);
            if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(parentColumn))
            {
                try
                {
                    lamadaExtention.GetExpression(parentColumn, Guid.Parse(parentId), ExpressionType.Equal);
                }
                catch (Exception)
                {
                    lamadaExtention.GetExpression(parentColumn, parentId, ExpressionType.Equal);
                }
            }

            if (default(T).HasField("IsActive"))
                lamadaExtention.GetExpression("IsActive", "true", ExpressionType.Equal);

            if (lamadaExtention.GetLambda() != null)
                query = query.Where(lamadaExtention.GetLambda());

            var sorterParam = JsonHelper.JsonToObj<Dictionary<string, string>>(sorter);

            if (sorterParam.Count == 0 && !string.IsNullOrEmpty(moduleCode))
            {
                SmModules module = ModuleInfo.GetModuleInfo(moduleCode);
                if (module != null && !string.IsNullOrEmpty(module.DefaultSort) && !string.IsNullOrEmpty(module.DefaultSortOrder))
                    sorterParam.Add(module.DefaultSort, module.DefaultSortOrder);
            }

            if (sorterParam.Count > 0)
            {
                foreach (var item in sorterParam)
                {
                    string type = typeof(T).GetProperties().Where(x => x.Name.ToLower() == item.Key.ToLower())
                                      .FirstOrDefault()
                                      ?.PropertyType.GetGenericArguments().FirstOrDefault()
                        ?.Name ?? typeof(T).GetProperties().Where(x => x.Name.ToLower() == item.Key.ToLower())
                                       .FirstOrDefault()
                                       ?.PropertyType.Name;

                    #region 排序
                    if (type == "Int32")
                    {
                        if (item.Value == "ascend")
                        {
                            if (lamadaExtention.GetLambda() != null)
                                query = query.OrderBy(LamadaExtention<T>.SortLambda<T, int>(defaultSorter, item.Key));
                            else
                                query = query.OrderBy(LamadaExtention<T>.SortLambda<T, int>(defaultSorter, item.Key));
                        }
                        else if (item.Value == "descend")
                        {
                            if (lamadaExtention.GetLambda() != null)
                                query = query.OrderByDescending(LamadaExtention<T>.SortLambda<T, int>(defaultSorter, item.Key));
                            else
                                query = query.OrderByDescending(LamadaExtention<T>.SortLambda<T, int>(defaultSorter, item.Key));
                        }
                    }
                    else if (type == "Decimal")
                    {
                        if (item.Value == "ascend")
                        {
                            if (lamadaExtention.GetLambda() != null)
                                query = query.OrderBy(LamadaExtention<T>.SortLambda<T, decimal>(defaultSorter, item.Key));
                            else
                                query = query
                                    .OrderBy(LamadaExtention<T>.SortLambda<T, decimal>(defaultSorter, item.Key));
                        }
                        else if (item.Value == "descend")
                        {
                            if (lamadaExtention.GetLambda() != null)
                                query = query.OrderByDescending(LamadaExtention<T>.SortLambda<T, decimal>(defaultSorter, item.Key));
                            else
                                query = query.OrderByDescending(LamadaExtention<T>.SortLambda<T, decimal>(defaultSorter, item.Key));
                        }
                    }
                    else if (type == "DateTime")
                    {
                        if (item.Value == "ascend")
                        {
                            if (lamadaExtention.GetLambda() != null)
                                query = query.OrderBy(LamadaExtention<T>.SortLambda<T, DateTime>(defaultSorter, item.Key));
                            else
                                query = query
                                    .OrderBy(LamadaExtention<T>.SortLambda<T, DateTime>(defaultSorter, item.Key));
                        }
                        else if (item.Value == "descend")
                        {
                            if (lamadaExtention.GetLambda() != null)
                                query = query.OrderByDescending(LamadaExtention<T>.SortLambda<T, DateTime>(defaultSorter, item.Key));
                            else
                                query = query.OrderByDescending(LamadaExtention<T>.SortLambda<T, DateTime>(defaultSorter, item.Key));
                        }
                    }
                    else
                    {
                        if (item.Value == "ascend")
                        {
                            if (lamadaExtention.GetLambda() != null)
                                query = query.OrderBy(LamadaExtention<T>.SortLambda<T, string>(defaultSorter, item.Key));
                            else
                                query = query
                                    .OrderBy(LamadaExtention<T>.SortLambda<T, string>(defaultSorter, item.Key));
                        }
                        else if (item.Value == "descend")
                        {
                            if (lamadaExtention.GetLambda() != null)
                                query = query.OrderByDescending(LamadaExtention<T>.SortLambda<T, string>(defaultSorter, item.Key));
                            else
                                query = query.OrderByDescending(LamadaExtention<T>.SortLambda<T, string>(defaultSorter, item.Key));
                        }
                    }
                    #endregion
                }
            }
            obj.data = await query.Skip((current - 1) * pageSize).Take(pageSize).ToListAsync();
            total = await query.LongCountAsync();

            message = ResponseText.QUERY_SUCCESS;
        }
        catch (Exception E)
        {
            success = false;
            message = E.Message;
        }

        obj.current = current;
        obj.pageSize = pageSize;
        obj.total = total;
        obj.success = success;
        obj.message = message;
        return Ok(obj);
    }
    /// <summary>
    /// 根据Id查询数据
    /// </summary>
    /// <param name="Id">id</param>
    /// <returns></returns>
    [HttpGet]
    public virtual async Task<IActionResult> GetById(Guid Id)
    {
        try
        {
            var value = await _BaseCrud.GetByIdAsync(Id);

            return Ok(value);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 根据Id查询数据
    /// </summary>
    /// <param name="Id">id</param>
    /// <returns></returns>
    [HttpGet("{Id}")]
    public virtual async Task<ServiceResult<T>> Get(Guid Id)
    {
        try
        {
            var value = await _BaseCrud.GetByIdAsync(Id);

            return ServiceResult<T>.OprateSuccess(value, ResponseText.QUERY_SUCCESS);

        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 新增数据
    /// </summary>
    /// <param name="Model"></param>
    /// <returns></returns>
    [HttpPost]
    public virtual IActionResult Add([FromBody] T Model)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {

            DoAddPrepare(Model);
            _BaseCrud.DoAdd(Model);

            obj.Id = Model.GetType().GetProperties().Where(x => x.Name.ToLower() == "id").FirstOrDefault()
                ?.GetValue(Model).ToString();

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

    /// <summary>
    /// 批量新增
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpPost]
    public virtual IActionResult BatchAdd(List<T> data)
    {

        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            data?.ForEach(o =>
            {
                DoAddPrepare(o);
                if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
                {
                    BasePoco ent = o as BasePoco;
                    ent.ID = Guid.NewGuid();
                }
            });
            _context.AddRange(data);
            _context.SaveChanges();

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

    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="modelModify">data</param>
    /// <returns></returns>
    [HttpPost]
    public virtual IActionResult Update(dynamic modelModify)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            Update<T>(modelModify);
            _context.SaveChanges();
            //DoEditPrepare(Model);
            //_BaseCrud.DoUpdate(Model);

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


    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="Id">id</param>
    /// <returns></returns>
    [HttpGet]
    public virtual IActionResult Delete(Guid Id)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            _BaseCrud.DoDelete(Id);

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
    /// <param name="entryList">list</param>
    /// <returns></returns>
    [HttpPost]
    public virtual IActionResult BatchDelete(List<T> entryList)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;

        try
        {
            for (int i = 0; i < entryList.Count; i++)
            {
                var navigations = _context.Entry(entryList[i]).Navigations.ToList();
                for (int n = 0; n < navigations.Count; n++)
                {
                    navigations[n].CurrentValue = null;
                }

                //if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BasePoco<Guid>)))
                //{
                //    PersistPoco ent = entryList[i] as PersistPoco;
                //    ent.IsDeleted = true;

                //}
                if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
                {
                    var ent = entryList[i] as BasePoco;
                    ent.IsDeleted = true;
                }

                _context.Update(entryList[i]);
            }
            _context.SaveChanges();

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

    #region 提交
    ///// <summary>
    ///// 提交
    ///// </summary>
    ///// <param name="moduleId">模块ID</param>
    ///// <param name="entryList"></param>
    ///// <returns></returns>
    //[HttpPost]
    //public async Task<ServiceResult> SubmitAudit(Guid moduleId, List<T> entryList)
    //{
    //    dynamic obj = new ExpandoObject();
    //    string message = string.Empty;

    //    try
    //    {
    //        //获取模块信息
    //        var moduleInfo = await _context.SmModules.AsNoTracking().Where(x => x.IsDeleted == false && x.ID == moduleId)
    //            .FirstOrDefaultAsync();
    //        if (moduleInfo is null)
    //            throw new Exception("无效的模块ID");
    //        for (int i = 0; i < entryList.Count; i++)
    //        {
    //            BasePoco ent = entryList[i] as BasePoco;
    //            if (ent.AuditStatus != "Add")
    //                throw new Exception("包含非新增数据，请重新选择！");

    //            //流程id
    //            var flow = await _context.SmProjectFlow
    //                .Where(x => x.IsDeleted == false && x.IsActive == true && x.SmModuleId == moduleId)
    //                .FirstOrDefaultAsync();
    //            var FlowId = flow?.ID;

    //            //该流程所有节点
    //            var nodes = await _context.SmNodes.Where(x => x.IsDeleted == false && x.SmProjectFlowId == FlowId).OrderBy(x => x.index)
    //                .ToListAsync();

    //            //该流程所有的线
    //            var edges = await _context.SmEdges.Where(x => x.IsDeleted == false && x.SmProjectFlowId == FlowId)
    //                .OrderBy(x => x.index).ToListAsync();

    //            //当前节点信息
    //            SmNode currentNodeInfo = nodes.Where(x => x.IsDeleted == false && x.label == "Start").SingleOrDefault();

    //            #region 保存节点信息
    //            ent.CurrentNode = currentNodeInfo.nodeid;
    //            ent.AuditStatus = "Auditing";

    //            var navigations = _context.Entry(entryList[i]).Navigations.ToList();
    //            for (int n = 0; n < navigations.Count; n++)
    //            {
    //                navigations[n].CurrentValue = null;
    //            }

    //            _context.Update(entryList[i]);
    //            #endregion

    //            //获取下一个节点
    //            string nextNodeid = string.Empty;
    //            //source为当前节点的线
    //            List<SmEdge> nextEdges = edges.Where(x => x.source == currentNodeInfo.nodeid).ToList();
    //            if (nextEdges.Count == 1)
    //            {
    //                nextNodeid = nextEdges[0].target;
    //            }
    //            else if (nextEdges.Count > 1)
    //            {
    //                for (int n = 0; n < nextEdges.Count; n++)
    //                {
    //                    var conditionField = nextEdges[n].ConditionField;
    //                    var condition = nextEdges[n].Condition;
    //                    var conditionValue = nextEdges[n].ConditionValue;

    //                    IQueryable<T> query = null;
    //                    var lamadaExtention = new LamadaExtention<T>();

    //                    lamadaExtention.GetExpression("ID", ent.ID, ExpressionType.Equal);
    //                    lamadaExtention.GetExpression(conditionField, conditionValue, GetExpressionType(condition));
    //                    var count = query.Where(lamadaExtention.GetLambda()).Count();
    //                    if (count > 0)
    //                    {
    //                        nextNodeid = nextEdges[n].target;
    //                    }
    //                }
    //            }

    //            //获取下一节点的用户
    //            string roles = nodes.Where(x => x.nodeid == nextNodeid).SingleOrDefault().role;
    //            var users = await _context.SmUserRole.AsNoTracking().Where(x => x.IsDeleted == false && roles.Contains(x.SmRoleId.ToString())).Select(y => y.SmUserId).Distinct()
    //                .ToListAsync();

    //            //获取数据创建人姓名
    //            var user = await _context.SmUsers.AsNoTracking().Where(x => x.ID == ent.UpdateBy).FirstOrDefaultAsync();
    //            var userName = user?.UserName;

    //            #region 待办事项
    //            for (int n = 0; n < users.Count; n++)
    //            {

    //                SmSchedule smSchedule = new SmSchedule();
    //                smSchedule.UserId = users[n];
    //                smSchedule.Title = moduleInfo.ModuleName;
    //                smSchedule.Status = "Auditing";
    //                smSchedule.ModuleId = moduleId;
    //                smSchedule.MasterId = ent.ID;
    //                smSchedule.Content = "由【" + userName + "】于【" + Utility.GetSysDate() + "】提交，请您尽快处理！";
    //                smSchedule.UpdateBy = new Guid(User.Identity.Name);
    //                smSchedule.UpdateTime = Utility.GetSysDate();
    //                await _context.AddAsync(smSchedule);
    //            }
    //            #endregion
    //        }
    //        await _context.SaveChangesAsync();

    //        message = "已提交！";
    //        return ServiceResult.OprateSuccess(message);
    //    }
    //    catch (Exception)
    //    {
    //        throw;
    //    }
    //}
    #endregion

    #region 审批
    ///// <summary>
    ///// 审批
    ///// </summary>
    ///// <param name="moduleId">模块id</param>
    ///// <param name="entryList"></param>
    ///// <returns></returns>
    //[HttpPost]
    //public async Task<ServiceResult> Audit(Guid moduleId, List<T> entryList)
    //{
    //    string message = string.Empty;

    //    try
    //    {
    //        //获取模块信息
    //        var moduleInfo = await _context.SmModules.AsNoTracking().Where(x => x.IsDeleted == false && x.ID == moduleId).FirstOrDefaultAsync();
    //        if (moduleInfo is null)
    //            throw new Exception("无效的模块ID");

    //        for (int i = 0; i < entryList.Count; i++)
    //        {
    //            BasePoco<Guid> ent = entryList[i] as BasePoco<Guid>;
    //            if (ent.AuditStatus != "Auditing")
    //                throw new Exception("包含非“审批中”的数据，请重新选择！");

    //            var currentNode = ent.CurrentNode;

    //            //流程id
    //            var flow = await _context.SmProjectFlow
    //                .Where(x => x.IsDeleted == false && x.IsActive == true && x.SmModuleId == moduleId)
    //                .FirstOrDefaultAsync();
    //            var FlowId = flow?.ID;

    //            //该流程所有节点
    //            var nodes = await _context.SmNodes.Where(x => x.IsDeleted == false && x.SmProjectFlowId == FlowId).OrderBy(x => x.index)
    //                .ToListAsync();

    //            //该流程所有的线
    //            var edges = await _context.SmEdges.Where(x => x.IsDeleted == false && x.SmProjectFlowId == FlowId)
    //                .OrderBy(x => x.index).ToListAsync();

    //            //当前节点信息
    //            SmNode currentNodeInfo = nodes.Where(x => x.IsDeleted == false && x.nodeid == currentNode).FirstOrDefault();

    //            #region 审批
    //            //获取审批的节点
    //            string nextNodeid = string.Empty;
    //            //source为当前节点的线
    //            List<SmEdge> nextEdges = edges.Where(x => x.source == currentNodeInfo.nodeid).ToList();
    //            if (nextEdges.Count == 1)
    //            {
    //                nextNodeid = nextEdges[0].target;
    //            }
    //            else if (nextEdges.Count > 1)
    //            {
    //                for (int n = 0; n < nextEdges.Count; n++)
    //                {
    //                    var conditionField = nextEdges[n].ConditionField;
    //                    var condition = nextEdges[n].Condition;
    //                    var conditionValue = nextEdges[n].ConditionValue;

    //                    IQueryable<T> query = null;
    //                    query = _context.Set<T>();

    //                    var lamadaExtention = new LamadaExtention<T>();

    //                    lamadaExtention.GetExpression("ID", ent.ID, ExpressionType.Equal);
    //                    lamadaExtention.GetExpression(conditionField, conditionValue, GetExpressionType(condition));
    //                    var count = query.Where(lamadaExtention.GetLambda()).Count();
    //                    if (count > 0)
    //                    {
    //                        nextNodeid = nextEdges[n].target;
    //                    }
    //                }
    //            }


    //            #endregion

    //            #region 向下一环节添加待办事项
    //            //获取审批后下一节点
    //            string nextnextNodeid = string.Empty;
    //            //source为当前节点的线
    //            List<SmEdge> nextnextEdges = edges.Where(x => x.source == nextNodeid).ToList();
    //            if (nextnextEdges.Count == 1)
    //            {
    //                nextnextNodeid = nextnextEdges[0].target;
    //            }
    //            else if (nextnextEdges.Count > 1)
    //            {
    //                for (int n = 0; n < nextnextEdges.Count; n++)
    //                {
    //                    var conditionField = nextnextEdges[n].ConditionField;
    //                    var condition = nextnextEdges[n].Condition;
    //                    var conditionValue = nextnextEdges[n].ConditionValue;

    //                    IQueryable<T> query = null;
    //                    query = _context.Set<T>();

    //                    var lamadaExtention = new LamadaExtention<T>();

    //                    lamadaExtention.GetExpression("ID", ent.ID, ExpressionType.Equal);
    //                    lamadaExtention.GetExpression(conditionField, conditionValue, GetExpressionType(condition));
    //                    var count = query.Where(lamadaExtention.GetLambda()).Count();
    //                    if (count > 0)
    //                    {
    //                        nextnextNodeid = nextnextEdges[n].target;
    //                    }
    //                }
    //            }

    //            ent.CurrentNode = nextNodeid;
    //            if (!string.IsNullOrEmpty(nextnextNodeid))
    //            {
    //                //审批后下一个节点信息
    //                SmNode nextnextNodeInfo = nodes.Where(x => x.IsDeleted == false && x.nodeid == nextnextNodeid).SingleOrDefault();
    //                if (nextnextNodeInfo.label == "End")
    //                {
    //                    ent.CurrentNode = "End";
    //                    ent.AuditStatus = "CompleteAudit";
    //                }
    //            }

    //            var navigations = _context.Entry(entryList[i]).Navigations.ToList();
    //            for (int n = 0; n < navigations.Count; n++)
    //            {
    //                navigations[n].CurrentValue = null;
    //            }
    //            _context.Update(entryList[i]);

    //            string roles = nodes.Where(x => x.nodeid == nextnextNodeid).SingleOrDefault().role;
    //            var users = await _context.SmUserRole.AsNoTracking()
    //                .Where(x => x.IsDeleted == false && roles.Contains(x.SmRoleId.ToString()))
    //                .Select(y => y.SmUserId).Distinct()
    //                .ToListAsync();

    //            //获取数据上次修改人姓名
    //            var user = await _context.SmUsers.AsNoTracking().Where(x => x.ID == ent.UpdateBy).FirstOrDefaultAsync();
    //            var userName = user?.UserName;
    //            for (int n = 0; n < users.Count; n++)
    //            {
    //                SmSchedule smSchedule = new SmSchedule();
    //                smSchedule.UserId = users[n];
    //                smSchedule.Title = moduleInfo.ModuleName;
    //                smSchedule.Status = "Auditing";
    //                smSchedule.Path = moduleInfo.RoutePath + "/FormPage";
    //                smSchedule.ModuleId = moduleId;
    //                smSchedule.MasterId = ent.ID;
    //                smSchedule.Content = "【" + userName + "】于【" + Utility.GetSysDate() + "】审核转入，请您尽快处理！"; ;
    //                smSchedule.UpdateBy = new Guid(User.Identity.Name);
    //                smSchedule.UpdateTime = Utility.GetSysDate();
    //                await _context.AddAsync(smSchedule);
    //            }
    //            #endregion

    //        }

    //        await _context.SaveChangesAsync();

    //        message = "审批完成！";
    //        return ServiceResult.OprateSuccess(message);
    //    }
    //    catch (Exception)
    //    {
    //        throw;
    //    }

    //}

    [NonAction]
    public ExpressionType GetExpressionType(string conditionValue)
    {
        switch (conditionValue)
        {
            case "String":
                {
                    return ExpressionType.Contains;
                }
            case "Contains":
                {
                    return ExpressionType.Contains;
                }
            case "Equal":
                {
                    return ExpressionType.Equal;
                }
            case "GreaterThan":
                {
                    return ExpressionType.GreaterThan;
                }
            case "GreaterThanOrEqual":
                {
                    return ExpressionType.GreaterThanOrEqual;
                }
            case "LessThan":
                {
                    return ExpressionType.LessThan;
                }
            case "LessThanOrEqual":
                {
                    return ExpressionType.LessThanOrEqual;
                }
            default:
                {
                    return ExpressionType.Equal;
                }
        }
    }
    #endregion

    #region 动态更新表字段
    /// <summary>
    /// 更新指定实体
    /// </summary>
    /// <typeparam name="TEntity">数据表实体Model模型</typeparam>
    /// <param name="modelNew">动态Json数据</param>
    protected virtual void Update<TEntity>(dynamic modelNew)
    {
        //序列化动态Json为字符串
        string json = modelNew.ToString();

        //反序列化为数据表中的实体对象
        TEntity model = JsonHelper.JsonToObj<TEntity>(json);

        //自动设定修改日期和添加人
        if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
        {
            BasePoco ent = model as BasePoco;
            if (string.IsNullOrEmpty(ent.UpdateBy.ToString()))
            {
                ent.UpdateBy = new Guid(User.Identity.Name);
            }
            if (ent.UpdateTime == null)
                ent.UpdateTime = Utility.GetSysDate();
        }

        //if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(Model.Base.BasePoco<Guid>)))
        //{
        //    Model.Base.BasePoco<Guid> ent = model as Model.Base.BasePoco<Guid>;
        //    ent.UpdateBy = !string.IsNullOrEmpty(User.Identity.Name) ? new Guid(User.Identity.Name) : Guid.Empty;
        //    ent.UpdateTime = Utility.GetSysDate();
        //    //ent.ModificationNum = 0;
        //    //ent.Tag = 0;
        //    //ent.IsActive = true;
        //}

        //把状态全部变为不可更改
        _context.Entry(model).State = EntityState.Unchanged;

        //反序列化为动态对象中的属性
        var jsonModel = JsonHelper.JsonToObj<dynamic>(json);

        //定义一个List来添加属性
        List<string> listName = new List<string>();

        //动态添加要修改的字段
        foreach (PropertyInfo info in model.GetType().GetProperties())
        {
            //如果EF表中有实体对象，则排除，否则更新会报错,保留枚举
            if ((info.PropertyType.IsClass && info.PropertyType == typeof(String)) || info.PropertyType.IsClass == false)
            {
                //解决大小写问题
                foreach (var property in jsonModel)
                {
                    if (info.Name.ToLower().Trim() == property.Name.ToLower().Trim())
                    {
                        listName.Add(info.Name);
                    }
                }
            }
        }
        //寻找主键
        PropertyInfo pkProp = typeof(T).GetProperties().Where(p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0).FirstOrDefault();

        //遍历修改,并排除主键
        foreach (string Name in listName)
        {
            if (Name.ToLower() != pkProp.Name.ToLower())
            {
                _context.Entry(model).Property(Name).IsModified = true;
            }
        }

        if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
        {
            _context.Entry(model).Property("UpdateBy").IsModified = true;
            _context.Entry(model).Property("UpdateTime").IsModified = true;
        }

        //if (typeof(T).GetTypeInfo().Name == "SmUser")
        //{
        //    SmUsers ent = model as SmUsers;
        //    if (!string.IsNullOrEmpty(ent.PassWord))
        //    {
        //        ent.PassWord = Utility.GetMD5String(ent.PassWord);
        //        _context.Entry(model).Property("PassWord").IsModified = true;
        //    }
        //}

        //return db.SaveChanges();
    }

    /// <summary>
    /// 更新指定实体,不更新指定字段
    /// </summary>
    /// <typeparam name="TEntity">数据表实体Model模型</typeparam>
    /// <param name="modelNew">动态Json数据</param>
    /// <param name="fieldProNames">不更新的字段列表数组</param>
    protected virtual void Update<TEntity>(dynamic modelNew, string fieldProNames)
    {
        //序列化动态Json为字符串
        string json = modelNew.ToString();

        //反序列化为数据表中的实体对象
        TEntity model = JsonHelper.JsonToObj<TEntity>(json);

        //自动设定修改日期和添加人
        if (typeof(TEntity).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
        {
            var ent = model as BasePoco;
            if (string.IsNullOrEmpty(ent.UpdateBy.ToString()))
            {
                ent.UpdateBy = new Guid(User.Identity.Name);
            }
            if (ent.UpdateTime == null)
            {
                ent.UpdateTime = Utility.GetSysDate();
            }
        }

        //把状态全部变为不可更改
        _context.Entry(model).State = EntityState.Unchanged;

        //反序列化为动态对象中的属性
        var jsonModel = JsonHelper.JsonToObj<dynamic>(json);

        //定义一个List来添加属性
        List<string> listName = new List<string>();

        //动态添加要修改的字段
        foreach (PropertyInfo info in model.GetType().GetProperties())
        {
            //如果EF表中有实体对象，则排除，否则更新会报错,保留枚举
            if ((info.PropertyType.IsClass && info.PropertyType == typeof(String)) || info.PropertyType.IsClass == false)
            {
                //解决大小写问题
                foreach (var property in jsonModel)
                {
                    if (info.Name.ToLower().Trim() == property.Name.ToLower().Trim())
                    {
                        listName.Add(info.Name);
                    }
                }
            }
        }//寻找主键
        PropertyInfo pkProp = typeof(T).GetProperties().Where(p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0).FirstOrDefault();

        //遍历修改,并排除主键
        foreach (string Name in listName)
        {
            if (Name.ToLower() != pkProp.Name.ToLower() && !fieldProNames.Split(",").Select(n => n.ToLower()).Contains(Name.ToLower()))
            {
                _context.Entry(model).Property(Name).IsModified = true;
            }
        }
        if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
        {
            _context.Entry(model).Property("UpdateBy").IsModified = true;
            _context.Entry(model).Property("UpdateTime").IsModified = true;
        }

        //if (typeof(T).GetTypeInfo().Name == "SmUser")
        //{
        //    var ent = model as SmUsers;
        //    if (!string.IsNullOrEmpty(ent.PassWord))
        //    {
        //        ent.PassWord = Utility.GetMD5String(ent.PassWord);
        //        _context.Entry(model).Property("PassWord").IsModified = true;
        //    }
        //}
        //return db.SaveChanges();
    }

    /// <summary>
    /// 更新指定实体,不更新指定字段,如果每个表中有相同不更新的字段，可以这样写
    /// </summary>
    /// <typeparam name="TEntity">数据表实体Model模型</typeparam>
    /// <param name="modelNew">动态Json数据</param>
    protected virtual void UpdateSpecify<TEntity>(dynamic modelNew)
    {
        //序列化动态Json为字符串
        string json = modelNew.ToString();

        //反序列化为数据表中的实体对象
        TEntity model = JsonHelper.JsonToObj<TEntity>(json);

        //自动设定修改日期和添加人
        if (typeof(TEntity).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
        {
            BasePoco ent = model as BasePoco;
            if (string.IsNullOrEmpty(ent.UpdateBy.ToString()))
                ent.UpdateBy = new Guid(User.Identity.Name);
            if (ent.UpdateTime == null)
                ent.UpdateTime = Utility.GetSysDate();
        }

        //把状态全部变为不可更改
        _context.Entry(model).State = EntityState.Unchanged;

        //反序列化为动态对象中的属性
        var jsonModel = JsonHelper.JsonToObj<dynamic>(json);

        //定义一个List来添加属性
        List<string> listName = new List<string>();

        //定义不需要更新的字段
        string fieldProNames = "CreatedBy,CreatedTime,ImportDataId";

        //动态添加要修改的字段
        foreach (PropertyInfo info in model.GetType().GetProperties())
        {
            //如果EF表中有实体对象，则排除，否则更新会报错,保留枚举
            if ((info.PropertyType.IsClass && info.PropertyType == typeof(String)) || info.PropertyType.IsClass == false)
            {
                //解决大小写问题
                foreach (var property in jsonModel && !fieldProNames.Split(",").Select(n => n.ToLower()).Contains(info.Name.ToLower()))
                {
                    if (info.Name.ToLower().Trim() == property.Name.ToLower().Trim())
                    {
                        listName.Add(info.Name);
                    }
                }
            }
        }//寻找主键
        PropertyInfo pkProp = typeof(T).GetProperties().Where(p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0).FirstOrDefault();

        //遍历修改,并排除主键
        foreach (string Name in listName)
        {
            if (Name.ToLower() != pkProp.Name.ToLower())
            {
                _context.Entry(model).Property(Name).IsModified = true;
            }
        }
        if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
        {
            _context.Entry(model).Property("UpdateBy").IsModified = true;
            _context.Entry(model).Property("UpdateTime").IsModified = true;
        }

        //if (typeof(T).GetTypeInfo().Name == "SmUser")
        //{
        //    var ent = model as SmUsers;
        //    if (!string.IsNullOrEmpty(ent.PassWord))
        //    {
        //        ent.PassWord = Utility.GetMD5String(ent.PassWord);
        //        _context.Entry(model).Property("PassWord").IsModified = true;
        //    }
        //}
        //return db.SaveChanges();
    }

    /// <summary>
    /// 更新指定实体,不更新指定字段,如果每个表中有相同不更新的字段，可以这样写,扩展方法
    /// </summary>
    /// <typeparam name="TEntity">数据表实体Model模型</typeparam>
    /// <param name="modelNew">动态Json数据</param>
    /// <param name="fieldProNames">不更新的字段列表数组</param>
    protected virtual void UpdateSpecify<TEntity>(dynamic modelNew, string fieldProNames)
    {
        //序列化动态Json为字符串
        string json = modelNew.ToString();

        //反序列化为数据表中的实体对象
        TEntity model = JsonHelper.JsonToObj<TEntity>(json);

        //自动设定修改日期和添加人
        if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
        {
            BasePoco ent = model as BasePoco;
            if (string.IsNullOrEmpty(ent.UpdateBy.ToString()))
                ent.UpdateBy = new Guid(User.Identity.Name);
            if (ent.UpdateTime == null)
                ent.UpdateTime = Utility.GetSysDate();
        }

        //把状态全部变为不可更改
        _context.Entry(model).State = EntityState.Unchanged;

        //反序列化为动态对象中的属性
        var jsonModel = JsonHelper.JsonToObj<dynamic>(json);

        //定义一个List来添加属性
        List<string> listName = new List<string>();

        //定义不需要更新的字段
        string fieldProNameses = "CreatedBy,CreatedTime,ImportDataId," + fieldProNames;

        //动态添加要修改的字段
        foreach (PropertyInfo info in model.GetType().GetProperties())
        {
            //如果EF表中有实体对象，则排除，否则更新会报错,保留枚举
            if ((info.PropertyType.IsClass && info.PropertyType == typeof(String)) || info.PropertyType.IsClass == false)
            {
                //解决大小写问题
                foreach (var property in jsonModel && !fieldProNameses.Split(",").Select(n => n.ToLower()).Contains(info.Name.ToLower()))
                {
                    if (info.Name.ToLower().Trim() == property.Name.ToLower().Trim())
                    {
                        listName.Add(info.Name);
                    }
                }
            }
        }

        //寻找主键
        PropertyInfo pkProp = typeof(T).GetProperties().Where(p => p.GetCustomAttributes(typeof(KeyAttribute), false).Length > 0).FirstOrDefault();

        //遍历修改,并排除主键
        foreach (string Name in listName)
        {
            if (Name.ToLower() != pkProp.Name.ToLower())
            {
                _context.Entry(model).Property(Name).IsModified = true;
            }
        }
        if (typeof(T).GetTypeInfo().IsSubclassOf(typeof(BasePoco)))
        {
            _context.Entry(model).Property("UpdateBy").IsModified = true;
            _context.Entry(model).Property("UpdateTime").IsModified = true;
        }

        //if (typeof(T).GetTypeInfo().Name == "SmUser")
        //{
        //    SmUser ent = model as SmUser;
        //    if (!string.IsNullOrEmpty(ent.PassWord))
        //    {
        //        ent.PassWord = Utility.GetMD5String(ent.PassWord);
        //        _context.Entry(model).Property("PassWord").IsModified = true;
        //    }
        //}
        //return db.SaveChanges();
    }
    #endregion

    #region 自定义列模块数据返回
    /// <summary>
    /// 自定义列模块数据返回
    /// </summary>
    /// <param name="paramData"></param>
    /// <param name="moduleCode"></param>
    /// <param name="sorter"></param>
    /// <param name="filter"></param>
    /// <param name="parentColumn"></param>
    /// <param name="parentId"></param>
    /// <returns></returns>
    [HttpGet]
    public virtual IActionResult GetGridList(string paramData, string moduleCode, string sorter = "{}", string filter = "{}", string parentColumn = null, string parentId = null)
    {
        dynamic obj = new ExpandoObject();
        string status = "error";
        string message = string.Empty;
        int current = 1;
        int pageSize = 20;
        int total = 0;

        //try
        //{
        var searchParam = JsonHelper.JsonToObj<Dictionary<string, object>>(paramData);
        var filterParam = JsonHelper.JsonToObj<Dictionary<string, object>>(filter);
        var sorterParam = JsonHelper.JsonToObj<Dictionary<string, string>>(sorter);

        string queryCodition = "1=1";
        string keyWord = string.Empty;

        #region 处理查询条件
        ModuleSqlColumn moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        List<SmModuleColumnExtend> moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

        foreach (var item in searchParam)
        {
            if (item.Key == "current")
            {
                current = int.Parse(item.Value.ToString());
                continue;
            }
            else if (item.Key == "pageSize")
            {
                pageSize = int.Parse(item.Value.ToString());
                continue;
            }
            else if (item.Key == "_timestamp")
            {
                continue;
            }
            else if (item.Key == "keyWord")
            {
                keyWord = item.Value.ToString();
                continue;
            }
            else if (!string.IsNullOrEmpty(item.Value.ToString()))
            {
                if (moduleColumns.Any())
                {
                    var column = moduleColumns.Where(a => a.DataIndex == item.Key).FirstOrDefault();
                    if (column != null)
                        queryCodition += " AND " + column.TableAlias + "." + item.Key + " like '%" + item.Value.ToString() + "%'";
                }
                else
                    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";
            }
            //if (string.IsNullOrEmpty(item.Value.ToString()))
            //    queryCodition += " AND A." + item.Key + " =''";
            //else
            //    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";
        }
        if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(parentColumn))
            queryCodition += " AND A." + parentColumn + " = '" + parentId + "'";
        #endregion

        #region 处理过滤条件
        foreach (var item in filterParam)
        {
            if (!string.IsNullOrEmpty(item.Value.ToString()))
                queryCodition += " AND A." + item.Key + " = '" + item.Value.ToString() + "'";
        }
        #endregion

        #region 处理关键字搜索
        string keyWordCondition = string.Empty;
        if (!string.IsNullOrEmpty(keyWord) && moduleColumns.Any())
            moduleColumns.ForEach(item =>
            {
                if (item.ValueType == null && item.HideInSearch == false)
                {
                    string TableAlias = item.TableAlias;
                    string dataIndex = item.DataIndex;
                    if (string.IsNullOrEmpty(keyWordCondition))
                        keyWordCondition = TableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                    else
                        keyWordCondition += " OR " + TableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                }
            });
        #endregion

        string userId = string.Empty;
        ModuleSql moduleSql = new ModuleSql(moduleCode);
        GridList grid = new GridList();
        string tableName = moduleSql.GetTableName();
        string SqlSelectBrwAndTable = moduleSql.GetSqlSelectBrwAndTable();
        string SqlSelectAndTable = moduleSql.GetSqlSelectAndTable();
        if (!string.IsNullOrEmpty(tableName))
        {
            SqlSelectBrwAndTable = string.Format(SqlSelectBrwAndTable, tableName);
            SqlSelectAndTable = string.Format(SqlSelectAndTable, tableName);
        }
        string SqlDefaultCondition = moduleSql.GetSqlDefaultCondition();

        #region 处理关键字搜索
        if (!string.IsNullOrEmpty(keyWordCondition))
            SqlDefaultCondition += " AND (" + keyWordCondition + ")";
        #endregion

        //SqlDefaultCondition = SqlDefaultCondition.Replace("[UserId]", userId);
        string DefaultSortField = moduleSql.GetDefaultSortField();
        string DefaultSortDirection = moduleSql.GetDefaultSortDirection();
        if (string.IsNullOrEmpty(DefaultSortDirection))
        {
            DefaultSortDirection = "ASC";
        }
        grid.SqlSelect = SqlSelectBrwAndTable;
        grid.SqlDefaultCondition = SqlDefaultCondition;
        grid.SqlQueryCondition = queryCodition;
        grid.SortField = DefaultSortField;
        grid.SortDirection = DefaultSortDirection;

        #region 处理排序
        if (sorterParam.Count > 0)
            foreach (var item in sorterParam)
            {
                grid.SortField = item.Key;
                if (item.Value == "ascend")
                    grid.SortDirection = "ASC";
                else if (item.Value == "descend")
                    grid.SortDirection = "DESC";
            }
        #endregion

        grid.PageSize = pageSize;
        grid.CurrentPage = current;
        grid.ModuleCode = moduleCode;
        total = grid.GetTotalCount();
        string sql = grid.GetQueryString();
        DataTable dtTemp = DBHelper.GetDataTable(sql);
        DataTable dt = Utility.FormatDataTableForTree(moduleCode, userId, dtTemp);
        obj.data = dt;
        status = "ok";
        message = "查询成功！";
        //}
        //catch (Exception E)
        //{
        //    message = E.Message;
        //}

        obj.current = current;
        obj.pageSize = pageSize;
        obj.total = total;
        obj.status = status;
        obj.message = message;
        return Ok(obj);
    }

    /// <summary>
    /// 自定义列模块数据返回
    /// </summary>
    /// <param name="paramData"></param>
    /// <param name="moduleCode"></param>
    /// <param name="sorter"></param>
    /// <param name="filter"></param>
    /// <param name="parentColumn"></param>
    /// <param name="parentId"></param>
    /// <returns></returns>
    [HttpGet]
    public virtual ServiceResult<DataTable> GetPageData(string paramData, string moduleCode, [FromFilter] QueryFilter filter, string sorter = "{}", string parentColumn = null, string parentId = null)
    {
        int current = 1;
        int pageSize = 20;
        int total = 0;

        //try
        //{
        string filter1 = "{}";
        var searchParam = JsonHelper.JsonToObj<Dictionary<string, object>>(paramData);
        var filterParam = JsonHelper.JsonToObj<Dictionary<string, object>>(filter1);
        var sorterParam = JsonHelper.JsonToObj<Dictionary<string, string>>(sorter);

        string queryCodition = "1=1";
        string keyWord = string.Empty;

        #region 处理查询条件
        ModuleSqlColumn moduleColumnInfo = new ModuleSqlColumn(moduleCode);
        List<SmModuleColumnExtend> moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

        foreach (var item in searchParam)
        {
            if (item.Key == "current")
            {
                current = int.Parse(item.Value.ToString());
                continue;
            }
            else if (item.Key == "pageSize")
            {
                pageSize = int.Parse(item.Value.ToString());
                continue;
            }
            else if (item.Key == "_timestamp")
            {
                continue;
            }
            else if (item.Key == "keyWord")
            {
                keyWord = item.Value.ToString();
                continue;
            }
            else if (!string.IsNullOrEmpty(item.Value.ToString()))
            {
                if (moduleColumns.Any())
                {
                    var column = moduleColumns.Where(a => a.DataIndex == item.Key).FirstOrDefault();
                    if (column != null)
                        queryCodition += " AND " + column.TableAlias + "." + item.Key + " like '%" + item.Value.ToString() + "%'";
                }
                else
                    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";
            }
            //if (string.IsNullOrEmpty(item.Value.ToString()))
            //    queryCodition += " AND A." + item.Key + " =''";
            //else
            //    queryCodition += " AND A." + item.Key + " like '%" + item.Value.ToString() + "%'";
        }
        if (!string.IsNullOrEmpty(parentId) && !string.IsNullOrEmpty(parentColumn))
            queryCodition += " AND A." + parentColumn + " = '" + parentId + "'";
        #endregion

        #region 处理过滤条件
        foreach (var item in filterParam)
        {
            if (!string.IsNullOrEmpty(item.Value.ToString()))
                queryCodition += " AND A." + item.Key + " = '" + item.Value.ToString() + "'";
        }
        #endregion

        #region 处理关键字搜索
        string keyWordCondition = string.Empty;
        if (!string.IsNullOrEmpty(keyWord) && moduleColumns.Any())
            moduleColumns.ForEach(item =>
            {
                if (item.ValueType == null && item.HideInSearch == false)
                {
                    string TableAlias = item.TableAlias;
                    string dataIndex = item.DataIndex;
                    if (string.IsNullOrEmpty(keyWordCondition))
                        keyWordCondition = TableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                    else
                        keyWordCondition += " OR " + TableAlias + "." + dataIndex + " LIKE '%" + keyWord + "%'";
                }
            });
        #endregion

        string userId = string.Empty;
        ModuleSql moduleSql = new ModuleSql(moduleCode);
        GridList grid = new GridList();
        string tableName = moduleSql.GetTableName();
        string SqlSelectBrwAndTable = moduleSql.GetSqlSelectBrwAndTable();
        string SqlSelectAndTable = moduleSql.GetSqlSelectAndTable();
        if (!string.IsNullOrEmpty(tableName))
        {
            SqlSelectBrwAndTable = string.Format(SqlSelectBrwAndTable, tableName);
            SqlSelectAndTable = string.Format(SqlSelectAndTable, tableName);
        }
        string SqlDefaultCondition = moduleSql.GetSqlDefaultCondition();

        #region 处理关键字搜索
        if (!string.IsNullOrEmpty(keyWordCondition))
            SqlDefaultCondition += " AND (" + keyWordCondition + ")";
        #endregion

        //SqlDefaultCondition = SqlDefaultCondition.Replace("[UserId]", userId);
        string DefaultSortField = moduleSql.GetDefaultSortField();
        string DefaultSortDirection = moduleSql.GetDefaultSortDirection();
        if (string.IsNullOrEmpty(DefaultSortDirection))
        {
            DefaultSortDirection = "ASC";
        }
        grid.SqlSelect = SqlSelectBrwAndTable;
        grid.SqlDefaultCondition = SqlDefaultCondition;
        grid.SqlQueryCondition = queryCodition;
        grid.SortField = DefaultSortField;
        grid.SortDirection = DefaultSortDirection;

        #region 处理排序
        if (sorterParam.Count > 0)
            foreach (var item in sorterParam)
            {
                grid.SortField = item.Key;
                if (item.Value == "ascend")
                    grid.SortDirection = "ASC";
                else if (item.Value == "descend")
                    grid.SortDirection = "DESC";
            }
        #endregion

        grid.PageSize = pageSize;
        grid.CurrentPage = current;
        grid.ModuleCode = moduleCode;
        total = grid.GetTotalCount();
        string sql = grid.GetQueryString();
        DataTable dtTemp = DBHelper.GetDataTable(sql);
        DataTable dt = Utility.FormatDataTableForTree(moduleCode, userId, dtTemp);
        //obj.data = dt;
        //}
        //catch (Exception E)
        //{
        //    message = E.Message;
        //}

        //obj.current = current;
        //obj.pageSize = pageSize;
        //obj.total = total;
        //obj.status = status;
        //obj.message = message;
        //return Ok(obj);
        return ServiceResult<DataTable>.OprateSuccess(dt, total, ResponseText.QUERY_SUCCESS);
    }

    #endregion

    #region 获得当前公司ID
    /// <summary>
    /// 获得当前公司ID
    /// </summary>
    /// <returns></returns>
    public static string GetCompanyId()
    {
        try
        {
            return Utility.GetCompanyId();
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion

    #region 获得当前集团ID
    /// <summary>
    /// 获得当前集团ID
    /// </summary>
    /// <returns></returns>
    public static string GetGroupId()
    {
        try
        {
            return Utility.GetGroupId();
        }
        catch (Exception)
        {
            throw;
        }
    }
    #endregion
}
