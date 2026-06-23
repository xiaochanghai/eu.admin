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
*│　版权所有：SahHsiao                                │
*└──────────────────────────────────┘
*/
namespace EU.Core.Services;

/// <summary>
/// 系统模块服务
/// 提供模块管理、菜单构建、权限控制等功能
/// </summary>
public class SmModulesServices : BaseServices<SmModules, SmModulesDto, InsertSmModulesInput, EditSmModulesInput>, ISmModulesServices
{
    #region 常量定义

    /// <summary>
    /// 默认打开方式 - 抽屉
    /// </summary>
    private const string DEFAULT_OPEN_TYPE = "Drawer";

    /// <summary>
    /// 操作栏最大显示按钮数
    /// </summary>
    private const int MAX_ACTION_COUNT = 3;

    /// <summary>
    /// 默认表单页宽度
    /// </summary>
    private const int DEFAULT_FORM_PAGE_WIDTH = 720;

    private const string ACCEPT_FORMAT_PATTERN = @"^\.[A-Za-z0-9]+(,\.[A-Za-z0-9]+)*$";

    private static bool IsValidAcceptFormat(string accept)
    {
        return string.IsNullOrWhiteSpace(accept)
            || System.Text.RegularExpressions.Regex.IsMatch(accept.Trim(), ACCEPT_FORMAT_PATTERN);
    }

    /// <summary>
    /// 排序号增量
    /// </summary>
    private const int TAXIS_INCREMENT = 100;

    #endregion

    #region 字段
    private readonly RedisCacheService _redisCacheService;
    private readonly IConnectionMultiplexer _connection;
    private readonly IConfiguration _configuration;
    private readonly ISmRoleFunctionServices _smRoleFunctionServices;
    private readonly ISmUserModuleColumnServices _smUserModuleColumnServices;
    private readonly ISmRolesServices _smRolesServices;
    private readonly ISmModuleColumnServices _smModuleColumnServices;
    private readonly IBaseRepository<SmModules> _dal;

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dal">模块数据访问层</param>
    /// <param name="smRoleFunctionServices">角色功能服务</param>
    /// <param name="smUserModuleColumnServices">用户模块列服务</param>
    /// <param name="smRolesServices">角色服务</param>
    /// <param name="smModuleColumnServices">模块列服务</param>
    public SmModulesServices(
        IBaseRepository<SmModules> dal,
        ISmRoleFunctionServices smRoleFunctionServices,
        ISmUserModuleColumnServices smUserModuleColumnServices,
        ISmRolesServices smRolesServices,
        ISmModuleColumnServices smModuleColumnServices,
        IConnectionMultiplexer connection,
        IConfiguration configuration)
    {
        this._dal = dal;
        base.BaseDal = dal;
        this._smRoleFunctionServices = smRoleFunctionServices;
        this._smUserModuleColumnServices = smUserModuleColumnServices;
        this._smRolesServices = smRolesServices;
        this._smModuleColumnServices = smModuleColumnServices;

        _connection = connection;
        _configuration = configuration;
        _redisCacheService = new RedisCacheService(_connection, _configuration, 1);
    }
    #endregion

    #region 新增

    /// <summary>
    /// 新增模块
    /// </summary>
    /// <param name="entity">模块实体</param>
    /// <returns>新增的模块ID</returns>
    /// <remarks>
    /// 新增后会重新初始化模块缓存
    /// 如果未指定打开方式，默认为抽屉模式
    /// </remarks>
    public override async Task<Guid> Add(object entity)
    {
        var entity1 = ConvertToEntity(entity);
        entity1.OpenType = entity1.OpenType ?? DEFAULT_OPEN_TYPE;
        var id = await base.Add(ConvertToString(entity1));

        // 重新初始化模块缓存
        ModuleInfo.Init();

        return id;
    }

    #endregion

    #region 更新

    /// <summary>
    /// 更新模块
    /// </summary>
    /// <param name="Id">模块ID</param>
    /// <param name="entity">模块实体</param>
    /// <returns>更新结果</returns>
    /// <remarks>
    /// 更新后会重新初始化模块缓存
    /// </remarks>
    public override async Task<bool> Update(Guid Id, object entity)
    {
        var result = await base.Update(Id, entity);

        // 重新初始化模块缓存
        ModuleInfo.Init();

        return result;
    }

    #endregion

    #region 获取左侧菜单

    /// <summary>
    /// 递归构建左侧菜单树（旧版格式）
    /// </summary>
    /// <param name="roleModule">角色拥有的模块ID列表</param>
    /// <param name="smModules">所有模块列表</param>
    /// <param name="moduleTree">当前菜单树节点</param>
    /// <remarks>
    /// 根据角色权限递归构建菜单树结构
    /// </remarks>
    public static void LoopToAppendChildren(List<Guid?> roleModule, List<SmModules> smModules, TreeMenuData moduleTree)
    {
        if (roleModule == null || smModules == null || moduleTree == null) return;

        var subItems = new List<TreeMenuData>();

        // 判断是否为根节点
        if (moduleTree.id.IsNullOrEmpty())
        {
            // 获取顶级父模块
            subItems = smModules
                .Where(x => x.IsParent == true && x.ParentId == null && roleModule.Contains(x.ID))
                .OrderBy(x => x.TaxisNo)
                .Select(y => new TreeMenuData
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
            if (!Guid.TryParse(moduleTree.id, out var parentId))
            {
                moduleTree.children = [];
                return;
            }

            // 获取子模块
            subItems = smModules
                .Where(x => x.ParentId == parentId && roleModule.Contains(x.ID))
                .OrderBy(x => x.TaxisNo)
                .Select(y => new TreeMenuData
                {
                    id = y.ID.ToString(),
                    path = y.RoutePath,
                    name = y.ModuleName,
                    icon = y.Icon,
                    component = y.RoutePath
                }).ToList();
        }

        // 设置子节点
        moduleTree.children = [.. subItems];

        // 递归处理每个子节点
        foreach (var subItem in subItems)
        {
            LoopToAppendChildren(roleModule, smModules, subItem);
        }
    }

    /// <summary>
    /// 递归构建左侧菜单树（新版Auth格式）
    /// </summary>
    /// <param name="roleModule">角色拥有的模块ID列表</param>
    /// <param name="smModules">所有模块列表</param>
    /// <param name="moduleTree">当前菜单树节点</param>
    /// <remarks>
    /// 根据角色权限递归构建菜单树结构，包含meta元数据
    /// </remarks>
    public static async Task LoopToAppendChildren1(ISqlSugarClient db, List<Guid?> roleModule, List<SmModules> smModules, TreeAuthMenu moduleTree)
    {
        var subItems = new List<TreeAuthMenu>();

        // 判断是否为根节点
        if (string.IsNullOrEmpty(moduleTree.id))
        {
            // 获取顶级父模块
            var rootModules = smModules
                .Where(x => x.IsParent == true && x.ParentId == null && roleModule.Contains(x.ID))
                .OrderBy(x => x.TaxisNo)
                .ToList();
            foreach (var y in rootModules)
            {
                var titleEn = await LanguageHelper.Get(db, "Module", y.ID, "ModuleName");
                subItems.Add(new TreeAuthMenu
                {
                    id = y.ID.ToString(),
                    path = y.RoutePath,
                    redirect = y.Element,
                    meta = new TreeAuthMenuMeta
                    {
                        key = y.ModuleCode,
                        icon = y.Icon,
                        title = y.ModuleName,
                        titleEn = titleEn?.Value_EN ?? y.ModuleName,
                        isFull = y.IsFull == true,
                        isHide = (y.ParentId == null || y.IsActive == true) ? false : true
                    }
                });
            }
        }
        else
        {
            if (!Guid.TryParse(moduleTree.id, out var parentId))
            {
                moduleTree.children = [];
                return;
            }

            // 获取子模块
            var childModules = smModules
                .Where(x => x.ParentId == parentId && roleModule.Contains(x.ID))
                .OrderBy(x => x.TaxisNo)
                .ToList();
            foreach (var y in childModules)
            {
                var titleEn = await LanguageHelper.Get(db, "Module", y.ID, "ModuleName");
                subItems.Add(new TreeAuthMenu
                {
                    id = y.ID.ToString(),
                    path = y.RoutePath,
                    element = y.Element,
                    meta = new TreeAuthMenuMeta
                    {
                        key = y.ModuleCode,
                        icon = y.Icon,
                        title = y.ModuleName,
                        titleEn = titleEn?.Value_EN,
                        isFull = y.IsFull == true,
                        isHide = (y.ParentId == null || y.IsActive == true) ? false : true
                    }
                });
            }
        }

        // 设置重定向到第一个子节点
        if (subItems.Any())
            moduleTree.redirect = subItems[0].path;

        // 设置子节点
        moduleTree.children = [.. subItems];

        // 递归处理每个子节点
        foreach (var subItem in subItems)
        {
            await LoopToAppendChildren1(db, roleModule, smModules, subItem);
        }
    }

    /// <summary>
    /// 递归获取用户有权限的模块及其所有父级模块
    /// </summary>
    /// <param name="smModules">模块列表</param>
    /// <param name="smRoleList">输出的模块ID列表</param>
    /// <remarks>
    /// 递归查找每个模块的父级目录，确保菜单层级完整
    /// </remarks>
    public static void LoopGetRoleModule(List<SmModules> smModules, List<Guid?> smRoleList)
    {
        for (int i = 0; i < smModules.Count; i++)
        {
            smRoleList.Add(smModules[i].ID);

            // 递归获取子模块的上级目录
            if (smModules[i].ParentId != null && smModules[i].ParentId != Guid.Empty)
            {
                var moduleList = ModuleInfo.GetModuleList();
                var data = moduleList
                    .Where(x => x.ID == smModules[i].ParentId && x.IsDeleted == false && x.IsActive == true)
                    .ToList();
                LoopGetRoleModule(data, smRoleList);
            }
        }
    }

    /// <summary>
    /// 获取左侧菜单数据（旧版格式）
    /// </summary>
    /// <returns>菜单树形数据</returns>
    /// <remarks>
    /// 先从Redis缓存获取，如果缓存不存在则从数据库查询并缓存
    /// 仅返回用户有权限的模块及其父级模块
    /// </remarks>
    public async Task<ServiceResult<List<TreeMenuData>>> GetMenuData()
    {
        // 尝试从缓存获取
        var _menus = await _redisCacheService.GetAsync<List<TreeMenuData>>(UserId1, "UserMenu");
        if (_menus != null && _menus.Any())
            return Success(_menus, ResponseText.QUERY_SUCCESS);

        // 从数据库查询用户有权限的模块
        var roleModule = new List<SmModules>();
        string sql = @"SELECT DISTINCT C.*
                        FROM SmRoleModule A
                             JOIN SmUserRole_V B
                                ON     A.SmRoleId = B.SmRoleId
                                   AND B.SmUserId = '{0}'
                             JOIN SmModules C ON A.SmModuleId = C.ID AND C.IsDeleted = 'false' AND C.BelongModuleId IS NULL
                        WHERE A.IsDeleted = 'false'";
        sql = string.Format(sql, UserId1);
        roleModule = DBHelper.QueryList<SmModules>(sql);

        // 递归获取有权限的子模块的上级目录
        var smRoleList = new List<Guid?>();
        LoopGetRoleModule(roleModule, smRoleList);

        // 构建菜单树
        TreeMenuData treeMenuData = new();
        var moduleList = ModuleInfo.GetModuleList();
        var smModules = moduleList.Where(x => x.IsDeleted == false && x.IsActive == true).ToList();
        LoopToAppendChildren(smRoleList, smModules, treeMenuData);

        // 添加首页菜单
        var data = new List<TreeMenuData>
        {
            new()
            {
                id = Guid.NewGuid().ToString(),
                path = "/",
                name = "首页",
                icon = "home",
                component = "/"
            }
        };
        data = data.Concat(treeMenuData.children).ToList();

        // 缓存菜单数据
        _redisCacheService.AddObject(UserId1, "UserMenu", data);
        _menus = data;

        return Success(_menus, ResponseText.QUERY_SUCCESS);
    }

    /// <summary>
    /// 获取左侧菜单数据（新版Auth格式）
    /// </summary>
    /// <returns>授权菜单树形数据</returns>
    /// <remarks>
    /// 先从Redis缓存获取，如果缓存不存在则从数据库查询并缓存
    /// 包含系统固定菜单（首页、个人信息、表单配置等）
    /// </remarks>
    public async Task<ServiceResult<List<TreeAuthMenu>>> GetAuthMenu()
    {
        // 尝试从缓存获取
        var _menus = await _redisCacheService.GetAsync<List<TreeAuthMenu>>(UserId1, "UserAuthMenu");
        if (_menus != null && _menus.Any())
            return Success(_menus, ResponseText.QUERY_SUCCESS);

        // 查询用户角色ID列表
        var roleIds = await Db.Queryable<SmUserRole>()
            .Where(x => x.SmUserId != null && x.SmRoleId != null && x.SmUserId == UserId)
            .Select(x => x.SmRoleId)
            .Distinct()
            .ToListAsync();

        // 查询角色关联的模块ID列表
        var moduleIds = await Db.Queryable<SmRoleModule>()
            .Where(x => x.SmRoleId != null && roleIds.Contains(x.SmRoleId))
            .Select(x => x.SmModuleId)
            .Distinct()
            .ToListAsync();

        // 查询用户有权限的模块（排除详情页）
        var roleModule = await Db.Queryable<SmModules>()
            .Where(x => moduleIds.Contains(x.ID) && x.BelongModuleId == null)
            .ToListAsync();

        // 递归获取有权限的子模块的上级目录
        var smRoleList = new List<Guid?>();
        LoopGetRoleModule(roleModule, smRoleList);

        // 构建菜单树
        var treeMenuData = new TreeAuthMenu();
        var moduleList = ModuleInfo.GetModuleList();
        var smModules = moduleList.Where(x => x.IsDeleted == false).ToList();
        await LoopToAppendChildren1(Db, smRoleList, smModules, treeMenuData);

        // 定义系统固定菜单
        var data = new List<TreeAuthMenu>
        {
            // 首页
            new()
            {
                id = "7fc933e1-0baa-4c71-bfb6-a49ade977ad5",
                path = "/home/index",
                element = "/home/index",
                meta = new TreeAuthMenuMeta
                {
                    key = "home",
                    icon = "HomeOutlined",
                    title = "首页",
                    titleEn = "Home",
                    isAffix = true
                },
                children = default
            },
            // 个人信息
            new()
            {
                id = "39576ba6-45ad-4e88-9867-9699cc8d077f",
                path = "/account/settings/index",
                element = "/account/settings/index",
                meta = new TreeAuthMenuMeta
                {
                    key = "account_setting",
                    icon = "UserOutlined",
                    title = "个人信息",
                    titleEn = "Profile",
                    isHide = true
                },
                children = default
            },
            // 表单配置
            new()
            {
                id = "39576ba6-45ad-4e88-9867-9699cc8d078f",
                path = "/system/config",
                element = "/system/config/form/index",
                meta = new TreeAuthMenuMeta
                {
                    key = "form_setting",
                    icon = "UserOutlined",
                    title = "表单配置",
                    titleEn = "Form Settings",
                    isHide = true,
                    isFull = true
                },
                children = default
            },
            // 工作流
            new()
            {
                id = "39576ba6-45ad-4e88-9867-9699cc8d079f",
                path = "/workflow",
                element = "/system/workflow/index",
                meta = new TreeAuthMenuMeta
                {
                    key = "workflow_setting",
                    icon = "UserOutlined",
                    title = "工作流",
                    titleEn = "Workflow",
                    isHide = true,
                    isFull = true
                },
                children = default
            },
            // 关于系统
            new()
            {
                id = "39576ba6-45ad-4e88-9867-9699cc8d080f",
                path = "/about",
                element = "/about/index",
                meta = new TreeAuthMenuMeta
                {
                    key = "about_setting",
                    icon = "InfoCircleOutlined",
                    title = "关于系统",
                    titleEn = "About",
                    isHide = true
                },
                children = default
            }
        };

        // 合并系统菜单和用户权限菜单
        data = data.Concat(treeMenuData.children).ToList();

        // 缓存菜单数据
        _redisCacheService.AddObject(UserId1, "UserAuthMenu", data);
        _menus = data;

        return Success(_menus, ResponseText.QUERY_SUCCESS);
    }

    #endregion

    #region 获取模块信息

    /// <summary>
    /// 获取模块完整配置信息
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns>模块配置信息（包含列配置、按钮权限、功能权限等）</returns>
    /// <remarks>
    /// 返回前端所需的模块完整配置，包括：
    /// - 表格列配置
    /// - 表单列配置
    /// - 操作按钮权限
    /// - 菜单按钮权限
    /// - 功能权限
    /// - 用户自定义列设置
    /// </remarks>
    public async Task<dynamic> GetModuleInfo(string moduleCode)
    {
        dynamic result = new ExpandoObject();
        dynamic obj = new ExpandoObject();
        string message = string.Empty;
        string userId = Utility.GetUserIdString();

        try
        {
            // 获取模块基本信息
            var module = ModuleInfo.GetModuleInfo(moduleCode);
            if (module == null)
                throw new Exception("未查询到模块【" + moduleCode + "】相关配置信息！");

            var moduleColumnInfo = new ModuleSqlColumn(module.ModuleCode);
            var moduleId = module.ID;

            // 获取表格列配置
            obj.columns = await GetModuleColumn(moduleId, module);

            // 获取表单列配置
            var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();
            moduleColumns.ForEach(x =>
            {
                x.FormTitle = x.FormTitle ?? x.Title;
                x.FromTaxisNo = x.FromTaxisNo ?? x.TaxisNo;
            });
            obj.formColumns = Mapper.Map(moduleColumns.Where(x => (x.ColumnMode != "list" || x.ColumnMode == null)).OrderBy(x => x.FromTaxisNo)).ToANew<List<SmModuleForm>>();

            // 获取功能权限列表
            var privileges = await FunctionPrivilege.QueryByModuleCodeAsync(Db, moduleCode);
            var ids = privileges.Select(x => x.ID).ToList();

            // 从缓存获取用户功能权限
            var functions = _redisCacheService.Get<List<SmRoleFunctionExtend>>(userId, "UserFunction." + moduleCode);
            if (functions == null)
            {
                // 查询用户的角色功能权限
                functions = await Db.Queryable<SmRoleFunction>()
                    .LeftJoin<SmUserRole>((x, y) => x.SmRoleId == y.SmRoleId)
                    .Where((x, y) => (moduleId == x.SmModuleId || (x.SmFunctionId != null && ids.Contains(x.SmFunctionId.Value))))
                    .Select(x => new SmRoleFunctionExtend
                    {
                        SmFunctionId = x.SmFunctionId,
                        NoActionCode = x.NoActionCode,
                        SmModuleId = x.SmModuleId,
                        ActionCode = x.ActionCode
                    }).Distinct().ToListAsync();

                // 缓存功能权限
                _redisCacheService.AddObject(userId, "UserFunction." + moduleCode, functions);
            }

            // 获取基本操作权限（查询、新增、修改等）
            var actions = functions.Where(x => x.SmModuleId != null && !string.IsNullOrWhiteSpace(x.ActionCode)).ToList();

            // 获取操作栏按钮（表格行操作按钮）
            var actionData = functions
                .Join(privileges, a => a.SmFunctionId, b => b.ID, (a, b) => new { a, b })
                .Where(z => z.b.SmModuleId == moduleId && z.b.DisplayPosition == "Action")
                .Select(y => y.b)
                .OrderBy(y => y.TaxisNo).Distinct().ToList();

            // 获取菜单栏按钮（顶部工具栏按钮）
            var menuData = functions
                .Join(privileges, a => a.SmFunctionId, b => b.ID, (a, b) => new { a, b })
                .Where(z => z.b.SmModuleId == moduleId && z.b.DisplayPosition == "Menu")
                .Select(y => y.b)
                .OrderBy(y => y.TaxisNo).ToList();

            // 获取隐藏菜单栏按钮
            var hideMenu = functions
                .Join(privileges, a => a.SmFunctionId, b => b.ID, (a, b) => new { a, b })
                .Where(z => z.b.SmModuleId == moduleId && z.b.DisplayPosition == "HideMenu")
                .Select(y => y.b)
                .OrderBy(y => y.TaxisNo).ToList();

            // 获取自定义操作按钮
            var customActionData = functions
                .Join(privileges, a => a.SmFunctionId, b => b.ID, (a, b) => new { a, b })
                .Where(z => z.b.SmModuleId == moduleId && z.b.DisplayPosition == "Custom")
                .Select(y => y.b)
                .OrderBy(y => y.TaxisNo).Distinct().ToList();

            // 根据模块配置过滤操作权限
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

            #region 操作栏按钮布局计算

            var allAction = new List<ActionActions>();
            var actionCount = actionData.Count;

            // 添加修改按钮
            if (actions.Any(x => x.ActionCode == "Update"))
            {
                allAction.Add(new ActionActions { id = "Update", taxisNo = 100 });
                actionCount++;
            }

            // 添加查看按钮
            if (actions.Any(x => x.ActionCode == "View"))
            {
                allAction.Add(new ActionActions { id = "View", taxisNo = 200 });
                actionCount++;
            }

            // 添加删除按钮
            if (actions.Any(x => x.ActionCode == "Delete"))
            {
                allAction.Add(new ActionActions { id = "Delete", taxisNo = 300 });
                actionCount++;
            }

            // 添加自定义操作按钮
            for (int i = 0; i < actionData.Count; i++)
            {
                allAction.Add(new ActionActions
                {
                    id = actionData[i].ID.ToString(),
                    taxisNo = actionData[i].TaxisNo
                });
            }

            // 计算前置按钮和下拉按钮
            var beforeActions = new List<ActionActions>();
            var dropActions = new List<ActionActions>();

            if (actionCount > MAX_ACTION_COUNT)
            {
                // 超过3个，前2个平铺，其余放下拉菜单
                beforeActions = allAction.Take(2).ToList();
                dropActions = allAction.Skip(2).Take(actionCount - 2).ToList();
            }
            else
            {
                // 不超过3个，全部平铺
                beforeActions = allAction.Take(MAX_ACTION_COUNT).ToList();
            }

            #endregion

            #region 获取子模块列表

            var modules = ModuleInfo.GetLowerModules(moduleCode);
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

            // 构建返回对象
            obj.children = children; // 子模块列表

            // 主键列配置
            obj.masterColumn = null;
            if (moduleColumns.Any(x => x.IsMasterId == true))
                obj.masterColumn = moduleColumns.FirstOrDefault(x => x.IsMasterId == true)?.DataIndex;

            obj.beforeActions = beforeActions; // 操作栏前置按钮
            obj.dropActions = dropActions; // 操作栏下拉按钮
            obj.actions = actions.Select(x => x.ActionCode).ToList(); // 可用操作代码列表
            obj.actionData = actionData; // 操作栏按钮数据
            obj.menuData = menuData; // 菜单栏按钮
            obj.customActionData = customActionData; // 自定义按钮
            obj.hideMenu = hideMenu; // 隐藏���单按钮
            obj.actionCount = actionCount; // 操作栏按钮总数
            obj.moduleId = moduleId; // 模块ID
            obj.isDetail = module.IsDetail; // 是否为从表
            obj.openType = module.OpenType; // 打开方式
            obj.formPage = module.FormPage; // 编辑页路径
            obj.formPageWidth = module.FormPageWidth ?? DEFAULT_FORM_PAGE_WIDTH; // 编辑页宽度
            obj.moduleCode = moduleCode; // 模块代码
            obj.moduleName = module.ModuleName; // 模块名称
            var titleEn = await LanguageHelper.Get(Db, "Module", module.ID, "ModuleName");
            obj.moduleName_EN = titleEn?.Value_EN; // 模块名称

            obj.moduleType = module.ModuleType; // 模块类型
            obj.url = module.ApiUrl; // API地址
            obj.IsShowAudit = module.IsShowAudit; // 是否显示审核
            obj.UserModuleColumn = await GetUserModuleColumn(moduleCode); // 用户自定义列配置
            obj.IsShowRowSelection = module.IsShowRowSelection;

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
    /// 获取模块表格列配置
    /// </summary>
    /// <param name="moduleId">模块ID</param>
    /// <param name="moduleInfo">模块信息</param>
    /// <returns>列配置JSON数组</returns>
    /// <remarks>
    /// 返回前端表格组件所需的列配置，包括显示类型、宽度、对齐方式等
    /// 支持布尔类型、枚举类型、自定义标签颜色等特性
    /// </remarks>
    public async Task<JArray> GetModuleColumn(Guid moduleId, SmModules moduleInfo)
    {
        var columns = new JArray();
        var moduleColumnInfo = new ModuleSqlColumn(moduleInfo.ModuleCode);
        var moduleColumns = moduleColumnInfo.GetModuleSqlColumn();

        var data = moduleColumns.Where(x => x.ColumnMode != "form").ToList();

        // 如果SQL配置的列为空，则从数据库查询
        if ((moduleColumns != null && !moduleColumns.Any()) || moduleColumns == null)
        {
            var data1 = Db.Queryable<SmModuleColumn>()
                .Where(x => x.SmModuleId == moduleId && x.IsDeleted == false)
                .OrderBy(x => x.TaxisNo).ToList();
            data = Mapper.Map(data1).ToANew<List<SmModuleColumnExtend>>();
        }

        for (int i = 0; i < data.Count; i++)
        {
            var column = data[i];

            // 构建列配置对象
            var item = new JObject
            {
                new JProperty("title", column.Title),
                new JProperty("title_en", column.Title_EN),
                new JProperty("id", column.ID),
                new JProperty("dataIndex", column.DataIndex),
                new JProperty("hideInTable", column.HideInTable),
                new JProperty("hideInForm", column.HideInForm),
                new JProperty("fieldType", column.FieldType),
                new JProperty("dataSource", column.DataSource),
                new JProperty("ellipsis", true),
                new JProperty("required", column.Required == true),
                new JProperty("align", column.Align.IsNullOrEmpty() ? "center" : column.Align),
                new JProperty("sorter", column.Sorter),
                new JProperty("editable", column.IsTableEditable == true)
            };

            // 添加列宽
            if (column.Width != null)
                item.Add(new JProperty("width", column.Width));

            // 添加值类型
            if (column.ValueType.IsNotEmptyOrNull())
                item.Add(new JProperty("valueType", column.ValueType));

            // 添加默认排序
            if (moduleInfo.DefaultSort == column.DataIndex)
                item.Add(new JProperty("defaultSortOrder", moduleInfo.DefaultSortOrder));

            // 添加复制功能
            if (column.IsCopy == true)
                item.Add(new JProperty("copyable", true));

            // 添加提示信息
            if (column.IsTooltip == true)
            {
                item.Add(new JProperty("tooltip", column.TooltipContent));
                item.Add(new JProperty("tooltip_en", column.TooltipContent_EN));

            }

            // 添加提示信息
            if (column.IsRedirect == true)
                item.Add(new JProperty("redirectUrl", column.RedirectUrl));

            // 添加颜色配置
            if (column.IsThemeColor != null && column.IsThemeColor == true)
            {
                if (column.Color.IsNotEmptyOrNull())
                    item.Add(new JProperty("color", column.Color));
                else
                    item.Add(new JProperty("color", null));
            }
            else
                item.Add(new JProperty("isFollowThemeColor", true));

            // 处理布尔类型
            if (column.IsBool != null && column.IsBool == true)
            {
                var trueObj = new JObject
                {
                    new JProperty("text", "是"),
                    new JProperty("status", "Success"),
                };
                var falseObj = new JObject
                {
                    new JProperty("text", "否"),
                    new JProperty("status", "Default"),
                };
                var enumobj = new JObject
                {
                    new JProperty("true", trueObj),
                    new JProperty("false", falseObj),
                };
                item.Add(new JProperty("valueEnum", enumobj));
                item.Add(new JProperty("filters", false));
            }
            // 处理枚举类型（Lov代码）
            else if (column.IsLovCode != null && column.IsLovCode == true)
            {
                JObject enumobj = new();

                var dataSource = column.DataSource ?? column.DataIndex;

                // 获取枚举数据
                var enumData = new List<LovInfo>();
                if (dataSource.IsNotEmptyOrNull())
                    enumData = await LovHelper.GetLovListAsync(Db, dataSource);

                if (enumData.Count() > 0)
                {
                    // 判断是否使用标签显示
                    if (enumData.Where(x => x.IsTagDisplay == true).Any())
                        item.Add(new JProperty("isTagDisplay", true));

                    // 构建枚举对象
                    for (int n = 0; n < enumData.Count(); n++)
                        enumobj.Add(new JProperty(enumData[n].Value, new JObject(
                            new JProperty("text", enumData[n].Text),
                            new JProperty("tagColor", enumData[n].TagColor),
                            new JProperty("tagVariant", enumData[n].TagVariant ?? "filled"),
                            new JProperty("tagIcon", enumData[n].TagIcon)
                        )));

                    item.Add(new JProperty("valueEnum", enumobj));
                    item.Add(new JProperty("filters", false));
                }
            }

            // 隐藏搜索
            if (column.HideInSearch.IsNotEmptyOrNull() && column.HideInSearch == true)
                item.Add(new JProperty("search", false));

            columns.Add(item);
        }

        return columns;
    }

    #endregion

    #region 获取用户模块列

    /// <summary>
    /// 获取用户自定义的模块列配置
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns>用户列配置JSON对象</returns>
    /// <remarks>
    /// 返回用户对该模块表格列的自定义配置，包括显示/隐藏、排序、固定列等
    /// 先从Redis缓存获取，如果缓存不存在则从数据库查询并缓存
    /// </remarks>
    public async Task<JObject> GetUserModuleColumn(string moduleCode)
    {
        var key = "UserModuleColumn." + moduleCode;
        JObject item = [];

        // 尝试从缓存获取
        var cache = _redisCacheService.Get<List<SmUserModuleColumn>>(UserId1, key);
        if (cache == null || (cache != null && !cache.Any()))
        {
            // 从数据库查询
            cache = await QueryUserModuleColumn(moduleCode);

            // 缓存结果
            _redisCacheService.AddObject(UserId1, key, cache);
        }

        // 构建配置对象
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
    /// 获取模块数据的日志信息（创建人、创建时间、更新人、更新时间）
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="id">数据ID</param>
    /// <returns>日志信息</returns>
    public async Task<ServiceResult<dynamic>> GetModuleLogInfo(string moduleCode, string id)
    {
        dynamic data = new ExpandoObject();

        // 获取模块信息
        var module = await base.QuerySingle(x => x.IsDeleted == false && x.ModuleCode == moduleCode);
        if (module == null)
            throw new Exception("未查询到模块【" + moduleCode + "】相关配置信息！");

        var moduleSql = new ModuleSql(moduleCode, Db);
        string tableName = moduleSql.GetTableName();

        // 初始化返回数据
        data.tableName = tableName;
        data.TableName = tableName;
        data.ID = id;
        data.CreatedBy = null;
        data.CreatedTime = null;
        data.UpdateBy = null;
        data.UpdateTime = null;
        data.ModuleCode = null;

        // 查询日志信息
        if (!string.IsNullOrEmpty(tableName))
        {
            dynamic dt1 = await Db.Queryable<object>()
                .AS(tableName, "A")
                .AddJoinInfo("SmUsers", "B", ObjectFuncModel.Create("Equals", "A.CreatedBy", "B.id"), JoinType.Left)
                .AddJoinInfo("SmUsers", "C", ObjectFuncModel.Create("Equals", "A.UpdateBy", "C.id"), JoinType.Left)
                .Select(new List<SelectModel>(){
                    new SelectModel { AsName = "ID", FieldName = "A.ID" },
                    new SelectModel { AsName = "CreatedBy", FieldName = "B.UserName" },
                    new SelectModel { AsName = "CreatedTime", FieldName = "A.CreatedTime" },
                    new SelectModel { AsName = "UpdateBy", FieldName = "C.UserName" },
                    new SelectModel { AsName = "UpdateTime", FieldName = "A.UpdateTime" }
                })
                .FirstAsync();

            // 获取 ID
            if (dt1 != null)
            {
                data.CreatedBy = dt1.CreatedBy;
                if (dt1.CreatedTime != null)
                {
                    var createdTime = (DateTime)dt1.CreatedTime;
                    data.CreatedTime = createdTime.ConvertToSecondString();
                }
                data.UpdateBy = dt1.UpdateBy;

                if (dt1.UpdateTime != null)
                {
                    var updateTime = (DateTime)dt1.UpdateTime;
                    data.UpdateTime = updateTime.ConvertToSecondString();
                }
            }
            data.ModuleCode = moduleCode;
        }

        return ServiceResult<dynamic>.OprateSuccess(data, ResponseText.QUERY_SUCCESS);
    }

    #endregion

    #region 导出模块SQL

    /// <summary>
    /// 导出模块SQL脚本
    /// </summary>
    /// <param name="ids">模块ID列表</param>
    /// <returns>导出文件ID</returns>
    /// <remarks>
    /// 导出包含模块配置、模块SQL配置、模块列配置的完整SQL脚本
    /// 生成的脚本包含删除和插入语句，可直接在数据库中执行
    /// </remarks>
    public async Task<ServiceResult<Guid>> ExportModuleSqlScript(List<Guid> ids)
    {
        var fileId = Utility.GuidId;

        if (ids.Any())
        {
            var sb = new StringBuilder();
            DBHelper dBHelper = new();

            foreach (var id in ids)
            {
                // 生成删除语句
                var sql = Db.Deleteable<SmModuleColumn>().Where(it => it.SmModuleId == id).ToSqlString();
                sb.Append($"{sql};\n");

                sql = Db.Deleteable<SmModuleSql>().Where(it => it.ModuleId == id).ToSqlString();
                sb.Append($"{sql};\n");

                sql = Db.Deleteable<SmModules>().Where(it => it.ID == id).ToSqlString();
                sb.Append($"{sql};\n");

                // 生成插入语句
                var module = await Query(id);
                sql = Db.Insertable(module).ToSqlString();
                sb.Append(sql + "\n");

                var moduleSql = await Db.Queryable<SmModuleSql>().Where(it => it.ModuleId == id).FirstAsync();
                if (moduleSql != null)
                {
                    sql = Db.Insertable(moduleSql).ToSqlString();
                    sb.Append(sql + "\n");
                }

                var moduleColumns = await Db.Queryable<SmModuleColumn>().Where(it => it.SmModuleId == id).ToListAsync();
                if (moduleColumns.Any())
                {
                    sql = Db.Insertable(moduleColumns).ToSqlString();
                    sb.Append(sql + "\n");
                }
            }

            // 保存文件
            string fileName = $"系统模块_{DateTime.Now.ToSecondString1()}.sql";
            string folder = DateTime.Now.ConvertToYearMonthString1();
            string savePath = $"/Download/SqlExport/{folder}/";
            FileHelper.CreateDirectory("wwwroot" + savePath);
            FileHelper.WriteFile("wwwroot" + savePath, fileName, sb.ToString());

            // 记录文件附件
            var fileAttachment = new FileAttachment()
            {
                ID = fileId,
                OriginalFileName = fileName,
                FileName = fileName,
                FileExt = "sql",
                Path = savePath
            };
            await Db.Insertable(fileAttachment).ExecuteCommandAsync();
        }

        return Success(fileId, "导出成功！");
    }

    #endregion

    #region 更新模块列排序号

    /// <summary>
    /// 更新模块列的排序号
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="columns">列信息列表</param>
    /// <param name="type">类型（form:表单排序，其他:表格排序）</param>
    /// <returns>更新结果</returns>
    /// <remarks>
    /// 按传入的列顺序，以100为增量重新设置排序号
    /// 更新后会重新加载模块列缓存
    /// </remarks>
    public async Task<ServiceResult> UpdateTaxisNoAsync(string moduleCode, List<SmModuleColumn> columns, string type)
    {
        if (columns is null || (columns != null && !columns.Any()))
            return Success();

        if (type == "form")
        {
            // 更新表单排序号
            int i = 1;
            columns.ForEach(x =>
            {
                x.FromTaxisNo = TAXIS_INCREMENT * i;
                i++;
            });
            await Db.Updateable(columns)
                .UpdateColumns(x => new { x.FromTaxisNo, x.UpdateBy, x.UpdateTime }, true)
                .ExecuteCommandAsync();
        }
        else
        {
            // 更新表格排序号
            int i = 1;
            columns.ForEach(x =>
            {
                x.TaxisNo = TAXIS_INCREMENT * i;
                i++;
            });
            await Db.Updateable(columns)
                .UpdateColumns(x => new { x.TaxisNo, x.UpdateBy, x.UpdateTime }, true)
                .ExecuteCommandAsync();
        }

        // 重新加载缓存
        await ModuleSqlColumn.Reload(moduleCode);

        return Success();
    }

    #endregion

    #region 更新模块表单列

    /// <summary>
    /// 更新模块列配置
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="column">列配置信息</param>
    /// <param name="type">类型（form:表单配置，其他:表格配置）</param>
    /// <returns>更新结果</returns>
    /// <remarks>
    /// 根据类型更新不同的列属性
    /// 更新后会重新加载模块列缓存
    /// </remarks>
    public async Task<ServiceResult> UpdateColumnAsync(string moduleCode, SmModuleFormOption column, string type)
    {
        if (column is null)
            return Success();

        var entity = Mapper.Map(column).ToANew<SmModuleColumn>();

        if (type == "form")
        {
            if (!IsValidAcceptFormat(column.Accept))
                return Failed("接受的文件类型格式不正确，请使用 .zip,.pdf 格式");

            if (column.Accept.IsNotEmptyOrNull())
                entity.Accept = column.Accept.Trim();

            // 更新表单列配置
            // 根据字段类型设置数据源
            if (column.FieldType == "ComboGrid")
                entity.DataSource = column.ComboGridDataSource;
            else if (column.FieldType == "ComboBox")
            {
                entity.DataSource = column.ComboBoxDataSource;

                if (column.DataSource.IsNotEmptyOrNull())
                    entity.IsLovCode = true;

                if (column.DataSource.IsNullOrEmpty() && column.IsLovCode == true)
                    entity.DataSource = entity.DataIndex;
            }
            else if (column.FieldType == "Input" && column.IsAutoCode == true)
            {
                entity.DataSource = column.AutoCodeDataSource;
            }

            await Db.Updateable(entity)
                .UpdateColumns(x => new
                {
                    x.FieldType,
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
                    x.ModifyHide,
                    x.Accept,
                    x.MaxFileSize,
                    x.ModifyDisabled,
                    x.GridSpan,
                    x.DataSource,
                    x.ColumnMode,
                    x.IsTooltip,
                    x.TooltipContent,
                    x.AllowClear,
                    x.Remark,
                    x.UpdateBy,
                    x.UpdateTime,
                    x.IsMultiple,
                    x.MultipleMaxCount,
                    x.IsAutoCode,
                    x.IsRedirect,
                    x.RedirectUrl
                }, true)
                .ExecuteCommandAsync();
        }
        else
        {
            if (entity.IsLovCode == true)
                entity.DataSource = column.ComboBoxDataSource;
            // 更新表格列配置
            await Db.Updateable(entity)
                .UpdateColumns(x => new
                {
                    x.FieldType,
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
                    //x.UpdateBy,
                    x.DataSource,
                    x.IsRedirect,
                    x.RedirectUrl
                }, true)
                .ExecuteCommandAsync();
        }

        // 重新加载缓存
        await ModuleSqlColumn.Reload(moduleCode);

        return Success();
    }

    #endregion

    #region 新增模块表单列

    /// <summary>
    /// 新增模块列
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <param name="column">列信息</param>
    /// <returns>新增结果</returns>
    /// <remarks>
    /// 新增后会重新加载模块列缓存
    /// </remarks>
    public async Task<ServiceResult> AddColumnAsync(string moduleCode, SmModuleColumn column)
    {
        if (column is null)
            return Success();

        var module = await Db.Queryable<SmModules>().Where(x => x.ModuleCode == moduleCode).FirstAsync();
        column.SmModuleId = module.ID;
        column.FieldType = "Input";
        column.FormTitle = column.Title;
        await Db.Insertable(column).ExecuteCommandAsync();

        // 重新加载缓存
        await ModuleSqlColumn.Reload(moduleCode);

        return Success();
    }

    #endregion

    #region 获取模块表单信息

    /// <summary>
    /// 获取模块表单列配置
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns>表单列配置列表</returns>
    public async Task<ServicePageResult<SmModuleFormOption>> GetModuleFormColumn(Guid id)
    {
        var result = new List<SmModuleFormOption>();

        // 获取模块信息
        //var module = ModuleInfo.GetModuleInfo(moduleCode);
        var module = await base.AnyAsync(x => x.ID == id);

        if (!module)
            throw new Exception("未查询到模块相关配置信息！");

        var moduleId = id;

        //// 按表单排序号排序
        //moduleColumns = moduleColumns.OrderBy(x => x.FromTaxisNo).ToList();

        var moduleColumns = await Db.Queryable<SmModuleColumn>()
            .OrderBy(x => x.FromTaxisNo)
            .Where(x => x.SmModuleId == moduleId).ToListAsync();

        result = Mapper.Map(moduleColumns).ToANew<List<SmModuleFormOption>>();

        int i = 0;
        result.ForEach(x =>
        {
            x.FormTitle = string.IsNullOrWhiteSpace(x.FormTitle) ? x.Title : x.FormTitle;

            // 根据字段类型设置对应的数据源
            if (x.FieldType == "ComboGrid")
                x.ComboGridDataSource = x.DataSource;
            else if (x.FieldType == "ComboBox" || x.IsLovCode == true)
            {
                if (x.DataSource.IsNullOrEmpty() && x.IsLovCode == true)
                    x.DataSource = x.DataIndex;
                x.ComboBoxDataSource = x.DataSource;
            }
            else if (x.FieldType == "Input" && x.IsAutoCode == true)
                x.AutoCodeDataSource = x.DataSource;

            result[i].ID = moduleColumns[i].ID;
            i++;
        });

        return new ServicePageResult<SmModuleFormOption>(1, 1, 1, result);
    }

    #endregion

    #region 记录用户模块列

    /// <summary>
    /// 记录用户自定义的模块列配置
    /// </summary>
    /// <param name="smModuleId">模块ID</param>
    /// <param name="param">列配置参数JSON对象</param>
    /// <returns>保存结果</returns>
    /// <remarks>
    /// 保存用户对表格列的自定义设置（显示/隐藏、排序、固定等）
    /// 保存成功后会更新Redis缓存
    /// </remarks>
    public async Task<ServiceResult> RecordUserModuleColumn(Guid smModuleId, JObject param)
    {
        // 删除用户旧的列配置
        await _smUserModuleColumnServices.Delete(x => x.UserId == UserId && x.SmModuleId == smModuleId);

        var moduleList = ModuleInfo.GetModuleList();
        var module = moduleList.Where(x => x.ID == smModuleId).FirstOrDefault();
        var inserts = new List<InsertSmUserModuleColumnInput>();

        // 解析并构建列配置数据
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

        // 批量保存
        await _smUserModuleColumnServices.Add(inserts);

        // 更新缓存
        var cache = await QueryUserModuleColumn(module.ModuleCode);
        _redisCacheService.AddObject(UserId1, "UserModuleColumn." + module.ModuleCode, cache);

        return Success(ResponseText.SAVE_SUCCESS);
    }

    /// <summary>
    /// 查询用户模块列数据
    /// </summary>
    /// <param name="moduleCode">模块代码</param>
    /// <returns>用户列配置列表</returns>
    public async Task<List<SmUserModuleColumn>> QueryUserModuleColumn(string moduleCode)
    {
        var list = await Db.Queryable<SmUserModuleColumn>()
            .LeftJoin<SmModules>((x, y) => x.SmModuleId == y.ID)
            .Where((x, y) => UserId == x.UserId && y.ModuleCode == moduleCode)
            .ToListAsync();

        return list;
    }

    #endregion

    #region 复制模块

    /// <summary>
    /// 复制模块及其配置
    /// </summary>
    /// <param name="moduleId">源模块ID</param>
    /// <param name="module1">新模块信息</param>
    /// <returns>复制结果</returns>
    /// <remarks>
    /// 复制模块的基本信息、SQL配置和列配置
    /// 新模块使用新的ID和代码
    /// </remarks>
    public async Task<ServiceResult> CopyAsync(Guid moduleId, SmModules module1)
    {
        // 检查模块代码是否已存在
        if (await Db.Queryable<SmModules>().AnyAsync(x => x.ModuleCode == module1.ModuleCode))
            return Failed($"系统中已存在相同模块代码【{module1.ModuleCode}】模块！");

        // 获取源模块信息
        var module = await base.QuerySingle(x => x.ID == moduleId);
        module.ModuleCode = module1.ModuleCode;
        module.ModuleName = module1.ModuleName;
        module.ID = Utility.GuidId;

        // 获取源模块的列配置和SQL配置
        var columns = await Db.Queryable<SmModuleColumn>().Where(x => x.SmModuleId == moduleId).ToListAsync();
        var moduleSql = await Db.Queryable<SmModuleSql>().Where(x => x.ModuleId == moduleId).FirstAsync();

        // 设置创建信息
        module.CreatedBy = UserId;
        module.CreatedTime = Utility.GetSysDate();
        module.UpdateBy = null;
        module.UpdateTime = null;

        // 处理SQL配置
        if (!moduleSql.IsNullOrEmpty())
        {
            moduleSql.ID = Utility.GuidId;
            moduleSql.CreatedBy = UserId;
            moduleSql.CreatedTime = Utility.GetSysDate();
            moduleSql.ModuleId = module.ID;
            moduleSql.UpdateBy = null;
            moduleSql.UpdateTime = null;
        }

        // 处理列配置
        columns.ForEach(x =>
        {
            x.ID = Utility.GuidId;
            x.CreatedBy = UserId;
            x.CreatedTime = Utility.GetSysDate();
            x.SmModuleId = module.ID;
            x.UpdateBy = null;
            x.UpdateTime = null;
        });

        // 保存新模块及配置
        await Db.Insertable(module).ExecuteCommandAsync();
        await Db.Insertable(moduleSql).ExecuteCommandAsync();
        await Db.Insertable(columns).ExecuteCommandAsync();

        return Success("复制成功！");
    }

    #endregion
}

