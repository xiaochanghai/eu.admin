namespace EU.Core.Api.Controllers
{
    /// <summary>
    /// 分表demo
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Permissions.Name), ApiExplorerSettings(GroupName = Grouping.GroupName_Other)]
    public class SplitDemoController : ControllerBase
    {
        readonly ISplitDemoServices splitDemoServices;
        readonly IUnitOfWorkManage unitOfWorkManage;
        public SplitDemoController(ISplitDemoServices _splitDemoServices, IUnitOfWorkManage _unitOfWorkManage)
        {
            splitDemoServices = _splitDemoServices;
            unitOfWorkManage = _unitOfWorkManage;
        }

        /// <summary>
        /// 分页获取数据
        /// </summary>
        /// <param name="beginTime"></param>
        /// <param name="endTime"></param>
        /// <param name="page"></param>
        /// <param name="key"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ServiceResult<PageModel<SplitDemo>>> Get(DateTime beginTime, DateTime endTime, int page = 1, string key = "", int pageSize = 10)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
            {
                key = "";
            }
            Expression<Func<SplitDemo, bool>> whereExpression = a => (a.Name != null && a.Name.Contains(key));
            var data = await splitDemoServices.QueryPageSplit(whereExpression, beginTime, endTime, page, pageSize, " Id desc ");
            return ServiceResult<PageModel<SplitDemo>>.OprateSuccess(data.dataCount >= 0, "获取成功", data);
        }

        /// <summary>
        /// 根据ID获取信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ServiceResult<SplitDemo>> GetById(long id)
        {
            var data = new ServiceResult<string>();
            var model = await splitDemoServices.QueryByIdSplit(id);
            if (model != null)
            {
                return ServiceResult<SplitDemo>.OprateSuccess(model, "获取成功");
            }
            else
            {
                return ServiceResult<SplitDemo>.OprateFailed("获取失败");
            }
        }

        /// <summary>
        /// 添加一条测试数据
        /// </summary>
        /// <param name="splitDemo"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<ServiceResult<string>> Post([FromBody] SplitDemo splitDemo)
        {
            var data = new ServiceResult<string>();
            //unitOfWorkManage.BeginTran();
            var id = (await splitDemoServices.AddSplit(splitDemo));
            data.Success = (id == null ? false : true);
            try
            {
                if (data.Success)
                {
                    data.Data = id.FirstOrDefault().ToString();
                    data.Message = "添加成功";
                }
                else
                {
                    data.Message = "添加失败";
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                //if (data.Success)
                //    unitOfWorkManage.CommitTran();
                //else
                //    unitOfWorkManage.RollbackTran();
            }
            return data;
        }

        /// <summary>
        /// 修改一条测试数据
        /// </summary>
        /// <param name="splitDemo"></param>
        /// <returns></returns>
        [HttpPut]
        [AllowAnonymous]
        public async Task<ServiceResult<string>> Put([FromBody] SplitDemo splitDemo)
        {
            var data = new ServiceResult<string>();
            if (splitDemo != null && splitDemo.Id > 0)
            {
                unitOfWorkManage.BeginTran();
                data.Success = await splitDemoServices.UpdateSplit(splitDemo, splitDemo.CreateTime);
                try
                {
                    if (data.Success)
                    {
                        data.Message = "修改成功";
                        data.Data = splitDemo?.Id.ObjToString();
                    }
                    else
                    {
                        data.Message = "修改失败";
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (data.Success)
                        unitOfWorkManage.CommitTran();
                    else
                        unitOfWorkManage.RollbackTran();
                }
            }
            return data;
        }

        /// <summary>
        /// 根据id删除数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [AllowAnonymous]
        public async Task<ServiceResult<string>> Delete(long id)
        {
            var data = new ServiceResult<string>();

            var model = await splitDemoServices.QueryByIdSplit(id);
            if (model != null)
            {
                unitOfWorkManage.BeginTran();
                data.Success = await splitDemoServices.DeleteSplit(model, model.CreateTime);
                try
                {
                    data.Data = id.ObjToString();
                    if (data.Success)
                    {
                        data.Message = "删除成功";
                    }
                    else
                    {
                        data.Message = "删除失败";
                    }

                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (data.Success)
                        unitOfWorkManage.CommitTran();
                    else
                        unitOfWorkManage.RollbackTran();
                }
            }
            else
            {
                data.Message = "不存在";
            }
            return data;

        }
    }
}
