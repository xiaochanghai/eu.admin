/*  代码由框架生成,任何更改都可能导致被代码生成器覆盖，可自行修改。
* SmModules.cs
*
*功 能： N / A
* 类 名： SmModules
*
* Ver    变更日期 负责人  变更内容
* ───────────────────────────────────
*V1.0  2024/4/20 23:12:41  SimonHsiao   初版
*
* Copyright(c) 2024 SUZHOU EU Corporation. All Rights Reserved.
*┌──────────────────────────────────┐
*│　此技术信息为本公司机密信息，未经本公司书面同意禁止向第三方披露．　│
*│　版权所有：苏州一优信息技术有限公司                                │
*└──────────────────────────────────┘
*/

namespace EU.Core.Services;

/// <summary>
/// 系统模板 (服务)
/// </summary>
public class SmModulesServices : BaseServices<SmModules, SmModulesDto, InsertSmModulesInput, EditSmModulesInput>, ISmModulesServices
{
    RedisCacheService RedisCacheService = new(1);
    ISmRoleFunctionServices _smRoleFunctionServices;
    ISmUserModuleColumnServices _smUserModuleColumnServices;
    ISmRolesServices _smRolesServices;
    ISmModuleColumnServices _smModuleColumnServices;
    private string userId = Utility.GetUserIdString();
    private readonly IBaseRepository<SmModules> _dal;
    public SmModulesServices(IBaseRepository<SmModules> dal,
        ISmRoleFunctionServices smRoleFunctionServices,
        ISmUserModuleColumnServices smUserModuleColumnServices,
        ISmRolesServices smRolesServices,
        DataContext context,
        ISmModuleColumnServices smModuleColumnServices)
    {
        this._dal = dal;
        base.BaseDal = dal;
        base._context = context;
        _smRoleFunctionServices = smRoleFunctionServices;
        _smUserModuleColumnServices = smUserModuleColumnServices;
        _smRolesServices = smRolesServices;
        _smModuleColumnServices = smModuleColumnServices;
    }

    #region 新增
    public override async Task<Guid> Add(object entity)
    {
        var id = await base.Add(entity);

        ModuleInfo.Init();

        return id;
    }
    #endregion

    #region 获取左侧菜单
    /// <summary>
    /// 左侧菜单递归
    /// </summary>
    /// <param name="roleModule"></param>
    /// <param name="smModules"></param>
    /// <param name="moduleTree"></param>
    public static void LoopToAppendChildren(List<Guid?> roleModule, List<SmModules> smModules, TreeMenuData moduleTree)
    {
        var subItems = new List<TreeMenuData>();
        if (string.IsNullOrEmpty(moduleTree.id))
        {
            subItems = smModules.Where(x => x.IsParent == true && string.IsNullOrEmpty(x.ParentId.ToString()) && roleModule.Contains(x.ID)).OrderBy(x => x.TaxisNo).Select(y => new TreeMenuData
            {
                id = y.ID.ToString(),
                path = y.RoutePath,
                name = y.ModuleName,
                icon = y.Icon,
                component = y.RoutePath
            }).ToList();
        }
        else
        {
            subItems = smModules.Where(x => x.ParentId == Guid.Parse(moduleTree.id) && roleModule.Contains(x.ID)).OrderBy(x => x.TaxisNo).Select(y => new TreeMenuData
            {
                id = y.ID.ToString(),
                path = y.RoutePath,
                name = y.ModuleName,
                icon = y.Icon,
                component = y.RoutePath
            }).ToList();
        }
        moduleTree.children = [.. subItems];
        foreach (var subItem in subItems)
        {
            LoopToAppendChildren(roleModule, smModules, subItem);
        }
    }

    public static void LoopToAppendChildren1(List<Guid?> roleModule, List<SmModules> smModules, TreeAuthMenu moduleTree)
    {
        var subItems = new List<TreeAuthMenu>();
        if (string.IsNullOrEmpty(moduleTree.id))
        {
            subItems = smModules.Where(x => x.IsParent == true && string.IsNullOrEmpty(x.ParentId.ToString()) && roleModule.Contains(x.ID)).OrderBy(x => x.TaxisNo).Select(y => new TreeAuthMenu
            {
                id = y.ID.ToString(),
                path = y.RoutePath,
                redirect = y.Element,
                meta = new TreeAuthMenuMeta
                {
                    key = y.ModuleCode,
                    icon = y.Icon,
                    title = y.ModuleName,
                    isHide = (y.ParentId == null || y.IsActive == true) ? false : true
                }
            }).ToList();
        }
        else
        {
            subItems = smModules.Where(x => x.ParentId == Guid.Parse(moduleTree.id) && roleModule.Contains(x.ID)).OrderBy(x => x.TaxisNo).Select(y => new TreeAuthMenu
            {
                id = y.ID.ToString(),
                path = y.RoutePath,
                element = y.Element,
                meta = new TreeAuthMenuMeta
                {
                    key = y.ModuleCode,
                    icon = y.Icon,
                    title = y.ModuleName,
                    isHide = (y.ParentId == null || y.IsActive == true) ? false : true
                }
            }).ToList();
        }

        if (subItems.Any())
            moduleTree.redirect = subItems[0].path;
        moduleTree.children = [.. subItems];
        foreach (var subItem in subItems)
        {
            LoopToAppendChildren1(roleModule, smModules, subItem);
        }
    }

    /// <summary>
    /// 获取用户有权限的目录及子模块
    /// </summary>
    /// <param name="smModules"></param>
    /// <param name="smRoleList"></param>
    public static void LoopGetRoleModule(List<SmModules> smModules, List<Guid?> smRoleList)
    {
        for (int i = 0; i < smModules.Count; i++)
        {
            smRoleList.Add(smModules[i].ID);
            if (smModules[i].ParentId != null && smModules[i].ParentId != Guid.Empty)//递归获取子模块的上级目录
            {
                var moduleList = ModuleInfo.GetModuleList();
                var data = moduleList.Where(x => x.ID == smModules[i].ParentId && x.IsDeleted == false && x.IsActive == true).ToList();
                LoopGetRoleModule(data, smRoleList);
            }
        }
    }

    /// <summary>
    /// 获取左侧菜单
    /// </summary>
    /// <returns></returns>
    public async Task<ServiceResult<List<TreeMenuData>>> GetMenuData()
    {
        var _menus = await RedisCacheService.GetAsync<List<TreeMenuData>>(userId, "UserMenu");
        if (_menus != null && _menus.Any())
            return ServiceResult<List<TreeMenuData>>.OprateSuccess(_menus, ResponseText.QUERY_SUCCESS);
        else
        {
            //获取用户角色有权限的子模块
            //var roleModule = _context.Set<SmRoleModule>()
            //    .Join(_context.Set<SmUserRole>(), x => x.SmRoleId, y => y.SmRoleId, (x, y) => new { x, y })
            //    .Where(z => z.y.SmUserId.ToString() == User.Identity.Name && z.x.IsDeleted == false && z.y.IsDeleted == false)
            //    .Join(_context.SmModules, a => a.x.SmModuleId, b => b.ID, (a, b) => new { a, b }).Select(c => c.b)
            //    .ToList();

            var roleModule = new List<SmModules>();
            string sql = @"SELECT DISTINCT C.*
                                    FROM SmRoleModule A
                                         JOIN SmUserRole_V B
                                            ON     A.SmRoleId = B.SmRoleId
                                               AND B.SmUserId = '{0}'
                                         JOIN SmModules C ON A.SmModuleId = C.ID AND C.IsDeleted = 'false' AND C.BelongModuleId IS NULL
                                    WHERE A.IsDeleted = 'false'";
            sql = string.Format(sql, userId);
            roleModule = DBHelper.QueryList<SmModules>(sql);

            var smRoleList = new List<Guid?>();
            LoopGetRoleModule(roleModule, smRoleList);//递归获取有权限的子模块的上级目录

            TreeMenuData treeMenuData = new();
            var moduleList = ModuleInfo.GetModuleList();
            var smModules = moduleList.Where(x => x.IsDeleted == false && x.IsActive == true).ToList();
            LoopToAppendChildren(smRoleList, smModules, treeMenuData);//将模块递归成树
            var data = new List<TreeMenuData>
            {
                new  ()
                {
                    id = Guid.NewGuid().ToString(),
                    path = "/",
                    name = "首页",
                    icon = "home",
                    component = "/"
                }
            };
            data = data.Concat(treeMenuData.children).ToList();
            RedisCacheService.AddObject(userId, "UserMenu", data);
            _menus = data;
            return ServiceResult<List<TreeMenuData>>.OprateSuccess(_menus, ResponseText.QUERY_SUCCESS);

        }
    }


    /// <summary>
    /// 获取左侧菜单
    /// </summary>
    /// <returns></returns>
    public async Task<ServiceResult<List<TreeAuthMenu>>> GetAuthMenu()
    {
        var _menus = await RedisCacheService.GetAsync<List<TreeAuthMenu>>(userId, "UserAuthMenu");
        if (_menus != null && _menus.Any())
            return ServiceResult<List<TreeAuthMenu>>.OprateSuccess(_menus, ResponseText.QUERY_SUCCESS);
        else
        {
            //获取用户角色有权限的子模块
            //var roleModule = _context.Set<SmRoleModule>()
            //    .Join(_context.Set<SmUserRole>(), x => x.SmRoleId, y => y.SmRoleId, (x, y) => new { x, y })
            //    .Where(z => z.y.SmUserId.ToString() == User.Identity.Name && z.x.IsDeleted == false && z.y.IsDeleted == false)
            //    .Join(_context.SmModules, a => a.x.SmModuleId, b => b.ID, (a, b) => new { a, b }).Select(c => c.b)
            //    .ToList();

            var roleModule = new List<SmModules>();
            string sql = @"SELECT DISTINCT C.*
                                    FROM SmRoleModule A
                                         JOIN SmUserRole_V B
                                            ON     A.SmRoleId = B.SmRoleId
                                               AND B.SmUserId = '{0}'
                                         JOIN SmModules C ON A.SmModuleId = C.ID AND C.IsDeleted = 'false' AND C.BelongModuleId IS NULL
                                    WHERE A.IsDeleted = 'false'";
            sql = string.Format(sql, userId);
            roleModule = await Db.Ado.SqlQueryAsync<SmModules>(sql);

            var smRoleList = new List<Guid?>();
            LoopGetRoleModule(roleModule, smRoleList);//递归获取有权限的子模块的上级目录

            var treeMenuData = new TreeAuthMenu();
            var moduleList = ModuleInfo.GetModuleList();
            var smModules = moduleList.Where(x => x.IsDeleted == false && x.IsActive == true).ToList();
            LoopToAppendChildren1(smRoleList, smModules, treeMenuData);//将模块递归成树
            var data = new List<TreeAuthMenu>
            {
                new  ()
                {
                    id = "7fc933e1-0baa-4c71-bfb6-a49ade977ad5",
                    path = "/home/index",
                    element= "/home/index",
                    meta = new TreeAuthMenuMeta
                    {
                        key = "home",
                        icon = "HomeOutlined",
                        title = "首页",
                        isAffix =true
                    },
                    children = default
                },
                new  ()
                {
                    id = "39576ba6-45ad-4e88-9867-9699cc8d077f",
                    path = "/account/settings/index",
                    element= "/account/settings/index",
                    meta = new TreeAuthMenuMeta
                    {
                        key = "account_setting",
                        icon = "UserOutlined",
                        title = "个人信息",
                        isHide =true
                    },
                    children = default
                },
                new ()
                {
                    id = "39576ba6-45ad-4e88-9867-9699cc8d078f",
                    path = "/system/config",
                    element= "/system/config/form/index",
                    meta = new TreeAuthMenuMeta
                    {
                        key = "account_setting",
                        icon = "UserOutlined",
                        title = "表单配置",
                        isHide = true,
                        isFull = true
                    },
                    children = default
                },
                new ()
                {
                    id = "39576ba6-45ad-4e88-9867-9699cc8d079f",
                    path = "/workflow",
                    element= "/system/workflow/index",
                    meta = new TreeAuthMenuMeta
                    {
                        key = "account_setting",
                        icon = "UserOutlined",
                        title = "工作流",
                        isHide = true,
                        isFull = true
                    },
                    children = default
                }
            };
            data = data.Concat(treeMenuData.children).ToList();
            RedisCacheService.AddObject(userId, "UserAuthMenu", data);
            _menus = data;
            return ServiceResult<List<TreeAuthMenu>>.OprateSuccess(_menus, ResponseText.QUERY_SUCCESS);

        }
    }
    #endregion

    #region 获取模块信息
    /// <summary>
    /// 获取模块信息
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    public async Task<dynamic> GetModuleInfo(string moduleCode)
    {
        dynamic result = new ExpandoObject();
        dynamic obj = new ExpandoObject();
        string message = string.Empty;
        string userId = Utility.GetUserIdString();

        try
        {
            //获取模块信息
            var module = ModuleInfo.GetModuleInfo(moduleCode);

            if (module == null)
                throw new Exception("未查询到模块【" + moduleCode + "】相关配置信息！");

            var moduleColumnInfo = new ModuleSqlColumn(module.ModuleCode);
            var moduleId = module.ID.ToString().ToLower();
            obj.columns = GetModuleColumn(Guid.Parse(moduleId), module);

            var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

            moduleColumns.ForEach(x =>
            {
                x.FormTitle = x.FormTitle ?? x.Title;
                x.FromTaxisNo = x.FromTaxisNo ?? x.TaxisNo;

            });
            obj.formColumns = Mapper.Map(moduleColumns.OrderBy(x => x.FromTaxisNo)).ToANew<List<SmModuleForm>>();

            var privileges = FunctionPrivilege.Query(moduleCode);
            var ids = privileges.Select(x => x.ID).ToList();
            var functions = RedisCacheService.Get<List<SmRoleFunctionExtend>>(userId, "UserFunction." + moduleCode);
            if (functions == null)
            {
                functions = await _context.SmUserRole.Where(x => x.IsDeleted == false && x.SmUserId == Guid.Parse(userId))
                    .Join(_context.SmRoleFunction, a => a.SmRoleId, b => b.SmRoleId, (a, b) => new { a, b }).Where(y => y.b.IsDeleted == false && (module.ID == y.b.SmModuleId || (y.b.SmFunctionId != null && ids.Contains(y.b.SmFunctionId.Value))))
                    .Select(z => new SmRoleFunctionExtend
                    {
                        SmFunctionId = z.b.SmFunctionId,
                        NoActionCode = z.b.NoActionCode,
                        SmModuleId = z.b.SmModuleId,
                        ActionCode = z.b.ActionCode
                    }).Distinct().ToListAsync();

                RedisCacheService.AddObject(userId, "UserFunction." + moduleCode, functions);
            }

            //获取有权限的基本按钮
            var actions = functions.Where(x => x.SmModuleId != null && !string.IsNullOrWhiteSpace(x.ActionCode)).ToList();

            //获取操作栏按钮
            var actionData = functions
                 .Join(privileges, a => a.SmFunctionId, b => b.ID, (a, b) => new { a, b })
                 .Where(z => z.b.SmModuleId == module.ID && z.b.DisplayPosition == "Action")
                 .Select(y => y.b)
                 .OrderBy(y => y.TaxisNo).Distinct().ToList();

            //获取菜单栏按钮
            var menuData = functions
                .Join(privileges, a => a.SmFunctionId, b => b.ID, (a, b) => new { a, b })
                .Where(z => z.b.SmModuleId == module.ID && z.b.DisplayPosition == "Menu")
                .Select(y => y.b)
                .OrderBy(y => y.TaxisNo).ToList();

            //获取隐藏菜单栏按钮
            var hideMenu = functions
                .Join(privileges, a => a.SmFunctionId, b => b.ID, (a, b) => new { a, b })
                .Where(z => z.b.SmModuleId == module.ID && z.b.DisplayPosition == "HideMenu")
                .Select(y => y.b)
                .OrderBy(y => y.TaxisNo).ToList();

            //获取自定义操作按钮
            var customActionData = functions
                 .Join(privileges, a => a.SmFunctionId, b => b.ID, (a, b) => new { a, b })
                 .Where(z => z.b.SmModuleId == module.ID && z.b.DisplayPosition == "Custom")
                 .Select(y => y.b)
                 .OrderBy(y => y.TaxisNo).Distinct().ToList();

            if (module.IsShowAdd == false)
                actions = actions.Where(x => x.ActionCode != "Add").ToList();

            if (module.IsShowDelete == false)
                actions = actions.Where(x => x.ActionCode != "Delete").ToList();

            if (module.IsShowUpdate == false)
                actions = actions.Where(x => x.ActionCode != "Update").ToList();

            if (module.IsShowView == false)
                actions = actions.Where(x => x.ActionCode != "View").ToList();

            if (module.IsShowSubmit == false)
                actions = actions.Where(x => x.ActionCode != "Submit").ToList();

            if (module.IsShowBatchDelete == false)
                actions = actions.Where(x => x.ActionCode != "BatchDelete").ToList();

            #region 操作栏按钮个数
            var allAction = new List<ActionActions>();//显示的按钮
            var actionCount = actionData.Count;
            if (actions.Any(x => x.ActionCode == "Update"))
            {
                allAction.Add(new ActionActions
                {
                    id = "Update",
                    taxisNo = 100
                });
                actionCount++;
            }
            if (actions.Any(x => x.ActionCode == "View"))
            {
                allAction.Add(new ActionActions
                {
                    id = "View",
                    taxisNo = 200
                });
                actionCount++;
            }
            if (actions.Any(x => x.ActionCode == "Delete"))
            {
                allAction.Add(new ActionActions
                {
                    id = "Delete",
                    taxisNo = 300
                });
                actionCount++;
            }
            #endregion

            #region 操作栏下拉计算
            var beforeActions = new List<ActionActions>();
            var dropActions = new List<ActionActions>();

            for (int i = 0; i < actionData.Count; i++)
            {
                allAction.Add(new ActionActions
                {
                    id = actionData[i].ID.ToString(),
                    taxisNo = actionData[i].TaxisNo
                });
            }

            if (actionCount > 3)
            {
                beforeActions = allAction.Take(2).ToList();
                dropActions = allAction.Skip(2).Take(actionCount - 2).ToList();
            }
            else
                beforeActions = allAction.Take(3).ToList();
            #endregion

            #region 获取模块子模块
            var modules = await base.Query(x => x.BelongModuleId == module.ID && x.IsActive == true, "TaxisNo ASC");

            var children = new JArray();
            modules.ForEach(x =>
            {
                JObject item = new JObject
                {
                    new JProperty("ID", x.ID),
                    new JProperty("ModuleCode", x.ModuleCode),
                    new JProperty("ModuleName", x.ModuleName),
                };
                children.Add(item);
            });

            #endregion

            obj.children = children;//子模块

            #region Master栏位
            obj.masterColumn = null;
            if (moduleColumns.Any(x => x.IsMasterId == true))
                obj.masterColumn = moduleColumns.FirstOrDefault(x => x.IsMasterId == true)?.DataIndex;
            #endregion

            obj.beforeActions = beforeActions;//操作栏前面平铺按钮
            obj.dropActions = dropActions;//操作栏下拉区域按钮
            obj.actions = actions.Select(x => x.ActionCode).ToList();//显示的按钮
            obj.actionData = actionData;

            obj.menuData = menuData;//菜单栏按钮
            obj.customActionData = customActionData;//菜单栏按钮
            obj.hideMenu = hideMenu;//菜单栏隐藏区按钮
            obj.actionCount = actionCount;//操作栏个数
            obj.moduleId = moduleId;//模块代码
            obj.isDetail = module.IsDetail;//是否从表
            obj.openType = module.OpenType;//打开方式
            obj.formPage = module.FormPage;//编辑页路径
            obj.formPageWidth = module.FormPageWidth ?? 720;//编辑页宽度
                                                            //obj.detailModules = detailModules;//需要显示的从表
            obj.moduleCode = moduleCode;//模块代码
            obj.moduleName = module.ModuleName;//模块名称
            obj.moduleType = module.ModuleType;//模块类型

            obj.url = module.ApiUrl;//模块名称
            obj.IsShowAudit = module.IsShowAudit;//是否显示审核
            obj.UserModuleColumn = GetUserModuleColumn(moduleCode, userId);//用户模块列

            message = "获取成功！";
        }
        catch (Exception E)
        {
            message = E.Message;
        }

        obj.Success = true;
        obj.message = message;
        result.Data = obj;
        result.Success = true;
        result.Status = 200;
        return result;
    }
    #endregion

    #region 获取模块列
    /// <summary>
    /// 获取模块列
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="moduleInfo">模块</param>
    /// <returns></returns>

    public JArray GetModuleColumn(Guid moduleId, SmModules moduleInfo)
    {
        var columns = new JArray();
        var moduleColumnInfo = new ModuleSqlColumn(moduleInfo.ModuleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();
        //moduleColumns = moduleColumns.OrderBy(y => y.TaxisNo).ToList();
        var data = moduleColumns;
        if ((moduleColumns != null && !moduleColumns.Any()) || moduleColumns == null)
        {
            var data1 = _context.SmModuleColumn.AsNoTracking().Where(x => x.SmModuleId == moduleId && x.IsDeleted == false).OrderBy(x => x.TaxisNo).ToList();
            data = Mapper.Map(data1).ToANew<List<SmModuleColumnExtend>>();
        }

        for (int i = 0; i < data.Count; i++)
        {
            var column = data[i];
            var item = new JObject
            {
                new JProperty("title", column.Title),
                new JProperty("id", column.ID),
                new JProperty("dataIndex", column.DataIndex),
                new JProperty("hideInTable", column.HideInTable),
                new JProperty("fieldType", column.FieldType),
                new JProperty("dataSource", column.DataSource),
                new JProperty("ellipsis", true),
            };

            if (column.Width != null)
                item.Add(new JProperty("width", column.Width));
            //else
            //    item.Add(new JProperty("width", 100));

            if (column.Align.IsNullOrEmpty())
                item.Add(new JProperty("align", "center"));
            else
                item.Add(new JProperty("align", column.Align));
            item.Add(new JProperty("sorter", column.Sorter));
            if (column.ValueType.IsNotEmptyOrNull() && string.IsNullOrEmpty(column.DataFormate))
                item.Add(new JProperty("valueType", column.ValueType));
            if (moduleInfo.DefaultSort == column.DataIndex)
                item.Add(new JProperty("defaultSortOrder", moduleInfo.DefaultSortOrder));
            if (column.IsTableEditable == true)
                item.Add(new JProperty("editable", true));
            else
                item.Add(new JProperty("editable", false));

            if (column.IsCopy == true)
                item.Add(new JProperty("copyable", true));

            if (column.IsTooltip == true)
                item.Add(new JProperty("tooltip", column.TooltipContent));
            if (column.IsThemeColor != true)
            {
                if (column.Color.IsNotEmptyOrNull())
                    item.Add(new JProperty("color", column.Color));
                else
                    item.Add(new JProperty("color", null));
            }
            else
                item.Add(new JProperty("isFollowThemeColor", true));

            if (column.IsBool == true)
            {
                var trueObj = new JObject
                {
                    new JProperty("text", "是"),
                    new JProperty("status","Success"),
                };
                var falseObj = new JObject
                {
                    new JProperty("text", "否"),
                    new JProperty("status","Default"),
                };
                var enumobj = new JObject
                {
                    new JProperty("true", trueObj),
                    new JProperty("false", falseObj),
                };
                item.Add(new JProperty("valueEnum", enumobj));
                item.Add(new JProperty("filters", false));
            }
            else if (column.IsLovCode == true)
            {
                JObject enumobj = new();

                var enumData = LovHelper.GetLovList(column.DataIndex);
                if (column.DataSource.IsNotEmptyOrNull())
                    enumData = LovHelper.GetLovList(column.DataSource);
                if (enumData.Count() > 0)
                {
                    if (enumData.Where(x => x.IsTagDisplay == true).Any())
                        item.Add(new JProperty("isTagDisplay", true));
                    for (int n = 0; n < enumData.Count(); n++)
                        enumobj.Add(new JProperty(enumData[n].Value, new JObject(
                            new JProperty("text", enumData[n].Text),
                            new JProperty("tagColor", enumData[n].TagColor),
                            new JProperty("tagBordered", enumData[n].TagBordered),
                            new JProperty("tagIcon", enumData[n].TagIcon)
                            )));

                    item.Add(new JProperty("valueEnum", enumobj));
                    item.Add(new JProperty("filters", false));
                }
            }

            if (column.HideInSearch == true)
                item.Add(new JProperty("hideInSearch", true));

            if (column.IsColor == true)
                item.Add(new JProperty("isColor", true));
            if (column.IsIcon == true)
                item.Add(new JProperty("isIcon", true));

            columns.Add(item);
        }

        return columns;
    }
    #endregion

    #region 获取用户模块列
    /// <summary>
    /// 获取用户模块列
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    public JObject GetUserModuleColumn(string moduleCode, string userId)
    {
        JObject item = [];
        var cache = RedisCacheService.Get<List<SmUserModuleColumn>>(userId.ToString(), "UserModuleColumn." + moduleCode);
        if (cache == null || (cache != null && !cache.Any()))
        {
            string sql = @$"SELECT A.* 
                    FROM SmUserModuleColumn A
                         JOIN SmModules B ON A.SmModuleId = B.ID AND A.IsDeleted = B.IsDeleted
                    WHERE A.IsDeleted = 'false' AND A.UserId = '{userId}' AND B.ModuleCode = '{moduleCode}'";
            cache = DBHelper.QueryList<SmUserModuleColumn>(sql);

            RedisCacheService.AddObject(userId.ToString(), "UserModuleColumn." + moduleCode, cache);
        }

        if (cache.Any())
        {
            cache.ForEach(x =>
            {
                JObject obj = new();
                if (x.TaxisNo != null)
                    obj.Add(new JProperty("order", x.TaxisNo));
                if (x.IsShow != null)
                    obj.Add(new JProperty("show", x.IsShow));
                if (x.Fixed != null)
                    obj.Add(new JProperty("fixed", x.Fixed));
                item.Add(new JProperty(x.DataIndex, obj));
            });
        }

        return item;
    }
    #endregion

    #region 获取模块日志信息
    /// <summary>
    /// 获取模块日志信息
    /// </summary>
    /// <param name="moduleCode"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ServiceResult<dynamic>> GetModuleLogInfo(string moduleCode, string id)
    {
        dynamic data = new ExpandoObject();
        //获取模块信息
        //var module = await _context.SmModules.AsNoTracking().Where(x => x.IsDeleted == false && x.ModuleCode == moduleCode).FirstOrDefaultAsync();
        var module = await base.QuerySingle(x => x.IsDeleted == false && x.ModuleCode == moduleCode);
        if (module == null)
            throw new Exception("未查询到模块【" + moduleCode + "】相关配置信息！");

        var moduleSql = new ModuleSql(moduleCode);
        string tableName = moduleSql.GetTableName();
        data.tableName = tableName;
        data.TableName = tableName;
        data.ID = id;
        data.CreatedBy = null;
        data.CreatedTime = null;
        data.UpdateBy = null;
        data.UpdateTime = null;

        if (!string.IsNullOrEmpty(tableName))
        {
            string sql = @"SELECT A.ID,
                                           B.UserName CreatedBy,
                                           A.CreatedTime,
                                           C.UserName UpdateBy,
                                           A.UpdateTime
                                    FROM {1} A
                                         LEFT JOIN SmUsers B ON A.CreatedBy = B.ID
                                         LEFT JOIN SmUsers C ON A.UpdateBy = C.ID
                                    WHERE A.ID = '{0}' AND A.IsDeleted = 'false'";
            sql = string.Format(sql, id, tableName);
            var dt = await DBHelper.GetDataTableAsync(sql);
            if (dt.Rows.Count > 0)
            {
                data.CreatedBy = dt.Rows[0]["CreatedBy"].ToString();
                data.CreatedTime = dt.Rows[0]["CreatedTime"].ToString();
                data.UpdateBy = dt.Rows[0]["UpdateBy"].ToString();
                data.UpdateTime = dt.Rows[0]["UpdateTime"].ToString();
            }
        }

        return ServiceResult<dynamic>.OprateSuccess(data, ResponseText.QUERY_SUCCESS);
    }
    #endregion

    #region 导出模块SQL
    /// <summary>
    /// 导出模块SQL
    /// </summary>
    /// <param name="list">ids</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ServiceResult<Guid>> ExportModuleSqlScript(List<SmModules> list)
    {
        var fileId = StringHelper.Id1;

        if (list.Count > 0)
        {
            var sb = new StringBuilder();
            StringBuilder temp;
            DBHelper dBHelper = new();
            foreach (SmModules item in list)
            {
                sb.Append("DELETE FROM SmModuleColumn WHERE SmModuleId='" + item.ID + "';\n");
                sb.Append("DELETE FROM SmModuleSql WHERE ModuleId='" + item.ID + "';\n");
                sb.Append("DELETE FROM SmModules WHERE ID='" + item.ID + "';\n");

                temp = dBHelper.GetInsertSql("SmModules", "ID", item.ID.ToString());
                sb.Append(temp.ToString());
                temp = dBHelper.GetInsertSql("SmModuleColumn", "SmModuleId", item.ID.ToString());
                sb.Append(temp.ToString());
                temp = dBHelper.GetInsertSql("SmModuleSql", "ModuleId", item.ID.ToString());
                sb.Append(temp.ToString() + "\n");
            }

            string fileName = $"系统模块.sql";
            string folder = Utility.GetSysID();
            string savePath = $"/Download/SqlExport/{folder}/";
            if (!Directory.Exists("wwwroot" + savePath))
                Directory.CreateDirectory("wwwroot" + savePath);
            FileHelper.WriteFile("wwwroot" + savePath, fileName, sb.ToString());


            #region 导入文件数据
            var di = new DbInsert("FileAttachment");
            di.IsInitRowId = false;
            di.Values("ID", fileId);
            di.Values("OriginalFileName", fileName);
            di.Values("FileName", fileName);
            di.Values("FileExt", "sql");
            di.Values("Path", savePath);
            await Db.Ado.ExecuteCommandAsync(di.GetSql());
            #endregion

            //return responseContent.OK("导出成功！", (savePath + "/" + fileName).EncryptDES(AppSetting.Secret.ExportFile));
        }
        return ServiceResult<Guid>.OprateSuccess(fileId, "导出成功！");

    }
    #endregion

    #region App.js动态加载路由
    /// <summary>
    /// App.js动态加载路由
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ServiceResult<List<TreeMenuData>>> GetPatchRoutes()
    {
        var _menus = RedisCacheService.Get<List<TreeMenuData>>(App.User.ID.ToString(), "UserPatchRoutes");
        if ((_menus != null && !_menus.Any()) || _menus is null)
        {
            var moduleIds = new List<Guid>();
            var modules = new List<SmModules>();
            string sql = @"SELECT DISTINCT C.ID
                                    FROM SmRoleModule A
                                         JOIN SmUserRole_V B
                                            ON     A.SmRoleId = B.SmRoleId
                                               AND B.SmUserId = '{0}'
                                         JOIN SmModules C ON A.SmModuleId = C.ID AND C.IsDeleted = 'false' AND C.BelongModuleId IS NULL
                                    WHERE A.IsDeleted = 'false'";
            sql = string.Format(sql, App.User.ID);
            modules = await Db.Ado.SqlQueryAsync<SmModules>(sql);
            moduleIds = modules.Select(o => o.ID).ToList();

            var moduleList = ModuleInfo.GetModuleList();
            var TreeMenuData = moduleList.Where(x => x.IsActive == true && x.IsParent == false && x.IsDetail != true && moduleIds.Contains(x.ID) && !string.IsNullOrEmpty(x.RoutePath))
                .Select(x => new TreeMenuData
                {
                    id = x.ID.ToString(),
                    path = x.RoutePath,
                    name = x.ModuleName,
                    icon = x.Icon,
                    component = x.RoutePath,
                    moduleCode = x.ModuleCode
                }).ToList();
            TreeMenuData.Add(new TreeMenuData
            {
                id = Guid.NewGuid().ToString(),
                path = "/account/settings",
                name = "个人设置",
                component = "/account/settings"
            });
            TreeMenuData.Add(new TreeMenuData
            {
                id = Guid.NewGuid().ToString(),
                path = "/",
                name = "首页",
                redirect = "/welcome"
            });
            TreeMenuData.Add(new TreeMenuData
            {
                id = Guid.NewGuid().ToString(),
                path = "/404",
                name = "404",
                component = "/404"
            });
            TreeMenuData.Add(new TreeMenuData
            {
                id = Guid.NewGuid().ToString(),
                path = "/welcome",
                name = "首页",
                component = "/dashboard/analysis"
            });
            RedisCacheService.AddObject(App.User.ID.ToString(), "UserPatchRoutes", TreeMenuData);
            _menus = TreeMenuData; ;
        }

        return Success<List<TreeMenuData>>(_menus);
    }
    #endregion

    #region 更新模块列排序号
    /// <summary>
    /// 更新模块列排序号
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="column">表单信息</param>
    /// <returns></returns>
    public async Task<ServiceResult> UpdateTaxisNoAsync(string moduleCode, List<SmModuleColumn> columns, string type)
    {
        if (columns is null || (columns != null && !columns.Any()))
            return ServiceResult.OprateSuccess();

        if (type == "form")
        {
            int i = 1;
            columns.ForEach(x =>
            {
                x.FromTaxisNo = 100 * i;
                i++;
            });
            await Db.Updateable(columns)
                .UpdateColumns(x => new { x.FromTaxisNo, x.UpdateBy, x.UpdateTime })
                .ExecuteCommandAsync();
        }
        else
        {
            int i = 1;
            columns.ForEach(x =>
            {
                x.TaxisNo = 100 * i;
                i++;
            });
            await Db.Updateable(columns)
                .UpdateColumns(x => new { x.TaxisNo, x.UpdateBy, x.UpdateTime })
                .ExecuteCommandAsync();
        }

        ModuleSqlColumn.Reload(moduleCode);

        return ServiceResult.OprateSuccess();
    }
    #endregion

    #region 更新模块表单列
    /// <summary>
    /// 更新模块表单列
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="column">表单信息</param>
    /// <returns></returns>
    public async Task<ServiceResult> UpdateColumnAsync(string moduleCode, SmModuleFormOption column, string type)
    {
        if (column is null)
            return ServiceResult.OprateSuccess();

        var entity = Mapper.Map(column).ToANew<SmModuleColumn>();

        if (type == "form")
        {
            if (column.FieldType == "ComboGrid")
                entity.DataSource = column.ComboGridDataSource;
            if (column.FieldType == "ComboBox")
            {
                entity.DataSource = column.ComboBoxDataSource;
                if (column.DataSource.IsNullOrEmpty() && column.IsLovCode == true)
                    entity.DataSource = entity.DataIndex;
            }

            await Db.Updateable(entity)
                .UpdateColumns(x => new
                {
                    x.FormTitle,
                    x.DataIndex,
                    x.DefaultValue,
                    x.HideInForm,
                    x.Required,
                    x.Disabled,
                    x.IsUnique,
                    x.MaxLength,
                    x.MinLength,
                    x.Maximum,
                    x.Minimum,
                    x.Placeholder,
                    x.CreateHide,
                    x.ModifyDisabled,
                    x.GridSpan,
                    x.DataSource,
                    x.ColumnMode,
                    x.IsTooltip,
                    x.TooltipContent,
                    x.Remark,
                    x.UpdateBy,
                    x.UpdateTime
                })
                .ExecuteCommandAsync();
        }
        else
        {
            await Db.Updateable(entity)
                .UpdateColumns(x => new
                {
                    x.Title,
                    x.DataIndex,
                    x.ValueType,
                    x.Width,
                    x.HideInTable,
                    x.Sorter,
                    x.IsExport,
                    x.IsLovCode,
                    x.IsBool,
                    x.HideInSearch,
                    x.DataFormate,
                    x.IsTableEditable,
                    x.IsSum,
                    x.Align,
                    x.Remark,
                    x.ColumnMode,
                    x.IsCopy,
                    x.IsTooltip,
                    x.TooltipContent,
                    x.Color,
                    x.IsThemeColor,
                    x.IsColor,
                    x.IsIcon,
                    x.UpdateBy,
                    x.UpdateTime
                })
                .ExecuteCommandAsync();
        }
        ModuleSqlColumn.Reload(moduleCode);

        return ServiceResult.OprateSuccess();
    }

    #endregion  

    #region 获取模块表单信息
    /// <summary>
    /// 获取模块表单信息
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns></returns>
    public ServicePageResult<SmModuleFormOption> GetModuleFormColumn(string moduleCode)
    {
        var result = new List<SmModuleFormOption>();

        //获取模块信息
        var module = ModuleInfo.GetModuleInfo(moduleCode);

        if (module == null)
            throw new Exception("未查询到模块【" + moduleCode + "】相关配置信息！");

        var moduleId = module.ID;
        var moduleColumnInfo = new ModuleSqlColumn(module.ModuleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

        moduleColumns = moduleColumns.OrderBy(x => x.FromTaxisNo).ToList();
        result = Mapper.Map(moduleColumns).ToANew<List<SmModuleFormOption>>();

        int i = 0;
        result.ForEach(x =>
        {
            x.FormTitle = string.IsNullOrWhiteSpace(x.FormTitle) ? x.Title : x.FormTitle;
            if (x.FieldType == "ComboGrid")
                x.ComboGridDataSource = x.DataSource;
            if (x.FieldType == "ComboBox")
            {
                if (x.DataSource.IsNullOrEmpty() && x.IsLovCode == true)
                    x.DataSource = x.DataIndex;
                x.ComboBoxDataSource = x.DataSource;

            }
            result[i].ID = moduleColumns[i].ID;
            i++;
        });
        return new ServicePageResult<SmModuleFormOption>(1, 1, 1, result);
    }
    #endregion

    #region 记录用户模块列
    /// <summary>
    /// 记录用户模块列
    /// </summary>
    /// <param name="column">表单信息</param>
    /// <returns></returns>
    public async Task<ServiceResult> RecordUserModuleColumn(Guid smModuleId, JObject param)
    {
        await _smUserModuleColumnServices.Delete(x => x.UserId == UserId && x.SmModuleId == smModuleId);

        var moduleList = ModuleInfo.GetModuleList();

        var module = moduleList.Where(x => x.ID == smModuleId).FirstOrDefault();
        var inserts = new List<InsertSmUserModuleColumnInput>();
        foreach (JProperty jProperty in param.Properties())
        {
            var name = jProperty.Name;
            var insert = new InsertSmUserModuleColumnInput
            {
                UserId = UserId,
                SmModuleId = smModuleId,
                DataIndex = name
            };

            var value = jProperty.Value.ToString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                var jsonParam = JsonHelper.JsonToObj<UserMoudleColumnParam>(value);
                insert.IsShow = jsonParam.show;
                insert.TaxisNo = jsonParam.order;
                insert.Fixed = jsonParam.@fixed;
            }

            inserts.Add(insert);

        }
        await _smUserModuleColumnServices.Add(inserts);
        string sql = @$"SELECT A.* 
                    FROM SmUserModuleColumn A
                         JOIN SmModules B ON A.SmModuleId = B.ID AND A.IsDeleted = B.IsDeleted
                    WHERE A.IsDeleted = 'false' AND A.UserId = '{userId}' AND B.ModuleCode = '{module.ModuleCode}'";
        var cache = DBHelper.QueryList<SmUserModuleColumn>(sql, null);

        RedisCacheService.AddObject(userId.ToString(), "UserModuleColumn." + module.ModuleCode, cache);
        return ServiceResult.OprateSuccess("保存成功！");
    }
    #endregion

    #region 复制模块
    /// <summary>
    /// 复制模块
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="module1">新模块信息</param>
    /// <returns></returns>
    public async Task<ServiceResult> CopyAsync(Guid moduleId, SmModules module1)
    {

        if (await Db.Queryable<SmModules>().AnyAsync(x => x.ModuleCode == module1.ModuleCode))
            return ServiceResult.OprateFailed($"系统中已存在相同模块代码【{module1.ModuleCode}】模块！");
        var module = await base.QuerySingle(x => x.ID == moduleId);

        module.ModuleCode = module1.ModuleCode;
        module.ModuleName = module1.ModuleName;
        module.ID = StringHelper.Id1;

        var columns = await Db.Queryable<SmModuleColumn>().Where(x => x.SmModuleId == moduleId).ToListAsync();
        var moduleSql = await Db.Queryable<SmModuleSql>().Where(x => x.ModuleId == moduleId).FirstAsync();

        module.CreatedBy = UserId;
        module.CreatedTime = Utility.GetSysDate();
        module.UpdateBy = null;
        module.UpdateTime = null;

        if (!moduleSql.IsNullOrEmpty())
        {

            moduleSql.ID = StringHelper.Id1;
            moduleSql.CreatedBy = UserId;
            moduleSql.CreatedTime = Utility.GetSysDate();
            moduleSql.ModuleId = module.ID;
            moduleSql.UpdateBy = null;
            moduleSql.UpdateTime = null;
        }

        columns.ForEach(x =>
        {
            x.ID = StringHelper.Id1;
            x.CreatedBy = UserId;
            x.CreatedTime = Utility.GetSysDate();
            x.SmModuleId = module.ID;
            x.UpdateBy = null;
            x.UpdateTime = null;
        });
        await Db.Insertable(module).ExecuteCommandAsync();
        await Db.Insertable(moduleSql).ExecuteCommandAsync();
        await Db.Insertable(columns).ExecuteCommandAsync();

        return ServiceResult.OprateSuccess("复制成功！");
    }
    #endregion

    #region 获取全部模块数据
    public async Task<ServiceResult<ModuleTree>> GetAllModuleList()
    {

        var moduleTree = new ModuleTree();
        moduleTree.key = "All";
        moduleTree.title = "请选择角色模块";

        var moduleList = ModuleInfo.GetModuleList();


        LoopToAppendChildren(moduleList, moduleTree);

        return ServiceResult<ModuleTree>.OprateSuccess(moduleTree, ResponseText.QUERY_SUCCESS);
    }


    public void LoopToAppendChildren(List<SmModules> smModules, ModuleTree moduleTree)
    {
        var subItems = new List<ModuleTree>();
        if (moduleTree.key == "All")
        {
            subItems = smModules
                .Where(x => x.IsParent == true && string.IsNullOrEmpty(x.ParentId.ToString()))
                .Select(y => new ModuleTree
                {
                    title = y.ModuleName,
                    key = y.ID.ToString(),
                    isLeaf = y.IsParent != null ? !y.IsParent : true
                }).ToList();
        }
        else
        {
            subItems = smModules
                .Where(x => x.ParentId == Guid.Parse(moduleTree.key))
                .Select(y => new ModuleTree
                {
                    title = y.IsDetail == true && y.BelongModuleId != null ? ModuleInfo.GetModuleNameById(y.BelongModuleId) + "/" + y.ModuleName : y.ModuleName,
                    key = y.ID.ToString(),
                    isLeaf = y.IsParent != null ? !y.IsParent : true
                }).ToList();
        }
        moduleTree.children = [.. subItems];
        foreach (var subItem in subItems)
        {
            LoopToAppendChildren(smModules, subItem);
        }
    }
    #endregion
}